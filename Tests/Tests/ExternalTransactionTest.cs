using System.Data;
using Artisan.Orm;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Tests;

[TestClass]
public class ExternalTransactionTest
{
    private static string ConnectionString => new AppSettings().ConnectionStrings.DatabaseConnection;

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task DisposingBorrowedRepository_LeavesConnectionAndTransactionUsable(bool disposeAsync)
    {
        using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();
        const string suppliedConnectionString = "Server=unused;Database=unused;Integrated Security=true";
        using var repository = new RepositoryBase(transaction, suppliedConnectionString);

        Assert.AreSame(connection, repository.Connection);
        Assert.AreSame(transaction, repository.Transaction);
        Assert.AreEqual(suppliedConnectionString, repository.ConnectionString);
        Assert.AreEqual(1, repository.ReadTo<int>("SELECT 1"));

        if (disposeAsync)
            await repository.DisposeAsync();
        else
            repository.Dispose();

        // Mixed and repeated disposal must preserve the caller's ownership too.
        repository.Dispose();
        await repository.DisposeAsync();
        Assert.IsNull(repository.Connection);
        Assert.IsNull(repository.Transaction);
        Assert.AreEqual(ConnectionState.Open, connection.State);
        Assert.AreSame(connection, transaction.Connection);

        using var command = new SqlCommand("SELECT 2", connection, transaction);
        Assert.AreEqual(2, (int)await command.ExecuteScalarAsync());
        await transaction.CommitAsync();
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task MultipleRepositories_ShareTheCallersCommitOrRollback(bool commit)
    {
        using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        using (var createTable = new SqlCommand("CREATE TABLE #ExternalTransactionAudit (Id int NOT NULL)", connection))
            await createTable.ExecuteNonQueryAsync();

        using var transaction = connection.BeginTransaction();
        using var first = new RepositoryBase(transaction, ConnectionString);
        using var second = new RepositoryBase(transaction, ConnectionString);
        first.Execute("INSERT INTO #ExternalTransactionAudit (Id) VALUES (1)");
        await second.ExecuteAsync("INSERT INTO #ExternalTransactionAudit (Id) VALUES (2)");
        Assert.AreEqual(2, first.ReadTo<int>("SELECT COUNT(*) FROM #ExternalTransactionAudit"));

        first.Dispose();
        Assert.AreEqual(2, await second.ReadToAsync<int>("SELECT COUNT(*) FROM #ExternalTransactionAudit"));
        await second.DisposeAsync();

        if (commit)
            await transaction.CommitAsync();
        else
            await transaction.RollbackAsync();

        using var count = new SqlCommand("SELECT COUNT(*) FROM #ExternalTransactionAudit", connection);
        Assert.AreEqual(commit ? 2 : 0, (int)await count.ExecuteScalarAsync());
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Constructor_RejectsCompletedTransactions(bool commit)
    {
        using var connection = new SqlConnection(ConnectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();
        if (commit)
            transaction.Commit();
        else
            transaction.Rollback();

        var exception = Assert.ThrowsExactly<ArgumentException>(() => new RepositoryBase(transaction, ConnectionString));

        Assert.AreEqual("transaction", exception.ParamName);
        Assert.AreEqual(ConnectionState.Open, connection.State);
    }

    [TestMethod]
    public void Constructor_RejectsDisposedTransactions()
    {
        using var connection = new SqlConnection(ConnectionString);
        connection.Open();
        var transaction = connection.BeginTransaction();
        transaction.Dispose();

        var exception = Assert.ThrowsExactly<ArgumentException>(() => new RepositoryBase(transaction, ConnectionString));

        Assert.AreEqual("transaction", exception.ParamName);
        Assert.AreEqual(ConnectionState.Open, connection.State);
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task ConstructorWithoutTransaction_OwnsItsResources(bool explicitNullTransaction, bool disposeAsync)
    {
        using var repository = explicitNullTransaction
            ? new RepositoryBase(null, ConnectionString)
            : new RepositoryBase(ConnectionString);
        var connection = repository.Connection;
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();
        repository.Transaction = transaction;

        if (disposeAsync)
            await repository.DisposeAsync();
        else
            repository.Dispose();

        Assert.IsNull(repository.Connection);
        Assert.IsNull(repository.Transaction);
        Assert.AreEqual(ConnectionState.Closed, connection.State);
        Assert.IsNull(transaction.Connection);
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task TransactionHelpers_RejectNestingWithoutChangingBorrowedTransaction(bool automaticCommit, bool asynchronous)
    {
        using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();
        using var repository = new RepositoryBase(transaction, ConnectionString);
        var callbackInvoked = false;

        if (asynchronous)
        {
            Task Callback(SqlTransaction _, CancellationToken __)
            {
                callbackInvoked = true;
                return Task.CompletedTask;
            }

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => automaticCommit
                ? repository.RunInTransactionAsync(Callback)
                : repository.BeginTransactionAsync(Callback));
        }
        else
        {
            void Callback(SqlTransaction _) => callbackInvoked = true;

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                if (automaticCommit)
                    repository.RunInTransaction(Callback);
                else
                    repository.BeginTransaction(Callback);
            });
        }

        Assert.IsFalse(callbackInvoked);
        Assert.AreSame(connection, repository.Connection);
        Assert.AreSame(transaction, repository.Transaction);
        Assert.AreSame(connection, transaction.Connection);
        Assert.AreEqual(1, repository.ReadTo<int>("SELECT 1"));
        await transaction.RollbackAsync();
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task TransactionHelpers_AfterDetachingCompletedTransaction_KeepTheBorrowedConnectionOpen(bool asynchronous)
    {
        using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();
        using var repository = new RepositoryBase(transaction, ConnectionString);
        transaction.Commit();
        repository.Transaction = null;
        SqlTransaction startedTransaction = null;

        if (asynchronous)
            await repository.RunInTransactionAsync(async (current, ct) =>
            {
                startedTransaction = current;
                Assert.AreSame(connection, current.Connection);
                Assert.AreEqual(1, await repository.ReadToAsync<int>("SELECT 1", ct));
            });
        else
            repository.RunInTransaction(current =>
            {
                startedTransaction = current;
                Assert.AreSame(connection, current.Connection);
                Assert.AreEqual(1, repository.ReadTo<int>("SELECT 1"));
            });

        Assert.IsNotNull(startedTransaction);
        Assert.IsNull(startedTransaction.Connection);
        Assert.IsNull(repository.Transaction);
        await repository.DisposeAsync();
        Assert.AreEqual(ConnectionState.Open, connection.State);
    }
}
