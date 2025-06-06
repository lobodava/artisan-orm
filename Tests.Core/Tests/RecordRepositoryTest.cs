using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Artisan.Orm;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.DAL.Records;
using Tests.DAL.Records.Models;

namespace Tests.Tests
{
	[TestClass]
	public class RecordRepositoryTest
	{
		private Repository _repository;


		private string _connectionString;

		[TestInitialize]
		public void TestInitialize()
		{
			var configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json")
				.Build();

			_connectionString = configuration.GetConnectionString("DatabaseConnection");

			_repository = new Repository(_connectionString);

			_repository.ExecuteCommand(cmd => {
				cmd.UseSql("delete from dbo.Records where Id > 676;");	
			});

		}

		[TestMethod]
		public void GetRecordById()
		{
			Record record = _repository.GetRecordById(1);

			var sw = new Stopwatch();
			sw.Start();
			
			for (var i = 1; i <= 676; i++)
			{
				record = _repository.GetRecordById(i);

				Assert.IsTrue(record.Id == i || record == null);
			}

			Assert.IsNotNull(record);

			sw.Stop();

			Console.WriteLine($"GetRecordById With Mapper reads 676 times for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms, or {(sw.Elapsed.TotalMilliseconds / 676).ToString("0.##")} ms for one read" );
			Console.Write(JsonSerializer.Serialize(record));
		}

		[TestMethod]
		public void GetRecordByIdWithAutoMapping()
		{
			Record record =_repository.GetRecordByIdWithAutoMapping(1);

			var sw = new Stopwatch();
			sw.Start();
			
			for (var i = 1; i <= 676; i++)
			{
				record = _repository.GetRecordByIdWithAutoMapping(i);

				Assert.IsTrue(record.Id == i || record == null);
			}

			Assert.IsNotNull(record);

			sw.Stop();

			Console.WriteLine($"GetRecordById With AutoMapping reads 676 times for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms, or {(sw.Elapsed.TotalMilliseconds / 676).ToString("0.##")} ms for one read" );
			Console.Write(JsonSerializer.Serialize(record));
		}
		
		[TestMethod]
		public void GetRecordByIdOnBaseLevel()
		{
			Record record =_repository.GetRecordByIdOnBaseLevel(1);

			var sw = new Stopwatch();
			sw.Start();
			
			for (var i = 1; i <= 676; i++)
			{
				record = _repository.GetRecordByIdOnBaseLevel(i);

				Assert.IsTrue(record.Id == i || record == null);
			}

			Assert.IsNotNull(record);

			sw.Stop();

			Console.WriteLine($"GetRecordByIdOnBaseLevel reads 676 times for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms, or {(sw.Elapsed.TotalMilliseconds / 676).ToString("0.##")} ms for one read" );
			Console.Write(JsonSerializer.Serialize(record));
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
			Console.Write(JsonSerializer.Serialize(record));
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
			Console.Write(JsonSerializer.Serialize(record));
		}
		

		[TestMethod]
		public async Task GetRecordByIdAsync2()
		{
			Record record = null;

			var sw = new Stopwatch();

			sw.Start();

			for (var i = 1; i <= 676; i++)
			{
				using (var repository2 = new Repository(_connectionString))
				{
					record = await repository2.GetRecordByIdAsync(i);

					Assert.IsTrue(record.Id == i || record == null);
				}
			}
			
			sw.Stop();

			Console.WriteLine($"GetRecordById reads 676 times for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms, or {(sw.Elapsed.TotalMilliseconds / 676).ToString("0.##")} ms for one read" );
			Console.Write(JsonSerializer.Serialize(record));
		}

		[TestMethod]
		public void GetRecords()
		{
			var records  = _repository.GetRecords();

			var sw = new Stopwatch();
			sw.Start();

			records  = _repository.GetRecords();
	
			sw.Stop();

			Assert.IsNotNull(records);
			Assert.IsTrue(records.Count > 1);

			Console.WriteLine($"GetRecords reads {records.Count} records for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");
			Console.Write(JsonSerializer.Serialize(records));
		}

		[TestMethod]
		public void GetRecordsWithAutoMapping()
		{
			var records  = _repository.GetRecordsWithAutoMapping();

			var sw = new Stopwatch();
			sw.Start();

			records  = _repository.GetRecordsWithAutoMapping();
	
			sw.Stop();

			Assert.IsNotNull(records);
			Assert.IsTrue(records.Count > 1);

			Console.WriteLine($"GetRecordsWithAutoMapping reads {records.Count} records for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");
			Console.Write(JsonSerializer.Serialize(records));
		}


		[TestMethod]
		public async Task GetRecordsAsync()
		{
			var records  = await _repository.GetRecordsAsync();

			var sw = new Stopwatch();
			sw.Start();

			records  = await _repository.GetRecordsAsync();
	
			sw.Stop();

			Assert.IsNotNull(records);
			Assert.IsTrue(records.Count > 1);

			Console.WriteLine($"GetRecordsAsync reads {records.Count} records for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");
			Console.Write(JsonSerializer.Serialize(records));
		}


		[TestMethod]
		public async Task GetRecordsWithAutoMappingAsync()
		{
			var records  = await _repository.GetRecordsWithAutoMappingAsync();

			var sw = new Stopwatch();
			sw.Start();

			records  = await _repository.GetRecordsWithAutoMappingAsync();
	
			sw.Stop();

			Assert.IsNotNull(records);
			Assert.IsTrue(records.Count > 1);

			Console.WriteLine($"GetRecordsWithAutoMappingAsync reads {records.Count} records for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");
			Console.Write(JsonSerializer.Serialize(records));
		}


		[TestMethod]
		public void GetRecordsToEnumerable()
		{
			var recordList = _repository.GetRecords();
			var json = JsonSerializer.Serialize(recordList);
			
			var recordEnumerable  = _repository.GetRecordsToEnumerable();
			json = JsonSerializer.Serialize(recordEnumerable);
			

			var sw = new Stopwatch();
			sw.Start();
			
			recordEnumerable  = _repository.GetRecordsToEnumerable();
			json = JsonSerializer.Serialize(recordEnumerable);
			
			sw.Stop();

			Assert.IsNotNull(json);

			Console.WriteLine($"GetRecordsAsEnumerable reads records for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");


			sw.Restart();
			
			recordList = _repository.GetRecords();
			json = JsonSerializer.Serialize(recordList);
			
			sw.Stop();

			Assert.IsNotNull(json);

			Console.WriteLine($"GetRecords reads records for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");
		}

		[TestMethod]
		public void GetRecordsToEnumerableOnBaseLevel()
		{
			var enumRecords  = _repository.GetRecordsToEnumerableOnBaseLevel();

			var records = enumRecords.ToList();

			Assert.IsNotNull(records);
			Assert.IsTrue(records.Count > 1);
		}

		[TestMethod]
		public void GetRecordsAsEnumerable()
		{
			var sw = new Stopwatch();
			sw.Start();

			var enumRecords  = _repository.GetRecordsAsEnumerable();

			sw.Stop();

			var records = enumRecords.ToList();

			Assert.IsNotNull(records);
			Assert.IsTrue(records.Count > 1);

			Console.WriteLine($"GetRecordsAsEnumerable reads {records.Count} records for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");
			Console.Write(JsonSerializer.Serialize(records));
		}


		[TestMethod]
		public void GetRecordsAsEnumerableOnBaseLevel()
		{
			var enumRecords  = _repository.GetRecordsAsEnumerableOnBaseLevel();

			var records = enumRecords.ToList();

			Assert.IsNotNull(records);
			Assert.IsTrue(records.Count > 1);
		}




		[TestMethod]
		public void GetRecordRows()
		{
			var sw = new Stopwatch();
			sw.Start();

			var records  = _repository.GetRecordRows();
	
			sw.Stop();

			Assert.IsNotNull(records);
			Assert.IsTrue(records.Count > 1);

			Console.WriteLine($"GetRecords reads {records.Count} records for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");
			Console.Write(JsonSerializer.Serialize(records));
		}
		

		[TestMethod]
		public void GetRecordRowsWithHandMapping()
		{
			var sw = new Stopwatch();
			sw.Start();

			var records  = _repository.GetRecordRowsWithHandMapping();
	
			sw.Stop();

			Assert.IsNotNull(records);
			Assert.IsTrue(records.Count > 1);

			Console.WriteLine($"GetRecordRowsWithHandMapping reads {records.Count} records for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");
			Console.Write(JsonSerializer.Serialize(records));
		}

		[TestMethod]
		public  async Task  GetRecordRowsWithHandMappingAsync()
		{
			var sw = new Stopwatch();
			sw.Start();

			var records  = await _repository.GetRecordRowsWithHandMappingAsync();
	
			sw.Stop();

			Assert.IsNotNull(records);
			Assert.IsTrue(records.Count > 1);

			Console.WriteLine($"GetRecordRowsWithHandMappingAsync reads {records.Count} records for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");
			Console.Write(JsonSerializer.Serialize(records));
		}


		
		[TestMethod]
		public void GetRecordsOnBaseLevel()
		{
			var records  = _repository.GetRecordsOnBaseLevel();

			Assert.IsNotNull(records);
			Assert.IsTrue(records.Count > 1);
		}

		[TestMethod]
		public void GetRecordRowsOnBaseLevel()
		{
			var recordRows  = _repository.GetRecordRowsOnBaseLevel();

			Assert.IsNotNull(recordRows);
			Assert.IsTrue(recordRows.Count > 1);
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
			Console.Write(JsonSerializer.Serialize(savedRecord));
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
			Console.Write(JsonSerializer.Serialize(savedRecord));
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
			Console.Write(JsonSerializer.Serialize(savedRecords.Take(10)));
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
			Console.Write(JsonSerializer.Serialize(savedRecords.Take(10)));
		}
		
		[TestMethod]
		public void RecordsAsDatatable()
		{
			var records = new List<Record>();

			var tableName = "Recods";
			var columnNames = new string[]
			{
				"Id"                ,
				"GrandRecordId"     ,
				"Name"              ,
				"RecordTypeId"      ,
				"Number"            ,
				"Date"              ,
				"Amount"            ,
				"IsActive"          ,
				"Comment"
			};

			var recordCount = 1000;

			for (int i = 0; i < recordCount; i++)
			{
				records.Add(CreateNewRecord(i.ToString()));
			}

			var recordDatatable = records.AsDataTable(tableName, columnNames);
			
			var sw = new Stopwatch();
			sw.Start();

			recordDatatable = records.AsDataTable(tableName, columnNames);
			
			sw.Stop();

			Assert.IsTrue(recordDatatable.Rows.Count == recordCount );

			Console.WriteLine($"{recordCount} Records converted to DataTable with AsDataTable method for {sw.Elapsed.TotalMilliseconds.ToString("0.####")} ms");
			Console.WriteLine();

			sw.Restart();

			recordDatatable = records.ToDataTable();
			
			sw.Stop();

			Assert.IsTrue(recordDatatable.Rows.Count == recordCount );

			Console.WriteLine($"{recordCount} Records converted to DataTable with ToDataTable method for {sw.Elapsed.TotalMilliseconds.ToString("0.####")} ms");
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
