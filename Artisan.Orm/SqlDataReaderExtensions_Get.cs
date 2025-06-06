using System;
using System.Data.SqlTypes;
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

		public static T GetValue<T>(this SqlDataReader reader, int ordinal)  
		{
			var underlyingType = typeof(T).GetUnderlyingType();
		
			return (T)Convert.ChangeType(reader.GetValue(ordinal), underlyingType);
		}
	
		internal static T GetValue<T>(SqlDataReader reader, Type underlyingType)
		{
			return (T)Convert.ChangeType(reader.GetValue(0), underlyingType);
		}

		public static T GetValueNullable<T>(this SqlDataReader reader, int ordinal)  
		{
			if (reader.IsDBNull(ordinal))
				return default;

			var underlyingType = typeof(T).GetUnderlyingType();
		
			return (T)Convert.ChangeType(reader.GetValue(ordinal), underlyingType);
		}

	
		public static bool? GetBooleanNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(bool?) : reader.GetBoolean(ordinal);
		}

		public static bool GetBoolean(this SqlDataReader reader, int ordinal, bool defaultValue)
		{
			return reader.IsDBNull(ordinal) ? defaultValue : reader.GetBoolean(ordinal);
		}

		public static byte? GetByteNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(byte?) : reader.GetByte(ordinal);
		}

		public static short? GetInt16Nullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(short?) : reader.GetInt16(ordinal);
		}
	
		public static int? GetInt32Nullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(int?) : reader.GetInt32(ordinal);
		}
	
		public static long? GetInt64Nullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(long?) : reader.GetInt64(ordinal);
		}

		public static float? GetFloatNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(float?) : reader.GetFloat(ordinal);
		}

		public static double? GetDoubleNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(double?) : reader.GetDouble(ordinal);
		}

		public static decimal? GetDecimalNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(decimal?) : reader.GetDecimal(ordinal);
		}

		public static decimal GetBigDecimal(this SqlDataReader reader, int ordinal)
		{
			return decimal.Parse(reader.GetSqlDecimal(ordinal).ToString(), CultureInfo.InvariantCulture);
		}

		public static decimal? GetBigDecimalNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(decimal?) : reader.GetBigDecimal(ordinal);
		}

		public static char GetCharacter(this SqlDataReader reader, int ordinal)
		{
			var buffer = new char[1];
			reader.GetChars(ordinal, 0, buffer, 0, 1);
			return buffer[0];
		}

		public static char? GetCharacterNullable(this SqlDataReader reader, int ordinal)
		{	
			if (reader.IsDBNull(ordinal))
				return null;

			return reader.GetCharacter(ordinal);
		}

		public static string GetStringNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
		}

		public static DateTime? GetDateTimeNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(DateTime?) : reader.GetDateTime(ordinal);
		}

		public static DateTimeOffset? GetDateTimeOffsetNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(DateTimeOffset?) : reader.GetDateTimeOffset(ordinal);
		}

		public static DateTime GetUtcDateTime(this SqlDataReader reader, int ordinal)
		{
			return DateTime.SpecifyKind(reader.GetDateTime(ordinal), DateTimeKind.Utc);
		}

		public static TimeSpan? GetTimeSpanNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(TimeSpan?) : reader.GetTimeSpan(ordinal);
		}
	
		public static Guid GetGuidFromString(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? Guid.Empty : Guid.Parse(reader.GetString(ordinal));
		}

		public static Guid? GetGuidFromStringNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(Guid?) : Guid.Parse(reader.GetString(ordinal));
		}

		public static Guid GetGuidFromString(this SqlDataReader reader, int ordinal, Guid defaultValue)
		{
			return reader.IsDBNull(ordinal) ? defaultValue : Guid.Parse(reader.GetString(ordinal));
		}
	
		public static Guid? GetGuidNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default(Guid?) : reader.GetGuid(ordinal);
		}
	
		public static Guid GetGuid(this SqlDataReader reader, int ordinal, Guid defaultValue)
		{
			return reader.IsDBNull(ordinal) ? defaultValue : reader.GetGuid(ordinal);
		}

		public static byte[] GetBytesFromRowVersion(this SqlDataReader reader, int ordinal)
		{
			if (reader.IsDBNull(ordinal))
				return null;
		
			return (byte[])reader.GetValue(ordinal);
		}

		public static long GetInt64FromRowVersion(this SqlDataReader reader, int ordinal)
		{
			return BitConverter.ToInt64((byte[])reader.GetValue(ordinal), 0);
		}

		public static long? GetInt64FromRowVersionNullable(this SqlDataReader reader, int ordinal)
		{
			if (reader.IsDBNull(ordinal))
				return null;
		
			return BitConverter.ToInt64((byte[])reader.GetValue(ordinal), 0);
		}

		public static string GetBase64StringFromRowVersion(this SqlDataReader reader, int ordinal)
		{
			if (reader.IsDBNull(ordinal))
				return null;
		
			return (Convert.ToBase64String((byte[])reader.GetValue(ordinal)));
		}

		public static byte[] GetBytesNullable(this SqlDataReader reader, int ordinal)
		{
			if (reader.IsDBNull(ordinal))
				return null;
		
			return (byte[])reader.GetValue(ordinal);
		}

		public static byte[] GetByteArrayFromString(this SqlDataReader reader, int ordinal)
		{
			if (reader.IsDBNull(ordinal))
				return Array.Empty<byte>();

			var ids = reader.GetStringNullable(ordinal);

			if (string.IsNullOrWhiteSpace(ids))
				return Array.Empty<byte>();

			return ids.Split(',').Select(s => Convert.ToByte(s)).ToArray();
		}

		public static short[] GetInt16ArrayFromString(this SqlDataReader reader, int ordinal)
		{
			if (reader.IsDBNull(ordinal))
				return Array.Empty<short>();

			var ids = reader.GetString(ordinal);

			if (string.IsNullOrWhiteSpace(ids))
				return Array.Empty<short>();

			return ids.Split(',').Select(s => Convert.ToInt16(s)).ToArray();
		}

		public static int[] GetInt32ArrayFromString(this SqlDataReader reader, int ordinal)
		{
			if (reader.IsDBNull(ordinal))
				return Array.Empty<int>();

			var ids = reader.GetString(ordinal);

			if (string.IsNullOrWhiteSpace(ids))
				return Array.Empty<int>();

			return ids.Split(',').Select(s => Convert.ToInt32(s)).ToArray();
		}

		public static SqlXml GetSqlXmlNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? default : reader.GetSqlXml(ordinal);
		}
	
		public static SqlXml GetSqlXml(this SqlDataReader reader, int ordinal, SqlXml defaultValue)
		{
			return reader.IsDBNull(ordinal) ? defaultValue : reader.GetSqlXml(ordinal);
		}
	}

}
