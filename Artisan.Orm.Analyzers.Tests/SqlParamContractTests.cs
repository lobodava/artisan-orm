using System.Collections.Immutable;
using Artisan.Orm.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Artisan.Orm.Analyzers.Tests;

[TestClass]
public class SqlParamContractTests
{
    private const string Header = """
        using System;
        using System.Data;
        using System.Collections.Generic;
        using Artisan.Orm;
        using Microsoft.Data.SqlClient;
        """;

    private const string Model = "public class Product { public int Id { get; set; } }";

    private const string ReadOnlyMapper = """
        [MapperFor(typeof(Product), RequiredMethod.CreateObject)]
        public static class ProductMapper
        {
            public static Product CreateObject(SqlDataReader reader) => new Product();
        }
        """;

    private const string TableMapper = """
        [MapperFor(typeof(Product), RequiredMethod.BothForDataTable)]
        public static class ProductMapper
        {
            public static DataTable CreateDataTable() => new DataTable("dbo.ProductTableType");
            public static object[] CreateDataRow(Product product) => new object[] { product.Id };
        }
        """;

    private static readonly MetadataReference[] References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Concat(new[] { typeof(MapperForAttribute).Assembly.Location, typeof(SqlCommand).Assembly.Location })
        .Distinct(StringComparer.OrdinalIgnoreCase).Select(path => MetadataReference.CreateFromFile(path)).ToArray();

    private static CSharpCompilation Compile(string source, params MetadataReference[] extra) =>
        CSharpCompilation.Create("SqlParamConsumer_" + Guid.NewGuid().ToString("N"),
            new[] { CSharpSyntaxTree.ParseText(Header + "\n" + source,
                new CSharpParseOptions(LanguageVersion.CSharp12), "Consumer.cs") },
            References.Concat(extra), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

    private static async Task<ImmutableArray<Diagnostic>> Analyze(
        string source, bool allowExternal = false, params MetadataReference[] extra)
    {
        var compilation = Compile(source, extra);
        var errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.IsEmpty(errors, string.Join("\n", errors.Select(diagnostic => diagnostic.ToString())));
        var options = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty, new ConfigProvider(allowExternal));
        return await compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new ArtisanMapperAnalyzer()), options).GetAnalyzerDiagnosticsAsync();
    }

    private static string Consumer(string expression) =>
        "public class Consumer { public void Configure() { " + expression + "; } }";

    private static void Expect(ImmutableArray<Diagnostic> diagnostics, params string[] ids)
    {
        CollectionAssert.AreEquivalent(ids, diagnostics.Select(diagnostic => diagnostic.Id).ToArray(), string.Join("\n", diagnostics));
        Assert.IsTrue(diagnostics.All(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
    }

    [TestMethod]
    [DataRow("SqlParam.Table(\"@Products\", new Product[0])")]
    [DataRow("SqlParam.Table(\"@Products\", new Product[0], \"dbo.OtherProductType\")")]
    [DataRow("SqlParam.TableRow(\"@Product\", new Product())")]
    public async Task MappedFactoriesReportMissingRegistrationAtTheFactoryCall(string expression)
    {
        var diagnostics = await Analyze(Model + Consumer(expression));
        Expect(diagnostics, "ART001");
        Assert.AreEqual(expression,
            diagnostics[0].Location.SourceTree!.GetText().ToString(diagnostics[0].Location.SourceSpan));
    }

    [TestMethod]
    [DataRow("SqlParam.Table(\"@Products\", new Product[0])")]
    [DataRow("SqlParam.Table(\"@Products\", new Product[0], \"dbo.OtherProductType\")")]
    [DataRow("SqlParam.TableRow(\"@Product\", new Product())")]
    public async Task MappedFactoriesRequireBothTableFunctionsIndependentlyOfDeclaredReadCapability(string expression)
    {
        var diagnostics = await Analyze(Model + ReadOnlyMapper + Consumer(expression));
        Expect(diagnostics, "ART006", "ART006");
        Assert.AreEqual(1, diagnostics.Count(diagnostic => diagnostic.GetMessage().Contains("'CreateDataTable'")));
        Assert.AreEqual(1, diagnostics.Count(diagnostic => diagnostic.GetMessage().Contains("'CreateDataRow'")));
        Assert.IsTrue(diagnostics.All(diagnostic =>
            diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan) == expression));
    }

    [TestMethod]
    [DataRow("SqlParam.Table(\"@Products\", new Product[0])")]
    [DataRow("SqlParam.Table(\"@Products\", new Product[0], \"dbo.OtherProductType\")")]
    [DataRow("SqlParam.TableRow(\"@Product\", new Product())")]
    public async Task MappedFactoriesAcceptRegisteredTableFunctions(string expression)
    {
        Expect(await Analyze(Model + TableMapper + Consumer(expression)));
    }

    [TestMethod]
    [DataRow("SqlParam.Table(\"@Products\", new Product[0], \"dbo.ProductTableType\", \"Id\")")]
    [DataRow("SqlParam.TableRow(\"@Product\", new Product(), \"dbo.ProductTableType\", \"Id\")")]
    public async Task ExplicitColumnFactoriesUseAutomaticMappingWithoutRegistration(string expression)
    {
        Expect(await Analyze(Model + Consumer(expression)));
    }

    [TestMethod]
    [DataRow("SqlParam.Table(\"@Ids\", new byte[0])")]
    [DataRow("SqlParam.Table(\"@Ids\", new short[0])")]
    [DataRow("SqlParam.Table(\"@Ids\", new int[0])")]
    [DataRow("SqlParam.Table(\"@Ids\", new long[0])")]
    [DataRow("SqlParam.TableRow(\"@Id\", (byte)1)")]
    [DataRow("SqlParam.TableRow(\"@Id\", (short)1)")]
    [DataRow("SqlParam.TableRow(\"@Id\", 1)")]
    [DataRow("SqlParam.TableRow(\"@Id\", 1L)")]
    [DataRow("SqlParam.Table(\"@Products\", new DataTable(\"dbo.ProductTableType\"))")]
    public async Task NonGenericFactoriesDoNotRequireRegistration(string expression)
    {
        Expect(await Analyze(Consumer(expression)));
    }

    [TestMethod]
    [DataRow("SqlParam.Table<int>(\"@Ids\", new int[0])")]
    [DataRow("SqlParam.TableRow<int>(\"@Id\", 1)")]
    public async Task ExplicitGenericIntegerFactoriesStillRequireAMapper(string expression)
    {
        Expect(await Analyze(Consumer(expression)), "ART001");
    }

    [TestMethod]
    public async Task JsonAndUnrelatedFactoryClassesAreIgnored()
    {
        Expect(await Analyze(Model + """
            namespace Application
            {
                public static class SqlParam
                {
                    public static object Table<T>(string name, IEnumerable<T> values) => new object();
                    public static object TableRow<T>(string name, T value) => new object();
                }
            }
            public class Consumer
            {
                public void Configure()
                {
                    Artisan.Orm.SqlParam.Json("@Json", new Product());
                    Application.SqlParam.Table("@Products", new Product[0]);
                    Application.SqlParam.TableRow("@Product", new Product());
                }
            }
            """));
    }

    [TestMethod]
    public async Task SameFullTypeNameInAnotherAssemblyIsIgnored()
    {
        Expect(await Analyze(Model + """
            namespace Artisan.Orm
            {
                public static class SqlParam
                {
                    public static object Table<T>(string name, IEnumerable<T> values) => new object();
                    public static object TableRow<T>(string name, T value) => new object();
                }
            }
            public class Consumer
            {
                public void Configure()
                {
                    SqlParam.Table("@Products", new Product[0]);
                    SqlParam.TableRow("@Product", new Product());
                }
            }
            """));
    }

    [TestMethod]
    public async Task RegisteredFactoryMethodGroupsAreCheckedAndGenericWrappersAreDeferred()
    {
        Expect(await Analyze(Model + """
            public class Consumer
            {
                public SqlParameter? Wrap<T>(IEnumerable<T> values) => SqlParam.Table("@Values", values);
                private Func<string, IEnumerable<Product>, SqlParameter?> table = SqlParam.Table<Product>;
                private Func<string, Product, SqlParameter?> row = SqlParam.TableRow<Product>;
            }
            """), "ART001", "ART001");
    }

    [TestMethod]
    public async Task AutomaticAndIntegerFactoryMethodGroupsDoNotRequireRegistration()
    {
        Expect(await Analyze(Model + """
            public class Consumer
            {
                private Func<string, IEnumerable<Product>, string, string, SqlParameter?> table = SqlParam.Table<Product>;
                private Func<string, Product, string, string, SqlParameter?> row = SqlParam.TableRow<Product>;
                private Func<string, IEnumerable<int>, SqlParameter?> ids = SqlParam.Table;
            }
            """));
    }

    [TestMethod]
    [DataRow("SqlParam.Table(\"@Products\", new Product[0])")]
    [DataRow("SqlParam.TableRow(\"@Product\", new Product())")]
    public async Task ExternalMapperOptionSuppressesOnlyMissingRegistration(string expression)
    {
        Expect(await Analyze(Model + Consumer(expression), allowExternal: true));
        Expect(await Analyze(Model + ReadOnlyMapper + Consumer(expression), allowExternal: true), "ART006", "ART006");
    }

    [TestMethod]
    public async Task MapperInReferencedAssemblySatisfiesFactories()
    {
        var dependency = Compile("namespace Catalog { " + Model + TableMapper + " }");
        using var image = new MemoryStream();
        var emitted = dependency.Emit(image);
        Assert.IsTrue(emitted.Success, string.Join("\n", emitted.Diagnostics));
        var reference = MetadataReference.CreateFromImage(image.ToArray());
        Expect(await Analyze("""
            public class Consumer
            {
                public void Configure()
                {
                    SqlParam.Table("@Products", new Catalog.Product[0]);
                    SqlParam.TableRow("@Product", new Catalog.Product());
                }
            }
            """, false, reference));
    }

    private sealed class ConfigProvider(bool allowExternal) : AnalyzerConfigOptionsProvider
    {
        private readonly Config options = new(allowExternal);
        public override AnalyzerConfigOptions GlobalOptions => options;
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => options;
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => options;
    }

    private sealed class Config(bool allowExternal) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string value)
        {
            value = allowExternal ? "true" : "false";
            return key == "artisan_orm.allow_external_mappers";
        }
    }
}
