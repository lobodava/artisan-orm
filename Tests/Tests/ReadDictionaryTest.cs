using System;
using System.Configuration;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Artisan.Orm;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.DAL.Users.Models;

namespace Tests.Tests
{
	[TestClass]
	public class ReadDictionaryTest
	{
		private RepositoryBase _repositoryBase;

		[TestInitialize]
		public void TestInitialize()
		{
			var connectionString = ConfigurationManager.ConnectionStrings["DatabaseConnection"].ConnectionString;

			_repositoryBase = new RepositoryBase(connectionString);
		}
		
		[TestMethod]
		public void GetDictionaryOfValues()
		{
			var sw = new Stopwatch();
			sw.Start();

			var dictionary = _repositoryBase.GetByCommand(cmd =>
			{
				cmd.UseSql("select Id, Name from dbo.Roles");
				
				return cmd.ReadToDictionary<byte, string>();

				/* or
				
				return cmd.GetByReader(reader => 
				{
					return reader.ReadToDictionary<byte, string>());
				}

				*/

			});

			sw.Stop();

			Assert.IsNotNull(dictionary);

			Console.WriteLine($"Role name dictionary has been read for {sw.Elapsed.TotalMilliseconds.ToString("0.####")} ms: ");
			Console.WriteLine();
			Console.Write(JsonSerializer.Serialize(dictionary));

		}

		[TestMethod]
		public void GetDictionaryOfObjects()
		{
			var sw = new Stopwatch();
			sw.Start();
			
			var dictionary = _repositoryBase.ReadToDictionary<byte, Role>("select * from dbo.Roles");
				
			//var dictionary = _repositoryBase.GetByCommand(cmd =>
			//{
			//	cmd.UseSql("select * from dbo.Roles");
			//	return cmd.ReadToDictionary<int, Role>();
			//});

			sw.Stop();

			Assert.IsNotNull(dictionary);
			Assert.IsTrue(dictionary.Count > 1);

			Console.WriteLine($"Role object dictionary has been read for {sw.Elapsed.TotalMilliseconds.ToString("0.####")} ms: ");
			Console.WriteLine();
			Console.Write(JsonSerializer.Serialize(dictionary));

		}

		[TestMethod]
		public  async Task  GetDictionaryOfObjectsAsync()
		{
			var sw = new Stopwatch();
			sw.Start();

			var records  = await _repositoryBase.ReadToDictionaryAsync<byte, Role>("select * from dbo.Roles");
	
			sw.Stop();

			Assert.IsNotNull(records);
			Assert.IsTrue(records.Count > 1);

			Console.WriteLine($"GetDictionaryOfObjectsAsync reads {records.Count} roles for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");
			Console.Write(JsonSerializer.Serialize(records));
		}

		[TestMethod]
		public void GetDictionaryWithHandmapping()
		{
			var sw = new Stopwatch();
			sw.Start();

			var dictionary = _repositoryBase.GetByCommand(cmd =>
			{
				cmd.UseSql("select * from dbo.Roles");
				
				return cmd.ReadToDictionary<byte, Role>(dr => new Role
				{
					Id		=	dr.GetByte(0)	,
					Code	=	dr.GetString(1)	,
					Name	=	dr.GetString(2)
				});

			});

			sw.Stop();

			Assert.IsNotNull(dictionary);
			Assert.IsTrue(dictionary.Count > 1);

			Console.WriteLine($"Role object dictionary has been read with Handmapping for {sw.Elapsed.TotalMilliseconds.ToString("0.####")} ms: ");
			Console.WriteLine();
			Console.Write(JsonSerializer.Serialize(dictionary));

		}

		[TestMethod]
		public void GetDictionaryWithAutomapping()
		{
			var dictionary = _repositoryBase.ReadAsDictionary<byte, Role>("select * from dbo.Roles");

			var sw = new Stopwatch();
			sw.Start();
			
			dictionary = _repositoryBase.ReadAsDictionary<byte, Role>("select * from dbo.Roles");
				
			//var dictionary = _repositoryBase.GetByCommand(cmd =>
			//{
			//	cmd.UseSql("select * from dbo.Roles");
			//	return cmd.ReadToDictionary<int, Role>();
			//});

			sw.Stop();

			Assert.IsNotNull(dictionary);
			Assert.IsTrue(dictionary.Count > 1);

			Console.WriteLine($"Role object dictionary has been read with Automapping for {sw.Elapsed.TotalMilliseconds.ToString("0.####")} ms: ");
			Console.WriteLine();
			Console.Write(JsonSerializer.Serialize(dictionary));

		}
	
		[TestCleanup]
		public void Dispose()
		{
			_repositoryBase.Dispose();
		}

	}
}
