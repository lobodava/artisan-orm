using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Tests;

[TestClass]
public class SaveUsersValidationRegressionTest
{
    [TestMethod]
    [DataRow("Login", "NON_UNIQUE_LOGIN")]
    [DataRow("Name", "NON_UNIQUE_NAME")]
    [DataRow("Email", "NON_UNIQUE_EMAIL")]
    public void NewBatchDuplicatesReturnValidationWithoutInserting(string field, string code)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        var first = NewUser(-101);
        var second = CopyField(NewUser(-102), first, field);
        var originalCount = CountUsers(connection, transaction);

        var result = Save(connection, transaction, first, second);

        AssertValidation(result, code);
        Assert.IsTrue(result.MessageIds.All(id => id == first.Id || id == second.Id));
        Assert.AreEqual(originalCount, CountUsers(connection, transaction));
    }

    [TestMethod]
    [DataRow("Login", "NON_UNIQUE_LOGIN")]
    [DataRow("Name", "NON_UNIQUE_NAME")]
    [DataRow("Email", "NON_UNIQUE_EMAIL")]
    public void NewUserConflictingWithExistingUserReturnsOnlyRelevantCode(string field, string code)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        var existing = InsertFixture(connection, transaction);
        var incoming = CopyField(NewUser(-103), existing, field);
        var originalCount = CountUsers(connection, transaction);

        AssertValidation(Save(connection, transaction, incoming), code);
        Assert.AreEqual(originalCount, CountUsers(connection, transaction));
    }

    [TestMethod]
    [DataRow("Login", "NON_UNIQUE_LOGIN")]
    [DataRow("Name", "NON_UNIQUE_NAME")]
    [DataRow("Email", "NON_UNIQUE_EMAIL")]
    public void ExistingUserUpdateCannotDuplicateAnotherUser(string field, string code)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        var existing = InsertFixture(connection, transaction);
        var target = InsertFixture(connection, transaction);
        var incoming = CopyField(target, existing, field);

        AssertValidation(Save(connection, transaction, incoming), code);
        Assert.AreEqual(target, ReadUser(connection, transaction, target.Id));
    }

    [TestMethod]
    public void ExistingUserMayKeepItsOwnUniqueValues()
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        var existing = InsertFixture(connection, transaction);

        var result = Save(connection, transaction, existing);

        Assert.IsNull(result.Status);
        CollectionAssert.AreEqual(new[] { existing }, result.Users);
    }

    [TestMethod]
    public void DistinctBatchInsertsAndUpdatesSuccessfully()
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        var existing = InsertFixture(connection, transaction);
        var updated = NewUser(existing.Id);
        var added = NewUser(-104);
        var originalCount = CountUsers(connection, transaction);

        var result = Save(connection, transaction, updated, added);

        Assert.IsNull(result.Status);
        Assert.AreEqual(2, result.Users.Count);
        Assert.AreEqual(updated, result.Users.Single(user => user.Id == existing.Id));
        var inserted = result.Users.Single(user => user.Login == added.Login);
        Assert.IsTrue(inserted.Id > 0);
        Assert.AreEqual(added with { Id = inserted.Id }, inserted);
        Assert.AreEqual(originalCount + 1, CountUsers(connection, transaction));
    }

    private static void AssertValidation(SaveResult result, string code)
    {
        Assert.AreEqual("Validation", result.Status);
        Assert.AreEqual(0, result.Users.Count);
        Assert.IsTrue(result.Codes.Count > 0);
        Assert.IsTrue(result.Codes.All(actual => actual == code),
            "Independent column checks must not emit unrelated validation codes.");
    }

    private static SaveResult Save(SqlConnection connection, SqlTransaction transaction, params UserRow[] users)
    {
        using var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Login", typeof(string));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Email", typeof(string));
        table.Columns.Add("RowVersion", typeof(byte[]));
        table.Columns.Add("RoleIds", typeof(string));
        foreach (var user in users)
            table.Rows.Add(user.Id, user.Login, user.Name, user.Email, DBNull.Value, DBNull.Value);

        using var command = new SqlCommand("dbo.SaveUsers", connection, transaction)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.Add(new SqlParameter("@Users", SqlDbType.Structured)
        {
            TypeName = "dbo.UserTableType",
            Value = table
        });
        using var reader = command.ExecuteReader();
        Assert.IsTrue(reader.Read());
        var status = reader.IsDBNull(0) ? null : reader.GetString(0);
        Assert.IsFalse(reader.Read());
        Assert.IsTrue(reader.NextResult());
        var result = new SaveResult(status);
        while (reader.Read())
        {
            if (status == "Validation")
            {
                result.Codes.Add(reader.GetString(reader.GetOrdinal("Code")));
                result.MessageIds.Add(reader.GetInt64(reader.GetOrdinal("Id")));
            }
            else
                result.Users.Add(new UserRow(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)));
        }
        Assert.IsFalse(reader.NextResult());
        return result;
    }

    private static UserRow NewUser(int id)
    {
        var token = "sv" + Guid.NewGuid().ToString("N")[..14];
        return new UserRow(id, token, token + " Name", token + "@example.test");
    }

    private static UserRow CopyField(UserRow target, UserRow source, string field) => field switch
    {
        "Login" => target with { Login = source.Login },
        "Name" => target with { Name = source.Name },
        "Email" => target with { Email = source.Email },
        _ => throw new ArgumentOutOfRangeException(nameof(field))
    };

    private static UserRow InsertFixture(SqlConnection connection, SqlTransaction transaction)
    {
        var user = NewUser(0);
        using var command = new SqlCommand("""
            INSERT dbo.Users ([Login], [Name], Email) OUTPUT inserted.Id VALUES (@Login, @Name, @Email);
            """, connection, transaction);
        command.Parameters.Add("@Login", SqlDbType.VarChar, 20).Value = user.Login;
        command.Parameters.Add("@Name", SqlDbType.NVarChar, 50).Value = user.Name;
        command.Parameters.Add("@Email", SqlDbType.VarChar, 50).Value = user.Email;
        return user with { Id = (int)command.ExecuteScalar() };
    }

    private static UserRow ReadUser(SqlConnection connection, SqlTransaction transaction, int id)
    {
        using var command = new SqlCommand("SELECT Id, [Login], [Name], Email FROM dbo.Users WHERE Id = @Id;", connection, transaction);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        using var reader = command.ExecuteReader();
        Assert.IsTrue(reader.Read());
        return new UserRow(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3));
    }

    private static int CountUsers(SqlConnection connection, SqlTransaction transaction)
    {
        using var command = new SqlCommand("SELECT COUNT(*) FROM dbo.Users;", connection, transaction);
        return (int)command.ExecuteScalar();
    }

    private static SqlConnection OpenConnection()
    {
        var connection = new SqlConnection(new AppSettings().ConnectionStrings.DatabaseConnection);
        connection.Open();
        return connection;
    }

    private sealed record UserRow(int Id, string Login, string Name, string Email);
    private sealed class SaveResult(string status)
    {
        public string Status { get; } = status;
        public List<string> Codes { get; } = new();
        public List<long> MessageIds { get; } = new();
        public List<UserRow> Users { get; } = new();
    }
}
