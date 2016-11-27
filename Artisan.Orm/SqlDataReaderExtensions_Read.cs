using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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
			if (typeof(T).IsSimpleType())
				return dr.ReadToListOfValues<T>(list, getNextResult);
			
			var key = GetAutoMappingFuncKey<T>(dr);
			var autoMappingFunc = MappingManager.GetAutoMappingFunc<T>(key); 

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
					MappingManager.AddAutoMappingFunc(key, autoMappingFunc);
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
		
		public static T GetValue<T>(this SqlDataReader dr)
		{
			var underlyingType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
			
			return (T)Convert.ChangeType(dr.GetValue(0), underlyingType);
		}

		internal static T GetValue<T>(SqlDataReader dr, Type underlyingType)
		{
			return (T)Convert.ChangeType(dr.GetValue(0), underlyingType);
		}
		
		public static T CreateObject<T>(this SqlDataReader dr)
		{
			var key = GetAutoMappingFuncKey<T>(dr);
			
			var autoMappingFunc = MappingManager.GetAutoMappingFunc<T>(key); 

			return CreateObject(dr, autoMappingFunc, key);
		}

		internal static T CreateObject<T>(this SqlDataReader dr, Func<SqlDataReader, T> autoMappingFunc, string key)
		{
			if (autoMappingFunc == null)
			{
				autoMappingFunc = CreateAutoMappingFunc<T>(dr);
				MappingManager.AddAutoMappingFunc(key, autoMappingFunc);
			}

			return autoMappingFunc(dr);
		}

		#endregion

		#region [ private members ] 

		private static readonly PropertyInfo CommandProperty = typeof(SqlDataReader).GetProperties(BindingFlags.NonPublic | BindingFlags.Instance).First(p => p.Name == "Command");

		private static string GetAutoMappingFuncKey<T>(SqlDataReader dr)
		{
			var command = (SqlCommand)CommandProperty.GetValue(dr);
			return GetAutoMappingFuncKey<T>(command.CommandText);
		}

		internal static string GetAutoMappingFuncKey<T>(string commandText)
		{
			return $"{commandText}+{typeof(T).FullName}";
		}
		
		private static readonly Dictionary<Type, string> ReaderGetMethodNames = new Dictionary<Type, string>
		{
			{ typeof(Boolean)		 , "GetBoolean"			},
			{ typeof(Byte)			 , "GetByte"			},
			{ typeof(Int16)			 , "GetInt16"			},
			{ typeof(Int32)			 , "GetInt32"			},
			{ typeof(Int64)			 , "GetInt64"			},
			{ typeof(Single)		 , "GetFloat"			},
			{ typeof(Double)		 , "GetDouble"			},
			{ typeof(Decimal)		 , "GetDecimal"			},
			{ typeof(String)		 , "GetString"			},
			{ typeof(DateTime)		 , "GetDateTime"		},
			{ typeof(DateTimeOffset) , "GetDateTimeOffset"	},
			{ typeof(TimeSpan)		 , "GetTimeSpan"		},
			{ typeof(Guid)			 , "GetGuid"			}
		};

		private static MethodCallExpression GetTypedValueMethodCallExpression(Type type, ParameterExpression sqlDataReaderParam, ConstantExpression indexConst, out bool isDefaultGetValueMethod)
		{
			var nonNullableType = Nullable.GetUnderlyingType(type) ?? type;

			isDefaultGetValueMethod = false;

			string methodName;

			if(ReaderGetMethodNames.TryGetValue(nonNullableType, out methodName))
				return Expression.Call(
					sqlDataReaderParam, 
					typeof(SqlDataReader).GetMethod(methodName, new[] { typeof(int) }), 
					indexConst
				);

			if (nonNullableType == typeof(Char))
				return Expression.Call(
					null, 
					typeof(SqlDataReaderExtensions).GetMethod("GetCharacter", new[] { typeof(SqlDataReader), typeof(int) }), 
					sqlDataReaderParam,
					indexConst
				);

			isDefaultGetValueMethod = true;

			return  Expression.Call(
				null,
				typeof(Convert).GetMethod("ChangeType", new[] { typeof(object), typeof(Type) }),
				Expression.Call(
					sqlDataReaderParam,
					typeof(SqlDataReader).GetMethod("GetValue", new[] { typeof(int) }),
					indexConst
				),
				Expression.Constant(nonNullableType, typeof(Type))
			);

			//return Expression.Call(
			//	sqlDataReaderParam,
			//	typeof(SqlDataReader).GetMethod("GetValue", new[] { typeof(int) }),
			//	indexConst
			//);
		}
		
		internal static Func<SqlDataReader, T> CreateAutoMappingFunc<T>(SqlDataReader dr)
		{ 
			var properties = typeof(T)
				.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(p => p.CanWrite && p.PropertyType.IsSimpleType()).ToList();


			var memberBindings = new List<MemberBinding>();
			var readerParam = Expression.Parameter(typeof(SqlDataReader), "reader");

			for (var i = 0; i < dr.FieldCount; i++)
			{
				var columnName = dr.GetName(i);
				var prop = properties.FirstOrDefault(p => p.Name == columnName);
				
				if (prop != null)
				{
					var indexConst = Expression.Constant(i, typeof(int));
					
					bool isDefaultGetValueMethod;
					MethodCallExpression getTypedValueExp = GetTypedValueMethodCallExpression(prop.PropertyType, readerParam, indexConst, out isDefaultGetValueMethod);
					
					Expression getValueExp = null;

					if (prop.PropertyType.IsNullableValueType())
					{
						getValueExp = Expression.Condition (
							Expression.Call(
								readerParam,
								typeof(SqlDataReader).GetMethod("IsDBNull", new[] { typeof(int) }),
								indexConst
							),
			
							Expression.Default(prop.PropertyType),

							Expression.Convert(getTypedValueExp, prop.PropertyType)
						);


						//getTypedValueExp = Expression.Call(
						//	null,
						//	typeof(Convert).GetMethod("ChangeType", new[] { typeof(object), typeof(Type) }),
						//	Expression.Call(
						//		readerParam,
						//		typeof(SqlDataReader).GetMethod("GetValue", new[] { typeof(int) }),
						//		indexConst
						//	),
						//	nonNullableTypeConst
						//);



					}
					else if (isDefaultGetValueMethod)
					{
						getValueExp  = Expression.Convert(getTypedValueExp, prop.PropertyType);
					}
					else
					{
						getValueExp = getTypedValueExp;
					}
			
					var binding = Expression.Bind(prop, getValueExp);
					memberBindings.Add(binding);
				}
			}

			var ctor = Expression.New(typeof(T));
			var memberInit = Expression.MemberInit(ctor, memberBindings);

			return Expression.Lambda<Func<SqlDataReader, T>>(memberInit, readerParam).Compile();
		}
		
		#endregion
	}
}
/* Old implementations with reflection:
 
  
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
		
*/
 

/*	ChangeType method
 
	getTypedValueExp = Expression.Call(
		null,
		typeof(Convert).GetMethod("ChangeType", new[] { typeof(object), typeof(Type) }),
		Expression.Call(
			readerParam,
			typeof(SqlDataReader).GetMethod("GetValue", new[] { typeof(int) }),
			indexConst
		),
		nonNullableTypeConst
	);
*/
