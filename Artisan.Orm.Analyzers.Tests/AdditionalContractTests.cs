using System.Collections.Immutable;
using System.Reflection;
using Artisan.Orm.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Artisan.Orm.Analyzers.Tests;

[TestClass]
public class AdditionalContractTests
{
    private static readonly MetadataReference[] References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator).Concat(new[] { typeof(MapperForAttribute).Assembly.Location, typeof(SqlCommand).Assembly.Location })
        .Distinct(StringComparer.OrdinalIgnoreCase).Select(p => MetadataReference.CreateFromFile(p)).ToArray();

    private static CSharpCompilation Compile(string text, params MetadataReference[] references) => CSharpCompilation.Create(
        "Case_" + Guid.NewGuid().ToString("N"), new[] { CSharpSyntaxTree.ParseText("""
            using System;
            using System.Data;
            using System.Collections.Generic;
            using Artisan.Orm;
            using Microsoft.Data.SqlClient;
            """ + "\n" + text, path: "Case.cs") }, References.Concat(references), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    private static async Task Check(string source, params string[] expected)
    {
        var compilation = Compile(source);
        Assert.IsFalse(compilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error), string.Join("\n", compilation.GetDiagnostics()));
        var diagnostics = await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new ArtisanMapperAnalyzer())).GetAnalyzerDiagnosticsAsync();
        CollectionAssert.AreEquivalent(expected, diagnostics.Select(d => d.Id).ToArray(), string.Join("\n", diagnostics));
    }

    [TestMethod]
    public async Task InheritedAttributeCreatesDuplicateRuntimeRegistration()
    {
        await Check("""
            public class User {}
            [MapperFor(typeof(User))] public class BaseMapper {}
            public class DerivedMapper : BaseMapper {}
            """, "ART004", "ART004");
    }

    [TestMethod]
    public async Task PartialAndNestedMappersAreRegisteredOnce()
    {
        await Check("""
            public class User {}
            public class Outer {
              [MapperFor(typeof(User), RequiredMethod.CreateObject)] public static partial class M {}
              public static partial class M { public static User CreateObject(SqlDataReader r) => new User(); }
            }
            public class C { void Run(SqlCommand c) { c.ReadTo<User>(); } }
            """);
    }

    [TestMethod]
    public async Task NonStaticMapperClassCanSupplyStaticMethods()
    {
        await Check("""
            public class User {}
            [MapperFor(typeof(User), RequiredMethod.CreateObject)]
            public class M { public static User CreateObject(SqlDataReader r) => new User(); }
            """);
    }

    [TestMethod]
    public async Task ClosedGenericEntitiesAndDataRowReferenceVarianceAreAccepted()
    {
        await Check("""
            [MapperFor(typeof(List<int>), RequiredMethod.BothForDataTable)]
            public static class M {
              public static DataTable CreateDataTable() => new DataTable();
              public static string[] CreateDataRow(IEnumerable<int> obj) => new string[0];
            }
            public class C { void Run() { new List<List<int>>().ToDataTable(); } }
            """);
    }

    [TestMethod]
    public async Task SimpleTypeBehaviorMatchesRuntimeIncludingEnums()
    {
        await Check("""
            public enum State { A }
            public class C { void Run(SqlCommand c) {
              c.ReadTo<State?>();
              c.ReadTo<State>();
              c.ReadToDictionary<int, State?>();
              c.ReadTo<DateOnly>();
              c.ReadToObjectRow<int>();
            } }
            """, "ART001", "ART001");
    }

    [TestMethod]
    public async Task EnumScalarCallsDoNotRequireRegisteredMappers()
    {
        await Check("""
            public enum State : long { Ready = 4294967296L }
            public class C { async System.Threading.Tasks.Task Run(SqlCommand c, SqlDataReader r) {
              c.ReadTo<State>();
              c.ReadToList<State>();
              c.ReadToArray<State?>();
              c.ReadToEnumerable<State>();
              c.ReadToDictionary<State, State?>();
              c.ReadToLists<State, State?>();
              await c.ReadToAsync<State>();
              await c.ReadToListAsync<State?>();
              await c.ReadToDictionaryAsync<State, State?>();
              r.ReadTo<State>();
              r.ReadToList<State?>();
              r.ReadToDictionary<State, State?>();
            } }
            """);
    }

    [TestMethod]
    public async Task ExplicitGenericTableParameterRequiresMapperEvenForScalar()
    {
        await Check("public class C { void Run(SqlCommand c) { c.AddTableParam<int>(\"@p\", new int[0]); c.AddTableRowParam<int>(\"@p\", 1); } }", "ART001", "ART001");
    }

    [TestMethod]
    public async Task ReferencedMapperWithBadSignatureIsReportedAtUse()
    {
        var dependency = Compile("public class ExternalUser {} [MapperFor(typeof(ExternalUser))] public static class M { public static string CreateObject(SqlDataReader r) => \"\"; }");
        using var stream = new MemoryStream();
        Assert.IsTrue(dependency.Emit(stream).Success);
        var client = Compile("public class C { void Run(SqlCommand c) { c.ReadTo<ExternalUser>(); } }", MetadataReference.CreateFromImage(stream.ToArray()));
        var diagnostics = await client.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new ArtisanMapperAnalyzer())).GetAnalyzerDiagnosticsAsync();
        CollectionAssert.AreEqual(new[] { "ART006" }, diagnostics.Select(d => d.Id).ToArray());
    }

    [TestMethod]
    public async Task ReferencedMapperMustSatisfyItsOwnRequirementsBeforeUse()
    {
        var dependency = Compile("public class ExternalUser {} [MapperFor(typeof(ExternalUser), RequiredMethod.AllMain)] public static class M { public static ExternalUser CreateObject(SqlDataReader r) => new ExternalUser(); }");
        using var stream = new MemoryStream();
        Assert.IsTrue(dependency.Emit(stream).Success);
        var client = Compile("public class C { void Run(SqlCommand c) { c.ReadTo<ExternalUser>(); } }", MetadataReference.CreateFromImage(stream.ToArray()));
        var diagnostics = await client.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new ArtisanMapperAnalyzer())).GetAnalyzerDiagnosticsAsync();
        CollectionAssert.AreEquivalent(new[] { "ART006", "ART006" }, diagnostics.Select(d => d.Id).ToArray());
    }

    [TestMethod]
    public async Task DuplicateReferencedMappersAreReportedAtUse()
    {
        var dependency = Compile("public class ExternalUser {} [MapperFor(typeof(ExternalUser))] public static class A {} [MapperFor(typeof(ExternalUser))] public static class B {}");
        using var stream = new MemoryStream();
        Assert.IsTrue(dependency.Emit(stream).Success);
        var client = Compile("public class C { void Run(SqlCommand c) { c.ReadTo<ExternalUser>(); } }", MetadataReference.CreateFromImage(stream.ToArray()));
        var diagnostics = await client.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new ArtisanMapperAnalyzer())).GetAnalyzerDiagnosticsAsync();
        CollectionAssert.AreEqual(new[] { "ART004" }, diagnostics.Select(d => d.Id).ToArray());
    }

    [TestMethod]
    [DataRow("public static User CreateObject(SqlDataReader r) => new User();", false)]
    [DataRow("public User CreateObject(SqlDataReader r) => new User();", true)]
    public async Task InheritedLookupAgreesWithReflection(string method, bool found)
    {
        var source = "public class User {} public class BaseMapper { " + method + " } [MapperFor(typeof(User), RequiredMethod.CreateObject)] public class M : BaseMapper {}";
        var compilation = Compile(source);
        using var stream = new MemoryStream();
        Assert.IsTrue(compilation.Emit(stream).Success);
        var assembly = Assembly.Load(stream.ToArray());
        Assert.AreEqual(found, assembly.GetType("M")!.GetMethod("CreateObject", new[] { typeof(SqlDataReader) }) != null);
        await Check(source, found ? "ART003" : "ART002");
    }

    [TestMethod]
    public async Task RefReturnIsNotCompatibleWithValueReturningDelegate()
    {
        await Check("public class User {} [MapperFor(typeof(User))] public class M { static User u; public static ref User CreateObject(SqlDataReader r) => ref u; }", "ART003");
    }

    [TestMethod]
    [DataRow("public enum Entity { A }", "public static int CreateObject(SqlDataReader r) => 0;")]
    [DataRow("public enum Entity : uint { A }", "public static int CreateObject(SqlDataReader r) => 0;")]
    [DataRow("public enum Entity : long { A }", "public static int CreateObject(SqlDataReader r) => 0;")]
    [DataRow("public enum Entity { A } public enum Other { A }", "public static Other CreateObject(SqlDataReader r) => 0;")]
    [DataRow("public enum Entity { A } public enum Other : uint { A }", "public static Other CreateObject(SqlDataReader r) => 0;")]
    [DataRow("public struct Entity {}", "public static object CreateObject(SqlDataReader r) => new Entity();")]
    [DataRow("public class Entity {}", "public static Entity CreateObject(object r) => new Entity(); public static Entity CreateObject(IDataRecord r) => new Entity();")]
    [DataRow("public class Entity {}", "public static Entity CreateObject(IDataRecord r) => new Entity(); public static Entity CreateObject(IDisposable r) => new Entity();")]
    public async Task OverloadsAndValueReturnsMatchRuntimeBinding(string entity, string methods)
    {
        var source = entity + " [MapperFor(typeof(Entity), RequiredMethod.CreateObject)] public class M { " + methods + " }";
        var compilation = Compile(source);
        using var stream = new MemoryStream();
        Assert.IsTrue(compilation.Emit(stream).Success);
        var assembly = Assembly.Load(stream.ToArray());
        var valid = false;
        try
        {
            var method = assembly.GetType("M")!.GetMethod("CreateObject", new[] { typeof(SqlDataReader) });
            Delegate.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(SqlDataReader), assembly.GetType("Entity")!), method!);
            valid = true;
        }
        catch (ArgumentException) { }
        catch (AmbiguousMatchException) { }
        var diagnostics = await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new ArtisanMapperAnalyzer())).GetAnalyzerDiagnosticsAsync();
        Assert.AreEqual(valid, diagnostics.Length == 0, string.Join("\n", diagnostics));
    }

    [TestMethod]
    [DataRow("public enum Entity { A }", "int")]
    [DataRow("public enum Entity : uint { A }", "int")]
    [DataRow("public struct Entity {}", "object")]
    public async Task DataRowValueParametersMatchRuntimeLookup(string entity, string input)
    {
        var source = entity + " [MapperFor(typeof(Entity), RequiredMethod.BothForDataTable)] public class M { public static DataTable CreateDataTable() => new DataTable(); public static object[] CreateDataRow(" + input + " x) => new object[0]; }";
        var compilation = Compile(source);
        using var stream = new MemoryStream();
        Assert.IsTrue(compilation.Emit(stream).Success);
        var assembly = Assembly.Load(stream.ToArray());
        var type = assembly.GetType("Entity")!;
        var method = assembly.GetType("M")!.GetMethod("CreateDataRow", new[] { type });
        var valid = false;
        if (method != null)
            try { Delegate.CreateDelegate(typeof(Func<,>).MakeGenericType(type, typeof(object[])), method); valid = true; }
            catch (ArgumentException) { }
        var diagnostics = await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new ArtisanMapperAnalyzer())).GetAnalyzerDiagnosticsAsync();
        Assert.AreEqual(valid, diagnostics.Length == 0, string.Join("\n", diagnostics));
    }

    [TestMethod]
    public async Task TreeMethodsRequireCreateObject()
    {
        await Check("""
            public class Node : INode<Node> {
              public int Id { get; set; }
              public int? ParentId { get; set; }
              public IList<Node> Children { get; set; }
            }
            public class C { void Run(SqlCommand c) { c.ReadToTree<Node>(); } }
            """, "ART001");
    }
}
