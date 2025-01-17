using System.Diagnostics;
using System.Text.Json;
using Artisan.Orm;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.DAL.GrandRecords;
using Tests.DAL.GrandRecords.Models;

namespace Tests.Tests
{
	[TestClass]
	public class GrandRecordRepositoryTest
	{
		private Repository _repository;

		[TestInitialize]
		public void TestInitialize()
		{
			var appSettings = new AppSettings();

			_repository = new Repository(appSettings.ConnectionStrings.DatabaseConnection);

			_repository.ExecuteCommand(cmd => {
				cmd.UseSql("delete from dbo.Records where Id > 676; delete from dbo.GrandRecords where Id > 26;");	
			});
		}

		[TestMethod]
		public void GetGrandRecordById()
		{
			GrandRecord grandRecord = null;

			var sw = new Stopwatch();
			sw.Start();

			for (var i = 1; i <= 26; i++)
			{
				grandRecord = _repository.GetGrandRecordById(i);

				Assert.IsNotNull(grandRecord);
				Assert.IsTrue(grandRecord.Id == i);
			}

			sw.Stop();

			Console.WriteLine($"GetGrandRecordById read 26 times for {sw.Elapsed.TotalMilliseconds:0.##} ms, or {sw.Elapsed.TotalMilliseconds / 26:0.##} ms for one read");
			Console.WriteLine();
			Console.WriteLine($"The last GrandRecord contains {grandRecord.Records.Count} Records and {grandRecord.Records.SelectMany(r => r.ChildRecords).ToList().Count} ChildRecords:");
			Console.WriteLine();
			Console.Write(JsonSerializer.Serialize(grandRecord));
		}


		[TestMethod]
		public async Task GetGrandRecordByIdAsync()
		{
			GrandRecord grandRecord = null;

			var sw = new Stopwatch();
			sw.Start();

			for (var i = 1; i <= 26; i++)
			{
				grandRecord = await _repository.GetGrandRecordByIdAsync(i);

				Assert.IsNotNull(grandRecord);
				Assert.IsTrue(grandRecord.Id == i);
			}

			sw.Stop();

			Console.WriteLine($"GetGrandRecordByIdAsync read 26 times for {sw.Elapsed.TotalMilliseconds:0.##} ms, or {sw.Elapsed.TotalMilliseconds / 26:0.##} ms for one read");
			Console.WriteLine();
			Console.WriteLine($"The last GrandRecord contains {grandRecord.Records.Count} Records and {grandRecord.Records.SelectMany(r => r.ChildRecords).ToList().Count} ChildRecords:");
			Console.WriteLine();
			Console.Write(JsonSerializer.Serialize(grandRecord));
		}


		[TestMethod]
		public void GetGrandRecords()
		{
			var sw = new Stopwatch();
			sw.Start();

			var grandRecords  = _repository.GetGrandRecords();
	
			sw.Stop();

			Assert.IsNotNull(grandRecords);
			Assert.IsTrue(grandRecords.Count >= 26);

			Console.WriteLine("GetGrandRecords read & combined:");
			Console.WriteLine($"	{grandRecords.Count} GrandRecords,");
			Console.WriteLine($"	{grandRecords.SelectMany(gr => gr.Records).ToList().Count} Records,");
			Console.WriteLine($"	{grandRecords.SelectMany(gr => gr.Records).SelectMany(r => r.ChildRecords).ToList().Count} ChildRecords");
			Console.WriteLine($"for {sw.Elapsed.TotalMilliseconds:0.##} ms.");
			Console.WriteLine();
			Console.WriteLine("The first GrandRecord looks like:");
			Console.WriteLine();
			Console.Write(JsonSerializer.Serialize(grandRecords.Take(1)));
		}


		[TestMethod]
		public async Task GetGrandRecordsAsync()
		{
			var sw = new Stopwatch();
			sw.Start();

			var grandRecords  = await _repository.GetGrandRecordsAsync();
	
			sw.Stop();

			Assert.IsNotNull(grandRecords);
			Assert.IsTrue(grandRecords.Count >= 26);

			Console.WriteLine("GetRecordsAsync read & combined:");
			Console.WriteLine($"	{grandRecords.Count} GrandRecords,");
			Console.WriteLine($"	{grandRecords.SelectMany(gr => gr.Records).ToList().Count} Records,");
			Console.WriteLine($"	{grandRecords.SelectMany(gr => gr.Records).SelectMany(r => r.ChildRecords).ToList().Count} ChildRecords");
			Console.WriteLine($"for {sw.Elapsed.TotalMilliseconds:0.##} ms.");
			Console.WriteLine();
			Console.WriteLine("The first GrandRecord looks like:");
			Console.WriteLine();
			Console.Write(JsonSerializer.Serialize(grandRecords.Take(1)));
		}


		[TestMethod]
		public void SaveNewGrandRecords()
		{
			var grandRecords = GetThreeNewGrandRecords();


			var sw = new Stopwatch();
			sw.Start();

			var savedGrandRecords  = _repository.SaveGrandRecords(grandRecords);
	
			sw.Stop();

			Assert.IsNotNull(savedGrandRecords);
			Assert.IsTrue(savedGrandRecords.Count == 3);

			Console.WriteLine("SaveGrandRecords saved, read back and joined:");
			Console.WriteLine($"	{savedGrandRecords.Count} GrandRecords,");
			Console.WriteLine($"	{savedGrandRecords.SelectMany(gr => gr.Records).ToList().Count} Records,");
			Console.WriteLine($"	{savedGrandRecords.SelectMany(gr => gr.Records).SelectMany(r => r.ChildRecords).ToList().Count} ChildRecords");
			Console.WriteLine($"for {sw.Elapsed.TotalMilliseconds:0.##} ms.");
			Console.WriteLine();
			Console.WriteLine("The first of saved GrandRecords looks like:");
			Console.WriteLine();
			Console.Write(JsonSerializer.Serialize(savedGrandRecords.Take(1)));
		}
		

		[TestMethod]
		public async Task SaveNewGrandRecordsAsync()
		{
			var grandRecords = GetThreeNewGrandRecords();

			var sw = new Stopwatch();
			sw.Start();

			var savedGrandRecords  = await _repository.SaveGrandRecordsAsync(grandRecords);
	
			sw.Stop();

			Assert.IsNotNull(grandRecords);
			Assert.IsTrue(grandRecords.Count == 3);

			Console.WriteLine("SaveGrandRecords saved, read back and joined:");
			Console.WriteLine($"	{savedGrandRecords.Count} GrandRecords,");
			Console.WriteLine($"	{savedGrandRecords.SelectMany(gr => gr.Records).ToList().Count} Records,");
			Console.WriteLine($"	{savedGrandRecords.SelectMany(gr => gr.Records).SelectMany(r => r.ChildRecords).ToList().Count} ChildRecords");
			Console.WriteLine($"for {sw.Elapsed.TotalMilliseconds:0.##} ms.");
			Console.WriteLine();
			Console.WriteLine("The first of saved GrandRecords looks like:");
			Console.WriteLine();
			Console.Write(JsonSerializer.Serialize(savedGrandRecords.Take(1)));
		}


		[TestMethod]
		public void SaveExistingGrandRecords()
		{
			var grandRecords = GetThreeNewGrandRecords();

			grandRecords = _repository.SaveGrandRecords(grandRecords);

			Assert.IsNotNull(grandRecords);
			Assert.IsTrue(grandRecords.Count == 3);
			Assert.IsTrue(grandRecords.SelectMany(gr => gr.Records).ToList().Count == 26 * 3);
			Assert.IsTrue(grandRecords.SelectMany(gr => gr.Records).SelectMany(r => r.ChildRecords).ToList().Count == 26 * 26 * 3);


			foreach (var grandRecord in grandRecords)
			{
				grandRecord.Name = "(QWERTY)";

				var record = grandRecord.Records.First();
				grandRecord.Records.Clear();
				grandRecord.Records.Add(record);

				var childRecord = record.ChildRecords.First();
				record.ChildRecords.Clear();
				record.ChildRecords.Add(childRecord);
			}


			var sw = new Stopwatch();
			sw.Start();

			var savedGrandRecords  = _repository.SaveGrandRecords(grandRecords);
	
			sw.Stop();

			Assert.IsNotNull(savedGrandRecords);
			Assert.IsTrue(savedGrandRecords.Count == 3);
			Assert.IsNotNull(savedGrandRecords.First().Name == "(QWERTY)");
			Assert.IsTrue(savedGrandRecords.SelectMany(gr => gr.Records).ToList().Count == 3);
			Assert.IsTrue(savedGrandRecords.SelectMany(gr => gr.Records).SelectMany(r => r.ChildRecords).ToList().Count == 3);


			Console.WriteLine($"Method SaveGrandRecords saved existing and changed entities for {sw.Elapsed.TotalMilliseconds:0.##} ms. ");
			Console.WriteLine();
			Console.WriteLine("The saved GrandRecords look like:");
			Console.WriteLine();
			Console.Write(JsonSerializer.Serialize(savedGrandRecords));
		}


		[TestMethod]
		public async Task SaveExistingGrandRecordsAsync()
		{
			var grandRecords = GetThreeNewGrandRecords();

			grandRecords = await _repository.SaveGrandRecordsAsync(grandRecords);

			Assert.IsNotNull(grandRecords);
			Assert.IsTrue(grandRecords.Count == 3);
			Assert.IsTrue(grandRecords.SelectMany(gr => gr.Records).ToList().Count == 26 * 3);
			Assert.IsTrue(grandRecords.SelectMany(gr => gr.Records).SelectMany(r => r.ChildRecords).ToList().Count == 26 * 26 * 3);


			foreach (var grandRecord in grandRecords)
			{
				grandRecord.Name = "(QWERTY)";

				var record = grandRecord.Records.First();
				grandRecord.Records.Clear();
				grandRecord.Records.Add(record);

				var childRecord = record.ChildRecords.First();
				record.ChildRecords.Clear();
				record.ChildRecords.Add(childRecord);
			}


			var sw = new Stopwatch();
			sw.Start();

			var savedGrandRecords  = await _repository.SaveGrandRecordsAsync(grandRecords);
	
			sw.Stop();

			Assert.IsNotNull(savedGrandRecords);
			Assert.IsTrue(savedGrandRecords.Count == 3);
			Assert.IsNotNull(savedGrandRecords.First().Name == "(QWERTY)");
			Assert.IsTrue(savedGrandRecords.SelectMany(gr => gr.Records).ToList().Count == 3);
			Assert.IsTrue(savedGrandRecords.SelectMany(gr => gr.Records).SelectMany(r => r.ChildRecords).ToList().Count == 3);


			Console.WriteLine($"Method SaveGrandRecords saved existing and changed entities for {sw.Elapsed.TotalMilliseconds:0.##} ms. ");
			Console.WriteLine();
			Console.WriteLine("The saved GrandRecords look like:");
			Console.WriteLine();
			Console.Write(JsonSerializer.Serialize(savedGrandRecords));
		}




		private IList<GrandRecord> GetThreeNewGrandRecords()
		{
			// take 3 GrandRecords from DB and replace all the Ids with 0
			// so these GrandRecords (and their descendants) will be recognized as new entries

			var grandRecords = _repository.GetGrandRecords().Take(3).ToList();

			foreach (var grandRecord in grandRecords)
			{
				grandRecord.Id = 0;

				foreach (var record in grandRecord.Records)
				{
					record.Id = 0;
					record.GrandRecordId = 0;

					foreach (var childRecord in record.ChildRecords)
					{
						childRecord.Id = 0;
						childRecord.RecordId = 0;
					}
				}
			}
			return grandRecords;
		}



		
		[TestCleanup]
		public void Dispose()
		{
			_repository.Dispose();
		}

	}
}
