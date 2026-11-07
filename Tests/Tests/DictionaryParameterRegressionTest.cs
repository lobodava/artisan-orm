using System.Data;
using System.Reflection;
using Artisan.Orm;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Tests;

[TestClass]
public class DictionaryParameterRegressionTest
{
    [TestMethod]
    [DataRow("Id")]
    [DataRow("@Id")]
    [DataRow("id")]
    [DataRow("@ID")]
    public void DictionaryNamesAcceptOptionalPrefixAndIgnoreCase(string key)
    {
        using var connection = new SqlConnection("Server=parameter-test;Database=parameter-test;User ID=parameter-test;Password=unused");
        using var command = new SqlCommand("dbo.Parameters_" + Guid.NewGuid().ToString("N"), connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        var cache = typeof(MappingManager).GetMethod("CacheProcedureParameters", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsTrue((bool)cache.Invoke(null, new object[] { command, new[]
        {
            new SqlParameter("@Id", SqlDbType.Int), new SqlParameter("@Name", SqlDbType.NVarChar, 40)
        }}));

        command.AddParams(new Dictionary<string, object> { [key] = 42, ["@name"] = null });

        Assert.AreEqual(42, command.Parameters["@Id"].Value);
        Assert.AreEqual(DBNull.Value, command.Parameters["@Name"].Value);
        Assert.AreEqual(ConnectionState.Closed, connection.State);
    }
}
