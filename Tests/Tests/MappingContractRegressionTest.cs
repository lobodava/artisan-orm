using System.Data;
using Artisan.Orm;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Tests;

[TestClass]
public class MappingContractRegressionTest
{
    public sealed class ReadOnlyEntity { public int Id { get; set; } }
    public sealed class TableOnlyEntity { }
    public sealed class RowOnlyEntity { }

    [MapperFor(typeof(ReadOnlyEntity), RequiredMethod.CreateObject)]
    public static class ReadOnlyMapper
    {
        public static ReadOnlyEntity CreateObject(SqlDataReader reader) => new() { Id = reader.GetInt32(0) };
    }

    [MapperFor(typeof(TableOnlyEntity))]
    public static class TableOnlyMapper
    {
        public static DataTable CreateDataTable() => new("TableOnly");
    }

    [MapperFor(typeof(RowOnlyEntity))]
    public static class RowOnlyMapper
    {
        public static object[] CreateDataRow(RowOnlyEntity entity) => Array.Empty<object>();
    }

    public sealed class TableEntity
    {
        public int Id { get; set; }
        public int Hidden { private get; set; }
        public int this[int index] => throw new InvalidOperationException("The indexer must not be called.");
    }

    public sealed class ReaderEntity
    {
        public int Id { get; set; }
        public int Hidden { get; private set; }
        public int this[int index]
        {
            get => throw new InvalidOperationException("The indexer must not be called.");
            set => throw new InvalidOperationException("The indexer must not be called.");
        }
    }

    [TestMethod]
    public void ReadOnlyMapperReportsItsMissingTableCapability()
    {
        AssertMissingPair<ReadOnlyEntity>();
        Assert.IsNull(MappingManager.GetCreateDataTableFunc<ReadOnlyEntity>());
        Assert.IsNull(MappingManager.GetCreateDataRowFunc<ReadOnlyEntity>());
    }

    [TestMethod]
    public void PartialMapperDoesNotReportACompleteTableCapability()
    {
        AssertMissingPair<TableOnlyEntity>();
        AssertMissingPair<RowOnlyEntity>();
        Assert.IsNotNull(MappingManager.GetCreateDataTableFunc<TableOnlyEntity>());
        Assert.IsNull(MappingManager.GetCreateDataRowFunc<TableOnlyEntity>());
        Assert.IsNull(MappingManager.GetCreateDataTableFunc<RowOnlyEntity>());
        Assert.IsNotNull(MappingManager.GetCreateDataRowFunc<RowOnlyEntity>());
    }

    private static void AssertMissingPair<T>()
    {
        Assert.IsFalse(MappingManager.GetCreateDataFuncs<T>(out var createTable, out var createRow));
        Assert.IsNull(createTable);
        Assert.IsNull(createRow);
        Assert.IsFalse(MappingManager.GetCreateDataFuncs(typeof(T), out var untypedTable, out var untypedRow));
        Assert.IsNull(untypedTable);
        Assert.IsNull(untypedRow);
        var exception = Assert.ThrowsExactly<ArtisanMappingException>(() => Array.Empty<T>().ToDataTable());
        StringAssert.Contains(exception.Message, typeof(T).FullName);
        StringAssert.Contains(exception.Message, "CreateDataTable");
        StringAssert.Contains(exception.Message, "CreateDataRow");
    }

    [TestMethod]
    public void IncompleteAutoMappingCacheEntryDoesNotCountAsAUsablePair()
    {
        var key = Guid.NewGuid().ToString("N");
        Assert.IsTrue(MappingManager.AddAutoCreateDataFuncs<ReadOnlyEntity>(key, null, null));
        Assert.IsFalse(MappingManager.GetAutoCreateDataFuncs<ReadOnlyEntity>(key, out var table, out var row));
        Assert.IsNull(table);
        Assert.IsNull(row);
    }

    [TestMethod]
    public void AutoTableMappingSkipsPrivateGettersAndIndexers()
    {
        var rows = new[] { new TableEntity { Id = 7, Hidden = 9 } };
        using var table = rows.AsDataTable("PublicValues");
        CollectionAssert.AreEqual(new[] { "Id" }, table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray());
        Assert.AreEqual(7, table.Rows[0][0]);
    }

    [TestMethod]
    public void ExplicitColumnSelectionStillSkipsUnreadableMembers()
    {
        using var table = new[] { new TableEntity { Id = 7 } }.AsDataTable("PublicValues", "Item,Hidden,Id");
        Assert.AreEqual(1, table.Columns.Count);
        Assert.AreEqual("Id", table.Columns[0].ColumnName);
        Assert.AreEqual(7, table.Rows[0][0]);
    }

    [TestMethod]
    public void AutoReaderMappingSkipsPrivateSettersAndIndexers()
    {
        using var connection = new SqlConnection(new AppSettings().ConnectionStrings.DatabaseConnection);
        using var command = new SqlCommand("SELECT 7 AS Id, 8 AS Hidden, 9 AS Item", connection);
        var row = command.ReadAs<ReaderEntity>();
        Assert.AreEqual(7, row.Id);
        Assert.AreEqual(0, row.Hidden);
    }

    [TestMethod]
    public void ManualTableMappingCanUseDifferentColumnNamesAndDerivedValues()
    {
        using var table = new[] { new TableEntity { Id = 7 } }.ToDataTable(
            () => new DataTable("CustomSqlType").AddColumn<int>("SqlNumber"),
            row => new object[] { row.Id + 42 });
        Assert.AreEqual("SqlNumber", table.Columns[0].ColumnName);
        Assert.AreEqual(49, table.Rows[0][0]);
    }

    [TestMethod]
    public void ExplicitTableMappingRejectsMissingDelegatesBeforeInvokingThem()
    {
        var rows = Array.Empty<TableEntity>();
        Assert.ThrowsExactly<ArgumentNullException>(() => rows.ToDataTable(null, _ => Array.Empty<object>()));
        Assert.ThrowsExactly<ArgumentNullException>(() => rows.ToDataTable(() => new DataTable(), null));
    }
}
