using System.Data;
using Artisan.Orm;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Tests;

[TestClass]
public class EnumMappingRegressionTest
{
    public enum State { Unknown = 0, Ready = 1, Complete = 2 }
    public enum ByteState : byte { Maximum = byte.MaxValue }
    public enum ShortState : short { Minimum = short.MinValue }
    public enum LongState : long { Large = 4294967296L }
    public enum UnsignedState : uint { Maximum = uint.MaxValue }
    public enum UnsignedLongState : ulong { Maximum = ulong.MaxValue }

    public sealed class EnumRow
    {
        public State State { get; set; }
        public State? Optional { get; set; }
        public LongState Large { get; set; }
    }

    private static SqlConnection Connection() => new(new AppSettings().ConnectionStrings.DatabaseConnection);

    [TestMethod]
    public void ScalarEnumsAndNullableEnumsUseNumericConversion()
    {
        using var connection = Connection();
        using var command = new SqlCommand("SELECT 1", connection);
        Assert.AreEqual(State.Ready, command.ReadTo<State>());
        Assert.AreEqual(State.Ready, command.ReadAs<State>());
        Assert.AreEqual(State.Ready, command.ReadTo<State?>());
        command.CommandText = "SELECT CAST(NULL AS int)";
        Assert.IsNull(command.ReadTo<State?>());
        command.CommandText = "SELECT 99";
        Assert.AreEqual((State)99, command.ReadTo<State>());
    }

    [TestMethod]
    public void EnumNumericWidthsArePreservedAndOverflowIsRejected()
    {
        using var connection = Connection();
        using var command = new SqlCommand("SELECT CAST(255 AS tinyint)", connection);
        Assert.AreEqual(ByteState.Maximum, command.ReadTo<ByteState>());
        command.CommandText = "SELECT CAST(-32768 AS smallint)";
        Assert.AreEqual(ShortState.Minimum, command.ReadTo<ShortState>());
        command.CommandText = "SELECT CAST(4294967296 AS bigint)";
        Assert.AreEqual(LongState.Large, command.ReadTo<LongState>());
        command.CommandText = "SELECT CAST(4294967295 AS bigint)";
        Assert.AreEqual(UnsignedState.Maximum, command.ReadTo<UnsignedState>());
        command.CommandText = "SELECT CAST(18446744073709551615 AS decimal(20,0))";
        Assert.AreEqual(UnsignedLongState.Maximum, command.ReadTo<UnsignedLongState>());
        command.CommandText = "SELECT 256";
        Assert.ThrowsExactly<OverflowException>(() => command.ReadTo<ByteState>());
    }

    [TestMethod]
    public void EnumCollectionsDictionariesAndMultipleResultsDoNotRequireMappers()
    {
        using var connection = Connection();
        using var command = new SqlCommand("SELECT Value FROM (VALUES (1), (2)) AS Samples(Value) ORDER BY Value", connection);
        CollectionAssert.AreEqual(new[] { State.Ready, State.Complete }, command.ReadToArray<State>());
        CollectionAssert.AreEqual(new[] { State.Ready, State.Complete }, command.ReadToEnumerable<State>().ToArray());
        command.CommandText = "SELECT 1 AS Id, CAST(NULL AS int) AS Value UNION ALL SELECT 2, 1";
        var dictionary = command.ReadToDictionary<State, State?>();
        Assert.IsNull(dictionary[State.Ready]);
        Assert.AreEqual(State.Ready, dictionary[State.Complete]);
        command.CommandText = "SELECT 1; SELECT CAST(NULL AS int) UNION ALL SELECT 2";
        var results = command.ReadToLists<State, State?>();
        CollectionAssert.AreEqual(new[] { State.Ready }, results.Item1.ToArray());
        CollectionAssert.AreEqual(new State?[] { null, State.Complete }, results.Item2.ToArray());
    }

    [TestMethod]
    public async Task AsyncEnumCollectionsAndScalarsMatchSynchronousConversion()
    {
        using var connection = Connection();
        using var command = new SqlCommand("SELECT 2", connection);
        Assert.AreEqual(State.Complete, await command.ReadToAsync<State>());
        command.CommandText = "SELECT CAST(NULL AS int) AS Value UNION ALL SELECT 1";
        CollectionAssert.AreEqual(new State?[] { null, State.Ready }, await command.ReadToArrayAsync<State?>());
        var streamed = new List<State?>();
        await foreach (var value in command.ReadToAsyncEnumerable<State?>()) streamed.Add(value);
        CollectionAssert.AreEqual(new State?[] { null, State.Ready }, streamed);
        command.CommandText = "SELECT 1 AS Id, CAST(NULL AS int) AS Value UNION ALL SELECT 2, 1";
        var dictionary = await command.ReadToDictionaryAsync<State, State?>();
        Assert.IsNull(dictionary[State.Ready]);
        Assert.AreEqual(State.Ready, dictionary[State.Complete]);
    }

    [TestMethod]
    public void AutoReaderMappingConvertsEnumPropertiesAndPreservesNull()
    {
        using var connection = Connection();
        using var command = new SqlCommand("SELECT 1 AS State, CAST(NULL AS int) AS Optional, CAST(4294967296 AS bigint) AS Large", connection);
        var row = command.ReadAs<EnumRow>();
        Assert.AreEqual(State.Ready, row.State);
        Assert.IsNull(row.Optional);
        Assert.AreEqual(LongState.Large, row.Large);
        command.CommandText = "SELECT 2 AS State, 1 AS Optional, CAST(4294967296 AS bigint) AS Large";
        row = command.ReadAs<EnumRow>();
        Assert.AreEqual(State.Complete, row.State);
        Assert.AreEqual(State.Ready, row.Optional);
    }

    [TestMethod]
    public void EnumDataTablesUseUnderlyingNumericColumnTypes()
    {
        using var scalar = new[] { State.Ready, State.Complete }.AsDataTable("States", "Value");
        Assert.AreEqual(typeof(int), scalar.Columns[0].DataType);
        CollectionAssert.AreEqual(new object[] { 1, 2 }, scalar.Rows.Cast<DataRow>().Select(r => r[0]).ToArray());
        using var nullable = new State?[] { null, State.Complete }.AsDataTable("States", "Value");
        Assert.AreEqual(typeof(int), nullable.Columns[0].DataType);
        Assert.AreEqual(DBNull.Value, nullable.Rows[0][0]);
        Assert.AreEqual(2, nullable.Rows[1][0]);
        using var wide = new[] { LongState.Large }.AsDataTable("States", "Value");
        Assert.AreEqual(typeof(long), wide.Columns[0].DataType);
        Assert.AreEqual(4294967296L, wide.Rows[0][0]);
        using var unsigned = new[] { UnsignedLongState.Maximum }.AsDataTable("States", "Value");
        Assert.AreEqual(typeof(ulong), unsigned.Columns[0].DataType);
        Assert.AreEqual(ulong.MaxValue, unsigned.Rows[0][0]);
    }

    [TestMethod]
    public void AutoTableMappingConvertsEnumPropertiesAndPreservesNull()
    {
        using var table = new[]
        {
            new EnumRow { State = State.Ready, Optional = null, Large = LongState.Large },
            new EnumRow { State = State.Complete, Optional = State.Ready, Large = LongState.Large }
        }.AsDataTable("EnumValues");
        Assert.AreEqual(typeof(int), table.Columns["State"].DataType);
        Assert.AreEqual(typeof(int), table.Columns["Optional"].DataType);
        Assert.AreEqual(typeof(long), table.Columns["Large"].DataType);
        Assert.AreEqual(1, table.Rows[0]["State"]);
        Assert.AreEqual(DBNull.Value, table.Rows[0]["Optional"]);
        Assert.AreEqual(1, table.Rows[1]["Optional"]);
        Assert.AreEqual(4294967296L, table.Rows[1]["Large"]);
    }

    [TestMethod]
    public void AddColumnSupportsEnumAndNullableEnumThroughNumericStorage()
    {
        using var table = new DataTable().AddColumn<ByteState>("State").AddColumn<ByteState?>("Optional");
        Assert.AreEqual(typeof(byte), table.Columns[0].DataType);
        Assert.AreEqual(typeof(byte), table.Columns[1].DataType);
    }
}
