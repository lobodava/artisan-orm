using System.Diagnostics;
using System.Globalization;
using Artisan.Orm;
using Microsoft.Data.SqlClient;
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
			var appSettings = new AppSettings();

			_repository = new RepositoryBase(appSettings.ConnectionStrings.DatabaseConnection);
		}


		[TestMethod]
		public void ReadToValue()
		{
			_repository.Connection.Open();
			

			// bit -> bool, Boolean

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(0 as bit)");
				Assert.AreEqual(false, cmd.ReadTo<bool>());
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(1 as bit)");
				Assert.AreEqual(true, cmd.ReadTo<bool>());
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select null");
				Assert.IsNull(cmd.ReadTo<bool?>());
			});

			// tinyint -> byte, Byte

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(0 as tinyint)");
				Assert.AreEqual(0, cmd.ReadTo<byte>());
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(255 as tinyint)");
				Assert.AreEqual(255, cmd.ReadTo<byte>());
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select null");
				Assert.IsNull(cmd.ReadTo<byte?>());
			});

			// tinyint -> sbyte, SByte

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(-128 as int)");
				Assert.AreEqual(-128, cmd.ReadTo<sbyte>());
			});

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(127 as smallint)");
				Assert.AreEqual(127, cmd.ReadTo<sbyte>());
			});


			// smallint -> short, Int16

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(-32768 as smallint)");
				Assert.AreEqual(-32768, cmd.ReadTo<short>());
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(32767 as smallint)");
				Assert.AreEqual(32767, cmd.ReadTo<short>());
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select null");
				Assert.IsNull(cmd.ReadTo<short?>());
			});


			// smallint -> UInt16, UInt16


			_repository.RunCommand(cmd => {
				cmd.UseSql("select 0");
				Assert.AreEqual(0, cmd.ReadTo<ushort>());
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select 65535 ");
				Assert.AreEqual(65535, cmd.ReadTo<ushort>());
			});


			// int -> int, Int32

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(-2147483648 as int)");
				Assert.AreEqual(-2147483648, cmd.ReadTo<int>());
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(2147483647 as int)");
				Assert.AreEqual(2147483647, cmd.ReadTo<int>());
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(null as int)");
				Assert.IsNull(cmd.ReadTo<int?>());
			});


			// bigint -> long, Int64

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(-9223372036854775808 as bigint)");
				Assert.AreEqual(-9223372036854775808, cmd.ReadTo<long>());
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(9223372036854775807 as bigint)");
				Assert.AreEqual(9223372036854775807, cmd.ReadTo<long>());
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select null");
				Assert.IsNull(cmd.ReadTo<long?>());
			});


			// decimal(9,4) -> decimal, Decimal

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(-99999.9999 as decimal(9,4))");
				Assert.AreEqual(-99999.9999m, cmd.ReadTo<decimal>());
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(99999.9999 as decimal(9,4))");
				Assert.AreEqual(99999.9999m, cmd.ReadTo<decimal>());
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select null");
				Assert.IsNull(cmd.ReadTo<decimal?>());
			});


			// decimal(29,0) -> decimal, Decimal

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(-79228162514264337593543950335 as decimal(29,0))");
				Assert.AreEqual(decimal.MinValue, cmd.ReadTo<decimal>());
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(79228162514264337593543950335 as decimal(29,0))");
				Assert.AreEqual(decimal.MaxValue, cmd.ReadTo<decimal>());
			});


			// decimal(38,4) -> decimal, Decimal
			
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(cast('9999999999999999999999999999999999.9999' as decimal(38,4)) as varchar(39))");
				Assert.AreEqual("9999999999999999999999999999999999.9999", cmd.ReadTo<string>());
			});

			
			// smallmoney -> decimal, Decimal

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(-214748.3648 as smallmoney)");
				Assert.AreEqual(-214748.3648m, cmd.ReadTo<decimal>());
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(214748.3647 as smallmoney)");
				Assert.AreEqual(214748.3647m, cmd.ReadTo<decimal>());
			});


			// money -> decimal, Decimal

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(-922337203685477.5808 as money)");
				Assert.AreEqual(-922337203685477.5808m, cmd.ReadTo<decimal>());
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(922337203685477.5807 as money)");
				Assert.AreEqual(922337203685477.5807m, cmd.ReadTo<decimal>());
			});
			

			// real -> float, Single
			
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast('-3.40E+38' as real)");
				Assert.AreEqual(-3.40E+38f, cmd.ReadTo<float>());
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast('3.40E+38' as real)");
				Assert.AreEqual(3.40E+38f, cmd.ReadTo<float>());
			});


			// float -> double, Double
			
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast('-1.79E+308' as float)");
				Assert.AreEqual(-1.79E+308d, cmd.ReadTo<double>());
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast('1.79E+308' as float)");
				Assert.AreEqual(1.79E+308d, cmd.ReadTo<double>());
			});


			// char(1), nchar(1) -> char, Char

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast('W' as char(1))");
				Assert.AreEqual('W', cmd.ReadTo<char>());
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select N'\u0100'");
				Assert.AreEqual('\u0100', cmd.ReadTo<char>());
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select null");
				Assert.IsNull(cmd.ReadTo<char?>());
			});


			// char(2), nchar(2) -> string, String

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast('WW' as char(2))");
				Assert.AreEqual("WW", cmd.ReadTo<string>());
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select N'\u0100\u0100'");
				Assert.AreEqual("\u0100\u0100", cmd.ReadTo<string>());
			});


			// varchar(3), nvarchar(3) -> string, String

			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast('WWW' as varchar(3))");
				Assert.AreEqual("WWW", cmd.ReadTo<string>());
			});
			_repository.RunCommand(cmd => {
				cmd.UseSql("select cast(N'\u0100\u0100\u0100' as nvarchar(3))");
				Assert.AreEqual("\u0100\u0100\u0100", cmd.ReadTo<string>());
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
				cmd.UseSql("select cast(replicate(N'\u0100', 3000) as nvarchar(max)) + cast(replicate(N'\u0100', 3000) as nvarchar(max)) + cast(replicate(N'\u0100', 4000) as nvarchar(max))");
				Assert.IsTrue(string.Equals(cmd.ReadTo<string>(), new string('\u0100', 10000)));
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
				Assert.AreEqual(new Guid("2ED5EEB0-E9B3-47E7-B14D-87B9E329DBDB"), cmd.ReadTo<Guid>());
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

				Assert.AreEqual(1, user.Id);
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

			Assert.AreEqual(1, user.Id);

			Console.WriteLine($"ReadToUser done {times} times for {sw.Elapsed.TotalMilliseconds:0.##} ms, or {sw.Elapsed.TotalMilliseconds / times:0.######} ms for one Read" );
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

			Console.WriteLine($"ReadToUsers done {times} times for {sw.Elapsed.TotalMilliseconds:0.##} ms, or {sw.Elapsed.TotalMilliseconds / times:0.######} ms for one Read" );
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

				Assert.AreEqual(1, user.Id);
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

			Assert.AreEqual(1, user.Id);

			Console.WriteLine($"ReadToUser done {times} times for {sw.Elapsed.TotalMilliseconds:0.##} ms, or {sw.Elapsed.TotalMilliseconds / times:0.######} ms for one Read" );
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

			Console.WriteLine($"ReadToUsers done {times} times for {sw.Elapsed.TotalMilliseconds:0.##} ms, or {sw.Elapsed.TotalMilliseconds / times:0.######} ms for one Read" );
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

			Console.WriteLine($"ReadDynamicList done {times} times for {sw.Elapsed.TotalMilliseconds:0.##} ms, or {sw.Elapsed.TotalMilliseconds / times:0.######} ms for one Read" );
			Console.WriteLine();

			_repository.RunCommand(cmd => {
				cmd.UseSql( "select Id, [Login], Name, Email, RowVersion from dbo.Users");
				users = cmd.ReadDynamicList();
			});

			Assert.IsTrue(users.Count > 1);

			_repository.Connection.Close();
		}


		[TestMethod]
		public async Task RunCommandAsyncWithCancellationToken1()
		{
			using var cts = new CancellationTokenSource();
			await _repository.Connection.OpenAsync();

			await AssertCommandCanceledAsync(() =>
				_repository.RunCommandAsync(async (cmd, token) => {
					Assert.AreEqual(cts.Token, token);
					cmd.UseSql("WAITFOR DELAY '00:00:05'; select cast(1 as bit);");
					cts.CancelAfter(TimeSpan.FromSeconds(2));
					await cmd.ReadToAsync<bool>(token);
				}, cts.Token), cts.Token);

			Assert.IsTrue(cts.IsCancellationRequested);
		}

		[TestMethod]
		public async Task GetByCommandAsyncWithCancellationToken()
		{
			using var cts = new CancellationTokenSource();
			await _repository.Connection.OpenAsync();

			await AssertCommandCanceledAsync(() =>
				_repository.GetByCommandAsync((cmd, token) => {
					Assert.AreEqual(cts.Token, token);
					cmd.UseSql("WAITFOR DELAY '00:00:05'; select cast(1 as bit);");
					cts.CancelAfter(TimeSpan.FromSeconds(2));
					return cmd.ReadToAsync<bool>(token);
				}, cts.Token), cts.Token);

			Assert.IsTrue(cts.IsCancellationRequested);
		}

		private static async Task AssertCommandCanceledAsync(Func<Task> action, CancellationToken cancellationToken)
		{
			var originalCulture = CultureInfo.CurrentUICulture;
			try
			{
				// SqlClient can report in-flight cancellation as a localized SqlException
				// with no unique error number. Match its cancellation message in a fixed culture.
				CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
				try
				{
					await action();
				}
				catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
				{
					return;
				}
				catch (SqlException ex) when (cancellationToken.IsCancellationRequested &&
					ex.Errors.Cast<SqlError>().All(error => error.Number == 0 && error.State == 0 && error.Class == 11) &&
					ex.Errors.Cast<SqlError>().Any(error => error.Message == "Operation cancelled by user."))
				{
					return;
				}

				Assert.Fail("The SQL command completed without cancellation.");
			}
			finally
			{
				CultureInfo.CurrentUICulture = originalCulture;
			}
		}

		
		[TestCleanup]
		public void TestCleanup()
		{
			_repository.Dispose();
		}

	}
}
