using Artisan.Orm;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.DAL.Records;
using Tests.DAL.Records.Models;

namespace Tests.Tests
{
	[TestClass]
	public class AsyncEnumerableTest
	{
		private Repository _repository = null!;

		[TestInitialize]
		public void TestInitialize()
		{
			var appSettings = new AppSettings();
			_repository = new Repository(appSettings.ConnectionStrings.DatabaseConnection);
		}

		[TestCleanup]
		public void TestCleanup() => _repository.Dispose();


		[TestMethod]
		public async Task ReadToAsyncEnumerable_RegisteredMapper_StreamsAllRows()
		{
			var count = 0;

			await foreach (var record in _repository.ReadToAsyncEnumerable<Record>("dbo.GetRecords"))
			{
				Assert.IsNotNull(record.Name);
				count++;
			}

			Assert.IsTrue(count > 0);
		}

		[TestMethod]
		public async Task ReadAsAsyncEnumerable_AutoMapping_StreamsAllRows()
		{
			var count = 0;

			await foreach (var record in _repository.ReadAsAsyncEnumerable<Record>(
				"select * from dbo.vwRecords"))
			{
				Assert.IsNotNull(record.Name);
				count++;
			}

			Assert.IsTrue(count > 0);
		}

		[TestMethod]
		public async Task ReadToAsyncEnumerable_EarlyBreak_DoesNotEnumerateRest()
		{
			// Verify that breaking out of the iterator releases the reader cleanly
			// — second call must succeed without "connection still open" errors.
			var first = 0;

			await foreach (var record in _repository.ReadToAsyncEnumerable<Record>("dbo.GetRecords"))
			{
				first++;
				if (first >= 1) break;
			}

			Assert.AreEqual(1, first);

			// Second invocation on the same repo must work — checks proper disposal.
			var allCount = 0;
			await foreach (var _ in _repository.ReadToAsyncEnumerable<Record>("dbo.GetRecords"))
				allCount++;

			Assert.IsTrue(allCount >= first);
		}

		[TestMethod]
		public async Task ReadToAsyncEnumerable_OnCommand_StreamsAllRows()
		{
			using var cmd = _repository.CreateCommand("dbo.GetRecords");

			var count = 0;
			await foreach (var record in cmd.ReadToAsyncEnumerable<Record>())
				count++;

			Assert.IsTrue(count > 0);
		}

		[TestMethod]
		public async Task ReadToAsyncEnumerable_WithCancellationToken_PropagatesCancel()
		{
			using var cts = new CancellationTokenSource();
			var seen = 0;

			await Assert.ThrowsAsync<OperationCanceledException>(async () =>
			{
				await foreach (var value in _repository
					.ReadToAsyncEnumerable<int>("SELECT Value FROM (VALUES (1), (2), (3)) AS Sample(Value) ORDER BY Value", cts.Token)
					.WithCancellation(cts.Token))
				{
					seen++;
					cts.Cancel();
				}
			});

			Assert.AreEqual(1, seen);
		}

		[TestMethod]
		public async Task ReadToAsyncEnumerable_SimpleType()
		{
			var ids = new List<int>();

			await foreach (var id in _repository.ReadToAsyncEnumerable<int>(
				"select Id from dbo.Records"))
			{
				ids.Add(id);
			}

			Assert.IsTrue(ids.Count > 0);
		}
	}
}
