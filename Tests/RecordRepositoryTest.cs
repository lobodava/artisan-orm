using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Artisan.Orm;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Tests.DAL.Records;
using Tests.DAL.Records.Models;

namespace Tests
{
	[TestClass]
	public class RecordRepositoryTest
	{
		private Repository _repository;

		[TestInitialize]
		public void TestInitialize()
		{
			_repository = new Repository();

			_repository.ExecuteCommand(cmd => {
				cmd.UseSql("delete from dbo.Records where Id > 676;");	
			});

		}

		[TestMethod]
		public void GetRecordByIdWithMapper()
		{
			Record record = null;

			var sw = new Stopwatch();
			sw.Start();
			
			for (var i = 1; i <= 676; i++)
			{
				record = _repository.GetRecordByIdWithMapper(i);

				Assert.IsTrue(record.Id == i || record == null);
			}

			Assert.IsNotNull(record);

			sw.Stop();

			Console.WriteLine($"GetRecordById With Mapper reads 676 times for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms, or {(sw.Elapsed.TotalMilliseconds / 676).ToString("0.##")} ms for one read" );
			Console.Write(JsonConvert.SerializeObject(record));
		}

		[TestMethod]
		public void GetRecordByIdWithReflection()
		{
			Record record = null;

			var sw = new Stopwatch();
			sw.Start();
			
			for (var i = 1; i <= 676; i++)
			{
				record = _repository.GetRecordByIdWithReflection(i);

				Assert.IsTrue(record.Id == i || record == null);
			}

			Assert.IsNotNull(record);

			sw.Stop();

			Console.WriteLine($"GetRecordById With Reflection reads 676 times for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms, or {(sw.Elapsed.TotalMilliseconds / 676).ToString("0.##")} ms for one read" );
			Console.Write(JsonConvert.SerializeObject(record));
		}


		[TestMethod]
		public async Task GetRecordByIdAsync()
		{
			Record record = null;

			var sw = new Stopwatch();
			sw.Start();
			
			for (var i = 1; i <= 676; i++)
			{
				record = await _repository.GetRecordByIdAsync(i);

				Assert.IsTrue(record.Id == i || record == null);
			}

			sw.Stop();

			Console.WriteLine($"GetRecordByIdAsync reads 676 times for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms, or {(sw.Elapsed.TotalMilliseconds / 676).ToString("0.##")} ms for one read" );
			Console.Write(JsonConvert.SerializeObject(record));
		}
		
		[TestMethod]
		public void GetRecordByIdWithOpenConnection()
		{
			Record record = null;
			
			_repository.Connection.Open();
			
			var sw = new Stopwatch();
			sw.Start();
		
			for (var i = 1; i <= 500; i++)
			{
				record = _repository.GetRecordById(i);

				//Assert.IsTrue(record.Id == i || record == null);
			}

			sw.Stop();

			_repository.Connection.Close();
		

			Console.WriteLine("Just to compare with dapper-dot-net performance results: https://github.com/StackExchange/dapper-dot-net#performance");
			Console.WriteLine();
			Console.WriteLine($"GetRecordByIdWithOpenConnection reads 500 times for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms, or {(sw.Elapsed.TotalMilliseconds / 500).ToString("0.##")} ms for one read" );
			Console.WriteLine();
			Console.Write(JsonConvert.SerializeObject(record));
		}
		

		[TestMethod]
		public async Task GetRecordByIdAsync2()
		{
			Record record = null;

			var sw = new Stopwatch();

			sw.Start();

			for (var i = 1; i <= 676; i++)
			{
				using (var repository2 = new Repository())
				{
					record = await repository2.GetRecordByIdAsync(i);

					Assert.IsTrue(record.Id == i || record == null);
				}
			}
			
			sw.Stop();

			Console.WriteLine($"GetRecordById reads 676 times for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms, or {(sw.Elapsed.TotalMilliseconds / 676).ToString("0.##")} ms for one read" );
			Console.Write(JsonConvert.SerializeObject(record));
		}

		[TestMethod]
		public void GetRecords()
		{
			var sw = new Stopwatch();
			sw.Start();

			var records  = _repository.GetRecords();
	
			sw.Stop();

			Assert.IsNotNull(records);
			Assert.IsTrue(records.Count > 0);

			Console.WriteLine($"GetRecords reads {records.Count} records for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");
			Console.Write(JsonConvert.SerializeObject(records));
		}

		[TestMethod]
		public void GetRecordsWithReflection()
		{
			var sw = new Stopwatch();
			sw.Start();

			var records  = _repository.GetRecordsWithReflection();
	
			sw.Stop();

			Assert.IsNotNull(records);
			Assert.IsTrue(records.Count > 0);

			Console.WriteLine($"GetRecordsWithReflection reads {records.Count} records for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");
			Console.Write(JsonConvert.SerializeObject(records));
		}


		[TestMethod]
		public async Task GetRecordsAsync()
		{
			var sw = new Stopwatch();
			sw.Start();

			var records  = await _repository.GetRecordsAsync();
	
			sw.Stop();

			Assert.IsNotNull(records);
			Assert.IsTrue(records.Count > 0);

			Console.WriteLine($"GetRecordsAsync reads {records.Count} records for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");
			Console.Write(JsonConvert.SerializeObject(records));
		}


		[TestMethod]
		public async Task GetRecordsWithReflectionAsync()
		{
			var sw = new Stopwatch();
			sw.Start();

			var records  = await _repository.GetRecordsWithReflectionAsync();
	
			sw.Stop();

			Assert.IsNotNull(records);
			Assert.IsTrue(records.Count > 0);

			Console.WriteLine($"GetRecordsWithReflectionAsync reads {records.Count} records for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");
			Console.Write(JsonConvert.SerializeObject(records));
		}


		[TestMethod]
		public void GetRecordsAsEnumerable()
		{
			var recordList = _repository.GetRecords();
			var json = JsonConvert.SerializeObject(recordList);
			
			var recordEnumerable  = _repository.GetRecordsAsEnumerable();
			json = JsonConvert.SerializeObject(recordEnumerable);
			

			var sw = new Stopwatch();
			sw.Start();
			
			recordEnumerable  = _repository.GetRecordsAsEnumerable();
			json = JsonConvert.SerializeObject(recordEnumerable);
			
			sw.Stop();

			Assert.IsNotNull(json);

			Console.WriteLine($"GetRecordsAsEnumerable reads records for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");


			sw.Restart();
			
			recordList = _repository.GetRecords();
			json = JsonConvert.SerializeObject(recordList);
			
			sw.Stop();

			Assert.IsNotNull(json);

			Console.WriteLine($"GetRecords reads records for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");
		}


		[TestMethod]
		public void GetRecordRows()
		{
			var sw = new Stopwatch();
			sw.Start();

			var records  = _repository.GetRecordDataRows();
	
			sw.Stop();

			Assert.IsNotNull(records);
			Assert.IsTrue(records.Count > 0);

			Console.WriteLine($"GetRecords reads {records.Count} records for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");
			Console.Write(JsonConvert.SerializeObject(records));

		}
		



		[TestMethod]
		public void SaveRecord()
		{
			var record = CreateNewRecord();
			
			var sw = new Stopwatch();
			sw.Start();

			var savedRecord  = _repository.SaveRecord(record);
	
			sw.Stop();

			Assert.IsNotNull(savedRecord);
			Assert.IsTrue(savedRecord.Id > 676);

			Console.WriteLine($"SaveRecord executes for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");
			Console.Write(JsonConvert.SerializeObject(savedRecord));
		}

		


		[TestMethod]
		public async Task SaveRecordAsync()
		{
			var record = CreateNewRecord();

			var sw = new Stopwatch();
			sw.Start();

			var savedRecord = await _repository.SaveRecordAsync(record);
	
			sw.Stop();

			Assert.IsNotNull(savedRecord);
			Assert.IsTrue(savedRecord.Id > 676);

			Console.WriteLine($"SaveRecordAsync executes for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");
			Console.Write(JsonConvert.SerializeObject(savedRecord));
		}




		[TestMethod]
		public void SaveRecords()
		{
			var records = new List<Record>();

			for (int i = 0; i < 1000; i++)
			{
				records.Add(CreateNewRecord(i.ToString()));
			}
	
			var sw = new Stopwatch();
			sw.Start();

			var savedRecords = _repository.SaveRecords(records);
	
			sw.Stop();

			Assert.IsNotNull(savedRecords);
			Assert.IsTrue(savedRecords.Count == 1000 );
			Assert.IsTrue(savedRecords.First().Id > 0 );

			Console.WriteLine($"SaveRecords saved and read back 1000 Records for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");
			Console.WriteLine();
			Console.WriteLine("The first 10 of the saved Records look like: ");
			Console.WriteLine();
			Console.Write(JsonConvert.SerializeObject(savedRecords.Take(10)));
		}

		


		[TestMethod]
		public async Task SaveRecordsAsync()
		{
			var records = new List<Record>();

			for (int i = 0; i < 1000; i++)
			{
				records.Add(CreateNewRecord(i.ToString()));
			}

			var sw = new Stopwatch();
			sw.Start();

			var savedRecords = await _repository.SaveRecordsAsync(records);
	
			sw.Stop();

			Assert.IsNotNull(savedRecords);
			Assert.IsTrue(savedRecords.Count == 1000 );
			Assert.IsTrue(savedRecords.First().Id > 0 );

			Console.WriteLine($"SaveRecords saved and read back 1000 Records for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");
			Console.WriteLine();
			Console.WriteLine("The first 10 of the saved Records look like: ");
			Console.WriteLine();
			Console.Write(JsonConvert.SerializeObject(savedRecords.Take(10)));
		}
		
		

		private static Record CreateNewRecord(string suffix = "")
		{
			var record = new Record
			{
				Id				=	0				,
				GrandRecordId	=	1				,
				Name			=	"AAA" + suffix	,
				RecordTypeId	=	1				,
				Number			=	123				,
				Date			=	DateTime.Now	,
				Amount			=	1000			,
				IsActive		=	true			,
				Comment			=	"Lorem ipsum"
			};
			return record;
		}


		
		[TestCleanup]
		public void Dispose()
		{
			_repository.Dispose();
		}

	}
}
