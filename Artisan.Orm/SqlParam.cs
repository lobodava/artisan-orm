using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;

namespace Artisan.Orm
{
    /// <summary>Creates typed SQL parameters for repository shortcuts and command parameter collections.</summary>
    /// <remarks>Each call creates a fresh, unattached parameter using the same conversion and validation
    /// as its corresponding Add*Param command helper. Scalar null values become DBNull.Value.
    /// A null TVP collection or DataTable produces null; repository parameter arrays omit null entries.
    /// Retain output parameters to read their values after command execution and reader disposal.
    /// Do not share mutable parameter instances between concurrent commands.</remarks>
    public static class SqlParam
    {
		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddBitParam(SqlCommand, string, bool)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter Bit(string parameterName, bool value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Bit,
				Value = value ? 1 : 0
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddBitParam(SqlCommand, string, Nullable{bool})"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter Bit(string parameterName, bool? value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Bit,
				Value = value == null ? DBNull.Value : (object)(value == true ? 1 : 0)
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddTinyIntParam(SqlCommand, string, byte)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter TinyInt(string parameterName, byte value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.TinyInt,
				Value = value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddTinyIntParam(SqlCommand, string, Nullable{byte})"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter TinyInt(string parameterName, byte? value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.TinyInt,
				Value = value == null ? DBNull.Value : (object)value.Value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddSmallIntParam(SqlCommand, string, short)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter SmallInt(string parameterName, short value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.SmallInt,
				Value = value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddSmallIntParam(SqlCommand, string, Nullable{short})"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter SmallInt(string parameterName, short? value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.SmallInt,
				Value = value == null ? DBNull.Value : (object)value.Value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddIntParam(SqlCommand, string, int)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter Int(string parameterName, int value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Int,
				Value = value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddIntParam(SqlCommand, string, Nullable{int})"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter Int(string parameterName, int? value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Int,
				Value = value == null ? DBNull.Value : (object)value.Value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddBigIntParam(SqlCommand, string, long)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter BigInt(string parameterName, long value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.BigInt,
				Value = value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddBigIntParam(SqlCommand, string, Nullable{long})"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter BigInt(string parameterName, long? value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.BigInt,
				Value = value == null ? DBNull.Value : (object)value.Value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddDecimalParam(SqlCommand, string, byte, byte, decimal, bool)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter Decimal(string parameterName, byte precision, byte scale, decimal value, bool truncateFraction = false)
		{
			return CreateDecimal(parameterName, precision, scale, value, truncateFraction, null);
		}

		internal static SqlParameter CreateDecimal(string parameterName, byte precision, byte scale, decimal value, bool truncateFraction, SqlCommand? command)
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
					throw new ArgumentException($"Fractional part of SqlParameter {parameterName} = {value} is longer than acceptable for SQL Server decimal({precision},{scale}) type.{GetCommandContext(command)}", nameof(value));
				value = truncated;
			}

			var integerPart = decimal.Truncate(value).ToString(CultureInfo.InvariantCulture).TrimStart('-').TrimStart('0');
			if (integerPart.Length > precision - scale)
				throw new ArgumentOutOfRangeException(nameof(value), value,
					$"Value of SqlParameter {parameterName} is outside the SQL Server decimal({precision},{scale}) range.{GetCommandContext(command)}");

			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Decimal,
				Scale = scale,
				Precision = precision,
				Value = value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddDecimalParam(SqlCommand, string, byte, byte, Nullable{decimal}, bool)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter Decimal(string parameterName, byte precision, byte scale, decimal? value, bool truncateFraction = false)
		{
			return CreateDecimal(parameterName, precision, scale, value, truncateFraction, null);
		}

		internal static SqlParameter CreateDecimal(string parameterName, byte precision, byte scale, decimal? value, bool truncateFraction, SqlCommand? command)
		{
			ValidateDecimalPrecisionAndScale(precision, scale);
			if (value != null)
				return CreateDecimal(parameterName, precision, scale, value.Value, truncateFraction, command);
			else
				return new SqlParameter
				{
					ParameterName = parameterName,
					Direction = ParameterDirection.Input,
					SqlDbType = SqlDbType.Decimal,
					Scale = scale,
					Precision = precision,
					Value = DBNull.Value
				};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddSmallMoneyParam(SqlCommand, string, decimal)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter SmallMoney(string parameterName, decimal value)
		{
			return CreateSmallMoney(parameterName, value, null);
		}

		internal static SqlParameter CreateSmallMoney(string parameterName, decimal value, SqlCommand? command)
		{
			if (value < -214748.3648m || 214748.3647m < value)
				throw new ArgumentOutOfRangeException($"Value of SqlParameter {parameterName} = {value} that is out of SQL Server smallmoney type range [-214748.3648, 214748.3647].{GetCommandContext(command)}");

			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.SmallMoney,
				Value = value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddSmallMoneyParam(SqlCommand, string, Nullable{decimal})"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter SmallMoney(string parameterName, decimal? value)
		{
			return CreateSmallMoney(parameterName, value, null);
		}

		internal static SqlParameter CreateSmallMoney(string parameterName, decimal? value, SqlCommand? command)
		{
			if (value != null)
				return CreateSmallMoney(parameterName, value.Value, command);
			else
				return new SqlParameter
				{
					ParameterName = parameterName,
					Direction = ParameterDirection.Input,
					SqlDbType = SqlDbType.SmallMoney,
					Value = DBNull.Value
				};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddMoneyParam(SqlCommand, string, decimal)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter Money(string parameterName, decimal value)
		{
			return CreateMoney(parameterName, value, null);
		}

		internal static SqlParameter CreateMoney(string parameterName, decimal value, SqlCommand? command)
		{
			if (value < -922337203685477.5808m || 922337203685477.5807m < value)
				throw new ArgumentOutOfRangeException($"Value of SqlParameter {parameterName} = {value} that is out of SQL Server money type range [-922337203685477.5808, 922337203685477.5807].{GetCommandContext(command)}");

			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Money,
				Value = value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddMoneyParam(SqlCommand, string, Nullable{decimal})"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter Money(string parameterName, decimal? value)
		{
			return CreateMoney(parameterName, value, null);
		}

		internal static SqlParameter CreateMoney(string parameterName, decimal? value, SqlCommand? command)
		{
			if (value != null)
				return CreateMoney(parameterName, value.Value, command);
			else
				return new SqlParameter
				{
					ParameterName = parameterName,
					Direction = ParameterDirection.Input,
					SqlDbType = SqlDbType.Money,
					Value = DBNull.Value
				};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddRealParam(SqlCommand, string, float)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter Real(string parameterName, float value)
		{
			return CreateReal(parameterName, value, null);
		}

		internal static SqlParameter CreateReal(string parameterName, float value, SqlCommand? command)
		{
			if (value < -3.40E+38f || 3.40E+38f < value)
				throw new ArgumentOutOfRangeException($"Value of SqlParameter {parameterName} = {value} that is out of SQL Server real type range [3.40E+38, 3.40E+38].{GetCommandContext(command)}");

			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Real,
				Value = value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddRealParam(SqlCommand, string, Nullable{float})"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter Real(string parameterName, float? value)
		{
			return CreateReal(parameterName, value, null);
		}

		internal static SqlParameter CreateReal(string parameterName, float? value, SqlCommand? command)
		{
			if (value != null)
				return CreateReal(parameterName, value.Value, command);
			else
				return new SqlParameter
				{
					ParameterName = parameterName,
					Direction = ParameterDirection.Input,
					SqlDbType = SqlDbType.Real,
					Value = DBNull.Value
				};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddFloatParam(SqlCommand, string, double)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter Float(string parameterName, double value)
		{
			return CreateFloat(parameterName, value, null);
		}

		internal static SqlParameter CreateFloat(string parameterName, double value, SqlCommand? command)
		{
			if (value < -1.79E+308d || 1.79E+308d < value)
				throw new ArgumentOutOfRangeException($"Value of SqlParameter {parameterName} = {value} that is out of SQL Server float type range [-1.79E+308, 1.79E+308].{GetCommandContext(command)}");

			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Float,
				Value = value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddFloatParam(SqlCommand, string, Nullable{double})"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter Float(string parameterName, double? value)
		{
			return CreateFloat(parameterName, value, null);
		}

		internal static SqlParameter CreateFloat(string parameterName, double? value, SqlCommand? command)
		{
			if (value != null)
				return CreateFloat(parameterName, value.Value, command);
			else
				return new SqlParameter
				{
					ParameterName = parameterName,
					Direction = ParameterDirection.Input,
					SqlDbType = SqlDbType.Float,
					Value = DBNull.Value
				};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddCharParam(SqlCommand, string, char)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter Char(string parameterName, char value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Char,
				Value = value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddCharParam(SqlCommand, string, Nullable{char})"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter Char(string parameterName, char? value)
		{
			if (value != null)
				return Char(parameterName, value.Value);
			else
				return new SqlParameter
				{
					ParameterName = parameterName,
					Direction = ParameterDirection.Input,
					SqlDbType = SqlDbType.Char,
					Value = DBNull.Value
				};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddNCharParam(SqlCommand, string, char)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter NChar(string parameterName, char value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.NChar,
				Value = value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddNCharParam(SqlCommand, string, Nullable{char})"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter NChar(string parameterName, char? value)
		{
			if (value != null)
				return NChar(parameterName, value.Value);
			else
				return new SqlParameter
				{
					ParameterName = parameterName,
					Direction = ParameterDirection.Input,
					SqlDbType = SqlDbType.NChar,
					Value = DBNull.Value
				};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddVarcharParam(SqlCommand, string, int, string, bool, bool)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter Varchar(string parameterName, int size, string? value, bool trimToNull = false, bool truncate = false)
		{
			return CreateVarchar(parameterName, size, value, trimToNull, truncate, null);
		}

		internal static SqlParameter CreateVarchar(string parameterName, int size, string? value, bool trimToNull, bool truncate, SqlCommand? command)
		{
			if (trimToNull)
				value = value.TrimToNull();

			if (value != null)
			{
				if (truncate)
					value = value.TruncateTo(size);
				else if (size < value.Length)
					throw new ArgumentException($"String value of SqlParameter {parameterName} is {value.Length} character length that exceeds size of varchar({size}) type and would be truncated.{GetCommandContext(command)}");
			}

			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.VarChar,
				Size = size,
				Value = (object?)value ?? DBNull.Value,
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddNVarcharParam(SqlCommand, string, int, string, bool, bool)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter NVarchar(string parameterName, int size, string? value, bool trimToNull = false, bool truncate = false)
		{
			return CreateNVarchar(parameterName, size, value, trimToNull, truncate, null);
		}

		internal static SqlParameter CreateNVarchar(string parameterName, int size, string? value, bool trimToNull, bool truncate, SqlCommand? command)
		{
			if (trimToNull)
				value = value.TrimToNull();

			if (value != null)
			{
				if (truncate)
					value = value.TruncateTo(size);
				else if (size < value.Length)
					throw new ArgumentException($"String value of SqlParameter {parameterName} is {value.Length} character length that exceeds size of nvarchar({size}) type and would be truncated.{GetCommandContext(command)}");
			}

			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.NVarChar,
				Size = size,
				Value = (object?)value ?? DBNull.Value,
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddVarcharMaxParam(SqlCommand, string, string, bool)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter VarcharMax(string parameterName, string? value, bool trimToNull = false)
		{
			if (trimToNull)
				value = value.TrimToNull();

			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.VarChar,
				Size = -1,
				Value = (object?)value ?? DBNull.Value,
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddNVarcharMaxParam(SqlCommand, string, string, bool)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter NVarcharMax(string parameterName, string? value, bool trimToNull = false)
		{
			if (trimToNull)
				value = value.TrimToNull();

			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.NVarChar,
				Size = -1,
				Value = (object?)value ?? DBNull.Value,
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddBinaryParam(SqlCommand, string, int, byte[])"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter Binary(string parameterName, int size, byte[]? value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Binary,
				Size = size,
				Value = (object?)value ?? DBNull.Value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddVarbinaryParam(SqlCommand, string, int, byte[])"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter Varbinary(string parameterName, int size, byte[]? value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.VarBinary,
				Size = size,
				Value = (object?)value ?? DBNull.Value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddVarbinaryMaxParam(SqlCommand, string, byte[])"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter VarbinaryMax(string parameterName, byte[]? value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.VarBinary,
				Size = -1,
				Value = (object?)value ?? DBNull.Value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddDateParam(SqlCommand, string, DateTime)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter Date(string parameterName, DateTime value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Date,
				Value = value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddDateParam(SqlCommand, string, Nullable{DateTime})"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter Date(string parameterName, DateTime? value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Date,
				Value = (object?)value ?? DBNull.Value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddTimeParam(SqlCommand, string, TimeSpan)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter Time(string parameterName, TimeSpan value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Time,
				Value = value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddTimeParam(SqlCommand, string, Nullable{TimeSpan})"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter Time(string parameterName, TimeSpan? value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Time,
				Value = (object?)value ?? DBNull.Value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddSmallDateTimeParam(SqlCommand, string, DateTime)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter SmallDateTime(string parameterName, DateTime value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.SmallDateTime,
				Value = value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddSmallDateTimeParam(SqlCommand, string, Nullable{DateTime})"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter SmallDateTime(string parameterName, DateTime? value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.SmallDateTime,
				Value = (object?)value ?? DBNull.Value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddDateTimeParam(SqlCommand, string, DateTime)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter DateTime(string parameterName, DateTime value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.DateTime,
				Value = value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddDateTimeParam(SqlCommand, string, Nullable{DateTime})"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter DateTime(string parameterName, DateTime? value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.DateTime,
				Value = (object?)value ?? DBNull.Value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddDateTime2Param(SqlCommand, string, DateTime)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter DateTime2(string parameterName, DateTime value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.DateTime2,
				Value = value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddDateTime2Param(SqlCommand, string, Nullable{DateTime})"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter DateTime2(string parameterName, DateTime? value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.DateTime2,
				Value = (object?)value ?? DBNull.Value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddDateTimeOffsetParam(SqlCommand, string, DateTimeOffset)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter DateTimeOffset(string parameterName, DateTimeOffset value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.DateTimeOffset,
				Value = value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddDateTimeOffsetParam(SqlCommand, string, Nullable{DateTimeOffset})"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter DateTimeOffset(string parameterName, DateTimeOffset? value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.DateTimeOffset,
				Value = (object?)value ?? DBNull.Value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddGuidParam(SqlCommand, string, Guid)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter Guid(string parameterName, Guid value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.UniqueIdentifier,
				Value = value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddGuidParam(SqlCommand, string, Nullable{Guid})"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter Guid(string parameterName, Guid? value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.UniqueIdentifier,
				Value = (object?)value ?? DBNull.Value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddRowVersionParam(SqlCommand, string, byte[])"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter RowVersion(string parameterName, byte[] value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Binary,
				Size = 8,
				Value = (object?)value ?? DBNull.Value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddRowVersionFromBase64StringParam(SqlCommand, string, string)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter RowVersionFromBase64String(string parameterName, string value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Binary,
				Size = 8,
				Value = (value == null) ? DBNull.Value : (object)Convert.FromBase64String(value)
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddRowVersionFromInt64Param(SqlCommand, string, long)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter RowVersionFromInt64(string parameterName, long value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Binary,
				Size = 8,
				Value = BitConverter.GetBytes(value)
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddRowVersionFromInt64Param(SqlCommand, string, Nullable{long})"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter RowVersionFromInt64(string parameterName, long? value)
		{
			if (value != null)
				return RowVersionFromInt64(parameterName, value.Value);
			else
				return new SqlParameter
				{
					ParameterName = parameterName,
					Direction = ParameterDirection.Input,
					SqlDbType = SqlDbType.Binary,
					Size = 8,
					Value = DBNull.Value
				};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddSqlVariantParam(SqlCommand, string, object)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter SqlVariant(string parameterName, object value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Variant,
				Value = value ?? DBNull.Value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddXmlParam(SqlCommand, string, string)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter Xml(string parameterName, string value)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Xml,
				Value = (object?)value ?? DBNull.Value
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddTableParam(SqlCommand, string, DataTable)"/>.</summary>
		/// <returns>A new parameter, or null when the corresponding add operation omits the TVP.</returns>
		public static SqlParameter? Table(string parameterName, DataTable? dataTable)
		{
			if (dataTable == null)
				return null;

			var param = new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Structured,
				TypeName = dataTable.TableName,
				Value = dataTable
			};

			return param;
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddTableParam(SqlCommand, string, IEnumerable{byte})"/>.</summary>
		/// <returns>A new parameter, or null when the corresponding add operation omits the TVP.</returns>
		public static SqlParameter? Table(string parameterName, IEnumerable<byte>? ids)
		{
			return Table(parameterName, ids?.ToTinyIntIdDataTable());
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddTableParam(SqlCommand, string, IEnumerable{short})"/>.</summary>
		/// <returns>A new parameter, or null when the corresponding add operation omits the TVP.</returns>
		public static SqlParameter? Table(string parameterName, IEnumerable<short>? ids)
		{
			return Table(parameterName, ids?.ToSmallIntIdDataTable());
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddTableParam(SqlCommand, string, IEnumerable{int})"/>.</summary>
		/// <returns>A new parameter, or null when the corresponding add operation omits the TVP.</returns>
		public static SqlParameter? Table(string parameterName, IEnumerable<int>? ids)
		{
			return Table(parameterName, ids?.ToIntIdDataTable());
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddTableParam(SqlCommand, string, IEnumerable{long})"/>.</summary>
		/// <returns>A new parameter, or null when the corresponding add operation omits the TVP.</returns>
		public static SqlParameter? Table(string parameterName, IEnumerable<long>? ids)
		{
			return Table(parameterName, ids?.ToBigIntIdDataTable());
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddTableParam{T}(SqlCommand, string, IEnumerable{T})"/>.</summary>
		/// <returns>A new parameter, or null when the corresponding add operation omits the TVP.</returns>
		public static SqlParameter? Table<T>(string parameterName, IEnumerable<T>? list)
		{
			return Table(parameterName, list?.ToDataTable<T>());
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddTableParam{T}(SqlCommand, string, IEnumerable{T}, string, string)"/>.</summary>
		/// <returns>A new parameter, or null when the corresponding add operation omits the TVP.</returns>
		public static SqlParameter? Table<T>(string parameterName, IEnumerable<T>? list, string tableName, string columnNames)
		{
			return Table(parameterName, list?.AsDataTable(tableName, columnNames));
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddTableParam{T}(SqlCommand, string, IEnumerable{T}, string)"/>.</summary>
		/// <returns>A new parameter, or null when the corresponding add operation omits the TVP.</returns>
		public static SqlParameter? Table<T>(string parameterName, IEnumerable<T>? list, string tableName)
		{
			var dataTable = list?.ToDataTable<T>();
			if (dataTable != null)
				dataTable.TableName = tableName;
			return Table(parameterName, dataTable);
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddTableRowParam(SqlCommand, string, byte)"/>.</summary>
		/// <returns>A new parameter, or null when the corresponding add operation omits the TVP.</returns>
		public static SqlParameter? TableRow(string parameterName, byte id)
		{
			var array = new byte[] { id };
			return Table(parameterName, array);
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddTableRowParam(SqlCommand, string, short)"/>.</summary>
		/// <returns>A new parameter, or null when the corresponding add operation omits the TVP.</returns>
		public static SqlParameter? TableRow(string parameterName, short id)
		{
			var array = new short[] { id };
			return Table(parameterName, array);
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddTableRowParam(SqlCommand, string, int)"/>.</summary>
		/// <returns>A new parameter, or null when the corresponding add operation omits the TVP.</returns>
		public static SqlParameter? TableRow(string parameterName, int id)
		{
			var array = new int[] { id };
			return Table(parameterName, array);
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddTableRowParam(SqlCommand, string, long)"/>.</summary>
		/// <returns>A new parameter, or null when the corresponding add operation omits the TVP.</returns>
		public static SqlParameter? TableRow(string parameterName, long id)
		{
			var array = new long[] { id };
			return Table(parameterName, array);
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddTableRowParam{T}(SqlCommand, string, T)"/>.</summary>
		/// <returns>A new parameter, or null when the corresponding add operation omits the TVP.</returns>
		public static SqlParameter? TableRow<T>(string parameterName, T obj)
		{
			var array = new T[] { obj };

			return Table(parameterName, array);
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddTableRowParam{T}(SqlCommand, string, T, string, string)"/>.</summary>
		/// <returns>A new parameter, or null when the corresponding add operation omits the TVP.</returns>
		public static SqlParameter? TableRow<T>(string parameterName, T obj, string tableName, string columnNames)
		{
			if (typeof(IEnumerable).IsAssignableFrom(typeof(T)))
				throw new ArgumentException("Type T for AddTableRowParam must not implement IEnumerable interface.");

			var array = new T[] { obj };

			return Table(parameterName, array.AsDataTable(tableName, columnNames));
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddBitOutputParam(SqlCommand, string)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter BitOutput(string parameterName)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Output,
				SqlDbType = SqlDbType.Bit,
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddTinyIntOutputParam(SqlCommand, string)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter TinyIntOutput(string parameterName)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Output,
				SqlDbType = SqlDbType.TinyInt,
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddSmallIntOutputParam(SqlCommand, string)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter SmallIntOutput(string parameterName)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Output,
				SqlDbType = SqlDbType.SmallInt,
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddIntOutputParam(SqlCommand, string)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter IntOutput(string parameterName)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Output,
				SqlDbType = SqlDbType.Int,
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddBigIntOutputParam(SqlCommand, string)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter BigIntOutput(string parameterName)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Output,
				SqlDbType = SqlDbType.BigInt,
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddDecimalOutputParam(SqlCommand, string, byte, byte)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter DecimalOutput(string parameterName, byte precision, byte scale)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Output,
				SqlDbType = SqlDbType.Decimal,
				Precision = precision,
				Scale = scale,
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddGuidOutputParam(SqlCommand, string)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter GuidOutput(string parameterName)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Output,
				SqlDbType = SqlDbType.UniqueIdentifier,
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddDateTime2OutputParam(SqlCommand, string)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter DateTime2Output(string parameterName)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Output,
				SqlDbType = SqlDbType.DateTime2,
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddDateTimeOffsetOutputParam(SqlCommand, string)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter DateTimeOffsetOutput(string parameterName)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Output,
				SqlDbType = SqlDbType.DateTimeOffset,
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddVarcharOutputParam(SqlCommand, string, int)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter VarcharOutput(string parameterName, int size)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Output,
				SqlDbType = SqlDbType.VarChar,
				Size = size,
			};
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddNVarcharOutputParam(SqlCommand, string, int)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter NVarcharOutput(string parameterName, int size)
		{
			return new SqlParameter
			{
				ParameterName = parameterName,
				Direction = ParameterDirection.Output,
				SqlDbType = SqlDbType.NVarChar,
				Size = size,
			};
		}

#if NET6_0_OR_GREATER
		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddDateParam(SqlCommand, string, DateOnly)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter Date(string parameterName, DateOnly value)
		{
			return Date(parameterName, value.ToDateTime(TimeOnly.MinValue));
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddDateParam(SqlCommand, string, Nullable{DateOnly})"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter Date(string parameterName, DateOnly? value)
		{
			return Date(parameterName, value.HasValue ? (DateTime?)value.Value.ToDateTime(TimeOnly.MinValue) : null);
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddTimeParam(SqlCommand, string, TimeOnly)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter Time(string parameterName, TimeOnly value)
		{
			return Time(parameterName, value.ToTimeSpan());
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddTimeParam(SqlCommand, string, Nullable{TimeOnly})"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter Time(string parameterName, TimeOnly? value)
		{
			return Time(parameterName, value.HasValue ? (TimeSpan?)value.Value.ToTimeSpan() : null);
		}

		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddJsonParam{T}(SqlCommand, string, T)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter Json<T>(string parameterName, T? value)
		{
			var json = value is null ? null : System.Text.Json.JsonSerializer.Serialize(value);
			return NVarcharMax(parameterName, json);
		}

#endif
		/// <summary>Creates a new unattached parameter corresponding to <see cref="SqlCommandExtensions.AddReturnValueParam(SqlCommand)"/>.</summary>
		/// <returns>A new parameter that has not been added to a command.</returns>
		public static SqlParameter ReturnValue()
		{
			var returnValueParam = new SqlParameter
			{
				ParameterName = "@ReturnValue",
				Direction = ParameterDirection.ReturnValue,
				SqlDbType = SqlDbType.Int,
			};

			return returnValueParam;
		}

		private static void ValidateDecimalPrecisionAndScale(byte precision, byte scale)
		{
			if (precision < 1 || precision > 38)
				throw new ArgumentOutOfRangeException(nameof(precision), precision, "SQL Server decimal precision must be between 1 and 38.");
			if (scale > precision)
				throw new ArgumentOutOfRangeException(nameof(scale), scale, "SQL Server decimal scale must not exceed precision.");
		}

		private static string GetCommandContext(SqlCommand? command)
		{
			return command == null ? string.Empty
				: $" CommandType: {command.CommandType}. CommandText: {command.CommandText}.";
		}
    }
}
