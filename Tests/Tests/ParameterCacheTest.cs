using System.Data;
using System.Reflection;
using Artisan.Orm;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Tests;

[TestClass]
public class ParameterCacheTest
{
    [TestMethod]
    [DataRow("other-server", "first-database", "first-user")]
    [DataRow("first-server", "other-database", "first-user")]
    [DataRow("first-server", "first-database", "other-user")]
    public void AddParamsUsesMetadataForItsServerDatabaseAndPrincipal(string server, string database, string user)
    {
        var procedure = ProcedureName();
        using var firstConnection = SqlLoginConnection("first-server", "first-database", "first-user");
        using var otherConnection = SqlLoginConnection(server, database, user);
        using var firstCommand = ProcedureCommand(firstConnection, procedure);
        using var otherCommand = ProcedureCommand(otherConnection, procedure);
        Assert.IsTrue(Cache(firstCommand, new SqlParameter("@Number", SqlDbType.Int)));
        Assert.IsTrue(Cache(otherCommand, new SqlParameter("@Text", SqlDbType.NVarChar, 40)));

        firstCommand.AddParams(new() { ["Number"] = 11 });
        otherCommand.AddParams(new() { ["Text"] = "second database" });

        Assert.AreEqual("@Number", firstCommand.Parameters[0].ParameterName);
        Assert.AreEqual(11, firstCommand.Parameters[0].Value);
        Assert.AreEqual("@Text", otherCommand.Parameters[0].ParameterName);
        Assert.AreEqual("second database", otherCommand.Parameters[0].Value);
        Assert.AreEqual(40, otherCommand.Parameters[0].Size);
        Assert.AreEqual(ConnectionState.Closed, firstConnection.State);
        Assert.AreEqual(ConnectionState.Closed, otherConnection.State);
    }

    [TestMethod]
    public void SqlLoginMetadataIsReusedByEquivalentShortLivedConnections()
    {
        var procedure = ProcedureName();
        using var firstConnection = SqlLoginConnection("cache-server", "cache-database", "cache-user");
        using var firstCommand = ProcedureCommand(firstConnection, procedure);
        Assert.IsTrue(Cache(firstCommand, new SqlParameter("@Id", SqlDbType.Int)));

        using var secondConnection = SqlLoginConnection("cache-server", "cache-database", "cache-user");
        using var secondCommand = ProcedureCommand(secondConnection, procedure);
        secondCommand.AddParams(new() { ["Id"] = 42 });

        Assert.AreEqual(42, secondCommand.Parameters[0].Value);
        Assert.AreEqual(ConnectionState.Closed, secondConnection.State);
    }

    [TestMethod]
    public void IntegratedAuthenticationReusesOnlyItsOwnConnectionMetadata()
    {
        var procedure = ProcedureName();
        using var firstConnection = new SqlConnection("Server=cache-server;Database=cache-database;Integrated Security=true");
        using var secondConnection = new SqlConnection(firstConnection.ConnectionString);
        using var firstCommand = ProcedureCommand(firstConnection, procedure);
        using var secondCommand = ProcedureCommand(secondConnection, procedure);
        Assert.IsTrue(Cache(firstCommand, new SqlParameter("@First", SqlDbType.Int)));
        Assert.IsTrue(Cache(secondCommand, new SqlParameter("@Second", SqlDbType.Int)));

        using var repeatedCommand = ProcedureCommand(firstConnection, procedure);
        repeatedCommand.AddParams(new() { ["First"] = 11 });
        secondCommand.AddParams(new() { ["Second"] = 22 });

        Assert.AreEqual("@First", repeatedCommand.Parameters[0].ParameterName);
        Assert.AreEqual(11, repeatedCommand.Parameters[0].Value);
        Assert.AreEqual("@Second", secondCommand.Parameters[0].ParameterName);
        Assert.AreEqual(22, secondCommand.Parameters[0].Value);
        Assert.AreEqual(ConnectionState.Closed, firstConnection.State);
        Assert.AreEqual(ConnectionState.Closed, secondConnection.State);
    }

    [TestMethod]
    public void ReconfiguredIntegratedConnectionDoesNotReuseAnotherDatabaseSignature()
    {
        var procedure = ProcedureName();
        using var connection = new SqlConnection("Server=cache-server;Database=first-database;Integrated Security=true");
        using var command = ProcedureCommand(connection, procedure);
        Assert.IsTrue(Cache(command, new SqlParameter("@First", SqlDbType.Int)));
        connection.ConnectionString = "Server=cache-server;Database=second-database;Integrated Security=true";
        Assert.IsTrue(Cache(command, new SqlParameter("@Second", SqlDbType.Int)));

        command.AddParams(new() { ["Second"] = 22 });

        Assert.AreEqual("@Second", command.Parameters[0].ParameterName);
        Assert.AreEqual(22, command.Parameters[0].Value);
        Assert.AreEqual(ConnectionState.Closed, connection.State);
    }

    [TestMethod]
    public void AddParamsPreservesFullMetadataAndIsolatesMutableTemplates()
    {
        using var connection = SqlLoginConnection("cache-server", "cache-database", "cache-user");
        var procedure = ProcedureName();
        using var command = ProcedureCommand(connection, procedure);
        var source = new SqlParameter("@Rows", SqlDbType.Structured)
        {
            TypeName = "dbo.ExpectedTableType",
            SourceColumn = "Original",
            SourceVersion = DataRowVersion.Original,
            IsNullable = true,
            Value = new DataTable("OriginalValue")
        };
        Assert.IsTrue(Cache(command, source));
        source.TypeName = "dbo.ChangedTableType";
        source.SourceColumn = "Changed";

        command.AddParams(new());
        var parameter = command.Parameters[0];
        Assert.AreEqual("dbo.ExpectedTableType", parameter.TypeName);
        Assert.AreEqual("Original", parameter.SourceColumn);
        Assert.AreEqual(DataRowVersion.Original, parameter.SourceVersion);
        Assert.IsTrue(parameter.IsNullable);
        Assert.IsNull(parameter.Value);
        parameter.TypeName = "dbo.CommandMutation";
        parameter.Value = new DataTable("CommandValue");

        using var nextCommand = ProcedureCommand(connection, procedure);
        nextCommand.AddParams(new());
        Assert.AreEqual("dbo.ExpectedTableType", nextCommand.Parameters[0].TypeName);
        Assert.IsNull(nextCommand.Parameters[0].Value);
        Assert.AreNotSame(parameter, nextCommand.Parameters[0]);
    }

    [TestMethod]
    public void PublicParameterCacheClonesInputAndEachReturnedCollection()
    {
        var key = Guid.NewGuid().ToString("N");
        var source = new SqlParameter("@Location", SqlDbType.Udt)
        {
            UdtTypeName = "dbo.LocationType",
            Direction = ParameterDirection.InputOutput,
            SourceColumnNullMapping = true
        };
        var input = new[] { source };
        Assert.IsTrue(MappingManager.AddSqlParameters(key, input));
        source.UdtTypeName = "dbo.Changed";
        input[0] = new SqlParameter("@Other", SqlDbType.Int);

        var first = MappingManager.GetSqlParameters(key);
        Assert.AreEqual("dbo.LocationType", first[0].UdtTypeName);
        Assert.AreEqual(ParameterDirection.InputOutput, first[0].Direction);
        Assert.IsTrue(first[0].SourceColumnNullMapping);
        first[0].UdtTypeName = "dbo.ReturnedMutation";
        first[0] = new SqlParameter("@Replacement", SqlDbType.Int);

        var second = MappingManager.GetSqlParameters(key);
        Assert.AreEqual("@Location", second[0].ParameterName);
        Assert.AreEqual("dbo.LocationType", second[0].UdtTypeName);
    }

    [TestMethod]
    public void DefaultSchemaDependentNamesAreNotStoredInSharedMetadataCache()
    {
        using var connection = SqlLoginConnection("cache-server", "cache-database", "cache-user");
        using var command = ProcedureCommand(connection, "UnqualifiedProcedure");
        Assert.IsFalse(Cache(command, new SqlParameter("@Id", SqlDbType.Int)));
    }

    [TestMethod]
    public void UnknownDefaultDatabaseIsNotUsedAsASharedCacheIdentity()
    {
        using var connection = SqlLoginConnection("cache-server", string.Empty, "cache-user");
        using var command = ProcedureCommand(connection, ProcedureName());
        Assert.IsFalse(Cache(command, new SqlParameter("@Id", SqlDbType.Int)));
    }

    [TestMethod]
    public void DerivedTableParameterMetadataSurvivesCachingAndCommandMutation()
    {
        using var connection = new SqlConnection(new AppSettings().ConnectionStrings.DatabaseConnection);
        using var first = ProcedureCommand(connection, "dbo.SaveRecords");

        // Discover the existing procedure's metadata without executing its body.
        first.AddParams(new());
        Assert.AreEqual(ConnectionState.Closed, connection.State);
        var parameter = first.Parameters["@Records"];
        Assert.AreEqual(SqlDbType.Structured, parameter.SqlDbType);
        var expectedTypeName = parameter.TypeName;
        Assert.IsFalse(string.IsNullOrEmpty(expectedTypeName));
        parameter.TypeName = "dbo.ChangedTableType";
        parameter.Value = new DataTable("CommandValue");

        using var second = ProcedureCommand(connection, "dbo.SaveRecords");
        second.AddParams(new());
        var cached = second.Parameters["@Records"];

        Assert.AreEqual(ConnectionState.Closed, connection.State);
        Assert.AreEqual(SqlDbType.Structured, cached.SqlDbType);
        Assert.AreEqual(expectedTypeName, cached.TypeName);
        Assert.AreNotSame(parameter, cached);
        Assert.IsNull(cached.Value);
    }

    private static SqlConnection SqlLoginConnection(string server, string database, string user) =>
        new(new SqlConnectionStringBuilder { DataSource = server, InitialCatalog = database, UserID = user, Password = "unused-test-password" }.ConnectionString);

    private static string ProcedureName() => "dbo.CacheTest" + Guid.NewGuid().ToString("N");

    private static SqlCommand ProcedureCommand(SqlConnection connection, string procedure) =>
        new(procedure, connection) { CommandType = CommandType.StoredProcedure };

    // Seed derived metadata without creating procedures or connecting to a database. The
    // assertions exercise the public AddParams path against that cached discovery result.
    private static bool Cache(SqlCommand command, params SqlParameter[] parameters) =>
        (bool)typeof(MappingManager).GetMethod("CacheProcedureParameters", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { command, parameters });
}
