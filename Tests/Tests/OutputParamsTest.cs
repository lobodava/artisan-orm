using System.Data;
using Artisan.Orm;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Tests
{
	[TestClass]
	public class OutputParamsTest
	{
		private RepositoryBase _repository = null!;

		[TestInitialize]
		public void TestInitialize()
		{
			var appSettings = new AppSettings();
			_repository = new RepositoryBase(appSettings.ConnectionStrings.DatabaseConnection);
		}

		[TestCleanup]
		public void TestCleanup() => _repository.Dispose();


		[TestMethod]
		public void AllOutputParams_RoundTrip()
		{
			_repository.RunCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetOutputParams");

				cmd.AddBitOutputParam			("@Bit");
				cmd.AddTinyIntOutputParam		("@TinyInt");
				cmd.AddSmallIntOutputParam		("@SmallInt");
				cmd.AddIntOutputParam			("@Int");
				cmd.AddBigIntOutputParam		("@BigInt");
				cmd.AddDecimalOutputParam		("@Decimal", precision: 19, scale: 4);
				cmd.AddGuidOutputParam			("@Guid");
				cmd.AddDateTime2OutputParam		("@DateTime2");
				cmd.AddDateTimeOffsetOutputParam("@DateTimeOffset");

				cmd.Connection.Open();
				cmd.ExecuteNonQuery();

				Assert.AreEqual(true,                                         (bool)    cmd.Parameters["@Bit"].Value!,       "@Bit");
				Assert.AreEqual((byte)200,                                    (byte)    cmd.Parameters["@TinyInt"].Value!,   "@TinyInt");
				Assert.AreEqual((short)12345,                                 (short)   cmd.Parameters["@SmallInt"].Value!,  "@SmallInt");
				Assert.AreEqual(1234567890,                                   (int)     cmd.Parameters["@Int"].Value!,       "@Int");
				Assert.AreEqual(1234567890123456789L,                         (long)    cmd.Parameters["@BigInt"].Value!,    "@BigInt");
				Assert.AreEqual(9999.1234m,                                   (decimal) cmd.Parameters["@Decimal"].Value!,   "@Decimal");
				Assert.AreEqual(new Guid("11111111-2222-3333-4444-555555555555"), (Guid)cmd.Parameters["@Guid"].Value!,       "@Guid");

				var dt2 = (DateTime)cmd.Parameters["@DateTime2"].Value!;
				Assert.AreEqual(new DateTime(2026, 4, 29, 12, 34, 56), new DateTime(dt2.Year, dt2.Month, dt2.Day, dt2.Hour, dt2.Minute, dt2.Second));

				var dto = (DateTimeOffset)cmd.Parameters["@DateTimeOffset"].Value!;
				Assert.AreEqual(TimeSpan.FromHours(3), dto.Offset);
				Assert.AreEqual(2026, dto.Year);
			});
		}

		[TestMethod]
		public void VarcharOutputParams_RoundTrip()
		{
			_repository.RunCommand(cmd =>
			{
				cmd.UseSql("set @VarChar = 'hello'; set @NVarChar = N'caf\u00E9';");

				cmd.AddVarcharOutputParam ("@VarChar",  size: 50);
				cmd.AddNVarcharOutputParam("@NVarChar", size: 50);

				cmd.Connection.Open();
				cmd.ExecuteNonQuery();

				Assert.AreEqual("hello",   (string)cmd.Parameters["@VarChar"].Value!);
				Assert.AreEqual("caf\u00E9", (string)cmd.Parameters["@NVarChar"].Value!);
			});
		}
	}
}
