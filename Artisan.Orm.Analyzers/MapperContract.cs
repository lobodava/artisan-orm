using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Artisan.Orm.Analyzers;

internal enum MapperFunction { CreateObject, CreateObjectRow, CreateDataTable, CreateDataRow }
internal enum MethodState { Missing, Valid, Invalid, Ambiguous }

internal sealed class MethodBinding
{
    internal MethodBinding(MethodState state, IMethodSymbol? method = null)
    { State = state; Method = method; }
    internal MethodState State { get; }
    internal IMethodSymbol? Method { get; }
}

internal sealed class MapperContract
{
    private readonly CSharpCompilation compilation;
    private readonly ITypeSymbol reader;
    private readonly ITypeSymbol objectRow;
    private readonly ITypeSymbol dataTable;
    private readonly ITypeSymbol objectArray;

    internal MapperContract(CSharpCompilation compilation, ITypeSymbol reader, ITypeSymbol objectRow, ITypeSymbol dataTable)
    {
        this.compilation = compilation;
        this.reader = reader;
        this.objectRow = objectRow;
        this.dataTable = dataTable;
        objectArray = compilation.CreateArrayTypeSymbol(compilation.GetSpecialType(SpecialType.System_Object));
    }

    internal string Signature(MapperFunction function, ITypeSymbol entity) => function switch
    {
        MapperFunction.CreateObject => $"{entity.ToDisplayString()} CreateObject(Microsoft.Data.SqlClient.SqlDataReader)",
        MapperFunction.CreateObjectRow => "Artisan.Orm.ObjectRow CreateObjectRow(Microsoft.Data.SqlClient.SqlDataReader)",
        MapperFunction.CreateDataTable => "System.Data.DataTable CreateDataTable()",
        _ => $"object[] CreateDataRow({entity.ToDisplayString()})"
    };

    // Reflection uses the default binder: exact input types win, followed by the most
    // specific compatible input type. Delegate binding permits reference variance.
    internal MethodBinding Bind(INamedTypeSymbol mapper, ITypeSymbol entity, MapperFunction function)
    {
        ITypeSymbol? input = function == MapperFunction.CreateDataTable ? null :
            function == MapperFunction.CreateDataRow ? entity : reader;
        var output = function == MapperFunction.CreateObject ? entity :
            function == MapperFunction.CreateObjectRow ? objectRow :
            function == MapperFunction.CreateDataTable ? dataTable : objectArray;
        var methods = PublicMethods(mapper, function.ToString()).ToList();
        var candidates = input == null ? methods : methods.Where(m =>
            m.Parameters.Length == 1 && m.Parameters[0].RefKind == RefKind.None &&
            BinderConversion(input, m.Parameters[0].Type)).ToList();

        if (input != null && candidates.Any(m => Same(input, m.Parameters[0].Type)))
            candidates = candidates.Where(m => Same(input, m.Parameters[0].Type)).ToList();
        if (input != null && candidates.Count > 1)
            candidates = candidates.Where(m => !candidates.Any(other =>
                !Same(other.Parameters[0].Type, m.Parameters[0].Type) &&
                BinderConversion(other.Parameters[0].Type, m.Parameters[0].Type))).ToList();

        if (candidates.Count == 0) return new MethodBinding(MethodState.Missing);
        if (candidates.Count > 1) return new MethodBinding(MethodState.Ambiguous);
        var method = candidates[0];
        var valid = method.IsStatic && method.Arity == 0 && !method.ReturnsByRef && !method.ReturnsByRefReadonly &&
            !ContainsTypeParameter(mapper) && method.Parameters.Length == (input == null ? 0 : 1) &&
            (input == null || DelegateConversion(input, method.Parameters[0].Type)) &&
            DelegateConversion(method.ReturnType, output);
        return new MethodBinding(valid ? MethodState.Valid : MethodState.Invalid, method);
    }

    private bool BinderConversion(ITypeSymbol from, ITypeSymbol to)
    {
        var conversion = compilation.ClassifyConversion(from, to);
        // DefaultBinder can widen an enum's underlying primitive to a primitive parameter.
        if (from is INamedTypeSymbol { EnumUnderlyingType: { } underlying } && to.TypeKind != TypeKind.Enum)
        {
            var numeric = compilation.ClassifyConversion(underlying, to);
            if (numeric.IsIdentity || numeric.IsImplicit && numeric.IsNumeric) return true;
        }
        return conversion.IsIdentity || conversion.IsImplicit && (conversion.IsReference || conversion.IsBoxing || conversion.IsNumeric);
    }

    private bool DelegateConversion(ITypeSymbol from, ITypeSymbol to)
    {
        // CLR delegate binding also accepts an enum and its exact underlying type.
        var fromUnderlying = (from as INamedTypeSymbol)?.EnumUnderlyingType ?? from;
        var toUnderlying = (to as INamedTypeSymbol)?.EnumUnderlyingType ?? to;
        if (Same(fromUnderlying, toUnderlying)) return true;
        var conversion = compilation.ClassifyConversion(from, to);
        return conversion.IsIdentity || conversion.IsImplicit && conversion.IsReference;
    }

    private static IEnumerable<IMethodSymbol> PublicMethods(INamedTypeSymbol type, string name)
    {
        var seen = new List<IMethodSymbol>();
        for (var current = type; current != null; current = current.BaseType)
        {
            foreach (var method in current.GetMembers(name).OfType<IMethodSymbol>())
            {
                if (method.DeclaredAccessibility != Accessibility.Public ||
                    !Same(current, type) && method.IsStatic) continue; // GetMethod has no FlattenHierarchy.
                if (seen.Any(m => m.Arity == method.Arity && m.Parameters.Length == method.Parameters.Length &&
                    m.Parameters.Zip(method.Parameters, (a, b) => a.RefKind == b.RefKind && Same(a.Type, b.Type)).All(x => x))) continue;
                seen.Add(method);
                yield return method;
            }
        }
    }

    internal static bool Same(ISymbol? a, ISymbol? b) => SymbolEqualityComparer.Default.Equals(a, b);

    internal static bool ContainsTypeParameter(ITypeSymbol type) => type switch
    {
        ITypeParameterSymbol _ => true,
        IArrayTypeSymbol array => ContainsTypeParameter(array.ElementType),
        INamedTypeSymbol named => named.IsUnboundGenericType || named.TypeArguments.Any(ContainsTypeParameter) ||
            named.ContainingType != null && ContainsTypeParameter(named.ContainingType),
        _ => false
    };

    internal static bool IsSimple(ITypeSymbol type, bool unwrapNullable = false)
    {
        if (type is INamedTypeSymbol named && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            type = named.TypeArguments[0];
        // Nullable and non-nullable enums use the same numeric scalar conversion at runtime.
        if (type.TypeKind == TypeKind.Enum) return true;
        switch (type.SpecialType)
        {
            case SpecialType.System_String: case SpecialType.System_Char:
            case SpecialType.System_Boolean: case SpecialType.System_Byte: case SpecialType.System_SByte:
            case SpecialType.System_Int16: case SpecialType.System_UInt16:
            case SpecialType.System_Int32: case SpecialType.System_UInt32:
            case SpecialType.System_Int64: case SpecialType.System_UInt64:
            case SpecialType.System_Single: case SpecialType.System_Double:
            case SpecialType.System_Decimal: case SpecialType.System_DateTime: return true;
        }
        return type.ToDisplayString() is "System.Guid" or "System.DateTimeOffset" or "System.TimeSpan";
    }

    internal static IEnumerable<MapperFunction> Required(int value) => value switch
    {
        0 => new[] { MapperFunction.CreateObject, MapperFunction.CreateObjectRow, MapperFunction.CreateDataTable, MapperFunction.CreateDataRow },
        1 => new[] { MapperFunction.CreateObject, MapperFunction.CreateDataTable, MapperFunction.CreateDataRow },
        2 => new[] { MapperFunction.CreateObject, MapperFunction.CreateObjectRow },
        3 => new[] { MapperFunction.CreateDataTable, MapperFunction.CreateDataRow },
        4 => new[] { MapperFunction.CreateObject },
        5 => new[] { MapperFunction.CreateObjectRow },
        _ => Array.Empty<MapperFunction>()
    };
}
