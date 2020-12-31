using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;

namespace Artisan.Orm
{
	public static partial class SqlDataReaderExtensions
	{

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
					obj = dr.GetValue<T>();
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

		public static dynamic ReadDynamic(this SqlDataReader dr, bool getNextResult = true) 
		{
			var obj = dr.Read() ? dr.CreateDynamic() : null;

			if (getNextResult) dr.NextResult();

			return obj;
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
				var underlyingType = type.GetUnderlyingType();
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
			if (typeof(T).IsSimpleType())
				return dr.ReadToListOfValues<T>(list, getNextResult);
			
			var key = GetAutoCreateObjectFuncKey<T>(dr);
			var autoMappingFunc = MappingManager.GetAutoCreateObjectFunc<T>(key); 

			list = dr.ReadAsList(list, autoMappingFunc, key);

			if (getNextResult) dr.NextResult();

			return list;
		}

		internal static IList<T> ReadAsList<T>(this SqlDataReader dr, IList<T> list, Func<SqlDataReader, T> autoMappingFunc, string key)
		{
			if (list == null) 
				list = new List<T>();

			if (dr.Read())
			{
				if (autoMappingFunc == null)
				{
					autoMappingFunc = CreateAutoMappingFunc<T>(dr);
					MappingManager.AddAutoCreateObjectFunc(key, autoMappingFunc);
				}

				list.Add(autoMappingFunc(dr));
			}

			while (dr.Read())
			{
				list.Add(autoMappingFunc(dr));
			}

			return list;
		}

		public static IList<T> ReadAsList<T>(this SqlDataReader dr,  bool getNextResult = true)
		{
			if (typeof(T).IsSimpleType())
				return dr.ReadToListOfValues<T>(null, getNextResult);
				
			return dr.ReadAsList<T>(null, getNextResult);
		}

		public static IList<dynamic> ReadDynamicList(this SqlDataReader dr, IList<dynamic> list, bool getNextResult = true) 
		{
			if (list == null)
				list = new List<dynamic>();


			while (dr.Read())
			{
				list.Add(dr.CreateDynamic());
			}

			if (getNextResult) dr.NextResult();

			return list;
		}

		public static IList<dynamic> ReadDynamicList(this SqlDataReader dr, bool getNextResult = true) 
		{
			return dr.ReadDynamicList( null, getNextResult);
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

		public static dynamic[] ReadDynamicArray(this SqlDataReader dr, bool getNextResult = true)
		{
			return dr.ReadDynamicList(getNextResult).ToArray();
		}
		
		#endregion
		

		#region [ ReadToEnumerable, ReadAsEnumerable ]
		
		private static IEnumerable<T> ReadToEnumerableOfValues<T>(this SqlDataReader dr, bool getNextResult = true)
		{
			var type = typeof(T);
			var isNullableValueType = type.IsNullableValueType();
			
			if (isNullableValueType)
			{
				var underlyingType = type.GetUnderlyingType();
				while (dr.Read())
					yield return dr.IsDBNull(0) ? default(T) : GetValue<T>(dr, underlyingType);
			}
			else
			{
				while (dr.Read())
					yield return GetValue<T>(dr, type);
			}

			if (getNextResult) dr.NextResult();
		}

		private static IEnumerable<T> ReadToEnumerableOfObjects<T>(this SqlDataReader dr, Func<SqlDataReader, T> createFunc, bool getNextResult = true)
		{
			while (dr.Read()) 
				yield return createFunc(dr);

			if (getNextResult) dr.NextResult();
		}
		
		public static IEnumerable<T> ReadToEnumerable<T>(this SqlDataReader dr, Func<SqlDataReader, T> createFunc, bool getNextResult = true) 
		{
			if (typeof(T).IsSimpleType())
				return dr.ReadToEnumerableOfValues<T>(getNextResult);

			return dr.ReadToEnumerableOfObjects<T>(createFunc, getNextResult);
		}
		
		public static IEnumerable<T> ReadToEnumerable<T>(this SqlDataReader dr, bool getNextResult = true)
		{
			if (typeof(T).IsSimpleType())
				return dr.ReadToEnumerableOfValues<T>(getNextResult);

			return dr.ReadToEnumerableOfObjects<T>(MappingManager.GetCreateObjectFunc<T>(), getNextResult);
		}
		
		public static IEnumerable<T> ReadAsEnumerable<T>(this SqlDataReader dr, bool getNextResult = true)
		{
			if (typeof(T).IsSimpleType())
				return dr.ReadToEnumerableOfValues<T>(getNextResult);
			
			var key = GetAutoCreateObjectFuncKey<T>(dr);
			var autoMappingFunc = MappingManager.GetAutoCreateObjectFunc<T>(key); 

			return dr.ReadToEnumerableOfObjects<T>(autoMappingFunc, getNextResult);
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


		#region [ ReadToDictionary, ReadAsDictionary ]

		private static Dictionary<TKey, TValue> ReadToDictionaryOfValues<TKey, TValue>(SqlDataReader dr, Type underlyingTypeOfValue)
		{
			var dictionary = new Dictionary<TKey,TValue>();

			var keyType = typeof(TKey);

			while (dr.Read())
			{
				if (!dr.IsDBNull(0))
				{
					var key = (TKey) Convert.ChangeType(dr.GetValue(0), keyType);
					var value = (TValue) (dr.IsDBNull(1) ? null : Convert.ChangeType(dr.GetValue(1), underlyingTypeOfValue));
					dictionary.Add(key, value);
				}
			}

			return dictionary;
		}

		private static Dictionary<TKey, TObject> ReadToDictionaryOfObjects<TKey, TObject>(SqlDataReader dr, Func<SqlDataReader, TObject> createFunc)
		{
			var dictionary = new Dictionary<TKey,TObject>();

			var keyType = typeof(TKey);

			while (dr.Read())
			{
				if (!dr.IsDBNull(0))
				{
					var key = (TKey) Convert.ChangeType(dr.GetValue(0), keyType);
					var objectValue = createFunc(dr);
					dictionary.Add(key, objectValue);
				}
			}

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

		public static Dictionary<TKey, TValue> ReadToDictionary<TKey,TValue>(this SqlDataReader dr,  bool getNextResult = true)
		{
			Dictionary<TKey,TValue> dictionary;

			var underlyingType = typeof(TValue).GetUnderlyingType();
			
			if (underlyingType.IsSimpleType())
				dictionary = ReadToDictionaryOfValues<TKey, TValue>(dr, underlyingType);
			else
				dictionary = ReadToDictionaryOfObjects<TKey, TValue>(dr, MappingManager.GetCreateObjectFunc<TValue>());

			if (getNextResult) dr.NextResult();

			return dictionary;
		}

		public static Dictionary<TKey, TValue> ReadAsDictionary<TKey,TValue>(this SqlDataReader dr,  bool getNextResult = true)
		{
			Dictionary<TKey,TValue> dictionary;

			var underlyingType = typeof(TValue).GetUnderlyingType();
			
			if (underlyingType.IsSimpleType())
				dictionary = ReadToDictionaryOfValues<TKey, TValue>(dr, underlyingType);
			else
			{
				var key = GetAutoCreateObjectFuncKey<TValue>(dr);
				var autoMappingFunc = MappingManager.GetAutoCreateObjectFunc<TValue>(key);

				if (autoMappingFunc == null)
				{
					autoMappingFunc = CreateAutoMappingFunc<TValue>(dr);
					MappingManager.AddAutoCreateObjectFunc(key, autoMappingFunc);
				}

				dictionary = ReadToDictionaryOfObjects<TKey, TValue>(dr, autoMappingFunc);
			}

			if (getNextResult) dr.NextResult();

			return dictionary;
		}
		
		#endregion 
		

		#region [ ReadToTree, ReadToTreeList ]

		public static T ReadToTree<T>(this SqlDataReader dr, Func<SqlDataReader, T> createFunc, IList<T> list, bool getNextResult = true, bool hierarchicallySorted = false) where T: class, INode<T>
		{
			return dr.ReadToListOfObjects<T>(createFunc, list, getNextResult).ToTree(hierarchicallySorted);
		}
		
		public static T ReadToTree<T>(this SqlDataReader dr, Func<SqlDataReader, T> createFunc, bool getNextResult = true, bool hierarchicallySorted = false) where T: class, INode<T>
		{
			return dr.ReadToEnumerableOfObjects<T>(createFunc, getNextResult).ToTree(hierarchicallySorted);
		}

		public static T ReadToTree<T>(this SqlDataReader dr, bool getNextResult = true, bool hierarchicallySorted = false) where T: class, INode<T>
		{
			return dr.ReadToEnumerableOfObjects<T>(MappingManager.GetCreateObjectFunc<T>(), getNextResult).ToTree(hierarchicallySorted);
		}


		public static IList<T> ReadToTreeList<T>(this SqlDataReader dr, Func<SqlDataReader, T> createFunc, IList<T> list, bool getNextResult = true, bool hierarchicallySorted = false) where T: class, INode<T>
		{
			return dr.ReadToListOfObjects<T>(createFunc, list, getNextResult).ToTreeList(hierarchicallySorted);
		}
		
		public static IList<T> ReadToTreeList<T>(this SqlDataReader dr, Func<SqlDataReader, T> createFunc, bool getNextResult = true, bool hierarchicallySorted = false) where T: class, INode<T>
		{
			return dr.ReadToEnumerableOfObjects<T>(createFunc, getNextResult).ToTreeList(hierarchicallySorted);
		}

		public static IList<T> ReadToTreeList<T>(this SqlDataReader dr, bool getNextResult = true, bool hierarchicallySorted = false) where T: class, INode<T>
		{
			return dr.ReadToEnumerableOfObjects<T>(MappingManager.GetCreateObjectFunc<T>(), getNextResult).ToTreeList(hierarchicallySorted);
		}

		
		#endregion 

	}
}
