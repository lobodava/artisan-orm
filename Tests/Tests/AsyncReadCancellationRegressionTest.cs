using System.Collections.ObjectModel;
using System.Data;
using Artisan.Orm;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Tests;

[TestClass]
public class AsyncReadCancellationRegressionTest
{
    private const string RowsSql = "SELECT Id, Name FROM (VALUES (1, 'one'), (2, 'two'), (3, 'three')) AS Sample(Id, Name) ORDER BY Id;";
    private static string ConnectionString => new AppSettings().ConnectionStrings.DatabaseConnection;

    [TestMethod]
    [DataRow("scalar")]
    [DataRow("single-projection")]
    [DataRow("single-auto")]
    [DataRow("scalar-list")]
    [DataRow("scalar-as-list")]
    [DataRow("mapped-list")]
    [DataRow("auto-list")]
    [DataRow("dynamic")]
    [DataRow("dynamic-list")]
    [DataRow("object-row")]
    [DataRow("object-rows")]
    [DataRow("mapped-object-row")]
    [DataRow("mapped-object-rows")]
    [DataRow("dictionary")]
    [DataRow("auto-dictionary")]
    [DataRow("multi-result")]
    [DataRow("stream")]
    [DataRow("auto-stream")]
    [DataRow("reader-callback")]
    public async Task PreCancelledOperation_DoesNotAttemptToOpenConnection(string operation)
    {
        // An empty connection string would throw InvalidOperationException if opening were attempted.
        using var connection = new SqlConnection();
        using var command = new SqlCommand(RowsSql, connection);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var token = cancellation.Token;

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            switch (operation)
            {
                case "scalar": await command.ReadToAsync<int>(token); break;
                case "single-projection": await command.ReadToAsync(row => row.GetInt32(0), token); break;
                case "single-auto": await command.ReadAsAsync<AsyncReadModel>(token); break;
                case "scalar-list": await command.ReadToListAsync<int>(token); break;
                case "scalar-as-list": await command.ReadAsListAsync(new List<int>(), token); break;
                case "mapped-list": await command.ReadToListAsync<AsyncReadModel>(token); break;
                case "auto-list": await command.ReadAsListAsync<AsyncReadModel>(token); break;
                case "dynamic": await command.ReadDynamicAsync(token); break;
                case "dynamic-list": await command.ReadDynamicListAsync(token); break;
                case "object-row": await command.ReadAsObjectRowAsync(token); break;
                case "object-rows": await command.ReadAsObjectRowsAsync(token); break;
                case "mapped-object-row": await command.ReadToObjectRowAsync<AsyncReadModel>(token); break;
                case "mapped-object-rows": await command.ReadToObjectRowsAsync<AsyncReadModel>(token); break;
                case "dictionary": await command.ReadToDictionaryAsync<int, string>(token); break;
                case "auto-dictionary": await command.ReadAsDictionaryAsync<int, AsyncReadModel>(token); break;
                case "multi-result": await command.ReadToListsAsync<int, int>(token); break;
                case "stream": await foreach (var _ in command.ReadToAsyncEnumerable<int>(token)) { } break;
                case "auto-stream": await foreach (var _ in command.ReadAsAsyncEnumerable<AsyncReadModel>(token)) { } break;
                case "reader-callback": await command.GetByReaderAsync(row => 1, token); break;
                default: throw new ArgumentOutOfRangeException(nameof(operation));
            }
        });

        Assert.AreEqual(ConnectionState.Closed, connection.State);
    }

    [TestMethod]
    [DataRow("scalar-list")]
    [DataRow("nullable-list")]
    [DataRow("auto-scalar-list")]
    [DataRow("auto-list")]
    [DataRow("mapped-list")]
    [DataRow("dynamic-list")]
    [DataRow("projection-list")]
    [DataRow("object-rows")]
    [DataRow("dictionary")]
    public async Task CancellationAfterFirstRow_StopsBufferedMaterialization(string operation)
    {
        using var connection = new SqlConnection(ConnectionString);
        using var command = new SqlCommand(RowsSql, connection);
        using var cancellation = new CancellationTokenSource();
        var seen = 0;
        void Cancel()
        {
            seen++;
            cancellation.Cancel();
        }
        var token = cancellation.Token;

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            switch (operation)
            {
                case "scalar-list": await command.ReadToListAsync(new CallbackCollection<int>(Cancel), token); break;
                case "nullable-list": await command.ReadToListAsync(new CallbackCollection<int?>(Cancel), token); break;
                case "auto-scalar-list": await command.ReadAsListAsync(new CallbackCollection<int>(Cancel), token); break;
                case "auto-list": await command.ReadAsListAsync(new CallbackCollection<AsyncReadModel>(Cancel), token); break;
                case "mapped-list": await command.ReadToListAsync(new CallbackCollection<AsyncReadModel>(Cancel), token); break;
                case "dynamic-list": await command.ReadDynamicListAsync(new CallbackCollection<dynamic>(Cancel), token); break;
                case "projection-list": await command.ReadToListAsync(row => { Cancel(); return row.GetInt32(0); }, token); break;
                case "object-rows": await command.ReadToObjectRowsAsync(row => { Cancel(); return new ObjectRow { row.GetInt32(0) }; }, token); break;
                case "dictionary": await command.ReadToDictionaryAsync<int, string>(row => { Cancel(); return row.GetString(1); }, token); break;
                default: throw new ArgumentOutOfRangeException(nameof(operation));
            }
        });

        Assert.AreEqual(1, seen);
        Assert.AreEqual(ConnectionState.Closed, connection.State);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task FailedCommand_ClosesOnlyConnectionOpenedByHelper(bool initiallyOpen)
    {
        using var connection = new SqlConnection(ConnectionString);
        if (initiallyOpen) await connection.OpenAsync();
        using var command = new SqlCommand("THROW 50000, 'Expected test failure.', 1;", connection);

        var error = await Assert.ThrowsExactlyAsync<SqlException>(() => command.ReadToAsync<int>());

        Assert.AreEqual(50000, error.Number);
        Assert.AreEqual(initiallyOpen ? ConnectionState.Open : ConnectionState.Closed, connection.State);
    }

    [TestMethod]
    public async Task EmptyAutoMappedResult_DoesNotRequireMatchingColumns()
    {
        using var connection = new SqlConnection(ConnectionString);
        using var command = new SqlCommand("SELECT 1 AS Unmatched WHERE 1 = 0;", connection);
        var existing = new AsyncReadModel { Id = 42, Name = "existing" };
        var destination = new List<AsyncReadModel> { existing };

        var result = await command.ReadAsListAsync(destination);
        var emptyList = await command.ReadAsListAsync<AsyncReadModel>();
        var emptyArray = await command.ReadAsArrayAsync<AsyncReadModel>();

        Assert.AreSame(destination, result);
        Assert.AreEqual(1, destination.Count);
        Assert.AreSame(existing, destination[0]);
        Assert.AreEqual(0, emptyList.Count);
        Assert.AreEqual(0, emptyArray.Length);
        Assert.AreEqual(ConnectionState.Closed, connection.State);
    }

    [TestMethod]
    public async Task AsyncObjectRowsAndDictionaries_PreserveValuesAndMapping()
    {
        using var connection = new SqlConnection(ConnectionString);
        using var command = new SqlCommand(RowsSql, connection);

        var automaticRows = await command.ReadAsObjectRowsAsync();
        var mappedRows = await command.ReadToObjectRowsAsync<AsyncReadModel>();
        Assert.AreEqual(3, automaticRows.Count);
        Assert.AreEqual(3, mappedRows.Count);
        CollectionAssert.AreEqual(new object[] { 1, "one" }, automaticRows[0]);
        CollectionAssert.AreEqual(new object[] { "one", 1 }, mappedRows[0]);
        CollectionAssert.AreEqual(new object[] { 1, "one" }, await command.ReadAsObjectRowAsync());
        CollectionAssert.AreEqual(new object[] { "one", 1 }, await command.ReadToObjectRowAsync<AsyncReadModel>());

        var scalars = await command.ReadToDictionaryAsync<int, string>();
        var mapped = await command.ReadToDictionaryAsync<int, AsyncReadModel>();
        var automatic = await command.ReadAsDictionaryAsync<int, AsyncReadModel>();
        Assert.AreEqual(3, scalars.Count);
        Assert.AreEqual("two", scalars[2]);
        Assert.AreEqual("mapped-two", mapped[2].Name);
        Assert.AreEqual("two", automatic[2].Name);

        command.CommandText = "SELECT CAST(NULL AS int) AS Id, CAST(NULL AS int) AS Value UNION ALL SELECT 1, NULL UNION ALL SELECT 2, 20;";
        var nullable = await command.ReadToDictionaryAsync<int, int?>();
        Assert.AreEqual(2, nullable.Count);
        Assert.IsNull(nullable[1]);
        Assert.AreEqual(20, nullable[2]);
    }

    private sealed class CallbackCollection<T>(Action callback) : Collection<T>
    {
        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
            callback();
        }
    }
}

public class AsyncReadModel
{
    public int Id { get; set; }
    public string Name { get; set; }
}

[MapperFor(typeof(AsyncReadModel), RequiredMethod.BothForObject)]
public static class AsyncReadModelMapper
{
    public static AsyncReadModel CreateObject(SqlDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        Name = "mapped-" + reader.GetString(1)
    };

    public static ObjectRow CreateObjectRow(SqlDataReader reader) => new() { reader.GetString(1), reader.GetInt32(0) };
}
