using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace Artisan.Orm
{ 

	public static partial class SqlCommandExtensions
	{
		/// <summary>Configures the command to invoke a stored procedure with the given <paramref name="procedureName"/>.</summary>
		public static void UseProcedure(this SqlCommand cmd, string procedureName)
		{
			cmd.CommandType = CommandType.StoredProcedure;
			cmd.CommandText = procedureName;
		}

		/// <summary>Configures the command to execute the given <paramref name="sql"/> as a text batch.</summary>
		public static void UseSql(this SqlCommand cmd, string sql)
		{
			cmd.CommandType = CommandType.Text;
			cmd.CommandText = sql;
		}

		// http://blogs.msmvps.com/jcoehoorn/blog/2014/05/12/can-we-stop-using-addwithvalue-already/


		/// <summary>Adds an input <c>bit</c> parameter named <paramref name="parameterName"/> with <paramref name="value"/>.</summary>
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

		/// <inheritdoc cref="AddBitParam(SqlCommand, string, bool)"/>
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


		/// <summary>Adds an input <c>tinyint</c> parameter.</summary>
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

		/// <inheritdoc cref="AddTinyIntParam(SqlCommand, string, byte)"/>
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


		/// <summary>Adds an input <c>smallint</c> parameter.</summary>
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

		/// <inheritdoc cref="AddSmallIntParam(SqlCommand, string, short)"/>
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


		/// <summary>Adds an input <c>int</c> parameter.</summary>
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

		/// <inheritdoc cref="AddIntParam(SqlCommand, string, int)"/>
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


		/// <summary>Adds an input <c>bigint</c> parameter.</summary>
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

		/// <inheritdoc cref="AddBigIntParam(SqlCommand, string, long)"/>
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


		/// <summary>Adds an input <c>decimal(<paramref name="precision"/>, <paramref name="scale"/>)</c> parameter.
		/// When <paramref name="truncateFraction"/> is <c>true</c>, fractional digits beyond <paramref name="scale"/>
		/// are discarded toward zero; when <c>false</c>, a value requiring fractional rounding is rejected.
		/// Insignificant trailing zeroes do not require rounding. Precision must be 1 through 38,
		/// and scale must not exceed precision.</summary>
		public static void AddDecimalParam(this SqlCommand cmd, string parameterName, byte precision,  byte scale,  decimal value, bool truncateFraction = false)
		{
			ValidateDecimalPrecisionAndScale(precision, scale);

			// Decimal supports at most 28 fractional digits. Truncating its invariant text
			// avoids multiplying large values by a power of ten and overflowing CLR decimal.
			var text = value.ToString(CultureInfo.InvariantCulture);
			var separator = text.IndexOf('.');
			if (separator >= 0 && text.Length - separator - 1 > scale)
			{
				var truncated = decimal.Parse(text.Substring(0, separator + (scale == 0 ? 0 : scale + 1)), CultureInfo.InvariantCulture);
				if (!truncateFraction && truncated != value)
					throw new ArgumentException($"Fractional part of SqlParameter {parameterName} = {value} is longer than acceptable for SQL Server decimal({precision},{scale}) type. CommandType: {cmd.CommandType}. CommandText: {cmd.CommandText}.", nameof(value));
				value = truncated;
			}

			var integerPart = decimal.Truncate(value).ToString(CultureInfo.InvariantCulture).TrimStart('-').TrimStart('0');
			if (integerPart.Length > precision - scale)
				throw new ArgumentOutOfRangeException(nameof(value), value,
					$"Value of SqlParameter {parameterName} is outside the SQL Server decimal({precision},{scale}) range. CommandType: {cmd.CommandType}. CommandText: {cmd.CommandText}.");

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

		/// <inheritdoc cref="AddDecimalParam(SqlCommand, string, byte, byte, decimal, bool)"/>
		public static void AddDecimalParam(this SqlCommand cmd, string parameterName, byte precision,  byte scale,  decimal? value, bool truncateFraction = false )
		{
			ValidateDecimalPrecisionAndScale(precision, scale);
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

		private static void ValidateDecimalPrecisionAndScale(byte precision, byte scale)
		{
			if (precision < 1 || precision > 38)
				throw new ArgumentOutOfRangeException(nameof(precision), precision, "SQL Server decimal precision must be between 1 and 38.");
			if (scale > precision)
				throw new ArgumentOutOfRangeException(nameof(scale), scale, "SQL Server decimal scale must not exceed precision.");
		}


		/// <summary>Adds an input <c>smallmoney</c> parameter.</summary>
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

		/// <inheritdoc cref="AddSmallMoneyParam(SqlCommand, string, decimal)"/>
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


		/// <summary>Adds an input <c>money</c> parameter.</summary>
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

		/// <inheritdoc cref="AddMoneyParam(SqlCommand, string, decimal)"/>
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


		/// <summary>Adds an input <c>real</c> (single-precision float) parameter.</summary>
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

		/// <inheritdoc cref="AddRealParam(SqlCommand, string, float)"/>
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


		/// <summary>Adds an input <c>float</c> (double-precision) parameter.</summary>
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

		/// <inheritdoc cref="AddFloatParam(SqlCommand, string, double)"/>
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


		/// <summary>Adds an input <c>char(1)</c> parameter.</summary>
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

		/// <inheritdoc cref="AddCharParam(SqlCommand, string, char)"/>
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


		/// <summary>Adds an input <c>nchar(1)</c> parameter.</summary>
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

		/// <inheritdoc cref="AddNCharParam(SqlCommand, string, char)"/>
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


		/// <summary>Adds an input <c>varchar(<paramref name="size"/>)</c> parameter.</summary>
		/// <remarks>When <paramref name="trimToNull"/>=<c>true</c>, the value is trimmed and an empty result is sent as <c>NULL</c>.
		/// After trimming, strings longer than <paramref name="size"/> are truncated when <paramref name="truncate"/>=<c>true</c>;
		/// otherwise an <see cref="ArgumentException"/> is thrown before adding the parameter.</remarks>
		public static void AddVarcharParam(this SqlCommand cmd, string parameterName, int size, string? value, bool trimToNull = false, bool truncate = false)
		{
			if (trimToNull)
				value = value.TrimToNull();

			if (value != null)
			{
				if (truncate)
					value = value.TruncateTo(size);
				else if (size < value.Length)
					throw new ArgumentException($"String value of SqlParameter {parameterName} is {value.Length} character length that exceeds size of varchar({size}) type and would be truncated. CommandType: {cmd.CommandType}. CommandText: {cmd.CommandText}.");
			}

			cmd.Parameters.Add(new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.VarChar,
				Size = size,
				Value = (object?)value ?? DBNull.Value,
			});
		}
	
		/// <summary>Adds an input <c>nvarchar(<paramref name="size"/>)</c> parameter.</summary>
		/// <remarks>When <paramref name="trimToNull"/>=<c>true</c>, the value is trimmed and an empty result is sent as <c>NULL</c>.
		/// After trimming, strings longer than <paramref name="size"/> are truncated when <paramref name="truncate"/>=<c>true</c>;
		/// otherwise an <see cref="ArgumentException"/> is thrown before adding the parameter.</remarks>
		public static void AddNVarcharParam(this SqlCommand cmd, string parameterName, int size, string? value, bool trimToNull = false, bool truncate = false)
		{
			if (trimToNull)
				value = value.TrimToNull();

			if (value != null)
			{
				if (truncate)
					value = value.TruncateTo(size);
				else if (size < value.Length)
					throw new ArgumentException($"String value of SqlParameter {parameterName} is {value.Length} character length that exceeds size of nvarchar({size}) type and would be truncated. CommandType: {cmd.CommandType}. CommandText: {cmd.CommandText}.");
			}
		
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.NVarChar, 
				Size = size,
				Value = (object?)value ?? DBNull.Value,
			});
		}


		/// <summary>Adds an input <c>varchar(max)</c> parameter.</summary>
		/// <remarks>When <paramref name="trimToNull"/>=<c>true</c>, the value is trimmed and an empty result is sent as <c>NULL</c>.</remarks>
		public static void AddVarcharMaxParam(this SqlCommand cmd, string parameterName, string? value, bool trimToNull = false)
		{
			if (trimToNull)
				value = value.TrimToNull();

			cmd.Parameters.Add(new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.VarChar,
				Size = -1,
				Value = (object?)value ?? DBNull.Value,
			});
		}

		/// <summary>Adds an input <c>nvarchar(max)</c> parameter.</summary>
		/// <remarks>When <paramref name="trimToNull"/>=<c>true</c>, the value is trimmed and an empty result is sent as <c>NULL</c>.</remarks>
		public static void AddNVarcharMaxParam(this SqlCommand cmd, string parameterName, string? value, bool trimToNull = false)
		{
			if (trimToNull)
				value = value.TrimToNull();

			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.NVarChar, 
				Size = -1,
				Value = (object?)value ?? DBNull.Value,
			});
		}


		/// <summary>Adds an input <c>binary(<paramref name="size"/>)</c> parameter (fixed-length).</summary>
		public static void AddBinaryParam(this SqlCommand cmd, string parameterName, int size, byte[]? value)
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Binary, 
				Size = size, 
				Value = (object?)value ?? DBNull.Value
			});
		}

		/// <summary>Adds an input <c>varbinary(<paramref name="size"/>)</c> parameter (variable-length, capped at <paramref name="size"/>).</summary>
		public static void AddVarbinaryParam(this SqlCommand cmd, string parameterName, int size, byte[]? value)
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.VarBinary, 
				Size = size, 
				Value = (object?)value ?? DBNull.Value
			});
		}
	
		/// <summary>Adds an input <c>varbinary(max)</c> parameter — for arbitrary-length blobs.</summary>
		public static void AddVarbinaryMaxParam(this SqlCommand cmd, string parameterName, byte[]? value)
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.VarBinary, 
				Size = -1, 
				Value = (object?)value ?? DBNull.Value
			});
		}


		/// <summary>Adds an input <c>date</c> parameter (time component is dropped on the SQL side).</summary>
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

		/// <inheritdoc cref="AddDateParam(SqlCommand, string, DateTime)"/>
		public static void AddDateParam(this SqlCommand cmd, string parameterName, DateTime? value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Date, 
				Value = (object?)value ?? DBNull.Value
			});
		}
	

		/// <summary>Adds an input <c>time</c> parameter.</summary>
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

		/// <inheritdoc cref="AddTimeParam(SqlCommand, string, TimeSpan)"/>
		public static void AddTimeParam(this SqlCommand cmd, string parameterName, TimeSpan? value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Time, 
				Value = (object?)value ?? DBNull.Value
			});
		}


		/// <summary>Adds an input <c>smalldatetime</c> parameter (1-minute precision, 1900-2079 range).</summary>
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
	
		/// <inheritdoc cref="AddSmallDateTimeParam(SqlCommand, string, DateTime)"/>
		public static void AddSmallDateTimeParam(this SqlCommand cmd, string parameterName, DateTime? value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.SmallDateTime, 
				Value = (object?)value ?? DBNull.Value
			});
		}
	

		/// <summary>Adds an input <c>datetime</c> parameter (~3.33ms precision, 1753-9999 range).
		/// Prefer <see cref="AddDateTime2Param(SqlCommand, string, DateTime)"/> for new schemas.</summary>
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
	
		/// <inheritdoc cref="AddDateTimeParam(SqlCommand, string, DateTime)"/>
		public static void AddDateTimeParam(this SqlCommand cmd, string parameterName, DateTime? value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.DateTime, 
				Value = (object?)value ?? DBNull.Value
			});
		}
	

		/// <summary>Adds an input <c>datetime2(7)</c> parameter (100ns precision, 0001-9999 range).</summary>
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
	
		/// <inheritdoc cref="AddDateTime2Param(SqlCommand, string, DateTime)"/>
		public static void AddDateTime2Param(this SqlCommand cmd, string parameterName, DateTime? value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.DateTime2, 
				Value = (object?)value ?? DBNull.Value
			});
		}


		/// <summary>Adds an input <c>datetimeoffset(7)</c> parameter — preserves the time-zone offset.</summary>
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

		/// <inheritdoc cref="AddDateTimeOffsetParam(SqlCommand, string, DateTimeOffset)"/>
		public static void AddDateTimeOffsetParam(this SqlCommand cmd, string parameterName, DateTimeOffset? value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.DateTimeOffset, 
				Value = (object?)value ?? DBNull.Value
			});
		}
	

		/// <summary>Adds an input <c>uniqueidentifier</c> parameter.</summary>
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

		/// <inheritdoc cref="AddGuidParam(SqlCommand, string, Guid)"/>
		public static void AddGuidParam(this SqlCommand cmd, string parameterName, Guid? value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.UniqueIdentifier, 
				Value = (object?)value ?? DBNull.Value
			});
		}


		/// <summary>Adds an input <c>binary(8)</c> parameter holding a SQL Server <c>rowversion</c>/<c>timestamp</c> value (raw bytes).</summary>
		public static void AddRowVersionParam(this SqlCommand cmd, string parameterName, byte[] value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Binary, 
				Size = 8,
				Value = (object?)value ?? DBNull.Value
			});
		}

		/// <summary>Decodes a base64 string and adds it as a <c>binary(8)</c> rowversion parameter.</summary>
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

		/// <summary>Encodes the <see cref="long"/> as 8 bytes and adds it as a <c>binary(8)</c> rowversion parameter.</summary>
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

		/// <inheritdoc cref="AddRowVersionFromInt64Param(SqlCommand, string, long)"/>
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
	

		/// <summary>Adds an input <c>sql_variant</c> parameter — accepts any boxed value of a SQL-compatible type.</summary>
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
	
		/// <summary>Adds an input <c>xml</c> parameter from a serialised XML string.</summary>
		public static void AddXmlParam(this SqlCommand cmd, string parameterName, string value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Xml,
				Value = (object?)value ?? DBNull.Value
			});
		}


		/// <summary>Adds a structured (table-valued) parameter built from <paramref name="dataTable"/>.
		/// The <see cref="DataTable.TableName"/> must match the user-defined SQL table type expected by the procedure.
		/// No-op when <paramref name="dataTable"/> is <c>null</c>.</summary>
		public static void AddTableParam(this SqlCommand cmd, string parameterName, DataTable? dataTable)
		{
			if (dataTable == null)
				return;

			var param = new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Structured,
				TypeName = dataTable.TableName,
				Value = dataTable
			};

			cmd.Parameters.Add(param);
		}

		/// <summary>Adds a TVP populated with <paramref name="ids"/> against the canonical
		/// <c>TinyIntIdTableType</c> (single-column <c>Id tinyint not null</c>).</summary>
		public static void AddTableParam(this SqlCommand cmd, string parameterName, IEnumerable<byte> ids)
		{
			cmd.AddTableParam(parameterName, ids?.ToTinyIntIdDataTable());
		}

		/// <summary>Adds a TVP populated with <paramref name="ids"/> against the canonical <c>SmallIntIdTableType</c>.</summary>
		public static void AddTableParam(this SqlCommand cmd, string parameterName, IEnumerable<short> ids)
		{
			cmd.AddTableParam(parameterName, ids.ToSmallIntIdDataTable());
		}

		/// <summary>
		/// <para>Convert <paramref name="ids"/> param to DataTable with name <c>ToIntIdDataTable</c> and <c>Id</c> column</para>
		/// <para>and add <paramref name="parameterName"/> SqlParameter to the <paramref name="cmd"/> SqlCommand</para>
		/// <para>Database must have the following user-defined table type:</para>
		/// <para><c>create type IntIdTableType as table (Id int not null primary key clustered)</c></para>
		/// </summary>
		/// <param name="cmd">SqlCommand</param>
		/// <param name="parameterName">The name of parameter in stored procedure or in SQL text</param>
		/// <param name="ids">Collection of int Ids</param>
		public static void AddTableParam(this SqlCommand cmd, string parameterName, IEnumerable<int> ids)
		{
			cmd.AddTableParam(parameterName, ids?.ToIntIdDataTable());
		}

		/// <summary>Adds a TVP populated with <paramref name="ids"/> against the canonical <c>BigIntIdTableType</c>.</summary>
		public static void AddTableParam(this SqlCommand cmd, string parameterName, IEnumerable<long> ids)
		{
			cmd.AddTableParam(parameterName, ids?.ToBigIntIdDataTable());
		}

		/// <summary>Adds a TVP built via the registered mapper for <typeparamref name="T"/>
		/// (<c>CreateDataTable</c>/<c>CreateDataRow</c>).</summary>
		/// <remarks>
		/// Requires three pieces wired together:
		/// <list type="number">
		/// <item>A user-defined SQL table type (e.g. <c>create type RecordTableType as table (Id int, Name nvarchar(30))</c>).</item>
		/// <item>A <c>[MapperFor(typeof(T))]</c> static class with <c>CreateDataTable</c> (its <see cref="DataTable.TableName"/>
		/// must equal the SQL type name) and <c>CreateDataRow(T)</c> producing an <c>object[]</c> in column order.</item>
		/// <item>A stored procedure parameter typed as <c>readonly</c> of that table type.</item>
		/// </list>
		/// </remarks>
		/// <example>
		/// <code><![CDATA[
		/// // 1) C# mapper:
		/// [MapperFor(typeof(Record), RequiredMethod.BothForDataTable)]
		/// public static class RecordMapper
		/// {
		///     public static DataTable CreateDataTable() => new DataTable("RecordTableType")
		///         .AddColumn<int>   ("Id")
		///         .AddColumn<string>("Name");
		///
		///     public static object[] CreateDataRow(Record r) => new object[] { r.Id, r.Name };
		/// }
		///
		/// // 2) Call site:
		/// using var cmd = repo.CreateCommand();
		/// cmd.UseProcedure("dbo.SaveRecords");
		/// cmd.AddTableParam("@Records", records);
		///
		/// // 3) T-SQL side:
		/// // create procedure dbo.SaveRecords @Records dbo.RecordTableType readonly as ...
		/// ]]></code>
		/// </example>
		public static void AddTableParam<T>(this SqlCommand cmd, string parameterName, IEnumerable<T> list)
		{
			cmd.AddTableParam(parameterName, list?.ToDataTable<T>());
		}

		/// <summary>Adds a TVP via auto-mapping (reflection-based). <paramref name="tableName"/> sets the
		/// SQL user-defined-table-type name, and <paramref name="columnNames"/> (comma- or semicolon-separated)
		/// selects which properties of <typeparamref name="T"/> become columns.</summary>
		public static void AddTableParam<T>(this SqlCommand cmd, string parameterName, IEnumerable<T> list, string tableName, string columnNames)
		{
			cmd.AddTableParam(parameterName, list?.AsDataTable(tableName, columnNames));
		}

		/// <summary>Adds a TVP built via the registered mapper, but overrides its <see cref="DataTable.TableName"/>
		/// with <paramref name="tableName"/> — useful when the same C# type maps to multiple SQL TVP types.</summary>
		public static void AddTableParam<T>(this SqlCommand cmd, string parameterName, IEnumerable<T> list, string tableName)
		{
			var dataTable = list?.ToDataTable<T>();
			if (dataTable != null)
				dataTable.TableName = tableName;
			cmd.AddTableParam(parameterName, dataTable);
		}

		/// <summary>Adds a single-row TVP holding <paramref name="id"/> — convenience for procedures that take
		/// a one-element <c>TinyIntIdTableType</c> instead of a scalar.</summary>
		public static void AddTableRowParam(this SqlCommand cmd, string parameterName, byte id)
		{
			var array = new byte[] { id };
			cmd.AddTableParam(parameterName, array);
		}

		/// <inheritdoc cref="AddTableRowParam(SqlCommand, string, byte)"/>
		public static void AddTableRowParam(this SqlCommand cmd, string parameterName, short id)
		{
			var array = new short[] { id };
			cmd.AddTableParam(parameterName, array);
		}

		/// <inheritdoc cref="AddTableRowParam(SqlCommand, string, byte)"/>
		public static void AddTableRowParam(this SqlCommand cmd, string parameterName, int id)
		{
			var array = new int[] { id };
			cmd.AddTableParam(parameterName, array);
		}

		/// <inheritdoc cref="AddTableRowParam(SqlCommand, string, byte)"/>
		public static void AddTableRowParam(this SqlCommand cmd, string parameterName, long id)
		{
			var array = new long[] { id };
			cmd.AddTableParam(parameterName, array);
		}


		/// <summary>Adds a single-row TVP from <paramref name="obj"/> using the registered mapper for <typeparamref name="T"/>.</summary>
		public static void AddTableRowParam<T>(this SqlCommand cmd, string parameterName, T obj)
		{
			var array = new T[] { obj };

			cmd.AddTableParam(parameterName, array);
		}

		/// <summary>Adds a single-row TVP via auto-mapping. <typeparamref name="T"/> must not implement <see cref="IEnumerable"/>.</summary>
		/// <exception cref="ArgumentException">Thrown when <typeparamref name="T"/> implements <see cref="IEnumerable"/>.</exception>
		public static void AddTableRowParam<T>(this SqlCommand cmd, string parameterName, T obj, string tableName, string columnNames)
		{
			if (typeof(IEnumerable).IsAssignableFrom(typeof(T)))
				throw new ArgumentException("Type T for AddTableRowParam must not implement IEnumerable interface.");

			var array = new T[] { obj };

			cmd.AddTableParam(parameterName, array.AsDataTable(tableName, columnNames));
		}


		/// <summary>Adds an output <c>bit</c> parameter; read its value via <c>cmd.Parameters[name].Value</c> after execute.</summary>
		public static void AddBitOutputParam(this SqlCommand cmd, string parameterName)
		{
			cmd.Parameters.Add(new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Output,
				SqlDbType = SqlDbType.Bit,
			});
		}

		/// <summary>Adds an output <c>tinyint</c> parameter.</summary>
		public static void AddTinyIntOutputParam(this SqlCommand cmd, string parameterName)
		{
			cmd.Parameters.Add(new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Output,
				SqlDbType = SqlDbType.TinyInt,
			});
		}

		/// <summary>Adds an output <c>smallint</c> parameter.</summary>
		public static void AddSmallIntOutputParam(this SqlCommand cmd, string parameterName)
		{
			cmd.Parameters.Add(new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Output,
				SqlDbType = SqlDbType.SmallInt,
			});
		}

		/// <summary>Adds an output <c>int</c> parameter.</summary>
		public static void AddIntOutputParam(this SqlCommand cmd, string parameterName)
		{
			cmd.Parameters.Add(new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Output,
				SqlDbType = SqlDbType.Int,
			});
		}

		/// <summary>Adds an output <c>bigint</c> parameter.</summary>
		public static void AddBigIntOutputParam(this SqlCommand cmd, string parameterName)
		{
			cmd.Parameters.Add(new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Output,
				SqlDbType = SqlDbType.BigInt,
			});
		}

		/// <summary>Adds an output <c>decimal(<paramref name="precision"/>, <paramref name="scale"/>)</c> parameter.</summary>
		public static void AddDecimalOutputParam(this SqlCommand cmd, string parameterName, byte precision, byte scale)
		{
			cmd.Parameters.Add(new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Output,
				SqlDbType = SqlDbType.Decimal,
				Precision = precision,
				Scale = scale,
			});
		}

		/// <summary>Adds an output <c>uniqueidentifier</c> parameter.</summary>
		public static void AddGuidOutputParam(this SqlCommand cmd, string parameterName)
		{
			cmd.Parameters.Add(new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Output,
				SqlDbType = SqlDbType.UniqueIdentifier,
			});
		}

		/// <summary>Adds an output <c>datetime2(7)</c> parameter.</summary>
		public static void AddDateTime2OutputParam(this SqlCommand cmd, string parameterName)
		{
			cmd.Parameters.Add(new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Output,
				SqlDbType = SqlDbType.DateTime2,
			});
		}

		/// <summary>Adds an output <c>datetimeoffset(7)</c> parameter.</summary>
		public static void AddDateTimeOffsetOutputParam(this SqlCommand cmd, string parameterName)
		{
			cmd.Parameters.Add(new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Output,
				SqlDbType = SqlDbType.DateTimeOffset,
			});
		}

		/// <summary>Adds an output <c>varchar(<paramref name="size"/>)</c> parameter.</summary>
		public static void AddVarcharOutputParam(this SqlCommand cmd, string parameterName, int size)
		{
			cmd.Parameters.Add( new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Output,
				SqlDbType = SqlDbType.VarChar,
				Size = size,
			});
		}

		/// <summary>Adds an output <c>nvarchar(<paramref name="size"/>)</c> parameter.</summary>
		public static void AddNVarcharOutputParam(this SqlCommand cmd, string parameterName, int size)
		{
			cmd.Parameters.Add( new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Output,
				SqlDbType = SqlDbType.NVarChar,
				Size = size,
			});
		}

#if NET6_0_OR_GREATER

		/// <summary>Adds an input <c>date</c> parameter from a <see cref="DateOnly"/> (NET6+).</summary>
		public static void AddDateParam(this SqlCommand cmd, string parameterName, DateOnly value)
		{
			cmd.AddDateParam(parameterName, value.ToDateTime(TimeOnly.MinValue));
		}

		/// <inheritdoc cref="AddDateParam(SqlCommand, string, DateOnly)"/>
		public static void AddDateParam(this SqlCommand cmd, string parameterName, DateOnly? value)
		{
			cmd.AddDateParam(parameterName, value.HasValue ? (DateTime?)value.Value.ToDateTime(TimeOnly.MinValue) : null);
		}

		/// <summary>Adds an input <c>time</c> parameter from a <see cref="TimeOnly"/> (NET6+).</summary>
		public static void AddTimeParam(this SqlCommand cmd, string parameterName, TimeOnly value)
		{
			cmd.AddTimeParam(parameterName, value.ToTimeSpan());
		}

		/// <inheritdoc cref="AddTimeParam(SqlCommand, string, TimeOnly)"/>
		public static void AddTimeParam(this SqlCommand cmd, string parameterName, TimeOnly? value)
		{
			cmd.AddTimeParam(parameterName, value.HasValue ? (TimeSpan?)value.Value.ToTimeSpan() : null);
		}

#endif


#if NET6_0_OR_GREATER

		/// <summary>
		/// Serializes <paramref name="value"/> to JSON (via <see cref="System.Text.Json.JsonSerializer"/>)
		/// and adds it as an <c>nvarchar(max)</c> parameter.
		/// Passes <c>NULL</c> when <paramref name="value"/> is <c>null</c>.
		/// </summary>
		/// <remarks>
		/// Useful for sending complex object graphs into a stored procedure without defining a TVP for each
		/// shape. The procedure parses the JSON server-side via <c>OPENJSON</c>. For <c>IEnumerable</c>-typed
		/// payloads consider <see cref="AddTableParam{T}(SqlCommand, string, IEnumerable{T})"/> instead — TVPs
		/// avoid the JSON parse cost and let SQL Server check column types at the boundary.
		/// </remarks>
		/// <example>
		/// <code><![CDATA[
		/// // C# side
		/// using var cmd = repo.CreateCommand();
		/// cmd.UseProcedure("dbo.UpsertUserPreferences");
		/// cmd.AddIntParam("@UserId", userId);
		/// cmd.AddJsonParam("@Preferences", new { Theme = "dark", FontSize = 14, Locale = "en-US" });
		/// cmd.ExecuteNonQuery();
		///
		/// // T-SQL side
		/// // create procedure dbo.UpsertUserPreferences
		/// //     @UserId      int,
		/// //     @Preferences nvarchar(max)
		/// // as begin
		/// //     declare @Theme nvarchar(50), @FontSize int, @Locale nvarchar(10);
		/// //     select @Theme = Theme, @FontSize = FontSize, @Locale = Locale
		/// //     from openjson(@Preferences)
		/// //     with (Theme nvarchar(50), FontSize int, Locale nvarchar(10));
		/// //     -- ...upsert...
		/// // end;
		/// ]]></code>
		/// </example>
		public static void AddJsonParam<T>(this SqlCommand cmd, string parameterName, T? value)
		{
			var json = value is null ? null : System.Text.Json.JsonSerializer.Serialize(value);
			cmd.AddNVarcharMaxParam(parameterName, json);
		}

#endif


		/// <summary>Returns the <c>@ReturnValue</c> parameter, adding it via <see cref="AddReturnValueParam(SqlCommand)"/>
		/// if not already present. Used internally by <c>Execute*</c> methods to expose the TSQL <c>RETURN</c> value.</summary>
		public static SqlParameter ReturnValueParam(this SqlCommand cmd)
		{
			if (!cmd.Parameters.Contains("@ReturnValue"))
				cmd.AddReturnValueParam();

			return cmd.Parameters["@ReturnValue"]!;  // Contains check above guarantees the parameter is present
		}

		/// <summary>Adds an output <c>@ReturnValue</c> parameter of type <c>int</c>. After execute, its <c>Value</c>
		/// holds the value supplied by TSQL <c>RETURN N;</c>, or <c>0</c> if no <c>RETURN</c> was hit.</summary>
		public static void AddReturnValueParam(this SqlCommand cmd)
		{
			var returnValueParam = new SqlParameter
			{
				ParameterName = "@ReturnValue",
				Direction = ParameterDirection.ReturnValue,
				SqlDbType = SqlDbType.Int,
			};

			cmd.Parameters.Add(returnValueParam);
		}

		/// <summary>Returns the <c>@ReturnValue</c> parameter if it was added previously, otherwise <c>null</c>.</summary>
		public static SqlParameter? GetReturnValueParam(this SqlCommand cmd)
		{
			return cmd.Parameters.Contains("@ReturnValue") ? cmd.Parameters["@ReturnValue"] : null;
		}

		/// <summary>Auto-discovers the procedure's parameter set via <see cref="SqlCommandBuilder.DeriveParameters(SqlCommand)"/>
		/// (cached for schema-qualified procedures), then fills each parameter from <paramref name="paramDictionary"/> by case-insensitive
		/// name match. Property keys may include or omit the leading <c>@</c>.</summary>
		/// <remarks>Useful when you don't want to hand-write a sequence of <c>Add*Param</c> calls — pass an anonymous-typed
		/// dictionary or one built from a model. Metadata is shared only for the same server, database, procedure,
		/// connection settings and SQL principal. Integrated and token-based authentication reuse metadata only
		/// on the same connection instance. Default-schema-dependent names and unknown databases are derived on each call.
		/// Passwords and tokens are not cached.</remarks>
		public static void AddParams(this SqlCommand cmd, Dictionary<string, object> paramDictionary)
		{
			var sqlParameters = MappingManager.GetProcedureParameters(cmd);

			if (sqlParameters == null)
			{
				var isConnectionClosed = true;
			
				try
				{
					isConnectionClosed = cmd.Connection.State == ConnectionState.Closed;

					if (isConnectionClosed)
						cmd.Connection.Open();

					SqlCommandBuilder.DeriveParameters(cmd);
				}
				finally
				{
					if (isConnectionClosed)
						cmd.Connection.Close();
				}

				cmd.Parameters.Remove(cmd.Parameters["@RETURN_VALUE"]);
			
				sqlParameters = new SqlParameter[cmd.Parameters.Count];
				cmd.Parameters.CopyTo(sqlParameters, 0);

				MappingManager.CacheProcedureParameters(cmd, sqlParameters);
			}
			else
			{
				cmd.Parameters.AddRange(sqlParameters);
			}

			foreach (SqlParameter sqlParameter in cmd.Parameters)
			{
				var key = paramDictionary.Keys.FirstOrDefault(k =>
					string.Equals(k.StartsWith("@", StringComparison.Ordinal) ? k.Substring(1) : k,
						sqlParameter.ParameterName.StartsWith("@", StringComparison.Ordinal) ? sqlParameter.ParameterName.Substring(1) : sqlParameter.ParameterName,
						StringComparison.OrdinalIgnoreCase)
				);

				if (key != null)
					sqlParameter.Value = paramDictionary[key] ?? DBNull.Value;
			}
		}

		internal static bool IsSqlText(string sql)
		{
			return (Regex.IsMatch(sql, @"\bselect\b|\binsert\b|\bupdate\b|\bdelete\b|\bmerge\b", RegexOptions.IgnoreCase));
		}

		internal static void ConfigureCommand(this SqlCommand cmd, string sql, params SqlParameter[] sqlParameters)
		{
			if(IsSqlText(sql))
				cmd.UseSql(sql);
			else
				cmd.UseProcedure(sql);

			foreach (var param in sqlParameters)
				cmd.Parameters.Add(param);
		}

		internal static void ConfigureCommand(this SqlCommand cmd, string sql, Action<SqlCommand> action)
		{
			if(IsSqlText(sql))
				cmd.UseSql(sql);
			else
				cmd.UseProcedure(sql);

			action(cmd);
		}

	}

}
