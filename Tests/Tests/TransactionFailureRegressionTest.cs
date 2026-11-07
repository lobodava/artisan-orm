using System.Data;
using Artisan.Orm;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Tests;

[TestClass]
public class TransactionFailureRegressionTest
{
    private static string ConnectionString => new AppSettings().ConnectionStrings.DatabaseConnection;

    [TestMethod]
    [DataRow("commit", false)]
    [DataRow("rollback", false)]
    [DataRow("close", false)]
    [DataRow("commit", true)]
    [DataRow("rollback", true)]
    [DataRow("close", true)]
    public async Task ManualTransaction_PreservesCallbackFailureAfterTransactionEnds(string completion, bool asynchronous)
    {
        using var repository = new RepositoryBase(ConnectionString);
        var expected = new ApplicationException("Original callback failure.");
        SqlTransaction started = null;
        void Complete(SqlTransaction transaction)
        {
            started = transaction;
            if (completion == "commit") transaction.Commit();
            else if (completion == "rollback") transaction.Rollback();
            else repository.Connection.Close();
        }

        Exception actual;
        if (asynchronous)
            actual = await Assert.ThrowsExactlyAsync<ApplicationException>(() => repository.BeginTransactionAsync((transaction, _) =>
            {
                Complete(transaction);
                return Task.FromException(expected);
            }));
        else
            actual = Assert.ThrowsExactly<ApplicationException>(() => repository.BeginTransaction(transaction =>
            {
                Complete(transaction);
                throw expected;
            }));

        Assert.AreSame(expected, actual);
        Assert.IsNull(repository.Transaction);
        Assert.IsNull(started.Connection);
        Assert.AreEqual(ConnectionState.Closed, repository.Connection.State);
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task TransactionFailure_RollsBackWorkAndPreservesCallerConnection(bool automaticCommit, bool asynchronous)
    {
        using var repository = new RepositoryBase(ConnectionString);
        await repository.Connection.OpenAsync();
        using (var createTable = new SqlCommand("CREATE TABLE #RollbackRegression (Id int NOT NULL)", repository.Connection))
            await createTable.ExecuteNonQueryAsync();
        var expected = new ApplicationException("Original callback failure.");
        using var cancellation = new CancellationTokenSource();

        if (asynchronous)
        {
            async Task Callback(SqlTransaction transaction, CancellationToken token)
            {
                using var insert = new SqlCommand("INSERT INTO #RollbackRegression VALUES (1)", repository.Connection, transaction);
                await insert.ExecuteNonQueryAsync(token);
                cancellation.Cancel();
                throw expected;
            }
            var actual = await Assert.ThrowsExactlyAsync<ApplicationException>(() => automaticCommit
                ? repository.RunInTransactionAsync(Callback, cancellation.Token)
                : repository.BeginTransactionAsync(Callback, cancellation.Token));
            Assert.AreSame(expected, actual);
        }
        else
        {
            void Callback(SqlTransaction transaction)
            {
                using var insert = new SqlCommand("INSERT INTO #RollbackRegression VALUES (1)", repository.Connection, transaction);
                insert.ExecuteNonQuery();
                throw expected;
            }
            var actual = Assert.ThrowsExactly<ApplicationException>(() =>
            {
                if (automaticCommit) repository.RunInTransaction(Callback);
                else repository.BeginTransaction(Callback);
            });
            Assert.AreSame(expected, actual);
        }

        Assert.IsNull(repository.Transaction);
        Assert.AreEqual(ConnectionState.Open, repository.Connection.State);
        Assert.AreEqual(0, repository.ReadTo<int>("SELECT COUNT(*) FROM #RollbackRegression"));
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task BeginFailure_ClosesConnectionOpenedByHelper(bool automaticCommit, bool asynchronous)
    {
        using var repository = new RepositoryBase(ConnectionString);
        var callbackInvoked = false;
        if (asynchronous)
        {
            Task Callback(SqlTransaction _, CancellationToken __)
            {
                callbackInvoked = true;
                return Task.CompletedTask;
            }
            await Assert.ThrowsAsync<ArgumentException>(() => automaticCommit
                ? repository.RunInTransactionAsync(IsolationLevel.Chaos, Callback)
                : repository.BeginTransactionAsync(IsolationLevel.Chaos, Callback));
        }
        else
        {
            Assert.Throws<ArgumentException>(() =>
            {
                if (automaticCommit) repository.RunInTransaction(IsolationLevel.Chaos, _ => callbackInvoked = true);
                else repository.BeginTransaction(IsolationLevel.Chaos, _ => callbackInvoked = true);
            });
        }

        Assert.IsFalse(callbackInvoked);
        Assert.IsNull(repository.Transaction);
        Assert.AreEqual(ConnectionState.Closed, repository.Connection.State);
    }
}
