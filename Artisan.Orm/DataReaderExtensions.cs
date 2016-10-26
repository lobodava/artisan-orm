using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Artisan.Orm
{
	public static class DataReaderExtensions
	{
		
		#region [ Read ENTITY extensions ] = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = 
		

		public static T ReadTo<T>(this SqlDataReader dr, bool getNextResult = true) 
		{
			if (typeof(T).IsValueType || typeof(T) == typeof(string))
				return dr.ReadTo(GetValue<T>, getNextResult);

			var createEntityFunc =  MappingManager.GetCreateEntityFunc<T>();

			return dr.ReadTo(createEntityFunc ?? CreateEntity<T>, getNextResult);
		}
		
		public static T ReadTo<T>(this SqlDataReader dr, Func<SqlDataReader, T> createFunc, bool getNextResult = true) 
		{
			var obj =  (dr != null && dr.Read()) ? createFunc(dr) : default(T);

			if (dr != null && getNextResult) dr.NextResult();

			return obj;
		}

		public static void ReadBy(this SqlDataReader dr, Action<SqlDataReader> action, bool getNextResult = true) 
		{
			if (dr != null && dr.Read()) 
				action(dr);

			if (dr != null && getNextResult) dr.NextResult();
		}

		
		public static IList<T> ReadToList<T>(this SqlDataReader dr, Func<SqlDataReader, T> createFunc, bool getNextResult = true) 
		{
			var list = new List<T>();
			var type = typeof(T);
			var isNullableScalar = type.IsValueType && Nullable.GetUnderlyingType(type) != null || type == typeof(String);

			if (dr != null)
				while (dr.Read()) 
				{
					if (isNullableScalar && dr.IsDBNull(0))
						list.Add(default(T));
					else if (!dr.IsDBNull(0))
						list.Add(createFunc(dr));
				}

			if (dr != null && getNextResult) dr.NextResult();
		
			return list;
		}

		public static IList<T> ReadToList<T>(this SqlDataReader dr,  bool getNextResult = true)
		{
			if (typeof(T).IsValueType || typeof(T) == typeof(string))
				return dr.ReadToList(GetValue<T>, getNextResult);

			var createEntityFunc =  MappingManager.GetCreateEntityFunc<T>();
				
			return dr.ReadToList(createEntityFunc ?? CreateEntity<T>, getNextResult);
		}

		public static Dictionary<T1,T2> ReadToDictionary<T1,T2>(this SqlDataReader dr,  bool getNextResult = true)
		{
			var dictionary = new Dictionary<T1,T2>();

			var type1 = typeof(T1);
			var type2 = Nullable.GetUnderlyingType(typeof(T2)) ?? typeof(T2);


			if (dr != null)
				while (dr.Read()) 
				{
					if (!dr.IsDBNull(0)) {
						var key = (T1)Convert.ChangeType(dr.GetValue(0), type1);
						var value = (T2)(dr.IsDBNull(1) ? null : Convert.ChangeType(dr.GetValue(1), type2));
						dictionary.Add(key, value);
					}
	
				}

			if (dr != null && getNextResult) dr.NextResult();

			return dictionary;
		}

		public static Dictionary<T1,T2> ReadToDictionary<T1,T2>(this SqlDataReader dr, Func<SqlDataReader, T2> createFunc,  bool getNextResult = true)
		{
			var dictionary = new Dictionary<T1,T2>();

			var type1 = typeof(T1);

			if (dr != null)
				while (dr.Read()) 
				{
					if (!dr.IsDBNull(0)) {
						var key = (T1)Convert.ChangeType(dr.GetValue(0), type1);
						var value = createFunc(dr);
						dictionary.Add(key, value);
					}
	
				}

			if (dr != null && getNextResult) dr.NextResult();

			return dictionary;
		}



		
		public static void ReadToList<T>(this SqlDataReader dr, Func<SqlDataReader, T> createFunc, IList<T> listToReadTo, bool getNextResult = true) 
		{
			var type = typeof(T);
			var isNullableScalar = type.IsValueType && Nullable.GetUnderlyingType(type) != null || type == typeof(String);

			if (dr != null)
				while (dr.Read())
				{
					if (isNullableScalar && dr.IsDBNull(0))
						listToReadTo.Add(default(T));
					else if (!dr.IsDBNull(0))
						listToReadTo.Add(createFunc(dr));
				}

			if (dr != null && getNextResult) dr.NextResult();
		}
	
		public static void ReadToList<T>(this SqlDataReader dr, IList<T> listToReadTo, bool getNextResult = true)
		{
			var createEntityFunc =  MappingManager.GetCreateEntityFunc<T>();

			dr.ReadToList(createEntityFunc ?? CreateEntity<T>, listToReadTo, getNextResult);
		}

		public static T[] ReadToArray<T>(this SqlDataReader dr, bool getNextResult = true)
		{
			if (typeof(T).IsValueType || typeof(T) == typeof(String))
				return dr.ReadToList(GetValue<T>, getNextResult).ToArray();

			var createEntityFunc =  MappingManager.GetCreateEntityFunc<T>();

			return dr.ReadToList(createEntityFunc ?? CreateEntity<T>, getNextResult).ToArray();
		}
		
		public static T[] ReadToArray<T>(this SqlDataReader dr, Func<SqlDataReader, T> createFunc, bool getNextResult = true) 
		{
			return ReadToList<T>(dr, createFunc, getNextResult).ToArray();
		}


		public static object[] ReadToRow(this SqlDataReader dr, Func<SqlDataReader, object[]> createFunc, bool getNextResult = true) 
		{
			object[] row = null;

			if (dr != null && dr.Read())
				row = createFunc(dr);

			if (dr != null && getNextResult) dr.NextResult();

			return row;
		}

		public static object[] ReadToRow(this SqlDataReader dr, bool getNextResult = true) 
		{
			object[] row = null;

			if (dr != null && dr.Read())
			{
				row = new object[dr.FieldCount];

				for (int i = 0; i < dr.FieldCount; i++)
				{
					row[i] = dr.GetValue(i);
				}
			}

			if (dr != null && getNextResult) dr.NextResult();

			return row;
		}


		public static Rows ReadToRows(this SqlDataReader dr, Func<SqlDataReader, object[]> createFunc, bool getNextResult = true) 
		{
			var rows = new Rows();

			if (dr != null)
				while (dr.Read())
				{
					rows.Add(createFunc(dr));
				}

			if (dr != null && getNextResult) dr.NextResult();

			return rows;
		}


		public static Rows ReadToRows<T>(this SqlDataReader dr, bool getNextResult = true) 
		{
			var createEntityRowFunc =  MappingManager.GetCreateEntityRowFunc<T>();

			if (createEntityRowFunc == null)
			{
				return dr.ReadToRows(getNextResult);
			}
			
			return dr.ReadToRows(createEntityRowFunc, getNextResult);
		}

		public static Rows ReadToRows(this SqlDataReader dr, bool getNextResult = true) 
		{
			var rows = new Rows();

			if (dr != null)
				while (dr.Read())
				{
					var row = new object[dr.FieldCount];

					for (int i = 0; i < dr.FieldCount; i++)
					{
						row[i] = dr.GetValue(i);
					}
				
					rows.Add(row);
				}

			if (dr != null && getNextResult) dr.NextResult();

			return rows;
		}



		
		internal static T GetValue<T>(SqlDataReader dr)
		{
			var type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
			
			return (T)Convert.ChangeType(dr.GetValue(0), type);
		}

		internal static T CreateEntity<T>(SqlDataReader dr)
		{
			var entity = Activator.CreateInstance<T>();
			
			for (var i = 0; i < dr.FieldCount; i++)
			{
				var columnName = dr.GetName(i);

				var prop = entity.GetType().GetProperty(columnName, BindingFlags.Public | BindingFlags.Instance);

				if (prop != null && prop.CanWrite)
				{
					var t = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

					var value = dr.IsDBNull(i) ? null : Convert.ChangeType(dr.GetValue(i), t);

					prop.SetValue(entity, value, null);
				}
				else
				{
					throw new DataException(
						$"There is no property of name '{columnName}' in the object of type {typeof (T).FullName} or this property is readonly.");
				}
			}

			return entity;
		}
		

		#endregion


		#region [ Get scalar property extensions ] = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = =
		

		public static Guid? GetGuidNullableFromString(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? null : (Guid?)Guid.Parse(reader.GetString(ordinal));
		}

		public static Guid GetGuidFromString(this SqlDataReader reader, int ordinal, Guid defaultValue)
		{
			return reader.IsDBNull(ordinal) ? defaultValue : Guid.Parse(reader.GetString(ordinal));
		}

		public static Guid GetGuidFromString(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? Guid.Empty : Guid.Parse(reader.GetString(ordinal));
		}

		public static Guid? GetGuidNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? null : (Guid?)reader.GetGuid(ordinal);
		}

		public static string GetStringNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
		}

		public static int? GetInt32Nullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? (int?)null : reader.GetInt32(ordinal);
		}

		public static short? GetInt16Nullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? (short?)null : reader.GetInt16(ordinal);
		}

		public static long? GetInt64Nullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? (long?)null : reader.GetInt64(ordinal);
		}

		public static byte? GetByteNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? (byte?)null : reader.GetByte(ordinal);
		}

		public static bool? GetBooleanNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? (bool?)null : reader.GetBoolean(ordinal);
		}

		public static Guid GetGuid(this SqlDataReader reader, int ordinal, Guid defaultValue)
		{
			return reader.IsDBNull(ordinal) ? defaultValue : reader.GetGuid(ordinal);
		}

		public static bool GetBoolean(this SqlDataReader reader, int ordinal, bool defaultValue)
		{
			return reader.IsDBNull(ordinal) ? defaultValue : reader.GetBoolean(ordinal);
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

		public static float? GetFloatNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? (float?)null : reader.GetFloat(ordinal);
		}

		public static double? GetDoubleNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? (double?)null : reader.GetDouble(ordinal);
		}

		public static DateTime? GetDateTimeNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? (DateTime?)null : reader.GetDateTime(ordinal);
		}

		public static DateTime GetUtcDateTime(this SqlDataReader reader, int ordinal)
		{
			return DateTime.SpecifyKind(reader.GetDateTime(ordinal), DateTimeKind.Utc);
		}

		public static TimeSpan? GetTimeSpanNullable(this SqlDataReader reader, int ordinal)
		{
			return reader.IsDBNull(ordinal) ? (TimeSpan?)null : reader.GetTimeSpan(ordinal);
		}
		
		public static string GetRowVersionToBase64String(this SqlDataReader reader, int ordinal)
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
