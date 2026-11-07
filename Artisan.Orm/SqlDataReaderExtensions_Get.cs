using System;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace Artisan.Orm
{ 

	public static partial class SqlDataReaderExtensions
	{
		internal static T GetValue<T>(this SqlDataReader reader)  
		{
			return reader.GetValue<T>(0);
		}

		/// <summary>Reads the value at <paramref name="ordinal"/> and converts it to <typeparamref name="T"/>
		/// via <see cref="Convert.ChangeType(object, Type)"/>. The non-null underlying type of <typeparamref name="T"/>
		/// is used, so this works for both value types and their <see cref="Nullable{T}"/> counterparts.
		/// Enums are converted through their underlying numeric type; unnamed numeric values are preserved.</summary>
		/// <exception cref="InvalidCastException">If the column value cannot be converted to <typeparamref name="T"/>.</exception>
		public static T GetValue<T>(this SqlDataReader reader, int ordinal)
		{
			var underlyingType = typeof(T).GetUnderlyingType();

			return (T)ConvertValue(reader.GetValue(ordinal), underlyingType);
		}

		internal static T GetValue<T>(SqlDataReader reader, Type underlyingType)
		{
			return (T)ConvertValue(reader.GetValue(0), underlyingType);
		}

		private static object ConvertValue(object value, Type underlyingType)
		{
			if (underlyingType.IsEnum)
				return Enum.ToObject(underlyingType, Convert.ChangeType(value, Enum.GetUnderlyingType(underlyingType), CultureInfo.InvariantCulture));
			return Convert.ChangeType(value, underlyingType);
		}

		/// <summary>Same as <see cref="GetValue{T}(SqlDataReader, int)"/> but returns <c>default</c>
		/// when the column is <c>NULL</c>.</summary>
		[return: MaybeNull]
		public static T GetValueNullable<T>(this SqlDataReader reader, int ordinal)
		{
			if (reader.IsDBNull(ordinal))
				return default;

			var underlyingType = typeof(T).GetUnderlyingType();
		
			return (T)ConvertValue(reader.GetValue(ordinal), underlyingType);
		}

	
		/// <summary>Reads the SQL <c>bit</c> at <paramref name="ordinal"/> as <see cref="bool"/>?, returning <c>null</c> for <c>NULL</c>.</summary>
		public static bool? GetBooleanNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(bool?) : reader.GetBoolean(ordinal);
		}

		/// <summary>Reads the SQL <c>bit</c> at <paramref name="ordinal"/>, falling back to <paramref name="defaultValue"/> for <c>NULL</c>.</summary>
		public static bool GetBoolean(this SqlDataReader reader, int ordinal, bool defaultValue)
		{
			return reader.IsDBNull(ordinal) ? defaultValue : reader.GetBoolean(ordinal);
		}

		/// <summary>Reads the SQL <c>tinyint</c> at <paramref name="ordinal"/> as <see cref="byte"/>?, returning <c>null</c> for <c>NULL</c>.</summary>
		public static byte? GetByteNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(byte?) : reader.GetByte(ordinal);
		}

		/// <summary>Reads the SQL <c>tinyint</c> at <paramref name="ordinal"/>, falling back to <paramref name="defaultValue"/> for <c>NULL</c>.</summary>
		public static byte GetByte(this SqlDataReader reader, int ordinal, byte defaultValue)
		{
			return reader.IsDBNull(ordinal) ? defaultValue : reader.GetByte(ordinal);
		}

		/// <summary>Reads the SQL <c>smallint</c> at <paramref name="ordinal"/> as <see cref="short"/>?, returning <c>null</c> for <c>NULL</c>.</summary>
		public static short? GetInt16Nullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(short?) : reader.GetInt16(ordinal);
		}

		/// <summary>Reads the SQL <c>smallint</c> at <paramref name="ordinal"/>, falling back to <paramref name="defaultValue"/> for <c>NULL</c>.</summary>
		public static short GetInt16(this SqlDataReader reader, int ordinal, short defaultValue)
		{
			return reader.IsDBNull(ordinal) ? defaultValue : reader.GetInt16(ordinal);
		}

		/// <summary>Reads the SQL <c>int</c> at <paramref name="ordinal"/> as <see cref="int"/>?, returning <c>null</c> for <c>NULL</c>.</summary>
		public static int? GetInt32Nullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(int?) : reader.GetInt32(ordinal);
		}

		/// <summary>Reads the SQL <c>int</c> at <paramref name="ordinal"/>, falling back to <paramref name="defaultValue"/> for <c>NULL</c>.</summary>
		public static int GetInt32(this SqlDataReader reader, int ordinal, int defaultValue)
		{
			return reader.IsDBNull(ordinal) ? defaultValue : reader.GetInt32(ordinal);
		}

		/// <summary>Reads the SQL <c>bigint</c> at <paramref name="ordinal"/> as <see cref="long"/>?, returning <c>null</c> for <c>NULL</c>.</summary>
		public static long? GetInt64Nullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(long?) : reader.GetInt64(ordinal);
		}

		/// <summary>Reads the SQL <c>bigint</c> at <paramref name="ordinal"/>, falling back to <paramref name="defaultValue"/> for <c>NULL</c>.</summary>
		public static long GetInt64(this SqlDataReader reader, int ordinal, long defaultValue)
		{
			return reader.IsDBNull(ordinal) ? defaultValue : reader.GetInt64(ordinal);
		}

		/// <summary>Reads the SQL <c>real</c> at <paramref name="ordinal"/> as <see cref="float"/>?, returning <c>null</c> for <c>NULL</c>.</summary>
		public static float? GetFloatNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(float?) : reader.GetFloat(ordinal);
		}

		/// <summary>Reads the SQL <c>real</c> at <paramref name="ordinal"/>, falling back to <paramref name="defaultValue"/> for <c>NULL</c>.</summary>
		public static float GetFloat(this SqlDataReader reader, int ordinal, float defaultValue)
		{
			return reader.IsDBNull(ordinal) ? defaultValue : reader.GetFloat(ordinal);
		}

		/// <summary>Reads the SQL <c>float</c> at <paramref name="ordinal"/> as <see cref="double"/>?, returning <c>null</c> for <c>NULL</c>.</summary>
		public static double? GetDoubleNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(double?) : reader.GetDouble(ordinal);
		}

		/// <summary>Reads the SQL <c>float</c> at <paramref name="ordinal"/>, falling back to <paramref name="defaultValue"/> for <c>NULL</c>.</summary>
		public static double GetDouble(this SqlDataReader reader, int ordinal, double defaultValue)
		{
			return reader.IsDBNull(ordinal) ? defaultValue : reader.GetDouble(ordinal);
		}

		/// <summary>Reads the SQL <c>decimal</c>/<c>money</c> at <paramref name="ordinal"/> as <see cref="decimal"/>?, returning <c>null</c> for <c>NULL</c>.</summary>
		public static decimal? GetDecimalNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(decimal?) : reader.GetDecimal(ordinal);
		}

		/// <summary>Reads the SQL <c>decimal</c> at <paramref name="ordinal"/>, falling back to <paramref name="defaultValue"/> for <c>NULL</c>.</summary>
		public static decimal GetDecimal(this SqlDataReader reader, int ordinal, decimal defaultValue)
		{
			return reader.IsDBNull(ordinal) ? defaultValue : reader.GetDecimal(ordinal);
		}

		/// <summary>Reads a wide SQL <c>decimal(p,s)</c> via <see cref="SqlDataReader.GetSqlDecimal"/> and culture-invariant
		/// string parsing, allowing values that fall outside the .NET <see cref="decimal"/> range that <see cref="GetDecimal(SqlDataReader, int, decimal)"/>
		/// would otherwise reject. Throws if the value still doesn't fit a <see cref="decimal"/>.</summary>
		public static decimal GetBigDecimal(this SqlDataReader reader, int ordinal)
		{
			return decimal.Parse(reader.GetSqlDecimal(ordinal).ToString(), CultureInfo.InvariantCulture);
		}

		/// <inheritdoc cref="GetBigDecimal(SqlDataReader, int)"/>
		public static decimal? GetBigDecimalNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(decimal?) : reader.GetBigDecimal(ordinal);
		}

		/// <summary>Reads a single <see cref="char"/> from a <c>char(1)</c>/<c>nchar(1)</c> column at <paramref name="ordinal"/>.</summary>
		public static char GetCharacter(this SqlDataReader reader, int ordinal)
		{
			var buffer = new char[1];
			reader.GetChars(ordinal, 0, buffer, 0, 1);
			return buffer[0];
		}

		/// <summary>Reads a single <see cref="char"/>?, returning <c>null</c> for <c>NULL</c>.</summary>
		public static char? GetCharacterNullable(this SqlDataReader reader, int ordinal)
		{
			if (reader.IsDBNull(ordinal))
				return null;

			return reader.GetCharacter(ordinal);
		}

		/// <summary>Reads the SQL <c>(n)varchar</c>/<c>(n)char</c> at <paramref name="ordinal"/>, returning <c>null</c> for <c>NULL</c>.</summary>
		public static string? GetStringNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
		}

		/// <summary>Reads a string, falling back to <paramref name="defaultValue"/> for <c>NULL</c>.</summary>
		public static string GetString(this SqlDataReader reader, int ordinal, string defaultValue)
		{
			return reader.IsDBNull(ordinal) ? defaultValue : reader.GetString(ordinal);
		}

		/// <summary>Reads the SQL <c>datetime</c>/<c>datetime2</c> at <paramref name="ordinal"/> as <see cref="DateTime"/>?, returning <c>null</c> for <c>NULL</c>.</summary>
		public static DateTime? GetDateTimeNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(DateTime?) : reader.GetDateTime(ordinal);
		}

		/// <summary>Reads a <see cref="DateTime"/>, falling back to <paramref name="defaultValue"/> for <c>NULL</c>.</summary>
		public static DateTime GetDateTime(this SqlDataReader reader, int ordinal, DateTime defaultValue)
		{
			return reader.IsDBNull(ordinal) ? defaultValue : reader.GetDateTime(ordinal);
		}

		/// <summary>Reads the SQL <c>datetimeoffset</c> at <paramref name="ordinal"/> as <see cref="DateTimeOffset"/>?, returning <c>null</c> for <c>NULL</c>.</summary>
		public static DateTimeOffset? GetDateTimeOffsetNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(DateTimeOffset?) : reader.GetDateTimeOffset(ordinal);
		}

		/// <summary>Reads a <see cref="DateTimeOffset"/>, falling back to <paramref name="defaultValue"/> for <c>NULL</c>.</summary>
		public static DateTimeOffset GetDateTimeOffset(this SqlDataReader reader, int ordinal, DateTimeOffset defaultValue)
		{
			return reader.IsDBNull(ordinal) ? defaultValue : reader.GetDateTimeOffset(ordinal);
		}

		/// <summary>Reads a <see cref="DateTime"/> and stamps its <see cref="DateTime.Kind"/> as <see cref="DateTimeKind.Utc"/>.
		/// Use when the database stores values that are known to be UTC but not annotated as such.</summary>
		public static DateTime GetUtcDateTime(this SqlDataReader reader, int ordinal)
		{
			return DateTime.SpecifyKind(reader.GetDateTime(ordinal), DateTimeKind.Utc);
		}

		/// <inheritdoc cref="GetUtcDateTime(SqlDataReader, int)"/>
		public static DateTime? GetUtcDateTimeNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(DateTime?) : DateTime.SpecifyKind(reader.GetDateTime(ordinal), DateTimeKind.Utc);
		}

		/// <summary>Reads the SQL <c>time</c> at <paramref name="ordinal"/> as <see cref="TimeSpan"/>?, returning <c>null</c> for <c>NULL</c>.</summary>
		public static TimeSpan? GetTimeSpanNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(TimeSpan?) : reader.GetTimeSpan(ordinal);
		}

		/// <summary>Reads a <see cref="TimeSpan"/>, falling back to <paramref name="defaultValue"/> for <c>NULL</c>.</summary>
		public static TimeSpan GetTimeSpan(this SqlDataReader reader, int ordinal, TimeSpan defaultValue)
		{
			return reader.IsDBNull(ordinal) ? defaultValue : reader.GetTimeSpan(ordinal);
		}
	
		/// <summary>Parses the textual GUID at <paramref name="ordinal"/> as <see cref="Guid"/>; returns <see cref="Guid.Empty"/> for <c>NULL</c>.
		/// Use when the column is a <c>(n)varchar</c> rather than a native <c>uniqueidentifier</c>.</summary>
		public static Guid GetGuidFromString(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? Guid.Empty : Guid.Parse(reader.GetString(ordinal));
		}

		/// <summary>Same as <see cref="GetGuidFromString(SqlDataReader, int)"/> but returns <c>null</c> for <c>NULL</c>.</summary>
		public static Guid? GetGuidFromStringNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(Guid?) : Guid.Parse(reader.GetString(ordinal));
		}

		/// <summary>Same as <see cref="GetGuidFromString(SqlDataReader, int)"/> but falls back to <paramref name="defaultValue"/> for <c>NULL</c>.</summary>
		public static Guid GetGuidFromString(this SqlDataReader reader, int ordinal, Guid defaultValue)
		{
			return reader.IsDBNull(ordinal) ? defaultValue : Guid.Parse(reader.GetString(ordinal));
		}

		/// <summary>Reads the SQL <c>uniqueidentifier</c> at <paramref name="ordinal"/> as <see cref="Guid"/>?, returning <c>null</c> for <c>NULL</c>.</summary>
		public static Guid? GetGuidNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(Guid?) : reader.GetGuid(ordinal);
		}

		/// <summary>Reads a <see cref="Guid"/>, falling back to <paramref name="defaultValue"/> for <c>NULL</c>.</summary>
		public static Guid GetGuid(this SqlDataReader reader, int ordinal, Guid defaultValue)
		{
			return reader.IsDBNull(ordinal) ? defaultValue : reader.GetGuid(ordinal);
		}

		/// <summary>Reads a SQL <c>rowversion</c>/<c>timestamp</c> column as the raw 8-byte array, or <c>null</c> for <c>NULL</c>.</summary>
		public static byte[]? GetBytesFromRowVersion(this SqlDataReader reader, int ordinal)
		{
			if (reader.IsDBNull(ordinal))
				return null;

			return (byte[])reader.GetValue(ordinal);
		}

		/// <summary>Reads a SQL <c>rowversion</c>/<c>timestamp</c> as a 64-bit integer (big-endian semantics preserved by SQL Server).</summary>
		public static long GetInt64FromRowVersion(this SqlDataReader reader, int ordinal)
		{
			return BitConverter.ToInt64((byte[])reader.GetValue(ordinal), 0);
		}

		/// <inheritdoc cref="GetInt64FromRowVersion(SqlDataReader, int)"/>
		public static long? GetInt64FromRowVersionNullable(this SqlDataReader reader, int ordinal)
		{
			if (reader.IsDBNull(ordinal))
				return null;

			return BitConverter.ToInt64((byte[])reader.GetValue(ordinal), 0);
		}

		/// <summary>Reads a SQL <c>rowversion</c>/<c>timestamp</c> and encodes it as a base64 string — useful for HTTP ETag-style concurrency tokens.</summary>
		public static string? GetBase64StringFromRowVersion(this SqlDataReader reader, int ordinal)
		{
			if (reader.IsDBNull(ordinal))
				return null;

			return Convert.ToBase64String((byte[])reader.GetValue(ordinal));
		}

		/// <summary>Reads a SQL <c>varbinary</c>/<c>varbinary(max)</c> as a byte array, returning <c>null</c> for <c>NULL</c>.</summary>
		public static byte[]? GetBytesNullable(this SqlDataReader reader, int ordinal)
		{
			if (reader.IsDBNull(ordinal))
				return null;

			return (byte[])reader.GetValue(ordinal);
		}

		/// <summary>Reads a comma-separated string of bytes (e.g. <c>"1,2,255"</c>) into a <see cref="byte"/>[].
		/// Returns an empty array for <c>NULL</c> or whitespace.</summary>
		public static byte[] GetByteArrayFromString(this SqlDataReader reader, int ordinal)
		{
			if (reader.IsDBNull(ordinal))
				return Array.Empty<byte>();

			var ids = reader.GetStringNullable(ordinal);

			if (string.IsNullOrWhiteSpace(ids))
				return Array.Empty<byte>();

			return ids.Split(',').Select(s => Convert.ToByte(s)).ToArray();
		}

		/// <summary>Reads a comma-separated string of values into a <see cref="short"/>[].</summary>
		public static short[] GetInt16ArrayFromString(this SqlDataReader reader, int ordinal)
		{
			if (reader.IsDBNull(ordinal))
				return Array.Empty<short>();

			var ids = reader.GetString(ordinal);

			if (string.IsNullOrWhiteSpace(ids))
				return Array.Empty<short>();

			return ids.Split(',').Select(s => Convert.ToInt16(s)).ToArray();
		}

		/// <summary>Reads a comma-separated string of values into an <see cref="int"/>[].</summary>
		public static int[] GetInt32ArrayFromString(this SqlDataReader reader, int ordinal)
		{
			if (reader.IsDBNull(ordinal))
				return Array.Empty<int>();

			var ids = reader.GetString(ordinal);

			if (string.IsNullOrWhiteSpace(ids))
				return Array.Empty<int>();

			return ids.Split(',').Select(s => Convert.ToInt32(s)).ToArray();
		}

		/// <summary>Reads the SQL <c>xml</c> at <paramref name="ordinal"/> as <see cref="SqlXml"/>?, returning <c>null</c> for <c>NULL</c>.</summary>
		public static SqlXml? GetSqlXmlNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? null : reader.GetSqlXml(ordinal);
		}

		/// <summary>Reads the SQL <c>xml</c>, falling back to <paramref name="defaultValue"/> for <c>NULL</c>.</summary>
		public static SqlXml GetSqlXml(this SqlDataReader reader, int ordinal, SqlXml defaultValue)
		{
			return reader.IsDBNull(ordinal) ? defaultValue : reader.GetSqlXml(ordinal);
		}

#if NET6_0_OR_GREATER

		/// <summary>Reads the SQL <c>date</c> at <paramref name="ordinal"/> as a .NET <see cref="DateOnly"/>.</summary>
		public static DateOnly GetDateOnly(this SqlDataReader reader, int ordinal)
		{
			return DateOnly.FromDateTime(reader.GetDateTime(ordinal));
		}

		/// <summary>Reads the SQL <c>date</c> as <see cref="DateOnly"/>?, returning <c>null</c> for <c>NULL</c>.</summary>
		public static DateOnly? GetDateOnlyNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(DateOnly?) : DateOnly.FromDateTime(reader.GetDateTime(ordinal));
		}

		/// <summary>Reads the SQL <c>date</c>, falling back to <paramref name="defaultValue"/> for <c>NULL</c>.</summary>
		public static DateOnly GetDateOnly(this SqlDataReader reader, int ordinal, DateOnly defaultValue)
		{
			return reader.IsDBNull(ordinal) ? defaultValue : DateOnly.FromDateTime(reader.GetDateTime(ordinal));
		}

		/// <summary>Reads the SQL <c>time</c> at <paramref name="ordinal"/> as a .NET <see cref="TimeOnly"/>.</summary>
		public static TimeOnly GetTimeOnly(this SqlDataReader reader, int ordinal)
		{
			return TimeOnly.FromTimeSpan(reader.GetTimeSpan(ordinal));
		}

		/// <summary>Reads the SQL <c>time</c> as <see cref="TimeOnly"/>?, returning <c>null</c> for <c>NULL</c>.</summary>
		public static TimeOnly? GetTimeOnlyNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(TimeOnly?) : TimeOnly.FromTimeSpan(reader.GetTimeSpan(ordinal));
		}

		/// <summary>Reads the SQL <c>time</c>, falling back to <paramref name="defaultValue"/> for <c>NULL</c>.</summary>
		public static TimeOnly GetTimeOnly(this SqlDataReader reader, int ordinal, TimeOnly defaultValue)
		{
			return reader.IsDBNull(ordinal) ? defaultValue : TimeOnly.FromTimeSpan(reader.GetTimeSpan(ordinal));
		}

#endif
	}

}
