using System;
using System.Diagnostics;
using Artisan.Orm;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Tests
{
	[TestClass]
	public class EspecialRepositoryTest
	{
		private RepositoryBase _repositoryBase;

		[TestInitialize]
		public void TestInitialize()
		{
			_repositoryBase = new RepositoryBase();

		}

		[TestMethod]
		public void GetScalar()
		{
			var sw = new Stopwatch();
			sw.Start();

			var name = _repositoryBase.GetByCommand(cmd =>
			{
				cmd.UseSql("select top 1 Name from dbo.Records");
				
				return cmd.ReadTo<string>();
			});

			sw.Stop();

			Assert.IsNotNull(name);

			Console.WriteLine($"Name '{name}' has been read for {sw.Elapsed.TotalMilliseconds.ToString("0.####")} ms. ");

		}

		[TestMethod]
		public void GetAnonymousType()
		{
			var sw = new Stopwatch();
			sw.Start();

			var nameAndNumber = _repositoryBase.GetByCommand(cmd =>
			{
				cmd.UseSql("select top 1 Name, Number from dbo.Records");
				
				return cmd.ReadTo(r => new {Name = r.GetString(0), Number = r.GetInt16Nullable(1)});
			});

			sw.Stop();

			Assert.IsNotNull(nameAndNumber);

			Console.WriteLine($"Name '{nameAndNumber.Name}' and Number '{nameAndNumber.Number}' has been read for {sw.Elapsed.TotalMilliseconds.ToString("0.####")} ms. ");

		}

		[TestMethod]
		public void GetDictionary()
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

			Console.WriteLine($"Role dictionary has been read for {sw.Elapsed.TotalMilliseconds.ToString("0.####")} ms: ");
			Console.WriteLine();
			Console.Write(JsonConvert.SerializeObject(dictionary));

		}




		
		[TestCleanup]
		public void Dispose()
		{
			_repositoryBase.Dispose();
		}

	}
}
