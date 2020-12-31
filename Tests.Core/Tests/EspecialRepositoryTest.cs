using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using Artisan.Orm;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Tests.Tests
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

		//[TestMethod]
		//public void GetNumb()
		//{
		//	_repositoryBase.Connection.Open();
			
		//	var sw = new Stopwatch();
		//	sw.Start();
		
		//	for (var i = 1; i <= 100000; i++)
		//	{
		//		_repositoryBase.RunCommand(cmd =>
		//		{
		//			cmd.UseSql("select Numb = cast(0 as int);");

		//			var numb = cmd.GetByReader(reader => 
		//			{
		//				return reader.ReadTo(r => r.GetInt32Nullable(0));
		//			});

		//		});
				
		//	}

		//	sw.Stop();

		//	_repositoryBase.Connection.Close();

		//	Console.WriteLine($"Numb {sw.Elapsed.TotalMilliseconds.ToString("0.####")} ms ");

		//}


		[TestMethod]
		public void GetInt32VsGetValueVsChangeType()
		{
			_repositoryBase.Connection.Open();


			_repositoryBase.RunCommand(cmd => {
				cmd.UseSql( "select 1");

				cmd.ExecuteReader(reader=>
				{
					reader.Read(r =>
					{
						var times = 1000000;


						// GetInt32

						var sw = new Stopwatch();
						sw.Start();

						for (int i = 0; i < times; i++)
						{
							Int32 id = r.GetInt32(0);
						}

						sw.Stop();

						Console.WriteLine($"GetInt32 done {times} times for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms, or {(sw.Elapsed.TotalMilliseconds / times).ToString("0.######")} ms for one GetInt32" );
						Console.WriteLine();

						// GetValue

						sw.Restart();

						for (int i = 0; i < times; i++)
						{
							var id = r.GetValue(0);
						}

						sw.Stop();

						Console.WriteLine($"GetValue done {times} times for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms, or {(sw.Elapsed.TotalMilliseconds / times).ToString("0.######")} ms for one GetValue" );
						Console.WriteLine();


						// (Int32) + GetValue

						sw.Restart();

						for (int i = 0; i < times; i++)
						{
							Int32 id = (Int32)r.GetValue(0);
						}

						sw.Stop();

						Console.WriteLine($"(Int32) + GetValue done {times} times for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms, or {(sw.Elapsed.TotalMilliseconds / times).ToString("0.######")} ms for one (Int32) + GetValue" );
						Console.WriteLine();


						// (Int32) + ChangeType + GetValue

						sw.Restart();

						for (int i = 0; i < times; i++)
						{
							Int32 id = (Int32)Convert.ChangeType(r.GetValue(0), typeof(Int32)) ;
						}

						sw.Stop();

						Console.WriteLine($"(Int32) + ChangeType + GetValue done {times} times for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms, or {(sw.Elapsed.TotalMilliseconds / times).ToString("0.######")} ms for one (Int32) + ChangeType + GetValue" );
						Console.WriteLine();


						// (Int32) + (object) + GetInt32

						sw.Restart();

						for (int i = 0; i < times; i++)
						{
							Int32 id = (Int32)(object)r.GetInt32(0);
						}

						sw.Stop();

						Console.WriteLine($"(Int32) + (object) + GetInt32 done {times} times for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms, or {(sw.Elapsed.TotalMilliseconds / times).ToString("0.######")} ms for one (Int32) + (object) + GetInt32" );
						Console.WriteLine();


						// typeof(Int32).Name

						sw.Restart();
						
						for (int i = 0; i < times; i++)
						{
							var name = typeof(Int32).Name;
						}

						sw.Stop();

						Console.WriteLine($"typeof(Int32).Name done {times} times for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms, or {(sw.Elapsed.TotalMilliseconds / times).ToString("0.######")} ms for one typeof(Int32).Name" );
						Console.WriteLine();


						// type == typeof(Int16)

						sw.Restart();

						var type = typeof(Int32);
						bool f = false;

						for (int i = 0; i < times; i++)
						{
							if ( type == typeof(Int16))
								f = true;
						}

						Assert.IsFalse(f);

						sw.Stop();

						Console.WriteLine($" type == typeof(Int32) done {times} times for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms, or {(sw.Elapsed.TotalMilliseconds / times).ToString("0.######")} ms for one  type == typeof(Int32)" );
						Console.WriteLine();


						// GetValue<Int32>(r);

						sw.Restart();

						for (int i = 0; i < times; i++)
						{
							Int32 id = GetValue<Int32>(r);
						}

						sw.Stop();

						Console.WriteLine($"GetValue<Int32> done {times} times for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms, or {(sw.Elapsed.TotalMilliseconds / times).ToString("0.######")} ms for one GetValue<Int32>" );
						Console.WriteLine();


					});



				});
			});
		}

		private static readonly Dictionary<Type, Func<SqlDataReader, object>>  GetValueFuncs = new Dictionary<Type, Func<SqlDataReader, object>>
		{
			{ typeof(Boolean), (dr) => dr.GetBoolean(0)},
			{ typeof(Byte), (dr) => dr.GetByte(0)},
			{ typeof(Int16), (dr) => dr.GetInt16(0)},
			{ typeof(Int32), (dr) => dr.GetInt32(0)},
			{ typeof(Int64), (dr) => dr.GetInt64(0)},
			{ typeof(String), (dr) => dr.GetString(0)}
		}; 

		private static T GetValue<T>(SqlDataReader dr) 
		{
			Func<SqlDataReader, object>  getValueFunc;

			if (GetValueFuncs.TryGetValue(typeof(T), out getValueFunc))
				return (T)getValueFunc(dr);
			
			return (T)dr.GetValue(0);
		}

		private class SomeClass
		{
			public Int32 Id {get; set;}

			public SByte Rate {get; set;}
			
			public UInt16 Number {get; set;}
		}

		[TestMethod]
		public void GetObjectWithAutomapping()
		{
			_repositoryBase.Connection.Open();

			SomeClass someclass = null;

			_repositoryBase.RunCommand(cmd => {
				cmd.UseSql( "select   cast(1 as tinyint) as Id,   cast(2 as int) as Rate,   cast(3 as bigint) as Number ;");

				cmd.ExecuteReader(reader=>
				{
					someclass = reader.ReadAs<SomeClass>();
				});
			});

			Assert.IsNotNull(someclass);
			Console.WriteLine("SomeClass:");
			Console.WriteLine(JsonConvert.SerializeObject(someclass));
		}

		[TestMethod]
		public void GetObjectWithHandmapping()
		{
			_repositoryBase.Connection.Open();

			SomeClass someclass = null;

			_repositoryBase.RunCommand(cmd => {
				cmd.UseSql( "select   cast(1 as tinyint) as Id,   cast(2 as int) as Rate,   cast(3 as bigint) as Number ;");

				cmd.ExecuteReader(reader=>
				{
					someclass = reader.ReadTo<SomeClass>(r => new SomeClass
					{
						Id		= r.GetValue<Int32>(0),
						Rate	= r.GetValue<SByte>(1),
						Number	= r.GetValue<UInt16>(2),
					});
				});
			});

			Assert.IsNotNull(someclass);
			Console.WriteLine("SomeClass:");
			Console.WriteLine(JsonConvert.SerializeObject(someclass));
		}
		

		//[TestMethod]
		//public void TryCatchPerformanceCost()
		//{
		//	Stopwatch sw = new Stopwatch();
		//	double d = 0;
		//	int times = 10000000;

		//	for (int i = 0; i < times; i++)
		//	{
		//		d = Math.Sin(1);
		//	}


		//	sw.Start();

		//	for (int i = 0; i < times; i++)
		//	{
		//		d = Math.Sin(1);
		//	}

		//	sw.Stop();

		//	Console.WriteLine($"Without try-catch done {times} times for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms, or {(sw.Elapsed.TotalMilliseconds / times).ToString("0.########")} ms for one loop" );
		//	Console.WriteLine();
			
		//	sw.Restart();

		//	for (int i = 0; i < times; i++)
		//	{
		//		try
		//		{
		//			d = Math.Sin(1);
		//		}
		//		catch (Exception ex)
		//		{
		//			Console.WriteLine(ex.ToString());
		//		}
		//	}

		//	sw.Stop();

		//	Console.WriteLine($"With try-catch done {times} times for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms, or {(sw.Elapsed.TotalMilliseconds / times).ToString("0.########")} ms for one loop" );
		//	Console.WriteLine();
		//}
		

	
		[TestCleanup]
		public void Dispose()
		{
			_repositoryBase.Dispose();
		}

	}
}
