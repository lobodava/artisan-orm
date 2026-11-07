using System.Data;
using Artisan.Orm;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Tests;

[TestClass]
public class DataReplyTest
{
    private static SqlConnection OpenConnection()
    {
        var connection = new SqlConnection(new AppSettings().ConnectionStrings.DatabaseConnection);
        connection.Open();
        return connection;
    }

    [TestMethod]
    [DataRow("Ok")]
    [DataRow("OK")]
    [DataRow("oK")]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void SuccessLeavesReaderAtFirstDataResult(string status)
    {
        using var connection = OpenConnection();
        using var command = new SqlCommand("SELECT @status; SELECT 42; SELECT 99;", connection);
        command.Parameters.Add("@status", SqlDbType.NVarChar, 20).Value = (object)status ?? DBNull.Value;
        using var reader = command.ExecuteReader();

        RepositoryBase.CheckForDataReplyException(reader);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(42, reader.GetInt32(0));
        Assert.IsFalse(reader.Read());
        Assert.IsTrue(reader.NextResult());
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(99, reader.GetInt32(0));
        Assert.IsFalse(reader.NextResult());
    }

    [TestMethod]
    public void OkWithoutFollowingResultDoesNotThrow()
    {
        using var connection = OpenConnection();
        using var command = new SqlCommand("SELECT N'Ok';", connection);
        using var reader = command.ExecuteReader();

        RepositoryBase.CheckForDataReplyException(reader);

        Assert.IsFalse(reader.NextResult());
    }

    [TestMethod]
    [DataRow("Fail", DataReplyStatus.Fail)]
    [DataRow("Missing", DataReplyStatus.Missing)]
    [DataRow("Validation", DataReplyStatus.Validation)]
    [DataRow("Concurrency", DataReplyStatus.Concurrency)]
    [DataRow("Denial", DataReplyStatus.Denial)]
    [DataRow("Error", DataReplyStatus.Error)]
    public void FailureWithoutMessagesPreservesStatus(string status, DataReplyStatus expected)
    {
        using var connection = OpenConnection();
        using var command = new SqlCommand("SELECT @status;", connection);
        command.Parameters.Add("@status", SqlDbType.NVarChar, 20).Value = status;
        using var reader = command.ExecuteReader();

        var exception = Assert.ThrowsExactly<DataReplyException>(
            () => RepositoryBase.CheckForDataReplyException(reader));

        Assert.AreEqual(expected, exception.Status);
        Assert.IsNull(exception.Messages);
    }

    [TestMethod]
    public void ValidationPreservesMessages()
    {
        using var connection = OpenConnection();
        using var command = new SqlCommand("""
            SELECT N'Validation';
            SELECT N'INVALID_NAME' AS Code, N'Name is required' AS Text,
                   CAST(7 AS bigint) AS Id, CAST(N'' AS sql_variant) AS Value;
            """, connection);
        using var reader = command.ExecuteReader();

        var exception = Assert.ThrowsExactly<DataReplyException>(
            () => RepositoryBase.CheckForDataReplyException(reader));

        Assert.AreEqual(DataReplyStatus.Validation, exception.Status);
        Assert.HasCount(1, exception.Messages);
        Assert.AreEqual("INVALID_NAME", exception.Messages[0].Code);
        Assert.AreEqual("Name is required", exception.Messages[0].Text);
        Assert.AreEqual(7L, exception.Messages[0].Id);
        Assert.AreEqual("", exception.Messages[0].Value);
    }

    [TestMethod]
    public void UnknownStatusStillThrowsInvalidCastException()
    {
        using var connection = OpenConnection();
        using var command = new SqlCommand("SELECT N'UnknownStatus';", connection);
        using var reader = command.ExecuteReader();

        Assert.ThrowsExactly<InvalidCastException>(() => RepositoryBase.CheckForDataReplyException(reader));
    }
}
