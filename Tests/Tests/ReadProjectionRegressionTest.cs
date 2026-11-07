using Artisan.Orm;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Tests
{
	[TestClass]
	public class ReadProjectionRegressionTest
	{
		private const string ValuesSql =
			"SELECT Value FROM (VALUES (1, 1), (2, 2), (3, 3)) AS SampleValues(Position, Value) " +
			"WHERE Position <= @Count ORDER BY Position;";
		private const string NullableValuesSql =
			"SELECT Value FROM (VALUES (1, CAST(NULL AS int)), (2, 2), (3, 3)) AS SampleValues(Position, Value) " +
			"WHERE Position <= @Count ORDER BY Position;";
		private const string ProjectionSql =
			"SELECT CASE WHEN Value = 1 THEN NULL ELSE Value END AS FirstValue, Value AS SecondValue " +
			"FROM (VALUES (1), (2), (3)) AS SampleValues(Value) WHERE Value <= @Count ORDER BY Value;";

		private RepositoryBase _repository;

		[TestInitialize]
		public void Initialize()
		{
			_repository = new RepositoryBase(new AppSettings().ConnectionStrings.DatabaseConnection);
		}

		[TestCleanup]
		public void Cleanup() => _repository.Dispose();

		[TestMethod]
		[DataRow(0)]
		[DataRow(1)]
		[DataRow(3)]
		public void CommandScalarEnumerables_PreserveEveryRow(int count)
		{
			using var cmd = CreateCommand(ValuesSql, count);
			var expected = Enumerable.Range(1, count).ToArray();
			CollectionAssert.AreEqual(expected, cmd.ReadToEnumerableValues<int>().ToArray());
			CollectionAssert.AreEqual(expected, cmd.ReadToEnumerable<int>().ToArray());
			CollectionAssert.AreEqual(expected, cmd.ReadAsEnumerable<int>().ToArray());

			cmd.CommandText = NullableValuesSql;
			var nullableExpected = new int?[] { null, 2, 3 }.Take(count).ToArray();
			CollectionAssert.AreEqual(nullableExpected, cmd.ReadToEnumerableValues<int?>().ToArray());
			CollectionAssert.AreEqual(nullableExpected, cmd.ReadToEnumerable<int?>().ToArray());
			CollectionAssert.AreEqual(nullableExpected, cmd.ReadAsEnumerable<int?>().ToArray());
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(1)]
		[DataRow(3)]
		public void RepositoryScalarEnumerables_PreserveEveryRow(int count)
		{
			var expected = Enumerable.Range(1, count).ToArray();
			CollectionAssert.AreEqual(expected,
				_repository.ReadToEnumerable<int>(ValuesSql, new SqlParameter("@Count", count)).ToArray());
			CollectionAssert.AreEqual(expected,
				_repository.ReadAsEnumerable<int>(ValuesSql, new SqlParameter("@Count", count)).ToArray());

			var nullableExpected = new int?[] { null, 2, 3 }.Take(count).ToArray();
			CollectionAssert.AreEqual(nullableExpected,
				_repository.ReadToEnumerable<int?>(NullableValuesSql, new SqlParameter("@Count", count)).ToArray());
			CollectionAssert.AreEqual(nullableExpected,
				_repository.ReadAsEnumerable<int?>(NullableValuesSql, new SqlParameter("@Count", count)).ToArray());
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(1)]
		[DataRow(3)]
		public void CommandProjections_HonorFactoryForScalarNullableAndStringOutputs(int count)
		{
			using var cmd = CreateCommand(ProjectionSql, count);
			AssertCommandProjection(cmd, ReadAdjustedValue, Enumerable.Range(101, count).ToArray());
			AssertCommandProjection(cmd, ReadNullableValue, new int?[] { 101, null, null }.Take(count).ToArray());
			AssertCommandProjection(cmd, ReadLabel, new[] { "item-1", "item-2", "item-3" }.Take(count).ToArray());
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(1)]
		[DataRow(3)]
		public async Task AsyncCommandProjections_HonorFactoryForScalarNullableAndStringOutputs(int count)
		{
			using var cmd = CreateCommand(ProjectionSql, count);
			await AssertAsyncCommandProjection(cmd, ReadAdjustedValue, Enumerable.Range(101, count).ToArray());
			await AssertAsyncCommandProjection(cmd, ReadNullableValue, new int?[] { 101, null, null }.Take(count).ToArray());
			await AssertAsyncCommandProjection(cmd, ReadLabel, new[] { "item-1", "item-2", "item-3" }.Take(count).ToArray());
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(1)]
		[DataRow(3)]
		public void ReaderProjections_HonorFactoryForScalarNullableAndStringOutputs(int count)
		{
			using var cmd = CreateCommand(ProjectionSql, count);
			AssertReaderProjection(cmd, ReadAdjustedValue, Enumerable.Range(101, count).ToArray());
			AssertReaderProjection(cmd, ReadNullableValue, new int?[] { 101, null, null }.Take(count).ToArray());
			AssertReaderProjection(cmd, ReadLabel, new[] { "item-1", "item-2", "item-3" }.Take(count).ToArray());
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(1)]
		[DataRow(3)]
		public async Task AsyncReaderProjections_HonorFactoryForScalarNullableAndStringOutputs(int count)
		{
			await _repository.Connection.OpenAsync();
			using var cmd = CreateCommand(ProjectionSql, count);
			await AssertAsyncReaderProjection(cmd, ReadAdjustedValue, Enumerable.Range(101, count).ToArray());
			await AssertAsyncReaderProjection(cmd, ReadNullableValue, new int?[] { 101, null, null }.Take(count).ToArray());
			await AssertAsyncReaderProjection(cmd, ReadLabel, new[] { "item-1", "item-2", "item-3" }.Take(count).ToArray());
		}

		[TestMethod]
		public async Task MultipleResultProjections_HaveTheSameSyncAndAsyncValues()
		{
			using var cmd = CreateCommand(ProjectionSql + ProjectionSql, 3);
			var expectedValues = new[] { 101, 102, 103 };
			var expectedNullableValues = new int?[] { 101, null, null };

			var sync = cmd.ReadToLists<int, int?>(ReadAdjustedValue, ReadNullableValue);
			var asyncResult = await cmd.ReadToListsAsync<int, int?>(ReadAdjustedValue, ReadNullableValue);
			CollectionAssert.AreEqual(expectedValues, sync.Item1.ToArray());
			CollectionAssert.AreEqual(expectedNullableValues, sync.Item2.ToArray());
			CollectionAssert.AreEqual(expectedValues, asyncResult.Item1.ToArray());
			CollectionAssert.AreEqual(expectedNullableValues, asyncResult.Item2.ToArray());
		}

		[TestMethod]
		[DataRow(true)]
		[DataRow(false)]
		public void ReaderProjection_PreservesResultAdvancement(bool getNextResult)
		{
			using var cmd = _repository.CreateCommand("SELECT 1; SELECT 42;");
			cmd.GetByReader(reader =>
			{
				var values = reader.ReadToList<int>(row => row.GetInt32(0) + 100, getNextResult);
				CollectionAssert.AreEqual(new[] { 101 }, values.ToArray());
				if (!getNextResult)
				{
					Assert.IsFalse(reader.Read());
					Assert.IsTrue(reader.NextResult());
				}
				Assert.IsTrue(reader.Read());
				Assert.AreEqual(42, reader.GetInt32(0));
				return true;
			});
		}

		private SqlCommand CreateCommand(string sql, int count)
		{
			var cmd = _repository.CreateCommand(sql);
			cmd.AddIntParam("@Count", count);
			return cmd;
		}

		private static int ReadAdjustedValue(SqlDataReader reader) => reader.GetInt32(1) + 100;

		private static int? ReadNullableValue(SqlDataReader reader) =>
			reader.IsDBNull(0) ? reader.GetInt32(1) + 100 : null;

		private static string ReadLabel(SqlDataReader reader) => "item-" + reader.GetInt32(1);

		private static void AssertCommandProjection<T>(SqlCommand cmd, Func<SqlDataReader, T> factory, T[] expected)
		{
			CollectionAssert.AreEqual(expected, cmd.ReadToList(factory).ToArray());
			var destination = new List<T> { default };
			Assert.AreSame(destination, cmd.ReadToList(factory, destination));
			CollectionAssert.AreEqual(new T[] { default }.Concat(expected).ToArray(), destination.ToArray());
			CollectionAssert.AreEqual(expected, cmd.ReadToArray(factory));
			CollectionAssert.AreEqual(expected, cmd.ReadToEnumerable(factory).ToArray());
		}

		private static async Task AssertAsyncCommandProjection<T>(SqlCommand cmd, Func<SqlDataReader, T> factory, T[] expected)
		{
			CollectionAssert.AreEqual(expected, (await cmd.ReadToListAsync(factory)).ToArray());
			var destination = new List<T> { default };
			Assert.AreSame(destination, await cmd.ReadToListAsync(factory, destination));
			CollectionAssert.AreEqual(new T[] { default }.Concat(expected).ToArray(), destination.ToArray());
			CollectionAssert.AreEqual(expected, await cmd.ReadToArrayAsync(factory));
			CollectionAssert.AreEqual(expected, await CollectAsync(cmd.ReadToAsyncEnumerable(factory)));
		}

		private static void AssertReaderProjection<T>(SqlCommand cmd, Func<SqlDataReader, T> factory, T[] expected)
		{
			CollectionAssert.AreEqual(expected, cmd.GetByReader(reader => reader.ReadToList(factory).ToArray()));
			var destination = new List<T> { default };
			Assert.AreSame(destination, cmd.GetByReader(reader => reader.ReadToList(factory, destination)));
			CollectionAssert.AreEqual(new T[] { default }.Concat(expected).ToArray(), destination.ToArray());
			CollectionAssert.AreEqual(expected, cmd.GetByReader(reader => reader.ReadToArray(factory)));
			CollectionAssert.AreEqual(expected, cmd.GetByReader(reader => reader.ReadToEnumerable(factory).ToArray()));
		}

		private static async Task AssertAsyncReaderProjection<T>(SqlCommand cmd, Func<SqlDataReader, T> factory, T[] expected)
		{
			await using var reader = await cmd.ExecuteReaderAsync();
			CollectionAssert.AreEqual(expected, await CollectAsync(reader.ReadToAsyncEnumerable(factory)));
		}

		private static async Task<T[]> CollectAsync<T>(IAsyncEnumerable<T> source)
		{
			var values = new List<T>();
			await foreach (var value in source)
				values.Add(value);
			return values.ToArray();
		}
	}
}
