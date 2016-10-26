using System;
using System.Diagnostics;
using Artisan.Orm;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Tests
{
	[TestClass]
	public class SqlParemeterTest
	{
		private RepositoryBase _repositoryBase;

		[TestInitialize]
		public void TestInitialize()
		{
			_repositoryBase = new RepositoryBase();

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


			var isOk = _repositoryBase.GetByCommand(cmd =>
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

				return cmd.GetByReader(reader =>
				{
					var i = 0;

					reader.ReadBy(r =>
					{
						Assert.AreEqual( r.GetBoolean(i++)			,	bit					);
						Assert.AreEqual( r.GetBooleanNullable(i++)	,	bitNull				);	
						Assert.AreEqual( r.GetBooleanNullable(i++)	,	bitNullable			);	

						Assert.AreEqual( r.GetByte(i++)				,	tinyInt				);	
						Assert.AreEqual( r.GetByteNullable(i++)		,	tinyIntNull			);	
						Assert.AreEqual( r.GetByteNullable(i++)		,	tinyIntNullable		);	

						Assert.AreEqual( r.GetInt16(i++)			,	smallInt			);	
						Assert.AreEqual( r.GetInt16Nullable(i++)	,	smallIntNull		);	
						Assert.AreEqual( r.GetInt16Nullable(i++)	,	smallIntNullable	);

						Assert.AreEqual( r.GetInt32(i++)			,	int_				);	
						Assert.AreEqual( r.GetInt32Nullable(i++)	,	intNull				);	
						Assert.AreEqual( r.GetInt32Nullable(i++)	,	intNullable			);	

						Assert.AreEqual( r.GetInt64(i++)			,	bigInt				);	
						Assert.AreEqual( r.GetInt64Nullable(i++)	,	bigIntNull			);	
						Assert.AreEqual( r.GetInt64Nullable(i++)	,	bigIntNullable		);

					});

					return true;
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



			var isOk = _repositoryBase.GetByCommand(cmd =>
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


				return cmd.GetByReader(reader =>
				{
					var i = 0;

					reader.ReadBy(r =>
					{
						Assert.AreEqual( r.GetDecimal(i++)			,	decimal_			);	
						Assert.AreEqual( r.GetDecimalNullable(i++)	,	decimalNull			);	
						Assert.AreEqual( r.GetDecimalNullable(i++)	,	decimalNullable		);	

						Assert.AreEqual( r.GetDecimal(i++)			,	smallMoney			);	
						Assert.AreEqual( r.GetDecimalNullable(i++)	,	smallMoneyNull		);	
						Assert.AreEqual( r.GetDecimalNullable(i++)	,	smallMoneyNullable	);	

						Assert.AreEqual( r.GetDecimal(i++)			,	money				);	
						Assert.AreEqual( r.GetDecimalNullable(i++)	,	moneyNull			);	
						Assert.AreEqual( r.GetDecimalNullable(i++)	,	moneyNullable		);	

						Assert.AreEqual( r.GetFloat(i++)			,	real				);	
						Assert.AreEqual( r.GetFloatNullable(i++)	,	realNull			);	
						Assert.AreEqual( r.GetFloatNullable(i++)	,	realNullable		);	

						Assert.AreEqual( r.GetDouble(i++)			,	float_				);	
						Assert.AreEqual( r.GetDoubleNullable(i++)	,	floatNull			);	
						Assert.AreEqual( r.GetDoubleNullable(i++)	,	floatNullable		);

					});

					return true;
				});

			});

		}





		
		[TestCleanup]
		public void Dispose()
		{
			_repositoryBase.Dispose();
		}

	}
}
