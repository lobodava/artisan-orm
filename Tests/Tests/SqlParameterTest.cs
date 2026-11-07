using System.Data;
using Artisan.Orm;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Tests
{
	[TestClass]
	public class SqlParameterTest
	{
		private RepositoryBase _repository = null!;

		[TestInitialize]
		public void TestInitialize()
		{
			var appSettings = new AppSettings();

			_repository = new RepositoryBase(appSettings.ConnectionStrings.DatabaseConnection);
		}


		[TestMethod]
		public void GetWholeNumberParams()
		{

			bool		bit					=	true	;
			bool?		bitNull				=	null	;
			bool?		bitNullable			=	false	;
					
			byte		tinyInt				=	byte.MaxValue		;
			byte?		tinyIntNull			=	null				;
			byte?		tinyIntNullable		=	byte.MinValue		;

			short		smallInt			=	short.MaxValue		;
			short?		smallIntNull		=	null				;
			short?		smallIntNullable	=	short.MinValue		;

			int			int_				=	int.MaxValue		;
			int?		intNull				=	null				;
			int?		intNullable			=	int.MinValue		;

			long		bigInt				=	long.MaxValue		;
			long?		bigIntNull			=	null				;
			long?		bigIntNullable		=	long.MinValue		;


			_repository.RunCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetWholeNumberParams");

				cmd.AddBitParam			("@Bit"					,			bit					);
				cmd.AddBitParam			("@BitNull"				,			bitNull				);
				cmd.AddBitParam			("@BitNullable"			,			bitNullable			);

				cmd.AddTinyIntParam		("@TinyInt"				,			tinyInt				);
				cmd.AddTinyIntParam		("@TinyIntNull"			,			tinyIntNull			);
				cmd.AddTinyIntParam		("@TinyIntNullable"		,			tinyIntNullable		);

				cmd.AddSmallIntParam	("@SmallInt"			,			smallInt			);
				cmd.AddSmallIntParam	("@SmallIntNull"		,			smallIntNull		);
				cmd.AddSmallIntParam	("@SmallIntNullable"	,			smallIntNullable	);

				cmd.AddIntParam			("@Int"					,			int_				);
				cmd.AddIntParam			("@IntNull"				,			intNull				);
				cmd.AddIntParam			("@IntNullable"			,			intNullable			);

				cmd.AddBigIntParam		("@BigInt"				,			bigInt				);
				cmd.AddBigIntParam		("@BigIntNull"			,			bigIntNull			);
				cmd.AddBigIntParam		("@BigIntNullable"		,			bigIntNullable		);


				cmd.ExecuteReader(reader =>
				{
					var i = 0;

					reader.Read(r =>
					{
						Assert.AreEqual(bit, r.GetBoolean(i++), "bit");
						Assert.AreEqual(bitNull, r.GetBooleanNullable(i++), "bitNull");	
						Assert.AreEqual(bitNullable, r.GetBooleanNullable(i++), "bitNullable");	

						Assert.AreEqual(tinyInt, r.GetByte(i++), "tinyInt");	
						Assert.AreEqual(tinyIntNull, r.GetByteNullable(i++), "tinyIntNull");	
						Assert.AreEqual(tinyIntNullable, r.GetByteNullable(i++), "tinyIntNullable");	

						Assert.AreEqual(smallInt, r.GetInt16(i++), "smallInt");	
						Assert.AreEqual(smallIntNull, r.GetInt16Nullable(i++), "smallIntNull");	
						Assert.AreEqual(smallIntNullable, r.GetInt16Nullable(i++), "smallIntNullable");

						Assert.AreEqual(int_, r.GetInt32(i++), "int_");	
						Assert.AreEqual(intNull, r.GetInt32Nullable(i++), "intNull");	
						Assert.AreEqual(intNullable, r.GetInt32Nullable(i++), "intNullable");	

						Assert.AreEqual(bigInt, r.GetInt64(i++), "bigInt");	
						Assert.AreEqual(bigIntNull, r.GetInt64Nullable(i++), "bigIntNull");	
						Assert.AreEqual(bigIntNullable, r.GetInt64Nullable(i++), "bigIntNullable");

					});

				});

			});

		}


		[TestMethod]
		public void GetFractionalNumberParams()
		{
			decimal		decimal_			=	decimal.MaxValue		; //  79228162514264337593543950335
			decimal?	decimalNull			=	null					;
			decimal?	decimalNullable		=	decimal.MinValue		; // -79228162514264337593543950335

			decimal		smallMoney			=	214748.3647m			;
			decimal?	smallMoneyNull		=	null					;
			decimal?	smallMoneyNullable	=	-214748.3648m			;

			decimal		money				=	-922337203685477.5808m	;
			decimal?	moneyNull			=	null					;
			decimal?	moneyNullable		=	922337203685477.5807m	;

			float		real				=	3.40E+38f				;
			float?		realNull			=	null					;
			float?		realNullable		=	-3.40E+38f				;

			double		float_				=	-1.79E+308d				;
			double?		floatNull			=	null					;
			double?		floatNullable		=	1.79E+308d				;


			_repository.RunCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetFractionalNumberParams");

				cmd.AddDecimalParam		("@Decimal"				,	29, 0,	decimal_			);
				cmd.AddDecimalParam		("@DecimalNull"			,	29, 0,	decimalNull			);
				cmd.AddDecimalParam		("@DecimalNullable"		,	29, 0,	decimalNullable		);

				cmd.AddSmallMoneyParam	("@SmallMoney"			,			smallMoney			);
				cmd.AddSmallMoneyParam	("@SmallMoneyNull"		,			smallMoneyNull		);
				cmd.AddSmallMoneyParam	("@SmallMoneyNullable"	,			smallMoneyNullable	);

				cmd.AddMoneyParam		("@Money"				,			money				);
				cmd.AddMoneyParam		("@MoneyNull"			,			moneyNull			);
				cmd.AddMoneyParam		("@MoneyNullable"		,			moneyNullable		);

				cmd.AddRealParam		("@Real"				,			real				);
				cmd.AddRealParam		("@RealNull"			,			realNull			);
				cmd.AddRealParam		("@RealNullable"		,			realNullable		);

				cmd.AddFloatParam		("@Float"				,			float_				);
				cmd.AddFloatParam		("@FloatNull"			,			floatNull			);
				cmd.AddFloatParam		("@FloatNullable"		,			floatNullable		);


				cmd.ExecuteReader(reader =>
				{
					var i = 0;

					reader.Read(r =>
					{
						Assert.AreEqual(decimal_, r.GetDecimal(i++), "decimal_");	
						Assert.AreEqual(decimalNull, r.GetDecimalNullable(i++), "decimalNull");	
						Assert.AreEqual(decimalNullable, r.GetDecimalNullable(i++), "decimalNullable");	

						Assert.AreEqual(smallMoney, r.GetDecimal(i++), "smallMoney");	
						Assert.AreEqual(smallMoneyNull, r.GetDecimalNullable(i++), "smallMoneyNull");	
						Assert.AreEqual(smallMoneyNullable, r.GetDecimalNullable(i++), "smallMoneyNullable");	

						Assert.AreEqual(money, r.GetDecimal(i++), "money");	
						Assert.AreEqual(moneyNull, r.GetDecimalNullable(i++), "moneyNull");	
						Assert.AreEqual(moneyNullable, r.GetDecimalNullable(i++), "moneyNullable");	

						Assert.AreEqual(real, r.GetFloat(i++), "real");	
						Assert.AreEqual(realNull, r.GetFloatNullable(i++), "realNull");	
						Assert.AreEqual(realNullable, r.GetFloatNullable(i++), "realNullable");	

						Assert.AreEqual(float_, r.GetDouble(i++), "float_");	
						Assert.AreEqual(floatNull, r.GetDoubleNullable(i++), "floatNull");	
						Assert.AreEqual(floatNullable, r.GetDoubleNullable(i++), "floatNullable");

					});

				});

			});

		}

		[TestMethod]
		public void GetStringParams()
		{
			char		char_			=	'W';
			char?		charNull		=	null;
			char?		charNullable	=	'R';

			char		nchar			=	'\u0101';
			char?		ncharNull		=	null;
			char?		ncharNullable	=	'\u0100';

			string		varchar			=	new('W', 8000);
			string		varcharNull		=	null;

			string		nvarchar			=	new('\u0100', 4000);
			string		nvarcharNull		=	null;

			string		varcharmax			=	new('W', 10000);
			string		varcharmaxNull		=	null;

			string		nvarcharmax			=	new('\u0100', 10000);
			string		nvarcharmaxNull		=	null;

			
			_repository.RunCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetStringParams");


				cmd.AddCharParam		("@Char"			,		char_			);			
				cmd.AddCharParam		("@CharNull"		,		charNull		);		
				cmd.AddCharParam		("@CharNullable"	,		charNullable	);	
				
				cmd.AddNCharParam		("@NChar"			,		nchar			);			
				cmd.AddNCharParam		("@NCharNull"		,		ncharNull		);		
				cmd.AddNCharParam		("@NCharNullable"	,		ncharNullable	);	
				
				cmd.AddVarcharParam		("@Varchar"			, 8000,	varchar			);		
				cmd.AddVarcharParam		("@VarcharNull"		, 8000,	varcharNull		);	
				
				cmd.AddNVarcharParam	("@NVarchar"		, 4000,	nvarchar		);		
				cmd.AddNVarcharParam	("@NVarcharNull"	, 4000,	nvarcharNull	);
				
				cmd.AddVarcharMaxParam	("@VarcharMax"		,		varcharmax		);		
				cmd.AddVarcharMaxParam	("@VarcharMaxNull"	,		varcharmaxNull	);	
				
				cmd.AddNVarcharMaxParam	("@NVarcharMax"		,		nvarcharmax		);	
				cmd.AddNVarcharMaxParam	("@NVarcharMaxNull"	,		nvarcharmaxNull	);


				cmd.ExecuteReader(reader =>
				{
					var i = 0;

					reader.Read(r =>
					{
						Assert.AreEqual(char_, r.GetCharacter(i++), "char_");	
						Assert.AreEqual(charNull, r.GetCharacterNullable(i++), "charNull");	
						Assert.AreEqual(charNullable, r.GetCharacterNullable(i++), "charNullable");	

						Assert.AreEqual(nchar, r.GetCharacter(i++), "nchar");	
						Assert.AreEqual(ncharNull, r.GetCharacterNullable(i++), "ncharNull");	
						Assert.AreEqual(ncharNullable, r.GetCharacterNullable(i++), "ncharNullable");	

						Assert.AreEqual(varchar, r.GetString(i++), "varchar");	
						Assert.AreEqual(varcharNull, r.GetStringNullable(i++), "varcharNull");	

						Assert.AreEqual(nvarchar, r.GetString(i++), "nvarchar");	
						Assert.AreEqual(nvarcharNull, r.GetStringNullable(i++), "nvarcharNull");	

						Assert.AreEqual(varcharmax, r.GetString(i++), "varcharmax");	
						Assert.AreEqual(varcharmaxNull, r.GetStringNullable(i++), "varcharmaxNull");

						Assert.AreEqual(nvarcharmax, r.GetString(i++), "nvarcharmax");	
						Assert.AreEqual(nvarcharmaxNull, r.GetStringNullable(i++), "nvarcharmaxNull");

					});

				});

			});

		}

		[TestMethod]
		public void GetDateTimeParams()
		{
			var now = DateTimeOffset.Now;
			var Y = now.Year;
			var M = now.Month;
			var D = now.Day;
			var h = now.Hour;
			var m = now.Minute;
			var s = now.Second;
			var o = now.Offset;


			DateTime		date					=	new(Y,M,D)					;
			DateTime?		dateNull				=	null						;
			DateTime?		dateNullable			=	new DateTime(Y,M,D)			;

			TimeSpan		time					=	new(h,m,s)					;
			TimeSpan?		timeTimeNull			=	null						;
			TimeSpan?		timeTimeNullable		=	new TimeSpan(h,m,s)			;

			DateTime		smallDateTime			=	new(Y,M,D,h,m,0)			;
			DateTime?		smallDateTimeNull		=	null						;
			DateTime?		smallDateTimeNullable	=	new DateTime(Y,M,D,h,m,0)	;

			DateTime		dateTime				=	new(Y,M,D,h,m,s)			;
			DateTime?		dateTimeNull			=	null						;
			DateTime?		dateTimeNullable		=	new DateTime(Y,M,D,h,m,s)	;

			DateTime		dateTime2				=	new(Y,M,D,h,m,s)			;
			DateTime?		dateTime2Null			=	null						;
			DateTime?		dateTime2Nullable		=	new DateTime(Y,M,D,h,m,s)	;

			DateTimeOffset	dateTimeOffset			=	new(Y,M,D,h,m,s,o)					;
			DateTimeOffset?	dateTimeOffsetNull		=	null								;
			DateTimeOffset?	dateTimeOffsetNullable	=	new DateTimeOffset(Y,M,D,h,m,s,o)	;


			_repository.RunCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetDateTimeParams");

				cmd.AddDateParam			("@Date"					,	date					);
				cmd.AddDateParam			("@DateNull"				,	dateNull				);
				cmd.AddDateParam			("@DateNullable"			,	dateNullable			);

				cmd.AddTimeParam			("@Time"					,	time					);
				cmd.AddTimeParam			("@TimeNull"				,	timeTimeNull			);
				cmd.AddTimeParam			("@TimeNullable"			,	timeTimeNullable		);

				cmd.AddSmallDateTimeParam	("@SmallDateTime"			,	smallDateTime			);
				cmd.AddSmallDateTimeParam	("@SmallDateTimeNull"		,	smallDateTimeNull		);
				cmd.AddSmallDateTimeParam	("@SmallDateTimeNullable"	,	smallDateTimeNullable	);

				cmd.AddDateTimeParam		("@DateTime"				,	dateTime				);
				cmd.AddDateTimeParam		("@DateTimeNull"			,	dateTimeNull			);
				cmd.AddDateTimeParam		("@DateTimeNullable"		,	dateTimeNullable		);

				cmd.AddDateTime2Param		("@DateTime2"				,	dateTime2				);
				cmd.AddDateTime2Param		("@DateTime2Null"			,	dateTime2Null			);
				cmd.AddDateTime2Param		("@DateTime2Nullable"		,	dateTime2Nullable		);

				cmd.AddDateTimeOffsetParam	("@DateTimeOffset"			,	dateTimeOffset			);
				cmd.AddDateTimeOffsetParam	("@DateTimeOffsetNull"		,	dateTimeOffsetNull		);
				cmd.AddDateTimeOffsetParam	("@DateTimeOffsetNullable"	,	dateTimeOffsetNullable	);
				

				cmd.ExecuteReader(reader =>
				{
					var i = 0;

					reader.Read(r =>
					{
						Assert.AreEqual(date, r.GetDateTime(i++), "date");
						Assert.AreEqual(dateNull, r.GetDateTimeNullable(i++), "dateNull");
						Assert.AreEqual(dateNullable, r.GetDateTimeNullable(i++), "dateNullable");

						Assert.AreEqual(time, r.GetTimeSpan(i++), "time");
						Assert.AreEqual(timeTimeNull, r.GetTimeSpanNullable(i++), "timeTimeNull");
						Assert.AreEqual(timeTimeNullable, r.GetTimeSpanNullable(i++), "timeTimeNullable");

						Assert.AreEqual(smallDateTime, r.GetDateTime(i++), "smallDateTime");
						Assert.AreEqual(smallDateTimeNull, r.GetDateTimeNullable(i++), "smallDateTimeNull");
						Assert.AreEqual(smallDateTimeNullable, r.GetDateTimeNullable(i++), "smallDateTimeNullable");

						Assert.AreEqual(dateTime, r.GetDateTime(i++), "dateTime");
						Assert.AreEqual(dateTimeNull, r.GetDateTimeNullable(i++), "dateTimeNull");
						Assert.AreEqual(dateTimeNullable.Value, r.GetDateTimeNullable(i++), "dateTimeNullable");

						Assert.AreEqual(dateTime2, r.GetDateTime(i++), "dateTime2");
						Assert.AreEqual(dateTime2Null, r.GetDateTimeNullable(i++), "dateTime2Null");
						Assert.AreEqual(dateTime2Nullable, r.GetDateTimeNullable(i++), "dateTime2Nullable");

						Assert.AreEqual(dateTimeOffset, r.GetDateTimeOffset(i++), "dateTimeOffset");	
						Assert.AreEqual(dateTimeOffsetNull, r.GetDateTimeOffsetNullable(i++), "dateTimeOffsetNull");	
						Assert.AreEqual(dateTimeOffsetNullable, r.GetDateTimeOffsetNullable(i++), "dateTimeOffsetNullable");

					});

				});

			});

		}


		[TestMethod]
		public void GetGuidAndTimestampParams()
		{

			Guid		guid					=	Guid.NewGuid()	;
			Guid?		guidNull				=	null	;
			Guid?		guidNullable			=	Guid.NewGuid()	;

			byte[]		rowVersion				=	new byte[8] {1,2,3,4,5,6,7,8};
			byte[]		rowVersionNull			=	null	;

			long		rowVersionInt64			=	2019864432875667456;
			long?		rowVersionInt64Null		=	null;
			long?		rowVersionInt64Nullable	=	2452209997103235072;	

			string		rowVersionBase64		=	"AAAAAAAACBM=";
			string		rowVersionBase64Null	=	null	;

				
			_repository.RunCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetGuidAndRowVersionParams");

				cmd.AddGuidParam						("@Guid"					,	guid					);
				cmd.AddGuidParam						("@GuidNull"				,	guidNull				);
				cmd.AddGuidParam						("@GuidNullable"			,	guidNullable			);

				cmd.AddRowVersionParam					("@RowVersion"				,	rowVersion				);
				cmd.AddRowVersionParam					("@RowVersionNull"			,	rowVersionNull			);

				cmd.AddRowVersionFromInt64Param			("@RowVersionInt64"			,	rowVersionInt64			);
				cmd.AddRowVersionFromInt64Param			("@RowVersionInt64Null"		,	rowVersionInt64Null		);
				cmd.AddRowVersionFromInt64Param			("@RowVersionInt64Nullable"	,	rowVersionInt64Nullable	);

				cmd.AddRowVersionFromBase64StringParam	("@RowVersionBase64"		,	rowVersionBase64		);
				cmd.AddRowVersionFromBase64StringParam	("@RowVersionBase64Null"	,	rowVersionBase64Null	);
				

				cmd.ExecuteReader(reader =>
				{
					var i = 0;

					reader.Read(r =>
					{
						Assert.AreEqual(guid, r.GetGuid(i++), "guid");
						Assert.AreEqual(guidNull, r.GetGuidNullable(i++), "guidNull");
						Assert.AreEqual(guidNullable, r.GetGuidNullable(i++), "guidNullable");
						
			  CollectionAssert.AreEqual(rowVersion, r.GetBytesFromRowVersion(i++), "rowVersion");
			  CollectionAssert.AreEqual(rowVersionNull, r.GetBytesFromRowVersion(i++), "rowVersionNull");
						
						Assert.AreEqual(rowVersionInt64, r.GetInt64FromRowVersion(i++), "rowVersionInt64");
						Assert.AreEqual(rowVersionInt64Null, r.GetInt64FromRowVersionNullable(i++), "rowVersionInt64Null");
						Assert.AreEqual(rowVersionInt64Nullable, r.GetInt64FromRowVersionNullable(i++), "rowVersionInt64Nullable");
						
						Assert.AreEqual(rowVersionBase64, r.GetBase64StringFromRowVersion(i++), "rowVersionBase64");
						Assert.AreEqual(rowVersionBase64Null, r.GetBase64StringFromRowVersion(i++), "rowVersionBase64Null");

						Console.WriteLine($"Byte[8]: [{string.Join(",", r.GetBytesFromRowVersion(i))}]");
						Console.WriteLine($"Int64: {r.GetInt64FromRowVersion(i)}");
						Console.WriteLine($"Base64String: {r.GetBase64StringFromRowVersion(i)}");

					});

				});

			});

		}

		[TestMethod]
		public void ToTinyIntIdTableType()
		{
			var numbers = new byte [] {1,2,3 };

			_repository.RunCommand(cmd =>
			{
				cmd.UseSql("select Id from @Numbers;");
				
				cmd.AddTableParam("@Numbers", numbers);

				var readNumbers = cmd.ReadToArray<byte>();
				
				CollectionAssert.AreEqual(numbers, readNumbers);
			});

			_repository.RunCommand(cmd =>
			{
				cmd.UseSql("select Id from @Numbers;");
				
				cmd.AddTableParam("@Numbers", numbers, "TinyIntIdTableType", "Id");

				var readNumbers = cmd.ReadToArray<byte>();
				
				CollectionAssert.AreEqual(numbers, readNumbers);
			});

			_repository.RunCommand(cmd =>
			{
				cmd.UseSql("select Id from @Numbers;");

				cmd.AddTableRowParam("@Numbers", (byte)123);
				
				Assert.AreEqual(123, cmd.ReadTo<byte>());
			});
		}

		[TestMethod]
		public void ToSmallIntIdTableType()
		{
			var numbers = new short [] {1,2,3 };

			_repository.RunCommand(cmd =>
			{
				cmd.UseSql("select Id from @Numbers;");

				cmd.AddTableParam("@Numbers", numbers);

				var readNumbers = cmd.ReadToArray<short>();
				
				CollectionAssert.AreEqual(numbers, readNumbers);
			});

			_repository.RunCommand(cmd =>
			{
				cmd.UseSql("select Id from @Numbers;");

				cmd.AddTableParam("@Numbers", numbers, "SmallIntIdTableType", "Id");

				var readNumbers = cmd.ReadToArray<short>();
				
				CollectionAssert.AreEqual(numbers, readNumbers);
			});

			_repository.RunCommand(cmd =>
			{
				cmd.UseSql("select Id from @Numbers;");

				cmd.AddTableRowParam("@Numbers", (short)123);
				
				Assert.AreEqual(123, cmd.ReadTo<short>());
			});
		}




		[TestMethod]
		public void ToIntIdTableType()
		{
			var numbers = new int [] {1,2,3 };

			_repository.RunCommand(cmd =>
			{
				cmd.UseSql("select Id from @Numbers;");

				cmd.AddTableParam("@Numbers", numbers);

				var readNumbers = cmd.ReadToArray<int>();
				
				CollectionAssert.AreEqual(numbers, readNumbers);
			});

			_repository.RunCommand(cmd =>
			{
				cmd.UseSql("select Id from @Numbers;");

				cmd.AddTableParam("@Numbers", numbers, "IntIdTableType", "Id");

				var readNumbers = cmd.ReadToArray<int>();
				
				CollectionAssert.AreEqual(numbers, readNumbers);
			});

			_repository.RunCommand(cmd =>
			{
				cmd.UseSql("select Id from @Numbers;");

				cmd.AddTableRowParam("@Numbers", 123);
				
				Assert.AreEqual(123, cmd.ReadTo<int>());
			});
		}

		private class A
		{
			public int Id {get; set; }
			public string Name {get; set; }
		}


		[TestMethod]
		public void AsDataTable()
		{
			List<A> aList = new()
			{
				new A {Id = 1, Name ="A" },
				new A {Id = 2, Name ="AA" },
			};
			var tableName = "ATable";


			DataTable dt = aList.AsDataTable(tableName, "Id, Name");

			Assert.AreEqual(tableName, dt.TableName);
			Assert.AreEqual("Id", dt.Columns[0].ColumnName);
			Assert.AreEqual(typeof(int), dt.Columns[0].DataType);
			Assert.AreEqual("Name", dt.Columns[1].ColumnName);
			Assert.AreEqual(typeof(string), dt.Columns[1].DataType);

			Assert.AreEqual(aList.First().Id, (int)dt.Rows[0]["Id"]);
			Assert.AreEqual(aList.First().Name, dt.Rows[0]["Name"].ToString());
			Assert.AreEqual(aList.Last().Id, (int)dt.Rows[1]["Id"]);
			Assert.AreEqual(aList.Last().Name, dt.Rows[1]["Name"].ToString());


			dt = aList.AsDataTable(tableName, new [] { "Id", "Name" });

			Assert.AreEqual(dt.TableName, tableName);
			Assert.AreEqual(aList.First().Id, (int)dt.Rows[0]["Id"]);
			Assert.AreEqual(aList.First().Name, dt.Rows[0]["Name"].ToString());
			Assert.AreEqual(aList.Last().Id, (int)dt.Rows[1]["Id"]);
			Assert.AreEqual(aList.Last().Name, dt.Rows[1]["Name"].ToString());

			
			dt = aList.AsDataTable(tableName);

			Assert.AreEqual(dt.TableName, tableName);
			Assert.AreEqual(aList.First().Id, (int)dt.Rows[0]["Id"]);
			Assert.AreEqual(aList.First().Name, dt.Rows[0]["Name"].ToString());
			Assert.AreEqual(aList.Last().Id, (int)dt.Rows[1]["Id"]);
			Assert.AreEqual(aList.Last().Name, dt.Rows[1]["Name"].ToString());


			dt = aList.AsDataTable();

			Assert.AreEqual("", dt.TableName);
			Assert.AreEqual(aList.First().Id, (int)dt.Rows[0]["Id"]);
			Assert.AreEqual(aList.First().Name, dt.Rows[0]["Name"].ToString());
			Assert.AreEqual(aList.Last().Id, (int)dt.Rows[1]["Id"]);
			Assert.AreEqual(aList.Last().Name, dt.Rows[1]["Name"].ToString());

			var numbers = new int[] {1,3,5,7,11,13};

			dt = numbers.AsDataTable();
			Assert.AreEqual("", dt.TableName);
			Assert.AreEqual("Int32", dt.Columns[0].ColumnName);

		}

		
		[TestCleanup]
		public void Dispose()
		{
			_repository.Dispose();
		}

	}
}
