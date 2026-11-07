using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using Artisan.Orm.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Artisan.Orm.Analyzers.Tests;

[TestClass]
public class MapperAnalyzerTests
{
    private const string Header = """
        using System;
        using System.Data;
        using System.Collections.Generic;
        using Artisan.Orm;
        using Microsoft.Data.SqlClient;
        public class User { public int Id { get; set; } }
        public class Role { }
        """;

    private const string FullMapper = """
        [MapperFor(typeof(User), RequiredMethod.All)]
        public static class UserMapper {
            public static User CreateObject(SqlDataReader r) => new User();
            public static ObjectRow CreateObjectRow(SqlDataReader r) => new ObjectRow(0);
            public static DataTable CreateDataTable() => new DataTable("UserTable");
            public static object[] CreateDataRow(User user) => new object[0];
        }
        """;

    private static readonly MetadataReference[] References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Concat(new[] { typeof(MapperForAttribute).Assembly.Location, typeof(SqlCommand).Assembly.Location })
        .Distinct(StringComparer.OrdinalIgnoreCase).Select(p => MetadataReference.CreateFromFile(p)).ToArray();

    private static CSharpCompilation Compile(string source, params MetadataReference[] extra) =>
        CSharpCompilation.Create("Consumer_" + Guid.NewGuid().ToString("N"),
            new[] { CSharpSyntaxTree.ParseText(Header + "\n" + source, new CSharpParseOptions(LanguageVersion.CSharp12), "Consumer.cs") },
            References.Concat(extra), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    private static async Task<ImmutableArray<Diagnostic>> Analyze(string source, bool external = false, params MetadataReference[] extra)
    {
        var compilation = Compile(source, extra);
        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.IsEmpty(errors, string.Join("\n", errors.Select(d => d.ToString())));
        var options = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty, new ConfigProvider(external));
        var diagnostics = await compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new ArtisanMapperAnalyzer()), options).GetAnalyzerDiagnosticsAsync();
        Assert.IsFalse(diagnostics.Any(d => d.Id == "AD0001"), string.Join("\n", diagnostics));
        return diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ThenBy(d => d.Id).ToImmutableArray();
    }

    private static void Ids(IEnumerable<Diagnostic> diagnostics, params string[] ids)
    {
        CollectionAssert.AreEquivalent(ids, diagnostics.Select(d => d.Id).ToArray(), string.Join("\n", diagnostics));
        Assert.IsTrue(diagnostics.All(d => d.Severity == DiagnosticSeverity.Error));
    }

    [TestMethod]
    [DataRow("[MapperFor(typeof(User), RequiredMethod.AllMain)] public static class M { public static User CreateObject(SqlDataReader r) => new User(); }", "ART002,ART002")]
    [DataRow("public class C { void Run(SqlCommand cmd) { cmd.ReadTo<User>(); } }", "ART001")]
    public async Task LiveAnalysisReportsSameErrorsAsBuild(string source, string expected)
    {
        var compilation = Compile(source).AddSyntaxTrees(CSharpSyntaxTree.ParseText(
            "using Artisan.Orm; [MapperFor(typeof(string), RequiredMethod.AllMain)] public static class OtherMapper {}",
            new CSharpParseOptions(LanguageVersion.CSharp12), "Other.cs"));
        Assert.IsFalse(compilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error));
        var tree = compilation.SyntaxTrees.First();
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new ArtisanMapperAnalyzer());

        // The editor requests diagnostics for a document, without running compilation-end actions.
        var live = await compilation.WithAnalyzers(analyzers).GetAnalyzerSemanticDiagnosticsAsync(
            compilation.GetSemanticModel(tree), null, CancellationToken.None);
        Ids(live, expected.Split(','));
        Assert.IsTrue(live.All(d => d.Location.SourceTree == tree));

        var build = await compilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync();
        CollectionAssert.AreEquivalent(
            build.Where(d => d.Location.SourceTree == tree).Select(d => d.ToString()).ToArray(),
            live.Select(d => d.ToString()).ToArray());
    }

    [TestMethod]
    public async Task LiveAnalysisUpdatesWhenMapperInAnotherDocumentChanges()
    {
        var compilation = Compile("public class C { void Run(SqlCommand cmd) { cmd.ReadTo<User>(); } }");
        var consumerTree = compilation.SyntaxTrees.Single();
        // Keep shared entity types in the consumer document only.
        var mapperTree = CSharpSyntaxTree.ParseText(
            "using System; using System.Data; using Artisan.Orm; using Microsoft.Data.SqlClient;\n" + FullMapper,
            new CSharpParseOptions(LanguageVersion.CSharp12), "Mapper.cs");
        compilation = compilation.AddSyntaxTrees(mapperTree);
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new ArtisanMapperAnalyzer());

        var live = await compilation.WithAnalyzers(analyzers).GetAnalyzerSemanticDiagnosticsAsync(
            compilation.GetSemanticModel(consumerTree), null, CancellationToken.None);
        Ids(live);

        var edited = compilation.RemoveSyntaxTrees(mapperTree);
        live = await edited.WithAnalyzers(analyzers).GetAnalyzerSemanticDiagnosticsAsync(
            edited.GetSemanticModel(consumerTree), null, CancellationToken.None);
        Ids(live, "ART001");
    }

    [TestMethod]
    public async Task CompleteMapperAndAllMainAreValid()
    {
        Ids(await Analyze(FullMapper));
        Ids(await Analyze(FullMapper.Replace("RequiredMethod.All", "RequiredMethod.AllMain")
            .Replace("public static ObjectRow CreateObjectRow(SqlDataReader r) => new ObjectRow(0);", "")));
    }

    [TestMethod]
    [DataRow("All", 4)]
    [DataRow("AllMain", 3)]
    [DataRow("BothForObject", 2)]
    [DataRow("BothForDataTable", 2)]
    [DataRow("CreateObject", 1)]
    [DataRow("CreateObjectRow", 1)]
    public async Task RequiredSetsAreCheckedWithoutCallSites(string required, int count)
    {
        Ids(await Analyze($"[MapperFor(typeof(User), RequiredMethod.{required})] public static class M {{ }}"),
            Enumerable.Repeat("ART002", count).ToArray());
    }

    [TestMethod]
    public async Task CombinedRequiredValuesAreAUnion()
    {
        Ids(await Analyze("[MapperFor(typeof(User), RequiredMethod.CreateObject, RequiredMethod.BothForDataTable, RequiredMethod.CreateObject)] public static class M {}"),
            "ART002", "ART002", "ART002");
        Ids(await Analyze("[MapperFor(typeof(User))] public static class M {}"));
    }

    [TestMethod]
    [DataRow("public static Role CreateObject(SqlDataReader r) => new Role();", "ART003")]
    [DataRow("public User CreateObject(SqlDataReader r) => new User();", "ART003")]
    [DataRow("public static User CreateObject<T>(SqlDataReader r) => new User();", "ART003")]
    [DataRow("public static User CreateObject(ref SqlDataReader r) => new User();", "ART002")]
    [DataRow("private static User CreateObject(SqlDataReader r) => new User();", "ART002")]
    [DataRow("public static User CreateObject(int r) => new User();", "ART002")]
    [DataRow("public static Func<SqlDataReader, User> CreateObject = r => new User();", "ART002")]
    [DataRow("public static DataTable CreateDataTable(int x = 0) => new DataTable();", "ART003")]
    public async Task InvalidMethodsAreCaught(string member, string id)
    {
        var requirement = member.Contains("CreateDataTable") ? "" : ", RequiredMethod.CreateObject";
        Ids(await Analyze($"[MapperFor(typeof(User){requirement})] public class M {{ {member} }}"), id);
    }

    [TestMethod]
    public async Task InvalidOptionalMethodStillFailsDelegateRegistration()
    {
        Ids(await Analyze("[MapperFor(typeof(User))] public static class M { public static Role CreateObject(SqlDataReader r) => new Role(); }"), "ART003");
    }

    [TestMethod]
    [DataRow("CreateDataTable() => new DataTable(); public static DataTable CreateDataTable(int x) => new DataTable();")]
    [DataRow("CreateObject(SqlDataReader r) => new DataTable(); public static DataTable CreateObject<T>(SqlDataReader r) => new DataTable();")]
    public async Task AmbiguousReflectionLookupIsAnError(string methods)
    {
        Ids(await Analyze("[MapperFor(typeof(User))] public static class M { public static DataTable " + methods + " }"), "ART005");
    }

    [TestMethod]
    public async Task DuplicateRegistrationsFailEvenWithDisjointFunctions()
    {
        Ids(await Analyze("""
            [MapperFor(typeof(User))] public static class A { public static User CreateObject(SqlDataReader r) => new User(); }
            [MapperFor(typeof(User))] public static class B { public static object[] CreateDataRow(User u) => new object[0]; }
            """), "ART004", "ART004");
    }

    [TestMethod]
    [DataRow("null")]
    [DataRow("typeof(void)")]
    [DataRow("typeof(List<>)")]
    public async Task InvalidTargetIsRejected(string target)
    {
        Ids(await Analyze($"[MapperFor({target})] public static class M {{ }}"), "ART007");
    }

    [TestMethod]
    [DataRow("null")]
    [DataRow("(RequiredMethod)99")]
    public async Task InvalidRequiredArrayIsRejected(string value)
    {
        Ids(await Analyze($"[MapperFor(typeof(User), {value})] public static class M {{ }}"), "ART007");
    }

    [TestMethod]
    public async Task GenericMapperAndGenericContainingTypeAreRejected()
    {
        Ids(await Analyze("[MapperFor(typeof(User))] public class M<T> {}"), "ART007");
        Ids(await Analyze("public class Outer<T> { [MapperFor(typeof(User))] public static class M {} }"), "ART007");
    }

    [TestMethod]
    [DataRow("cmd.ReadTo<User>()")]
    [DataRow("cmd.ReadToAsync<User>()")]
    [DataRow("cmd.ReadToList<User>()")]
    [DataRow("cmd.ReadToArrayAsync<User>()")]
    [DataRow("reader.ReadToEnumerable<User>()")]
    [DataRow("reader.ReadToAsyncEnumerable<User>()")]
    [DataRow("cmd.ReadToObjectRows<User>()")]
    [DataRow("reader.ReadToObjectRow<User>()")]
    [DataRow("cmd.ReadToDictionary<int, User>()")]
    [DataRow("MappingManager.GetCreateObjectFunc<User>()")]
    [DataRow("MappingManager.GetCreateObjectRowFunc<User>()")]
    [DataRow("new List<User>().ToDataTable()")]
    [DataRow("cmd.AddTableParam(\"@p\", new List<User>())")]
    [DataRow("cmd.AddTableRowParam(\"@p\", new User())")]
    [DataRow("connection.BulkCopy(new List<User>(), \"Users\")")]
    public async Task MissingMapperIsReportedAtCall(string expression)
    {
        var diagnostics = await Analyze($"public class C {{ void Run(SqlCommand cmd, SqlDataReader reader, SqlConnection connection) {{ {expression}; }} }}");
        Ids(diagnostics, "ART001");
        Assert.AreEqual(expression, diagnostics[0].Location.SourceTree!.GetText().ToString(diagnostics[0].Location.SourceSpan));
    }

    [TestMethod]
    public async Task AsScalarInlineAndNullableGettersDoNotRequireRegistration()
    {
        Ids(await Analyze("""
            public class C {
              void Run(SqlCommand cmd, SqlDataReader r, SqlConnection c) {
                cmd.ReadAs<User>(); cmd.ReadAsListAsync<User>(); r.ReadAs<User>();
                cmd.ReadTo<int>(); cmd.ReadToList<string>(); r.ReadTo<Guid?>();
                cmd.ReadTo(x => new User()); cmd.ReadToList(x => new { Id = 1 });
                r.ReadToObjectRow(x => new ObjectRow(0));
                cmd.ReadToDictionary<int, User>(x => new User());
                new List<User>().ToDataTable(() => new DataTable(), u => new object[0]);
                cmd.AddTableParam("@p", new List<User>(), "UserType", "Id");
                cmd.AddTableRowParam("@p", new User(), "UserType", "Id");
                cmd.AddTableParam("@ids", new int[0]);
                c.BulkCopyAs(new List<User>(), "Users");
                MappingManager.GetCreateDataTableFunc<User>();
                MappingManager.GetCreateDataRowFunc<User>();
                MappingManager.GetCreateDataFuncs<User>(out var table, out var row);
              }
            }
            """));
    }

    [TestMethod]
    public async Task RepositoryConfigurationLambdaDoesNotCountAsMapper()
    {
        Ids(await Analyze("public class Repo : RepositoryBase { public Repo() : base(\"\") {} void Run() { ReadTo<User>(\"sql\", cmd => {}); } }"), "ART001");
    }

    [TestMethod]
    public async Task RequiredByCallDoesNotDependOnRequiredMethod()
    {
        Ids(await Analyze("""
            [MapperFor(typeof(User), RequiredMethod.CreateObject)]
            public static class M { public static User CreateObject(SqlDataReader r) => new User(); }
            public class C { void Run(SqlCommand cmd) { cmd.ReadToObjectRows<User>(); new List<User>().ToDataTable(); } }
            """), "ART006", "ART006", "ART006");
    }

    [TestMethod]
    public async Task EveryMultiResultTypeIsCheckedAndDictionaryKeyIsIgnored()
    {
        Ids(await Analyze("public class C { void Run(SqlCommand cmd) { cmd.ReadToLists<User, int, Role>(); cmd.ReadToDictionary<User, int>(); } }"), "ART001", "ART001");
    }

    [TestMethod]
    public async Task GenericWrappersAreDeferredAndMethodGroupsAreChecked()
    {
        Ids(await Analyze("""
            public class C {
              T Run<T>(SqlCommand cmd) => cmd.ReadTo<T>();
              Func<SqlCommand, User> read = SqlCommandExtensions.ReadTo<User>;
            }
            """), "ART001");
    }

    [TestMethod]
    public async Task SameNamedUserMethodsAreIgnored()
    {
        Ids(await Analyze("public class C { T ReadTo<T>() => default(T); void Run() { ReadTo<User>(); } }"));
    }

    [TestMethod]
    public async Task ExternalMapperOptionOnlyDisablesMissingRegistration()
    {
        Ids(await Analyze("public class C { void Run(SqlCommand cmd) { cmd.ReadTo<User>(); } }", external: true));
        Ids(await Analyze("[MapperFor(typeof(User), RequiredMethod.All)] public static class M {}", external: true),
            "ART002", "ART002", "ART002", "ART002");
    }

    [TestMethod]
    public async Task MapperInReferencedAssemblyIsFound()
    {
        var dependency = Compile("[MapperFor(typeof(User))] internal static class M { public static User CreateObject(SqlDataReader r) => new User(); }");
        using var stream = new MemoryStream();
        Assert.IsTrue(dependency.Emit(stream).Success);
        // The consumer uses the entity from the referenced assembly, not a same-named local type.
        var source = "public class C { void Run(SqlCommand cmd) { cmd.ReadTo<External.User>(); } }";
        var reference = MetadataReference.CreateFromImage(stream.ToArray(), new MetadataReferenceProperties(aliases: ImmutableArray.Create("External")));
        var compilation = CSharpCompilation.Create("Client", new[] { CSharpSyntaxTree.ParseText("extern alias External; using Artisan.Orm; using Microsoft.Data.SqlClient; " + source) },
            References.Append(reference), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.IsFalse(compilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error));
        var diagnostics = await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new ArtisanMapperAnalyzer())).GetAnalyzerDiagnosticsAsync();
        Ids(diagnostics);
    }

    [TestMethod]
    public async Task EnglishResourcesFallbackAndMessagesAreUseful()
    {
        var diagnostics = await Analyze("[MapperFor(typeof(User), RequiredMethod.CreateObject)] public static class M {}");
        var message = diagnostics.Single().GetMessage(CultureInfo.GetCultureInfo("en"));
        StringAssert.Contains(message, "CreateObject");
        StringAssert.Contains(message, "User");
        StringAssert.Contains(message, "Microsoft.Data.SqlClient.SqlDataReader");
        Assert.AreEqual(message, diagnostics.Single().GetMessage(CultureInfo.GetCultureInfo("de")));
    }

    [TestMethod]
    [DataRow("public static User CreateObject(SqlDataReader r) => new User();", true)]
    [DataRow("public static User CreateObject(IDataRecord r) => new User();", true)]
    [DataRow("public static Derived CreateObject(SqlDataReader r) => new Derived();", true)]
    [DataRow("public static object CreateObject(SqlDataReader r) => new User();", false)]
    [DataRow("public User CreateObject(SqlDataReader r) => new User();", false)]
    [DataRow("public static User CreateObject<T>(SqlDataReader r) => new User();", false)]
    public async Task SignatureChecksAgreeWithActualReflection(string method, bool valid)
    {
        var source = $"public class Derived : User {{ }} [MapperFor(typeof(User), RequiredMethod.CreateObject)] public class M {{ {method} }}";
        var compilation = Compile(source);
        using var stream = new MemoryStream();
        Assert.IsTrue(compilation.Emit(stream).Success);
        var assembly = Assembly.Load(stream.ToArray());
        var reflected = assembly.GetType("M")!.GetMethod("CreateObject", new[] { typeof(SqlDataReader) });
        bool bound;
        try
        {
            Delegate.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(SqlDataReader), assembly.GetType("User")!), reflected!);
            bound = true;
        }
        catch (ArgumentException) { bound = false; }
        Assert.AreEqual(valid, bound);
        var diagnostics = await Analyze(source);
        Assert.AreEqual(valid, diagnostics.Length == 0, string.Join("\n", diagnostics));
    }

    private sealed class ConfigProvider(bool external) : AnalyzerConfigOptionsProvider
    {
        private readonly Config options = new(external);
        public override AnalyzerConfigOptions GlobalOptions => options;
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => options;
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => options;
    }
    private sealed class Config(bool external) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string value)
        { value = external ? "true" : "false"; return key == "artisan_orm.allow_external_mappers"; }
    }
}
