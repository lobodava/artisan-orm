﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Reflection;

namespace Artisan.Orm
{
	public static partial class DbCommandExtensions
	{

		public static void UseProcedure(this SqlCommand cmd, string procedureName)
		{
			cmd.CommandType = CommandType.StoredProcedure;
			cmd.CommandText = procedureName;
		}

		public static void UseSql(this SqlCommand cmd, string sql)
		{
			cmd.CommandType = CommandType.Text;
			cmd.CommandText = sql;
		}

		// http://blogs.msmvps.com/jcoehoorn/blog/2014/05/12/can-we-stop-using-addwithvalue-already/


		public static void AddBitParam(this SqlCommand cmd, string parameterName, bool value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Bit, 
				Value = value ? 1 : 0
			});
		}

		public static void AddBitParam(this SqlCommand cmd, string parameterName, bool? value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Bit, 
				Value = value == null ? DBNull.Value : (object)(value == true ? 1 : 0)
			});
		}


		public static void AddTinyIntParam(this SqlCommand cmd, string parameterName, byte value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.TinyInt, 
				Value = value
			});
		}

		public static void AddTinyIntParam(this SqlCommand cmd, string parameterName, byte? value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.TinyInt, 
				Value = value == null ? DBNull.Value : (object)value.Value
			});
		}


		public static void AddSmallIntParam(this SqlCommand cmd, string parameterName, short value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.SmallInt, 
				Value = value
			});
		}

		public static void AddSmallIntParam(this SqlCommand cmd, string parameterName,  short? value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.SmallInt, 
				Value = value == null ? DBNull.Value : (object)value.Value
			});
		}


		public static void AddIntParam(this SqlCommand cmd, string parameterName, int value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Int, 
				Value = value
			});
		}

		public static void AddIntParam(this SqlCommand cmd, string parameterName, int? value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Int, 
				Value = value == null ? DBNull.Value : (object)value.Value
			});
		}


		public static void AddBigIntParam(this SqlCommand cmd, string parameterName, long value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.BigInt, 
				Value = value
			});
		}

		public static void AddBigIntParam(this SqlCommand cmd, string parameterName, long? value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.BigInt, 
				Value = value == null ? DBNull.Value : (object)value.Value
			});
		}


		public static void AddDecimalParam(this SqlCommand cmd, string parameterName, byte precision,  byte scale,  decimal value, bool truncateFraction = false)
		{
			var valueString = Math.Abs(value).ToString(CultureInfo.InvariantCulture);
			var split = valueString.Split('.');

			if (split.Length > 1 && scale < split[1].Length && !truncateFraction)
				throw new ArgumentException($"Fractional part of SqlParameter {parameterName} = {value} is longer than acceptable for SQL Server decimal({precision},{scale}) type. CommandType: {cmd.CommandType}. CommandText: {cmd.CommandText}.");

			if (precision - scale < split[0].Length)
			{
				var integerPart		=  new string('9', precision - scale);
				var fractionalPart	=  new string('9', scale);
				var minValue = $"-{integerPart}.{fractionalPart}";
				var maxValue = $"{integerPart}.{fractionalPart}";

				throw new ArgumentOutOfRangeException($"Value of SqlParameter {parameterName} = {value} that is out of SQL Server decimal({precision},{scale}) type range [{minValue}, {maxValue}]. CommandType: {cmd.CommandType}. CommandText: {cmd.CommandText}.");
			}

			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Decimal,
				Scale = scale,
 				Precision = precision,
				Value = value
			});
		}

		public static void AddDecimalParam(this SqlCommand cmd, string parameterName, byte precision,  byte scale,  decimal? value, bool truncateFraction = false )
		{
			if (value != null)
				cmd.AddDecimalParam(parameterName, precision, scale, value.Value, truncateFraction);
			else
				cmd.Parameters.Add(new SqlParameter
				{
					ParameterName = parameterName,
					Direction = ParameterDirection.Input,
					SqlDbType = SqlDbType.Decimal,
					Scale = scale,
					Precision = precision,
					Value = DBNull.Value
				});
		}


		public static void AddSmallMoneyParam(this SqlCommand cmd, string parameterName, decimal value )
		{
			if (value < -214748.3648m || 214748.3647m < value) 
				throw new ArgumentOutOfRangeException($"Value of SqlParameter {parameterName} = {value} that is out of SQL Server smallmoney type range [-214748.3648, 214748.3647]. CommandType: {cmd.CommandType}. CommandText: {cmd.CommandText}.");

			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.SmallMoney,
				Value = value
			});
		}

		public static void AddSmallMoneyParam(this SqlCommand cmd, string parameterName, decimal? value )
		{
			if (value != null)
				cmd.AddSmallMoneyParam(parameterName, value.Value);
			else
				cmd.Parameters.Add(new SqlParameter
				{
					ParameterName = parameterName,
					Direction = ParameterDirection.Input,
					SqlDbType = SqlDbType.SmallMoney,
					Value = DBNull.Value
				});
		}


		public static void AddMoneyParam(this SqlCommand cmd, string parameterName, decimal value )
		{
			if (value < -922337203685477.5808m || 922337203685477.5807m < value) 
				throw new ArgumentOutOfRangeException($"Value of SqlParameter {parameterName} = {value} that is out of SQL Server money type range [-922337203685477.5808, 922337203685477.5807]. CommandType: {cmd.CommandType}. CommandText: {cmd.CommandText}.");

			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Money,
				Value = value
			});
		}

		public static void AddMoneyParam(this SqlCommand cmd, string parameterName, decimal? value )
		{
			if (value != null)
				cmd.AddMoneyParam(parameterName, value.Value);
			else
				cmd.Parameters.Add( new SqlParameter
				{ 
					ParameterName = parameterName,
					Direction = ParameterDirection.Input,
					SqlDbType = SqlDbType.Money,
					Value = DBNull.Value
				});
		}


		public static void AddRealParam(this SqlCommand cmd, string parameterName, float value )
		{
			if (value < -3.40E+38f || 3.40E+38f < value) 
				throw new ArgumentOutOfRangeException($"Value of SqlParameter {parameterName} = {value} that is out of SQL Server real type range [3.40E+38, 3.40E+38]. CommandType: {cmd.CommandType}. CommandText: {cmd.CommandText}.");
			
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Real,
				Value = value
			});
		}

		public static void AddRealParam(this SqlCommand cmd, string parameterName, float? value )
		{
			if (value != null)
				cmd.AddRealParam(parameterName, value.Value);
			else
				cmd.Parameters.Add( new SqlParameter
				{ 
					ParameterName = parameterName,
					Direction = ParameterDirection.Input,
					SqlDbType = SqlDbType.Real,
					Value = DBNull.Value
				});
		}


		public static void AddFloatParam(this SqlCommand cmd, string parameterName, double value )
		{
			if (value < -1.79E+308d || 1.79E+308d < value) 
				throw new ArgumentOutOfRangeException($"Value of SqlParameter {parameterName} = {value} that is out of SQL Server float type range [-1.79E+308, 1.79E+308]. CommandType: {cmd.CommandType}. CommandText: {cmd.CommandText}.");
	
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Float,
				Value = value
			});
		}

		public static void AddFloatParam(this SqlCommand cmd, string parameterName, double? value )
		{
			if (value != null)
				cmd.AddFloatParam(parameterName, value.Value);
			else
				cmd.Parameters.Add( new SqlParameter
				{ 
					ParameterName = parameterName,
					Direction = ParameterDirection.Input,
					SqlDbType = SqlDbType.Float,
					Value = DBNull.Value
				});
		}


		public static void AddCharParam(this SqlCommand cmd, string parameterName, char value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Char,
				Value = value
			});
		}

		public static void AddCharParam(this SqlCommand cmd, string parameterName, char? value )
		{
			if (value != null)
				cmd.AddCharParam(parameterName, value.Value);
			else
				cmd.Parameters.Add( new SqlParameter
				{ 
					ParameterName = parameterName,
					Direction = ParameterDirection.Input,
					SqlDbType = SqlDbType.Char,
					Value = DBNull.Value
				});
		}


		public static void AddNCharParam(this SqlCommand cmd, string parameterName, char value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.NChar,
				Value = value
			});
		}

		public static void AddNCharParam(this SqlCommand cmd, string parameterName, char? value )
		{
			if (value != null)
				cmd.AddNCharParam(parameterName, value.Value);
			else
				cmd.Parameters.Add( new SqlParameter
				{ 
					ParameterName = parameterName,
					Direction = ParameterDirection.Input,
					SqlDbType = SqlDbType.NChar,
					Value = DBNull.Value
				});
		}


		public static void AddVarcharParam(this SqlCommand cmd, string parameterName, int size, string value, bool trimToNull = false, bool truncate = false)
		{
			if (value != null)
			{
				if (trimToNull)
					value.TrimToNull();

				if (truncate)
					value.TruncateTo(size);
				else if (size < value.Length)
					throw new ArgumentException($"String value of SqlParameter {parameterName} is {value.Length} character length that exceeds size of varchar({size}) type and would be truncated. CommandType: {cmd.CommandType}. CommandText: {cmd.CommandText}.");
			}

			cmd.Parameters.Add(new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.VarChar,
				Size = size,
				Value = (object)value ?? DBNull.Value,
			});
		}
		
		public static void AddNVarcharParam(this SqlCommand cmd, string parameterName, int size, string value, bool trimToNull = false, bool truncate = false )
		{
			if (value != null)
			{
				if (trimToNull)
					value.TrimToNull();

				if (truncate)
					value.TruncateTo(size);
				else if (size < value.Length)
					throw new ArgumentException($"String value of SqlParameter {parameterName} is {value.Length} character length that exceeds size of nvarchar({size}) type and would be truncated. CommandType: {cmd.CommandType}. CommandText: {cmd.CommandText}.");
			}
			
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.NVarChar, 
				Size = size,
				Value = (object)value ?? DBNull.Value,
			});
		}


		public static void AddVarcharMaxParam(this SqlCommand cmd, string parameterName, string value, bool trimToNull = false) 
		{
			if (trimToNull)
				value.TrimToNull();

			cmd.Parameters.Add(new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.VarChar,
				Size = -1,
				Value = (object)value ?? DBNull.Value,
			});
		}

		public static void AddNVarcharMaxParam(this SqlCommand cmd, string parameterName, string value, bool trimToNull = false) 
		{
			if (trimToNull)
				value.TrimToNull();

			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.NVarChar, 
				Size = -1,
				Value = (object)value ?? DBNull.Value,
			});
		}


		public static void AddBinaryParam(this SqlCommand cmd, string parameterName, int size, byte[] value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Binary, 
				Size = size, 
				Value = (object)value ?? DBNull.Value
			});
		}

		public static void AddVarbinaryParam(this SqlCommand cmd, string parameterName, int size, byte[] value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.VarBinary, 
				Size = size, 
				Value = (object)value ?? DBNull.Value
			});
		}
		
		public static void AddVarbinaryMaxParam(this SqlCommand cmd, string parameterName, byte[] value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.VarBinary, 
				Size = -1, 
				Value = (object)value ?? DBNull.Value
			});
		}


		public static void AddDateParam(this SqlCommand cmd, string parameterName, DateTime value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Date, 
				Value = value
			});
		}

		public static void AddDateParam(this SqlCommand cmd, string parameterName, DateTime? value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Date, 
				Value = (object)value ?? DBNull.Value
			});
		}
		

		public static void AddTimeParam(this SqlCommand cmd, string parameterName, TimeSpan value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Time, 
				Value = value
			});
		}

		public static void AddTimeParam(this SqlCommand cmd, string parameterName, TimeSpan? value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Time, 
				Value = (object)value ?? DBNull.Value
			});
		}


		public static void AddSmallDateTimeParam(this SqlCommand cmd, string parameterName, DateTime value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.SmallDateTime, 
				Value = value
			});
		}
		
		public static void AddSmallDateTimeParam(this SqlCommand cmd, string parameterName, DateTime? value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.SmallDateTime, 
				Value = (object)value ?? DBNull.Value
			});
		}
		

		public static void AddDateTimeParam(this SqlCommand cmd, string parameterName, DateTime value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.DateTime, 
				Value = value
			});
		}
		
		public static void AddDateTimeParam(this SqlCommand cmd, string parameterName, DateTime? value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.DateTime, 
				Value = (object)value ?? DBNull.Value
			});
		}
		

		public static void AddDateTime2Param(this SqlCommand cmd, string parameterName, DateTime value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.DateTime2, 
				Value = value
			});
		}
		
		public static void AddDateTime2Param(this SqlCommand cmd, string parameterName, DateTime? value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.DateTime2, 
				Value = (object)value ?? DBNull.Value
			});
		}


		public static void AddDateTimeOffsetParam(this SqlCommand cmd, string parameterName, DateTimeOffset value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.DateTimeOffset, 
				Value = value
			});
		}

		public static void AddDateTimeOffsetParam(this SqlCommand cmd, string parameterName, DateTimeOffset? value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.DateTimeOffset, 
				Value = (object)value ?? DBNull.Value
			});
		}
		

		public static void AddGuidParam(this SqlCommand cmd, string parameterName, Guid value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.UniqueIdentifier, 
				Value = value
			});
		}

		public static void AddGuidParam(this SqlCommand cmd, string parameterName, Guid? value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.UniqueIdentifier, 
				Value = (object)value ?? DBNull.Value
			});
		}


		public static void AddRowVersionParam(this SqlCommand cmd, string parameterName, byte[] value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Binary, 
				Size = 8,
				Value = (object)value ?? DBNull.Value
			});
		}

		public static void AddRowVersionFromBase64StringParam(this SqlCommand cmd, string parameterName, string value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Binary, 
				Size = 8,
				Value = (value == null) ? DBNull.Value : (object)Convert.FromBase64String(value)
			});
		}

		public static void AddRowVersionFromInt64Param(this SqlCommand cmd, string parameterName, long value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Binary, 
				Size = 8,
				Value = BitConverter.GetBytes(value)
			});
		}

		public static void AddRowVersionFromInt64Param(this SqlCommand cmd, string parameterName, long? value )
		{
			if (value != null)
				cmd.AddRowVersionFromInt64Param(parameterName, value.Value);
			else
				cmd.Parameters.Add( new SqlParameter
				{ 
					ParameterName = parameterName,
					Direction = ParameterDirection.Input,
					SqlDbType = SqlDbType.Binary, 
					Size = 8,
					Value = DBNull.Value
				});
		}
		

		public static void AddSqlVariantParam(this SqlCommand cmd, string parameterName, object value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Variant,
				Value = value ?? DBNull.Value
			});
		}
		
		public static void AddXmlParam(this SqlCommand cmd, string parameterName, string value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Xml,
				Value = (object)value ?? DBNull.Value
			});
		}

		public static void AddTableParamFrom<T>(this SqlCommand cmd, string parameterName, T obj)
		{
			var param = CreateInputParam(parameterName, SqlDbType.Structured);

			if (obj == null)
				param.Value = DBNull.Value;
			else
			{ 
				var dataTable = DataTableHelpers.GetDataTableFrom(obj);

				param.TypeName = dataTable.TableName;
				param.Value = dataTable;
			}

			cmd.Parameters.Add(param);
		}

		public static void AddTableParamFrom<T>(this SqlCommand cmd, string parameterName, IEnumerable<T> list)
		{
			var param = CreateInputParam(parameterName, SqlDbType.Structured);

			if (list == null)
				param.Value = DBNull.Value;
			else
			{ 
				DataTable dataTable;

				if (typeof(T).IsValueType)
					dataTable = DataTableHelpers.GetValueTypeDataTable(list);
				else
					dataTable = DataTableHelpers.GetDataTableFromList(list);

				param.TypeName = dataTable.TableName;
				param.Value = dataTable;
			}

			cmd.Parameters.Add(param);
		}


		public static void AddTableParam(this SqlCommand cmd, string parameterName, object obj)
		{
			var param = CreateInputParam(parameterName, SqlDbType.Structured);
			
			if (obj == null)
			{
				param.Value = DBNull.Value;
			}
			else
			{
				DataTable dataTable;
				var objType = obj.GetType();
			
				if (obj is IEnumerable)
				{
					if (objType.IsGenericType)
					{
						var itemType = obj.GetType().GetGenericArguments()[0];

						if (!itemType.IsValueType)
							dataTable = DataTableHelpers.GetDataTableFromList(obj, itemType);
						else
							dataTable = DataTableHelpers.GetValueTypeDataTable(obj);
					}
					else
						dataTable = DataTableHelpers.GetValueTypeDataTable(obj);;
				}
				else
				{
					dataTable =  obj as DataTable;

					if (dataTable == null)
					{
						dataTable =  DataTableHelpers.GetDataTableFrom(obj, objType);
					}
				}

				if (dataTable == null)
					throw new NullReferenceException($"No mapping function found to create DataTable and DataRow for type {objType.FullName}");

				param.TypeName = dataTable.TableName;
				param.Value = dataTable;
			}

			cmd.Parameters.Add(param);
		}


		public static void AddVarcharOutputParam(this SqlCommand cmd, string parameterName, int size )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Output,
				SqlDbType = SqlDbType.VarChar, 
				Size = size,					
			});
		}

		public static void AddNVarcharOutputParam(this SqlCommand cmd, string parameterName, int size )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Output,
				SqlDbType = SqlDbType.NVarChar, 
				Size = size,					
			});
		}


		public static SqlParameter ReturnValueParam(this SqlCommand cmd)
		{
			var returnValueParam = new SqlParameter
			{
				ParameterName = "ReturnValue",
				Direction = ParameterDirection.ReturnValue,
				SqlDbType = SqlDbType.Int,
			};

			cmd.Parameters.Add(returnValueParam);

			return returnValueParam;
		}


		private static SqlParameter CreateInputParam(string parameterName, SqlDbType parameterType)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = parameterType,
			};
		}
		

		
	}
}