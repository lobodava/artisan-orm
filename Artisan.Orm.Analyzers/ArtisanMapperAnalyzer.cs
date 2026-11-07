using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Artisan.Orm.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ArtisanMapperAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Diagnostics.All;

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(start =>
        {
            var attribute = start.Compilation.GetTypeByMetadataName("Artisan.Orm.MapperForAttribute");
            var reader = start.Compilation.GetTypeByMetadataName("Microsoft.Data.SqlClient.SqlDataReader");
            var row = start.Compilation.GetTypeByMetadataName("Artisan.Orm.ObjectRow");
            var table = start.Compilation.GetTypeByMetadataName("System.Data.DataTable");
            if (attribute == null || reader == null || row == null || table == null) return;
            var contract = new MapperContract((CSharpCompilation)start.Compilation, reader, row, table);
            var declarationDiagnostics = new List<Diagnostic>();
            // Build the complete registry once per compilation, including declarations in other files.
            // Reporting must happen in document/operation callbacks so live analysis sees the same errors.
            var registrations = CollectRegistrations(start.Compilation, start.CancellationToken,
                attribute, contract, declarationDiagnostics.Add);
            var diagnosticsByTree = declarationDiagnostics.Where(d => d.Location.IsInSource)
                .ToLookup(d => d.Location.SourceTree!);
            start.RegisterSemanticModelAction(model =>
            {
                foreach (var diagnostic in diagnosticsByTree[model.SemanticModel.SyntaxTree])
                {
                    model.CancellationToken.ThrowIfCancellationRequested();
                    model.ReportDiagnostic(diagnostic);
                }
            });
            start.RegisterOperationAction(operation =>
            {
                var method = operation.Operation is IInvocationOperation invocation ? invocation.TargetMethod :
                    ((IMethodReferenceOperation)operation.Operation).Method;
                foreach (var call in Calls(method, operation.Operation.Syntax.GetLocation(), attribute.ContainingAssembly))
                    AnalyzeCall(call, registrations, contract, operation.Options, operation.ReportDiagnostic);
            }, OperationKind.Invocation, OperationKind.MethodReference);
        });
    }

    private sealed class Registration
    {
        internal Registration(INamedTypeSymbol mapper, ITypeSymbol entity, AttributeData attribute, MapperContract contract)
        {
            Mapper = mapper; Entity = entity; Attribute = attribute;
            Bindings = Enum.GetValues(typeof(MapperFunction)).Cast<MapperFunction>()
                .ToDictionary(f => f, f => contract.Bind(mapper, entity, f));
        }
        internal INamedTypeSymbol Mapper { get; }
        internal ITypeSymbol Entity { get; }
        internal AttributeData Attribute { get; }
        internal Dictionary<MapperFunction, MethodBinding> Bindings { get; }
        internal Location Location => Attribute.ApplicationSyntaxReference != null && Mapper.GetAttributes().Contains(Attribute)
            ? Attribute.ApplicationSyntaxReference!.GetSyntax().GetLocation()
            : Mapper.Locations.FirstOrDefault(l => l.IsInSource) ?? Location.None;
    }

    private sealed class Call
    {
        internal Call(IMethodSymbol method, ITypeSymbol entity, Location location, params MapperFunction[] functions)
        { Method = method; Entity = entity; Location = location; Functions = functions; }
        internal IMethodSymbol Method { get; }
        internal ITypeSymbol Entity { get; }
        internal Location Location { get; }
        internal MapperFunction[] Functions { get; }
    }

    private static Dictionary<ITypeSymbol, List<Registration>> CollectRegistrations(
        Compilation compilation, CancellationToken cancellationToken, INamedTypeSymbol attributeType,
        MapperContract contract, Action<Diagnostic> report)
    {
        var registrations = new Dictionary<ITypeSymbol, List<Registration>>(SymbolEqualityComparer.Default);
        var assemblies = new[] { compilation.Assembly }.Concat(compilation.SourceModule.ReferencedAssemblySymbols)
            .Distinct<IAssemblySymbol>(SymbolEqualityComparer.Default)
            .Where(a => MapperContract.Same(a, compilation.Assembly) || MapperContract.Same(a, attributeType.ContainingAssembly) ||
                a.Modules.Any(m => m.ReferencedAssemblySymbols.Any(r => MapperContract.Same(r, attributeType.ContainingAssembly))));
        foreach (var assembly in assemblies)
        foreach (var type in Types(assembly.GlobalNamespace, cancellationToken))
        {
            var attribute = FindAttribute(type, attributeType);
            if (attribute == null) continue;
            var source = MapperContract.Same(assembly, compilation.Assembly);
            var entity = attribute.ConstructorArguments.FirstOrDefault().Value as ITypeSymbol;
            if (entity == null || entity.SpecialType == SpecialType.System_Void || entity.TypeKind == TypeKind.Pointer ||
                entity is INamedTypeSymbol n && (n.IsStatic || n.IsRefLikeType) ||
                MapperContract.ContainsTypeParameter(entity))
            {
                if (source) report(Diagnostic.Create(Diagnostics.InvalidRegistration,
                    type.Locations.FirstOrDefault(), type.ToDisplayString(), Diagnostics.Text("InvalidTarget")));
                continue;
            }
            if (entity.TypeKind == TypeKind.Error) continue;
            var registration = new Registration(type, entity, attribute, contract);
            if (!registrations.TryGetValue(entity, out var list)) registrations.Add(entity, list = new List<Registration>());
            list.Add(registration);
            if (source) Validate(report, registration, contract);
        }

        foreach (var group in registrations.Values.Where(g => g.Count > 1))
        foreach (var registration in group.Where(r => r.Location.IsInSource))
            report(Diagnostic.Create(Diagnostics.DuplicateMapper, registration.Location,
                registration.Entity.ToDisplayString(), MapperNames(group)));
        return registrations;
    }

    private static void AnalyzeCall(Call call, Dictionary<ITypeSymbol, List<Registration>> registrations,
        MapperContract contract, AnalyzerOptions analyzerOptions, Action<Diagnostic> report)
    {
        if (MapperContract.ContainsTypeParameter(call.Entity) || call.Entity.TypeKind == TypeKind.Error) return;
        if (!registrations.TryGetValue(call.Entity, out var group))
        {
            var options = analyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(call.Location.SourceTree!);
            if (options.TryGetValue("artisan_orm.allow_external_mappers", out var value) &&
                bool.TryParse(value, out var allow) && allow) return;
            report(Diagnostic.Create(Diagnostics.MissingMapper, call.Location,
                call.Method.Name, call.Entity.ToDisplayString()));
            return;
        }
        if (group.Count > 1)
        {
            if (group.All(r => !r.Location.IsInSource))
                report(Diagnostic.Create(Diagnostics.DuplicateMapper, call.Location,
                    call.Entity.ToDisplayString(), MapperNames(group)));
            return;
        }
        var mapper = group[0];
        IEnumerable<MapperFunction> functions = call.Functions;
        if (!mapper.Location.IsInSource)
        {
            var required = RequiredMethods(mapper.Attribute, out var validRequirements);
            if (!validRequirements || MapperContract.ContainsTypeParameter(mapper.Mapper))
            {
                report(Diagnostic.Create(Diagnostics.InvalidRegistration, call.Location,
                    mapper.Mapper.ToDisplayString(), Diagnostics.Text(validRequirements ? "OpenMapper" : "InvalidRequiredMethods")));
                return;
            }
            // Initialization validates the entire referenced mapper, including optional
            // methods it finds and requirements unrelated to this particular call.
            functions = functions.Concat(required).Concat(mapper.Bindings
                .Where(b => b.Value.State is MethodState.Invalid or MethodState.Ambiguous).Select(b => b.Key)).Distinct();
        }
        foreach (var function in functions)
        {
            var binding = mapper.Bindings[function];
            if (binding.State == MethodState.Valid) continue;
            // Source declaration diagnostics already explain broken delegate bindings.
            if (mapper.Location.IsInSource && binding.State != MethodState.Missing) continue;
            report(Diagnostic.Create(Diagnostics.RequiredByCall, call.Location,
                call.Method.Name, function.ToString(), mapper.Mapper.ToDisplayString(), call.Entity.ToDisplayString(),
                contract.Signature(function, call.Entity)));
        }
    }

    private static void Validate(Action<Diagnostic> report, Registration registration, MapperContract contract)
    {
        if (MapperContract.ContainsTypeParameter(registration.Mapper))
        {
            report(Diagnostic.Create(Diagnostics.InvalidRegistration, registration.Location,
                registration.Mapper.ToDisplayString(), Diagnostics.Text("OpenMapper")));
            return;
        }
        var required = RequiredMethods(registration.Attribute, out var validRequirements);
        if (!validRequirements)
            report(Diagnostic.Create(Diagnostics.InvalidRegistration, registration.Location,
                registration.Mapper.ToDisplayString(), Diagnostics.Text("InvalidRequiredMethods")));
        foreach (var pair in registration.Bindings)
        {
            var function = pair.Key;
            var binding = pair.Value;
            var signature = contract.Signature(function, registration.Entity);
            switch (binding.State)
            {
                case MethodState.Ambiguous:
                    report(Diagnostic.Create(Diagnostics.AmbiguousMethod, registration.Location,
                        function.ToString(), registration.Mapper.ToDisplayString(), signature));
                    break;
                case MethodState.Invalid:
                    report(Diagnostic.Create(Diagnostics.InvalidSignature,
                        binding.Method!.Locations.FirstOrDefault(l => l.IsInSource) ?? registration.Location,
                        binding.Method.ToDisplayString(), registration.Entity.ToDisplayString(), signature));
                    break;
                case MethodState.Missing when required.Contains(function):
                    report(Diagnostic.Create(Diagnostics.MissingMethod, registration.Location,
                        registration.Mapper.ToDisplayString(), registration.Entity.ToDisplayString(), function.ToString(), signature));
                    break;
            }
        }
    }

    private static HashSet<MapperFunction> RequiredMethods(AttributeData attribute, out bool valid)
    {
        var required = new HashSet<MapperFunction>();
        valid = true;
        if (attribute.ConstructorArguments.Length < 2) return required;
        var values = attribute.ConstructorArguments[1];
        valid = !values.IsNull && values.Kind == TypedConstantKind.Array &&
            values.Values.All(v => v.Value is int i && i >= 0 && i <= 5);
        if (valid)
            foreach (var value in values.Values) required.UnionWith(MapperContract.Required((int)value.Value!));
        return required;
    }

    private static string MapperNames(IEnumerable<Registration> group) =>
        string.Join(", ", group.Select(r => r.Mapper.ToDisplayString()).OrderBy(n => n, StringComparer.Ordinal));

    private static AttributeData? FindAttribute(INamedTypeSymbol type, INamedTypeSymbol attributeType)
    {
        // MapperForAttribute is inherited. Its arguments come from the nearest base declaration.
        for (var current = type; current != null; current = current.BaseType)
        {
            var attribute = current.GetAttributes().FirstOrDefault(a => MapperContract.Same(a.AttributeClass, attributeType));
            if (attribute != null) return attribute;
        }
        return null;
    }

    private static IEnumerable<INamedTypeSymbol> Types(INamespaceOrTypeSymbol parent, CancellationToken cancellationToken)
    {
        foreach (var member in parent.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (member is INamedTypeSymbol type) yield return type;
            if (member is INamespaceOrTypeSymbol container)
                foreach (var child in Types(container, cancellationToken)) yield return child;
        }
    }

    private static IEnumerable<Call> Calls(IMethodSymbol method, Location location, IAssemblySymbol artisan)
    {
        if (!MapperContract.Same(method.ContainingAssembly, artisan) || !method.IsGenericMethod) yield break;
        var owner = method.ContainingType.ToDisplayString();
        if (owner != "Artisan.Orm.SqlCommandExtensions" && owner != "Artisan.Orm.SqlDataReaderExtensions" &&
            owner != "Artisan.Orm.RepositoryBase" && owner != "Artisan.Orm.DataTableExtensions" &&
            owner != "Artisan.Orm.SqlConnectionExtensions" && owner != "Artisan.Orm.MappingManager") yield break;
        // Action<SqlCommand> configures a command and must not be mistaken for a mapper.
        if (method.Parameters.Any(p => p.Type is INamedTypeSymbol t && t.Name == "Func" &&
            t.ContainingNamespace.ToDisplayString() == "System")) yield break;

        var name = method.Name;
        var entity = method.TypeArguments.Last();
        if (owner == "Artisan.Orm.MappingManager")
        {
            // Nullable getters and Try-style getters do not require a registration.
            if (name == "GetCreateObjectFunc") yield return new Call(method, entity, location, MapperFunction.CreateObject);
            if (name == "GetCreateObjectRowFunc") yield return new Call(method, entity, location, MapperFunction.CreateObjectRow);
            yield break;
        }
        if (name is "ToDataTable" or "AddTableParam" or "AddTableRowParam" or "BulkCopy" or "BulkCopyAsync")
        {
            if (method.Parameters.Any(p => p.Name == "columnNames")) yield break;
            yield return new Call(method, entity, location, MapperFunction.CreateDataTable, MapperFunction.CreateDataRow);
            yield break;
        }
        if (name is "ReadToObjectRow" or "ReadToObjectRowAsync" or "ReadToObjectRows" or "ReadToObjectRowsAsync")
        {
            yield return new Call(method, entity, location, MapperFunction.CreateObjectRow);
            yield break;
        }
        if (name is "ReadToLists" or "ReadToListsAsync")
        {
            foreach (var item in method.TypeArguments.Where(t => !MapperContract.IsSimple(t)))
                yield return new Call(method, item, location, MapperFunction.CreateObject);
            yield break;
        }
        if (name is "ReadToTree" or "ReadToTreeAsync" or "ReadToTreeList" or "ReadToTreeListAsync")
            yield return new Call(method, entity, location, MapperFunction.CreateObject);
        else if (name is "ReadTo" or "ReadToAsync" or "ReadToList" or "ReadToListAsync" or
            "ReadToArray" or "ReadToArrayAsync" or "ReadToEnumerable" or "ReadToAsyncEnumerable" or
            "ReadToDictionary" or "ReadToDictionaryAsync")
        {
            if (!MapperContract.IsSimple(entity, name is "ReadToDictionary" or "ReadToDictionaryAsync"))
                yield return new Call(method, entity, location, MapperFunction.CreateObject);
        }
    }
}
