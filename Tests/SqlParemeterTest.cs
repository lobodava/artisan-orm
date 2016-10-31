using System;
using System.Data;
using System.Diagnostics;
using Artisan.Orm;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.DAL.Records.Models;

namespace Tests
{
	[TestClass]
	public class SqlParemeterTest
	{
		private RepositoryBase _repository;

		[TestInitialize]
		public void TestInitialize()
		{
			_repository = new RepositoryBase();

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
						Assert.AreEqual( r.GetBoolean(i++)			,	bit					,	"bit"				);
						Assert.AreEqual( r.GetBooleanNullable(i++)	,	bitNull				,	"bitNull"			);	
						Assert.AreEqual( r.GetBooleanNullable(i++)	,	bitNullable			,	"bitNullable"		);	

						Assert.AreEqual( r.GetByte(i++)				,	tinyInt				,	"tinyInt"			);	
						Assert.AreEqual( r.GetByteNullable(i++)		,	tinyIntNull			,	"tinyIntNull"		);	
						Assert.AreEqual( r.GetByteNullable(i++)		,	tinyIntNullable		,	"tinyIntNullable"	);	

						Assert.AreEqual( r.GetInt16(i++)			,	smallInt			,	"smallInt"			);	
						Assert.AreEqual( r.GetInt16Nullable(i++)	,	smallIntNull		,	"smallIntNull"		);	
						Assert.AreEqual( r.GetInt16Nullable(i++)	,	smallIntNullable	,	"smallIntNullable"	);

						Assert.AreEqual( r.GetInt32(i++)			,	int_				,	"int_"				);	
						Assert.AreEqual( r.GetInt32Nullable(i++)	,	intNull				,	"intNull"			);	
						Assert.AreEqual( r.GetInt32Nullable(i++)	,	intNullable			,	"intNullable"		);	

						Assert.AreEqual( r.GetInt64(i++)			,	bigInt				,	"bigInt"			);	
						Assert.AreEqual( r.GetInt64Nullable(i++)	,	bigIntNull			,	"bigIntNull"		);	
						Assert.AreEqual( r.GetInt64Nullable(i++)	,	bigIntNullable		,	"bigIntNullable"	);

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
						Assert.AreEqual( r.GetDecimal(i++)			,	decimal_			,	"decimal_"			);	
						Assert.AreEqual( r.GetDecimalNullable(i++)	,	decimalNull			,	"decimalNull"		);	
						Assert.AreEqual( r.GetDecimalNullable(i++)	,	decimalNullable		,	"decimalNullable"	);	

						Assert.AreEqual( r.GetDecimal(i++)			,	smallMoney			,	"smallMoney"		);	
						Assert.AreEqual( r.GetDecimalNullable(i++)	,	smallMoneyNull		,	"smallMoneyNull"	);	
						Assert.AreEqual( r.GetDecimalNullable(i++)	,	smallMoneyNullable	,	"smallMoneyNullable");	

						Assert.AreEqual( r.GetDecimal(i++)			,	money				,	"money"				);	
						Assert.AreEqual( r.GetDecimalNullable(i++)	,	moneyNull			,	"moneyNull"			);	
						Assert.AreEqual( r.GetDecimalNullable(i++)	,	moneyNullable		,	"moneyNullable"		);	

						Assert.AreEqual( r.GetFloat(i++)			,	real				,	"real"				);	
						Assert.AreEqual( r.GetFloatNullable(i++)	,	realNull			,	"realNull"			);	
						Assert.AreEqual( r.GetFloatNullable(i++)	,	realNullable		,	"realNullable"		);	

						Assert.AreEqual( r.GetDouble(i++)			,	float_				,	"float_"			);	
						Assert.AreEqual( r.GetDoubleNullable(i++)	,	floatNull			,	"floatNull"			);	
						Assert.AreEqual( r.GetDoubleNullable(i++)	,	floatNullable		,	"floatNullable"		);

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

			char		nchar			=	'Ф';
			char?		ncharNull		=	null;
			char?		ncharNullable	=	'Ж';

			string		varchar			=	new string('W', 8000);
			string		varcharNull		=	null;

			string		nvarchar			=	new string('Ж', 4000);
			string		nvarcharNull		=	null;

			string		varcharmax			=	new string('W', 10000);
			string		varcharmaxNull		=	null;

			string		nvarcharmax			=	new string('Ж', 10000);
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
						Assert.AreEqual( r.GetCharacter(i++)		,	char_			,	"char_"				);	
						Assert.AreEqual( r.GetCharacterNullable(i++),	charNull		,	"charNull"			);	
						Assert.AreEqual( r.GetCharacterNullable(i++),	charNullable	,	"charNullable"		);	

						Assert.AreEqual( r.GetCharacter(i++)		,	nchar			,	"nchar"				);	
						Assert.AreEqual( r.GetCharacterNullable(i++),	ncharNull		,	"ncharNull"			);	
						Assert.AreEqual( r.GetCharacterNullable(i++),	ncharNullable	,	"ncharNullable"		);	

						Assert.AreEqual( r.GetString(i++)			,	varchar			,	"varchar"			);	
						Assert.AreEqual( r.GetStringNullable(i++)	,	varcharNull		,	"varcharNull"		);	

						Assert.AreEqual( r.GetString(i++)			,	nvarchar		,	"nvarchar"			);	
						Assert.AreEqual( r.GetStringNullable(i++)	,	nvarcharNull	,	"nvarcharNull"		);	

						Assert.AreEqual( r.GetString(i++)			,	varcharmax		,	"varcharmax"		);	
						Assert.AreEqual( r.GetStringNullable(i++)	,	varcharmaxNull	,	"varcharmaxNull"	);

						Assert.AreEqual( r.GetString(i++)			,	nvarcharmax		,	"nvarcharmax"		);	
						Assert.AreEqual( r.GetStringNullable(i++)	,	nvarcharmaxNull ,	"nvarcharmaxNull"	);

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


			DateTime		date					=	new DateTime(Y,M,D)			;
			DateTime?		dateNull				=	null						;
			DateTime?		dateNullable			=	new DateTime(Y,M,D)			; 

			TimeSpan		time					=	new TimeSpan(h,m,s)			;
			TimeSpan?		timeTimeNull			=	null						;
			TimeSpan?		timeTimeNullable		=	new TimeSpan(h,m,s)			; 

			DateTime		smallDateTime			=	new DateTime(Y,M,D,h,m,0)	;
			DateTime?		smallDateTimeNull		=	null						;
			DateTime?		smallDateTimeNullable	=	new DateTime(Y,M,D,h,m,0)	; 

			DateTime		dateTime				=	new DateTime(Y,M,D,h,m,s)	;
			DateTime?		dateTimeNull			=	null						;
			DateTime?		dateTimeNullable		=	new DateTime(Y,M,D,h,m,s)	; 

			DateTime		dateTime2				=	new DateTime(Y,M,D,h,m,s)	;
			DateTime?		dateTime2Null			=	null						;
			DateTime?		dateTime2Nullable		=	new DateTime(Y,M,D,h,m,s)	; 

			DateTimeOffset	dateTimeOffset			=	new DateTimeOffset(Y,M,D,h,m,s,o)	;
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
						Assert.AreEqual( r.GetDateTime(i++)					,	date					, "date"					);
						Assert.AreEqual( r.GetDateTimeNullable(i++)			,	dateNull				, "dateNull"				);
						Assert.AreEqual( r.GetDateTimeNullable(i++)			,	dateNullable			, "dateNullable"			);

						Assert.AreEqual( r.GetTimeSpan(i++)					,	time					, "time"					);
						Assert.AreEqual( r.GetTimeSpanNullable(i++)			,	timeTimeNull			, "timeTimeNull"			);
						Assert.AreEqual( r.GetTimeSpanNullable(i++)			,	timeTimeNullable		, "timeTimeNullable"		);

						Assert.AreEqual( r.GetDateTime(i++)					,	smallDateTime			, "smallDateTime"			);
						Assert.AreEqual( r.GetDateTimeNullable(i++)			,	smallDateTimeNull		, "smallDateTimeNull"		);
						Assert.AreEqual( r.GetDateTimeNullable(i++)			,	smallDateTimeNullable	, "smallDateTimeNullable"	);

						Assert.AreEqual(  r.GetDateTime(i++)				,	dateTime				, "dateTime"				);
						Assert.AreEqual(  r.GetDateTimeNullable(i++)		,	dateTimeNull			, "dateTimeNull"			);
						Assert.AreEqual(  r.GetDateTimeNullable(i++)		,	dateTimeNullable.Value	, "dateTimeNullable"		);

						Assert.AreEqual( r.GetDateTime(i++)					,	dateTime2				, "dateTime2"				);
						Assert.AreEqual( r.GetDateTimeNullable(i++)			,	dateTime2Null			, "dateTime2Null"			);
						Assert.AreEqual( r.GetDateTimeNullable(i++)			,	dateTime2Nullable		, "dateTime2Nullable"		);

						Assert.AreEqual( r.GetDateTimeOffset(i++)			,	dateTimeOffset			, "dateTimeOffset"			);	
						Assert.AreEqual( r.GetDateTimeOffsetNullable(i++)	,	dateTimeOffsetNull		, "dateTimeOffsetNull"		);	
						Assert.AreEqual( r.GetDateTimeOffsetNullable(i++)	,	dateTimeOffsetNullable	, "dateTimeOffsetNullable"	);

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
						Assert.AreEqual( r.GetGuid(i++)							,	guid					,	"guid"						);
						Assert.AreEqual( r.GetGuidNullable(i++)					,	guidNull				,	"guidNull"					);
						Assert.AreEqual( r.GetGuidNullable(i++)					,	guidNullable			,	"guidNullable"				);
						
			  CollectionAssert.AreEqual( r.GetBytesFromRowVersion(i++)			,	rowVersion				,	"rowVersion"				);
			  CollectionAssert.AreEqual( r.GetBytesFromRowVersion(i++)			,	rowVersionNull			,	"rowVersionNull"			);
						
						Assert.AreEqual( r.GetInt64FromRowVersion(i++)			,	rowVersionInt64			,	"rowVersionInt64"			);
						Assert.AreEqual( r.GetInt64NullableFromRowVersion(i++)	,	rowVersionInt64Null		,	"rowVersionInt64Null"		);
						Assert.AreEqual( r.GetInt64NullableFromRowVersion(i++)	,	rowVersionInt64Nullable	,	"rowVersionInt64Nullable"	);
						
						Assert.AreEqual( r.GetBase64StringFromRowVersion(i++)	,	rowVersionBase64		,	"rowVersionBase64"			);
						Assert.AreEqual( r.GetBase64StringFromRowVersion(i++)	,	rowVersionBase64Null	,	"rowVersionBase64Null"		);

						Console.WriteLine($"Byte[8]: [{String.Join(",", r.GetBytesFromRowVersion(i))}]");
						Console.WriteLine($"Int64: {r.GetInt64FromRowVersion(i)}");
						Console.WriteLine($"Base64String: {r.GetBase64StringFromRowVersion(i)}");

					});

				});

			});

		}
		

		[TestMethod]
		public void GetDataTableFor()
		{
			var record = new Record
			{
				Id				=	0				,
				GrandRecordId	=	1				,
				Name			=	"AAA"			,
				RecordTypeId	=	1				,
				Number			=	123				,
				Date			=	DateTime.Now	,
				Amount			=	1000			,
				IsActive		=	true			,
				Comment			=	"Lorem ipsum"
			};

			DataTable dataTable = DataTableHelpers.GetDataTableFor(record);
			dataTable = DataTableHelpers.GetDataTableFor(record, typeof(Tests.DAL.Records.Models.Record));


			var sw = new Stopwatch();
			sw.Start();

			for (var i = 1; i <= 10000; i++)
			{
				dataTable = DataTableHelpers.GetDataTableFor<Record>(record);
			}

			sw.Stop();

			Console.WriteLine($"GetDataTableFor<Record>(record) created 10,000 DataTables for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms, or {(sw.Elapsed.TotalMilliseconds / 10000).ToString("0.####")} ms for one DataTable" );
			Console.WriteLine();

			var type = typeof(Record);

			sw.Restart();

			for (var i = 1; i <= 10000; i++)
			{
				dataTable = DataTableHelpers.GetDataTableFor(record, type);
			}

			sw.Stop();

			Console.WriteLine($"GetDataTableFor(record, type) created 10,000 DataTables for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms, or {(sw.Elapsed.TotalMilliseconds / 10000).ToString("0.####")} ms for one DataTable" );
	

		}



		
		[TestCleanup]
		public void Dispose()
		{
			_repository.Dispose();
		}

	}
}
