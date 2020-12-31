using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Artisan.Orm;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.DAL.Users.Models;

namespace Tests.Tests
{
	[TestClass]
	public class CommandReadTest
	{
		private RepositoryBase _repository;

		[TestInitialize]
		public void TestInitialize()
		{
			_repository = new RepositoryBase();

		}


		[TestMethod]
		public void ReadToValue()
		{
			_repository.Connection.Open();
			

			// bit -> bool, Boolean

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(0 as bit)");
				Assert.AreEqual(cmd.ReadTo<bool>(), false);
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(1 as bit)");
				Assert.AreEqual(cmd.ReadTo<Boolean>(), true);
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select null");
				Assert.IsNull(cmd.ReadTo<bool?>());
			});

			// tinyint -> byte, Byte

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(0 as tinyint)");
				Assert.AreEqual(cmd.ReadTo<byte>(), 0);
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(255 as tinyint)");
				Assert.AreEqual(cmd.ReadTo<Byte>(), 255);
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select null");
				Assert.IsNull(cmd.ReadTo<byte?>());
			});

			// tinyint -> sbyte, SByte

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(-128 as int)");
				Assert.AreEqual(cmd.ReadTo<sbyte>(), -128);
			});

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(127 as smallint)");
				Assert.AreEqual(cmd.ReadTo<SByte>(), 127);
			});


			// smallint -> short, Int16

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(-32768 as smallint)");
				Assert.AreEqual(cmd.ReadTo<short>(), -32768);
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(32767 as smallint)");
				Assert.AreEqual(cmd.ReadTo<Int16>(), 32767);
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select null");
				Assert.IsNull(cmd.ReadTo<short?>());
			});


			// smallint -> UInt16, UInt16


			_repository.RunCommand(cmd => {
				cmd.UseSql("select 0");
				Assert.AreEqual(cmd.ReadTo<ushort>(), 0);
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select 65535 ");
				Assert.AreEqual(cmd.ReadTo<UInt16>(), 65535);
			});


			// int -> int, Int32

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(-2147483648 as int)");
				Assert.AreEqual(cmd.ReadTo<int>(), -2147483648);
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(2147483647 as int)");
				Assert.AreEqual(cmd.ReadTo<Int32>(), 2147483647);
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(null as int)");
				Assert.IsNull(cmd.ReadTo<int?>());
			});


			// bigint -> long, Int64

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(-9223372036854775808 as bigint)");
				Assert.AreEqual(cmd.ReadTo<long>(), -9223372036854775808);
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(9223372036854775807 as bigint)");
				Assert.AreEqual(cmd.ReadTo<Int64>(), 9223372036854775807);
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select null");
				Assert.IsNull(cmd.ReadTo<long?>());
			});


			// decimal(9,4) -> decimal, Decimal

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(-99999.9999 as decimal(9,4))");
				Assert.AreEqual(cmd.ReadTo<decimal>(), -99999.9999m);
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(99999.9999 as decimal(9,4))");
				Assert.AreEqual(cmd.ReadTo<Decimal>(), 99999.9999m);
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select null");
				Assert.IsNull(cmd.ReadTo<decimal?>());
			});


			// decimal(29,0) -> decimal, Decimal

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(-79228162514264337593543950335 as decimal(29,0))");
				Assert.AreEqual(cmd.ReadTo<decimal>(), decimal.MinValue);
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(79228162514264337593543950335 as decimal(29,0))");
				Assert.AreEqual(cmd.ReadTo<Decimal>(), decimal.MaxValue);
			});


			// decimal(38,4) -> decimal, Decimal
			
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(cast('9999999999999999999999999999999999.9999' as decimal(38,4)) as varchar(39))");
				Assert.AreEqual(cmd.ReadTo<string>(), "9999999999999999999999999999999999.9999");
			});

			
			// smallmoney -> decimal, Decimal

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(-214748.3648 as smallmoney)");
				Assert.AreEqual(cmd.ReadTo<decimal>(), -214748.3648m);
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(214748.3647 as smallmoney)");
				Assert.AreEqual(cmd.ReadTo<Decimal>(), 214748.3647m);
			});


			// money -> decimal, Decimal

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(-922337203685477.5808 as money)");
				Assert.AreEqual(cmd.ReadTo<decimal>(), -922337203685477.5808m);
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(922337203685477.5807 as money)");
				Assert.AreEqual(cmd.ReadTo<Decimal>(), 922337203685477.5807m);
			});
			

			// real -> float, Single
			
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast('-3.40E+38' as real)");
				Assert.AreEqual(cmd.ReadTo<float>(), -3.40E+38f);
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast('3.40E+38' as real)");
				Assert.AreEqual(cmd.ReadTo<Single>(), 3.40E+38f);
			});


			// float -> double, Double
			
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast('-1.79E+308' as float)");
				Assert.AreEqual(cmd.ReadTo<double>(), -1.79E+308d);
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast('1.79E+308' as float)");
				Assert.AreEqual(cmd.ReadTo<Double>(), 1.79E+308d);
			});


			// char(1), nchar(1) -> char, Char

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast('W' as char(1))");
				Assert.AreEqual(cmd.ReadTo<char>(), 'W');
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select N'Ж'");
				Assert.AreEqual(cmd.ReadTo<char>(), 'Ж');
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select null");
				Assert.IsNull(cmd.ReadTo<char?>());
			});


			// char(2), nchar(2) -> string, String

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast('WW' as char(2))");
				Assert.AreEqual(cmd.ReadTo<string>(), "WW");
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select N'ЖЖ'");
				Assert.AreEqual(cmd.ReadTo<string>(), "ЖЖ");
			});


			// varchar(3), nvarchar(3) -> string, String

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast('WWW' as varchar(3))");
				Assert.AreEqual(cmd.ReadTo<string>(), "WWW");
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(N'ЖЖЖ' as nvarchar(3))");
				Assert.AreEqual(cmd.ReadTo<string>(), "ЖЖЖ");
			});

			_repository.RunCommand(cmd => {
				cmd.UseSql("select null");
				Assert.IsNull(cmd.ReadTo<string>());
			});

			// varchar(max), nvarchar(max) -> string, String

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(replicate('W', 5000) as varchar(max)) + cast(replicate('W', 5000) as varchar(max))");
				Assert.AreEqual(cmd.ReadTo<string>(), new string('W', 10000));
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(replicate(N'Ж', 3000) as nvarchar(max)) + cast(replicate(N'Ж', 3000) as nvarchar(max)) + cast(replicate(N'Ж', 4000) as nvarchar(max))");
				Assert.IsTrue(String.Equals(cmd.ReadTo<string>(), new string('Ж', 10000)));
			});


			// date -> DateTime

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast('0001-01-01' as date)");
				Assert.AreEqual(cmd.ReadTo<DateTime>(), new DateTime(1,1,1));
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast('9999-12-31' as date)");
				Assert.AreEqual(cmd.ReadTo<DateTime>(), new DateTime(9999,12,31));
			});

			_repository.RunCommand(cmd => {
				cmd.UseSql("select null");
				Assert.IsNull(cmd.ReadTo<DateTime?>());
			});

			// datetime -> DateTime

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast('1753-01-01T15:45:30' as datetime)");
				Assert.AreEqual(cmd.ReadTo<DateTime>(), new DateTime(1753,1,1, 15,45,30));
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast('9999-12-30T15:45:30' as datetime)");
				Assert.AreEqual(cmd.ReadTo<DateTime>(), new DateTime(9999,12,30, 15,45,30));
			});

			// smalldatetime -> DateTime

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast('1900-01-01T15:45:00' as datetime)");
				Assert.AreEqual(cmd.ReadTo<DateTime>(), new DateTime(1900,1,1, 15,45,00));
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast('2079-06-05T15:45:00' as datetime)");
				Assert.AreEqual(cmd.ReadTo<DateTime>(), new DateTime(2079,06,05, 15,45,00));
			});

			// datetime2(0) -> DateTime

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast('0001-01-01T15:45:32' as datetime2(0))");
				Assert.AreEqual(cmd.ReadTo<DateTime>(), new DateTime(1,1,1, 15,45,32));
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast('9999-12-30T15:45:32' as datetime2(0))");
				Assert.AreEqual(cmd.ReadTo<DateTime>(), new DateTime(9999,12,30, 15,45,32));
			});

			// datetimeoffset -> DateTimeOffset

			_repository.RunCommand(cmd =>
			{
				cmd.UseSql("select cast('0001-01-01 15:45:32. 123 -03:00' as datetimeoffset(3))");
				Assert.AreEqual(cmd.ReadTo<DateTimeOffset>(), new DateTimeOffset(1, 1, 1, 15, 45, 32, 123, new TimeSpan(-3, 0, 0)));
			});
			_repository.RunCommand(cmd =>
			{
				cmd.UseSql("select cast('9999-12-30 15:45:32. 123 -03:00' as datetimeoffset(3))");
				Assert.AreEqual(cmd.ReadTo<DateTimeOffset>(), new DateTimeOffset(9999, 12, 30, 15, 45, 32, 123, new TimeSpan(-3, 0, 0)));
			});
			_repository.RunCommand(cmd =>
			{
				cmd.UseSql("select null");
				Assert.IsNull(cmd.ReadTo<DateTimeOffset?>());
			});

			// time -> Timespan

			_repository.RunCommand(cmd =>
			{
				cmd.UseSql("select cast('00:00:00. 123' as time(3))");
				Assert.AreEqual(cmd.ReadTo<TimeSpan>(), new TimeSpan(0, 0, 0, 0, 123));
			});
			_repository.RunCommand(cmd =>
			{
				cmd.UseSql("select cast('23:59:59. 123' as time(3))");
				Assert.AreEqual(cmd.ReadTo<TimeSpan>(), new TimeSpan(0, 23, 59, 59, 123));
			});
			_repository.RunCommand(cmd =>
			{
				cmd.UseSql("select null");
				Assert.IsNull(cmd.ReadTo<TimeSpan?>());
			});

			
			// uniqueidentifier -> GUID

			_repository.RunCommand(cmd =>
			{
				cmd.UseSql("select cast('2ED5EEB0-E9B3-47E7-B14D-87B9E329DBDB' as uniqueidentifier)");
				Assert.AreEqual(cmd.ReadTo<Guid>(), new Guid("2ED5EEB0-E9B3-47E7-B14D-87B9E329DBDB"));
			});
			_repository.RunCommand(cmd =>
			{
				cmd.UseSql("select null");
				Assert.IsNull(cmd.ReadTo<Guid?>());
			});

			_repository.Connection.Close();
		}

		[TestMethod]
		public void ReadToArray()
		{
			_repository.Connection.Open();

			// bit -> bool, Boolean

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(0 as bit) " +
							"union all select cast(1 as bit) " +
							"union all select null");
				CollectionAssert.AreEqual(cmd.ReadToArray<bool?>(), new bool?[] { false, true, null });
			});

			// tinyint -> byte, Byte

			_repository.RunCommand(cmd => {
				cmd.UseSql( "select cast(0 as tinyint) union all " +
							"select cast(255 as tinyint) union all " +
							"select null");
				CollectionAssert.AreEqual(cmd.ReadToArray<byte?>(), new byte?[] { 0, 255, null });
			});

			// smallint -> short, Int16

			_repository.RunCommand(cmd => {
				cmd.UseSql( "select cast(-32768 as smallint) union all " +
							"select cast(32767 as smallint) union all " +
							"select null");
				CollectionAssert.AreEqual(cmd.ReadToArray<short?>(), new short?[] { -32768, 32767, null });
			});

			// int -> int, Int32

			_repository.RunCommand(cmd => {
				cmd.UseSql( "select cast(-2147483648 as int) union all " +
							"select cast(2147483647 as int) union all " +
							"select cast(null as int)");
				CollectionAssert.AreEqual(cmd.ReadToArray<int?>(), new int?[] { -2147483648, 2147483647, null });
			});

			// bigint -> long, Int64

			_repository.RunCommand(cmd => {
				cmd.UseSql( "select cast(-9223372036854775808 as bigint) union all " +
							"select cast(9223372036854775807 as bigint) union all " +
							"select null");
				CollectionAssert.AreEqual(cmd.ReadToArray<long?>(), new long?[] { -9223372036854775808, 9223372036854775807, null });
			});


			// decimal(9,4) -> decimal, Decimal

			_repository.RunCommand(cmd => {
				cmd.UseSql( "select cast(-99999.9999 as decimal(9,4)) union all " +
							"select cast(99999.9999 as decimal(9,4)) union all " +
							"select null");
				CollectionAssert.AreEqual(cmd.ReadToArray<decimal?>(), new decimal?[] { -99999.9999m, 99999.9999m, null });
			});
			
			_repository.Connection.Close();
		}
		


		[TestMethod]
		public void ReadToUser()
		{
			User user = null; 

			_repository.Connection.Open();
			
			_repository.RunCommand(cmd => {
				cmd.UseSql( "select Id, [Login], Name, Email, RowVersion from dbo.Users where Id = 1");

				user = cmd.ReadTo<User>();

				Assert.AreEqual(user.Id, 1);
			});
			

			var times = 1000;

			var sw = new Stopwatch();
			sw.Start();

			for (int i = 0; i < times; i++)
			{
				_repository.RunCommand(cmd => {
					cmd.UseSql( "select Id, [Login], Name, Email, RowVersion from dbo.Users where Id = 1");
					user = cmd.ReadTo<User>();
				});
			}

			sw.Stop();

			Assert.AreEqual(user.Id, 1);

			Console.WriteLine($"ReadToUser done {times} times for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms, or {(sw.Elapsed.TotalMilliseconds / times).ToString("0.######")} ms for one Read" );
			Console.WriteLine();

			_repository.Connection.Close();
		}


		[TestMethod]
		public void ReadToUsers()
		{
			IList<User> users = null; 

			_repository.Connection.Open();
			
			_repository.RunCommand(cmd => {
				cmd.UseSql( "select Id, [Login], Name, Email, RowVersion from dbo.Users");

				users = cmd.ReadToList<User>();

				Assert.IsTrue(users.Count > 1);
			});
			

			var times = 1000;

			var sw = new Stopwatch();
			sw.Start();

			for (int i = 0; i < times; i++)
			{
				_repository.RunCommand(cmd => {
					cmd.UseSql( "select Id, [Login], Name, Email, RowVersion from dbo.Users");
					users = cmd.ReadToList<User>();
				});
			}

			sw.Stop();

			Assert.IsTrue(users.Count > 1);

			Console.WriteLine($"ReadToUsers done {times} times for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms, or {(sw.Elapsed.TotalMilliseconds / times).ToString("0.######")} ms for one Read" );
			Console.WriteLine();

			_repository.Connection.Close();
		}


		[TestMethod]
		public void ReadAsUser()
		{
			User user = null; 

			_repository.Connection.Open();
			
			_repository.RunCommand(cmd => {
				cmd.UseSql( "select Id, [Login], Name, Email from dbo.Users where Id = 1");

				user = cmd.ReadAs<User>();

				Assert.AreEqual(user.Id, 1);
			});
			

			var times = 1000;

			var sw = new Stopwatch();
			sw.Start();

			for (int i = 0; i < times; i++)
			{
				_repository.RunCommand(cmd => {
					cmd.UseSql( "select Id, [Login], Name, Email from dbo.Users where Id = 1");
					user = cmd.ReadAs<User>();
				});
			}

			sw.Stop();

			Assert.AreEqual(user.Id, 1);

			Console.WriteLine($"ReadToUser done {times} times for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms, or {(sw.Elapsed.TotalMilliseconds / times).ToString("0.######")} ms for one Read" );
			Console.WriteLine();

			_repository.Connection.Close();
		}


		[TestMethod]
		public void ReadAsUsers()
		{
			IList<User> users = null; 

			_repository.Connection.Open();
			
			_repository.RunCommand(cmd => {
				cmd.UseSql( "select Id, [Login], Name, Email from dbo.Users");

				users = cmd.ReadAsList<User>();

				Assert.IsTrue(users.Count > 1);
			});
			

			var times = 1000;

			var sw = new Stopwatch();
			sw.Start();

			for (int i = 0; i < times; i++)
			{
				_repository.RunCommand(cmd => {
					cmd.UseSql( "select Id, [Login], Name, Email from dbo.Users");
					users = cmd.ReadAsList<User>();
				});
			}

			sw.Stop();

			Assert.IsTrue(users.Count > 1);

			Console.WriteLine($"ReadToUsers done {times} times for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms, or {(sw.Elapsed.TotalMilliseconds / times).ToString("0.######")} ms for one Read" );
			Console.WriteLine();

			_repository.Connection.Close();
		}
		

		[TestMethod]
		public void ReadDynamicUsers()
		{
			dynamic user = null;
			IList<dynamic> users = null;

			_repository.Connection.Open();
			
			_repository.RunCommand(cmd => {
				cmd.UseSql( "select Id, [Login], Name, Email, RowVersion from dbo.Users where Id = 1");

				user = cmd.ReadDynamic();

				Assert.AreEqual(user.Id, 1);
			});
			

			var times = 1000;

			var sw = new Stopwatch();
			sw.Start();

			for (int i = 0; i < times; i++)
			{
				_repository.RunCommand(cmd => {
					cmd.UseSql( "select Id, [Login], Name, Email, RowVersion from dbo.Users");
					users = cmd.ReadDynamicList();
				});
			}

			sw.Stop();

			Assert.AreEqual(user.Id, 1);

			Console.WriteLine($"ReadDynamicList done {times} times for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms, or {(sw.Elapsed.TotalMilliseconds / times).ToString("0.######")} ms for one Read" );
			Console.WriteLine();

			_repository.RunCommand(cmd => {
				cmd.UseSql( "select Id, [Login], Name, Email, RowVersion from dbo.Users");
				users = cmd.ReadDynamicList();
			});

			Assert.IsTrue(users.Count > 1);

			_repository.Connection.Close();
		}
		
		[TestCleanup]
		public void Dispose()
		{
			_repository.Dispose();
		}

	}
}
