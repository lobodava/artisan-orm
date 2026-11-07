using System.Data;
using Artisan.Orm;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Tests;

[TestClass]
public class EnumerableLifetimeRegressionTest
{
    private const string RowsSql = "SELECT Value FROM (VALUES (1), (2), (3)) AS Sample(Value) ORDER BY Value";

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void RepositoryEnumerable_KeepsCommandAliveUntilEnumerationEnds(bool automaticMapping, bool stopEarly)
    {
        using var repository = new RepositoryBase(new AppSettings().ConnectionStrings.DatabaseConnection);
        var disposed = false;
        SqlCommand captured = null;
        void Configure(SqlCommand command)
        {
            captured = command;
            command.Disposed += (_, _) => disposed = true;
        }

        var rows = automaticMapping
            ? repository.ReadAsEnumerable<int>(RowsSql, Configure)
            : repository.ReadToEnumerable<int>(RowsSql, Configure);
        Assert.IsNull(captured, "Command creation is deferred until enumeration begins.");
        Assert.AreEqual(ConnectionState.Closed, repository.Connection.State);

        using (var enumerator = rows.GetEnumerator())
        {
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(1, enumerator.Current);
            Assert.IsNotNull(captured);
            Assert.IsFalse(disposed);
            Assert.AreEqual(ConnectionState.Open, repository.Connection.State);
            if (!stopEarly)
            {
                Assert.IsTrue(enumerator.MoveNext());
                Assert.AreEqual(2, enumerator.Current);
                Assert.IsTrue(enumerator.MoveNext());
                Assert.AreEqual(3, enumerator.Current);
                Assert.IsFalse(enumerator.MoveNext());
            }
        }

        Assert.IsTrue(disposed);
        Assert.AreEqual(ConnectionState.Closed, repository.Connection.State);
        Assert.AreEqual(42, repository.ReadTo<int>("SELECT 42"));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void RepositoryEnumerable_WithParameters_ReleasesReaderOnEarlyExit(bool automaticMapping)
    {
        using var repository = new RepositoryBase(new AppSettings().ConnectionStrings.DatabaseConnection);
        var parameter = new SqlParameter("@Minimum", 1);
        var sql = "SELECT Value FROM (VALUES (1), (2), (3)) AS Sample(Value) WHERE Value >= @Minimum ORDER BY Value";
        var rows = automaticMapping
            ? repository.ReadAsEnumerable<int>(sql, parameter)
            : repository.ReadToEnumerable<int>(sql, parameter);

        using (var enumerator = rows.GetEnumerator())
        {
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(1, enumerator.Current);
            Assert.AreEqual(ConnectionState.Open, repository.Connection.State);
        }

        Assert.AreEqual(ConnectionState.Closed, repository.Connection.State);
        Assert.AreEqual(42, repository.ReadTo<int>("SELECT 42"));
    }
}
