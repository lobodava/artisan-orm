using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Artisan.Orm
{
	public static class SqlDataReaderExtensions
	{
		
		#region [ Read extensions ] = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = 
		
		

		public static void Read(this SqlDataReader dr, Action<SqlDataReader> action, bool getNextResult = true) 
		{
			if (dr.Read()) 
				action(dr);

			if (getNextResult) dr.NextResult();
		}


		#region [ ReadTo, ReadAs ]

		private static T ReadToValue<T>(this SqlDataReader dr, bool getNextResult = true)
		{
			T obj;

			if (dr.Read())
				if (typeof(T).IsNullableValueType() && dr.IsDBNull(0))
					obj = default(T);
				else
					obj = GetValue<T>(dr);
			else
				obj = default(T);

			if (getNextResult) dr.NextResult();

			return obj;
		}


		public static T ReadTo<T>(this SqlDataReader dr, Func<SqlDataReader, T> createFunc, bool getNextResult = true) 
		{
			var obj = dr.Read() ? createFunc(dr) : default(T);

			if (getNextResult) dr.NextResult();

			return obj;
		}

		public static T ReadTo<T>(this SqlDataReader dr, bool getNextResult = true) 
		{
			if (typeof(T).IsSimpleType())
				return dr.ReadToValue<T>(getNextResult);

			return dr.ReadTo(MappingManager.GetCreateObjectFunc<T>(), getNextResult);
		}


		public static T ReadAs<T>(this SqlDataReader dr, bool getNextResult = true) 
		{
			if (typeof(T).IsSimpleType())
				return dr.ReadToValue<T>(getNextResult);

			return dr.ReadTo(CreateObject<T>, getNextResult);
		}

		#endregion
		

		#region [ ReadToList, ReadAsList, ReadToArray, ReadAsArray ]
		
		private static IList<T> ReadToListOfValues<T>(this SqlDataReader dr, IList<T> list, bool getNextResult = true)
		{
			if (list == null)
				list = new List<T>();

			var type = typeof(T);
			var isNullableValueType = type.IsNullableValueType();
			
			if (isNullableValueType)
			{
				var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
				while (dr.Read())
					list.Add(dr.IsDBNull(0) ? default(T) : GetValue<T>(dr, underlyingType));
			}
			else
			{
				while (dr.Read())
					list.Add(GetValue<T>(dr, type));
			}

			if (getNextResult) dr.NextResult();

			return list;
		}

		private static IList<T> ReadToListOfObjects<T>(this SqlDataReader dr, Func<SqlDataReader, T> createFunc, IList<T> list, bool getNextResult = true)
		{
			if (list == null)
				list = new List<T>();
			
			while (dr.Read()) 
				list.Add(createFunc(dr));

			if (getNextResult) dr.NextResult();

			return list;
		}
	

		public static IList<T> ReadToList<T>(this SqlDataReader dr, Func<SqlDataReader, T> createFunc, IList<T> list, bool getNextResult = true) 
		{
			if (list == null)
				list = new List<T>();

			var isNullableValueType = typeof(T).IsNullableValueType();

			while (dr.Read())
			{
				if (isNullableValueType && dr.IsDBNull(0))
					list.Add(default(T));
				else 
					list.Add(createFunc(dr));
			}

			if (getNextResult) dr.NextResult();

			return list;
		}

		public static IList<T> ReadToList<T>(this SqlDataReader dr, Func<SqlDataReader, T> createFunc, bool getNextResult = true) 
		{
			if (typeof(T).IsSimpleType())
				return dr.ReadToListOfValues<T>(null, getNextResult);

			return dr.ReadToListOfObjects<T>(createFunc, null, getNextResult);
		}

		public static IList<T> ReadToList<T>(this SqlDataReader dr, IList<T> list, bool getNextResult = true)
		{
			if (typeof(T).IsSimpleType())
				return dr.ReadToListOfValues<T>(list, getNextResult);

			return dr.ReadToListOfObjects<T>(MappingManager.GetCreateObjectFunc<T>(), list, getNextResult);
		}

		public static IList<T> ReadToList<T>(this SqlDataReader dr,  bool getNextResult = true)
		{
			if (typeof(T).IsSimpleType())
				return dr.ReadToListOfValues<T>(null, getNextResult);

			return dr.ReadToListOfObjects<T>(MappingManager.GetCreateObjectFunc<T>(), null, getNextResult);
		}


		public static IList<T> ReadAsList<T>(this SqlDataReader dr, IList<T> list, bool getNextResult = true)
		{
			if (list == null) 
				list = new List<T>();

			if (typeof(T).IsSimpleType())
				return dr.ReadToListOfValues<T>(list, getNextResult);


			var dict = new Dictionary<string, Tuple<PropertyInfo, Type>>();

			var properties = typeof(T)
				.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(p => p.CanWrite && p.PropertyType.IsSimpleType()).ToList();

			foreach (var property in properties)
			{
				var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
				dict.Add(property.Name, new Tuple<PropertyInfo, Type>(property, type));
			}

	
			while (dr.Read())
			{
				var item = Activator.CreateInstance<T>();
					
				for (var i = 0; i < dr.FieldCount; i++)
				{
					var columnName = dr.GetName(i);

					Tuple<PropertyInfo, Type> propTuple;

					if (dict.TryGetValue(columnName, out propTuple))
					{
						var value = dr.IsDBNull(i) ? null : Convert.ChangeType(dr.GetValue(i), propTuple.Item2);

						propTuple.Item1.SetValue(item, value, null);
					}
				}

				list.Add(item);
			}

			if (getNextResult) dr.NextResult();

			return list;
		}

		public static IList<T> ReadAsList<T>(this SqlDataReader dr,  bool getNextResult = true)
		{
			if (typeof(T).IsSimpleType())
				return dr.ReadToListOfValues<T>(null, getNextResult);
				
			return dr.ReadAsList<T>(null, getNextResult);
		}


		public static T[] ReadToArray<T>(this SqlDataReader dr, Func<SqlDataReader, T> createFunc, bool getNextResult = true) 
		{
			return dr.ReadToList<T>(createFunc, getNextResult).ToArray();
		}

		public static T[] ReadToArray<T>(this SqlDataReader dr, bool getNextResult = true)
		{
			return dr.ReadToList<T>(getNextResult).ToArray();
		}

		public static T[] ReadAsArray<T>(this SqlDataReader dr, bool getNextResult = true)
		{
			return dr.ReadAsList<T>(getNextResult).ToArray();
		}
		
		#endregion



		#region [ ReadToObjectRow(s), ReadAsObjectRow(s) ]

		public static ObjectRow ReadToObjectRow(this SqlDataReader dr, Func<SqlDataReader, ObjectRow> createFunc, bool getNextResult = true) 
		{
			var objectRow = dr.Read() ? createFunc(dr) : null;

			if (getNextResult) dr.NextResult();

			return objectRow;
		}

		public static ObjectRow ReadToObjectRow<T>(this SqlDataReader dr, bool getNextResult = true) 
		{
			return dr.ReadToObjectRow(MappingManager.GetCreateObjectRowFunc<T>(), getNextResult);
		}
		

		public static ObjectRows ReadToObjectRows(this SqlDataReader dr, Func<SqlDataReader, ObjectRow> createFunc, bool getNextResult = true) 
		{
			var objectRows = new ObjectRows();

			while (dr.Read())
				objectRows.Add(createFunc(dr));

			if (getNextResult) dr.NextResult();

			return objectRows;
		}
		
		public static ObjectRows ReadToObjectRows<T>(this SqlDataReader dr, bool getNextResult = true) 
		{
			return dr.ReadToObjectRows(MappingManager.GetCreateObjectRowFunc<T>(), getNextResult);
		}


		public static ObjectRow ReadAsObjectRow(this SqlDataReader dr, bool getNextResult = true) 
		{
			ObjectRow objectRow = null;

			if (dr.Read())
			{
				objectRow = new ObjectRow(dr.FieldCount);

				for (var i = 0; i < dr.FieldCount; i++)
					objectRow.Add(dr.GetValue(i));
			}

			if (getNextResult) dr.NextResult();

			return objectRow;
		}

		public static ObjectRows ReadAsObjectRows(this SqlDataReader dr, bool getNextResult = true) 
		{
			var objectRows = new ObjectRows();
		
			while (dr.Read())
			{
				var objectRow = new ObjectRow(dr.FieldCount);

				for (var i = 0; i < dr.FieldCount; i++)
					objectRow.Add(dr.GetValue(i));
				
				objectRows.Add(objectRow);
			}

			if (getNextResult) dr.NextResult();

			return objectRows;
		}
		
		#endregion

		#region [ ReadToDictionary ]

		public static Dictionary<TKey, TValue> ReadToDictionary<TKey,TValue>(this SqlDataReader dr,  bool getNextResult = true)
		{
			var dictionary = new Dictionary<TKey,TValue>();

			var type1 = typeof(TKey);
			var type2 = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
			
			while (dr.Read()) 
			{
				if (!dr.IsDBNull(0)) {
					var key = (TKey)Convert.ChangeType(dr.GetValue(0), type1);
					var value = (TValue)(dr.IsDBNull(1) ? null : Convert.ChangeType(dr.GetValue(1), type2));
					dictionary.Add(key, value);
				}
	
			}

			if (getNextResult) dr.NextResult();

			return dictionary;
		}

		public static Dictionary<TKey,TValue> ReadToDictionary<TKey,TValue>(this SqlDataReader dr, Func<SqlDataReader, TValue> createFunc,  bool getNextResult = true)
		{
			var dictionary = new Dictionary<TKey,TValue>();

			var type1 = typeof(TKey);

			while (dr.Read()) 
			{
				if (!dr.IsDBNull(0)) {
					var key = (TKey)Convert.ChangeType(dr.GetValue(0), type1);
					var value = createFunc(dr);
					dictionary.Add(key, value);
				}
	
			}

			if (getNextResult) dr.NextResult();

			return dictionary;
		}
		
		#endregion 
		

		#region [ GetValue, CreateObject ]
		
		internal static T GetValue<T>(SqlDataReader dr)
		{
			var underlyingType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
			
			return (T)Convert.ChangeType(dr.GetValue(0), underlyingType);
		}

		internal static T GetValue<T>(SqlDataReader dr, Type underlyingType)
		{
			return (T)Convert.ChangeType(dr.GetValue(0), underlyingType);
		}


		internal static T CreateObject<T>(SqlDataReader dr)
		{
			var obj = Activator.CreateInstance<T>();
			
			for (var i = 0; i < dr.FieldCount; i++)
			{
				var columnName = dr.GetName(i);

				var prop = obj.GetType().GetProperty(columnName, BindingFlags.Public | BindingFlags.Instance);

				if (prop != null && prop.CanWrite)
				{
					var t = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

					var value = dr.IsDBNull(i) ? null : Convert.ChangeType(dr.GetValue(i), t);

					prop.SetValue(obj, value, null);
				}
			}

			return obj;
		}
		
		#endregion 


		#endregion


		#region [ Get extensions ] = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = =

		public static bool? GetBooleanNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? (bool?)null : reader.GetBoolean(ordinal);
		}

		public static bool GetBoolean(this SqlDataReader reader, int ordinal, bool defaultValue)
		{
			return reader.IsDBNull(ordinal) ? defaultValue : reader.GetBoolean(ordinal);
		}

		public static byte? GetByteNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? (byte?)null : reader.GetByte(ordinal);
		}

		public static short? GetInt16Nullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? (short?)null : reader.GetInt16(ordinal);
		}
		
		public static int? GetInt32Nullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? (int?)null : reader.GetInt32(ordinal);
		}
		
		public static long? GetInt64Nullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? (long?)null : reader.GetInt64(ordinal);
		}

		public static float? GetFloatNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? (float?)null : reader.GetFloat(ordinal);
		}

		public static double? GetDoubleNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? (double?)null : reader.GetDouble(ordinal);
		}

		public static decimal? GetDecimalNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? (decimal?)null : reader.GetDecimal(ordinal);
		}

		public static decimal GetBigDecimal(this SqlDataReader reader, int ordinal)
		{
			return decimal.Parse(reader.GetSqlDecimal(ordinal).ToString(), CultureInfo.InvariantCulture);
		}

		public static decimal? GetBigDecimalNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? (decimal?)null : reader.GetBigDecimal(ordinal);
		}

		public static Char GetCharacter(this SqlDataReader reader, int ordinal)
		{
			var buffer = new char[1];
			reader.GetChars(ordinal, 0, buffer, 0, 1);
			return buffer[0];
		}

		public static Char? GetCharacterNullable(this SqlDataReader reader, int ordinal)
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
			return reader.IsDBNull(ordinal) ? (DateTime?)null : reader.GetDateTime(ordinal);
		}

		public static DateTimeOffset? GetDateTimeOffsetNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? (DateTimeOffset?)null : reader.GetDateTimeOffset(ordinal);
		}

		public static DateTime GetUtcDateTime(this SqlDataReader reader, int ordinal)
		{
			return DateTime.SpecifyKind(reader.GetDateTime(ordinal), DateTimeKind.Utc);
		}

		public static TimeSpan? GetTimeSpanNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? (TimeSpan?)null : reader.GetTimeSpan(ordinal);
		}
		
		public static Guid GetGuidFromString(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? Guid.Empty : Guid.Parse(reader.GetString(ordinal));
		}

		public static Guid? GetGuidFromStringNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? null : (Guid?)Guid.Parse(reader.GetString(ordinal));
		}

		public static Guid GetGuidFromString(this SqlDataReader reader, int ordinal, Guid defaultValue)
		{
			return reader.IsDBNull(ordinal) ? defaultValue : Guid.Parse(reader.GetString(ordinal));
		}
		
		public static Guid? GetGuidNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? null : (Guid?)reader.GetGuid(ordinal);
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

		public static Byte[] GetByteArrayFromString(this SqlDataReader reader, int ordinal)
		{
			if (reader.IsDBNull(ordinal))
				return new Byte[] {};

			var ids = reader.GetStringNullable(ordinal);

			if (String.IsNullOrWhiteSpace(ids))
				return new Byte[] {};

			return ids.Split(',').Select(s => Convert.ToByte(s)).ToArray();
		}

		public static Int16[] GetInt16ArrayFromString(this SqlDataReader reader, int ordinal)
		{
			if (reader.IsDBNull(ordinal))
				return new Int16[] {};

			var ids = reader.GetString(ordinal);

			if (String.IsNullOrWhiteSpace(ids))
				return new Int16[] {};

			return ids.Split(',').Select(s => Convert.ToInt16(s)).ToArray();
		}

		public static Int32[] GetInt32ArrayFromString(this SqlDataReader reader, int ordinal)
		{
			if (reader.IsDBNull(ordinal))
				return new Int32[] {};

			var ids = reader.GetString(ordinal);

			if (String.IsNullOrWhiteSpace(ids))
				return new Int32[] {};

			return ids.Split(',').Select(s => Convert.ToInt32(s)).ToArray();
		}
		

		#endregion

	}
}
