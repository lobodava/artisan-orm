using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;


namespace Artisan.Orm
{
	public static partial class SqlCommandExtensions
	{
		#region [ GetReaderFlagsAndOpenConnection ]

		private static CommandBehavior GetReaderFlagsAndOpenConnection(SqlCommand cmd, CommandBehavior defaultReaderFlag = CommandBehavior.Default)
		{
			var readerFlags = defaultReaderFlag;

			if (cmd.Connection.State == ConnectionState.Closed)
			{
				cmd.Connection.Open();

				if (defaultReaderFlag == CommandBehavior.Default)
					readerFlags = CommandBehavior.CloseConnection;
				else
					readerFlags = readerFlags | CommandBehavior.CloseConnection;
			}

			return readerFlags;
		}
		
		#endregion
		

		#region [ GetByReader, ExecuteReader ]

		public static T GetByReader<T>(this SqlCommand cmd,  Func<SqlDataReader, T> func)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd);

            using (var dr = cmd.ExecuteReader(readerFlags))
			{
				return func(dr);
			}
		}

		public static T GetByReader<T>(this SqlCommand cmd,  Func<SqlDataReader, SqlParameter, T> func)
		{
			var returnValueParam = cmd.ReturnValueParam();

			var readerFlags = GetReaderFlagsAndOpenConnection(cmd);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				return func(dr, returnValueParam);
			}
		}

		public static int ExecuteReader(this SqlCommand cmd, Action<SqlDataReader> action)
		{
			var returnValueParam = cmd.ReturnValueParam();

			var readerFlags = GetReaderFlagsAndOpenConnection(cmd);

            using (var dr = cmd.ExecuteReader(readerFlags))
			{
				action(dr);
			}

			return (int)returnValueParam.Value;
		}

		#endregion


		#region [ ReadTo, ReadAs ]

		private static T ReadToValue<T>(this SqlCommand cmd)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleRow);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				if (dr.Read())
				{
					if (typeof(T).IsNullableValueType() && dr.IsDBNull(0))
						return default(T);
					
					return SqlDataReaderExtensions.GetValue<T>(dr);
				}

				return default(T);
			}
		}


		public static T ReadTo<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleRow);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				if (dr.Read())
				{
					if (typeof(T).IsNullableValueType() & dr.IsDBNull(0))
						return default(T);

					return createFunc(dr);
				}

				return default(T);
			}
		}
		
		public static T ReadTo<T>(this SqlCommand cmd)
		{
			if (typeof(T).IsSimpleType())
				return cmd.ReadToValue<T>();

			return cmd.ReadTo(MappingManager.GetCreateObjectFunc<T>());
		}

		
		public static T ReadAs<T>(this SqlCommand cmd)
		{
			if (typeof(T).IsSimpleType())
				return cmd.ReadToValue<T>();

			return cmd.ReadTo(SqlDataReaderExtensions.CreateObject<T>);
		}

		#endregion


		#region [ ReadToList, ReadAsList, ReadToArray, ReadAsArray ]

		private static IList<T> ReadToListOfValues<T>(this SqlCommand cmd, IList<T> list)
		{
			if (list == null)
				list = new List<T>();

			var type = typeof(T);
			var isNullableValueType = type.IsNullableValueType();
			
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				if (isNullableValueType)
				{
					var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
					while (dr.Read())
						list.Add(dr.IsDBNull(0) ? default(T) : SqlDataReaderExtensions.GetValue<T>(dr, underlyingType));
				}
				else
				{
					while (dr.Read())
						list.Add(SqlDataReaderExtensions.GetValue<T>(dr, type));
				}
			}

			return list;
		}
		
		private static IList<T> ReadToListOfObjects<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, IList<T> list)
		{
			if (list == null)
				list = new List<T>();

			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using (var dr = cmd.ExecuteReader(readerFlags))
				while (dr.Read())
					list.Add(createFunc(dr));

			return list;
		}
		

		public static IList<T> ReadToList<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, IList<T> list) 
		{
			if (list == null)
				list = new List<T>();

			var isNullableValueType = typeof(T).IsNullableValueType();

			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

            using (var dr = cmd.ExecuteReader(readerFlags))
			{
				while (dr.Read())
				{
					if (isNullableValueType && dr.IsDBNull(0))
						list.Add(default(T));
					else 
						list.Add(createFunc(dr));
				}
			}

			return list;
		}

		public static IList<T> ReadToList<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc)
		{
			if (typeof(T).IsSimpleType())
				return cmd.ReadToListOfValues<T>(null);

			return cmd.ReadToListOfObjects<T>(createFunc, null);
		}

		public static IList<T> ReadToList<T>(this SqlCommand cmd, IList<T> list) 
		{
			if (typeof(T).IsSimpleType())
				return cmd.ReadToListOfValues<T>(list);

			return cmd.ReadToListOfObjects<T>(MappingManager.GetCreateObjectFunc<T>(), list);
		}
		
		public static IList<T> ReadToList<T>(this SqlCommand cmd) 
		{
			if (typeof(T).IsSimpleType())
				return cmd.ReadToListOfValues<T>(null);

			return cmd.ReadToListOfObjects<T>(MappingManager.GetCreateObjectFunc<T>(), null);
		}
		

		public static IList<T> ReadAsList<T>(this SqlCommand cmd, IList<T> list) 
		{
			if (list == null)
				list = new List<T>();
			else if (typeof(T).IsSimpleType())
				return cmd.ReadToListOfValues<T>(list);

			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

            using (var dr = cmd.ExecuteReader(readerFlags))
			{
				dr.ReadAsList<T>(list, false);
			}

			return list;
		}

		public static IList<T> ReadAsList<T>(this SqlCommand cmd) 
		{
			if (typeof(T).IsSimpleType())
				return cmd.ReadToListOfValues<T>(null);

			return cmd.ReadAsList<T>(null);
		}
		

		public static T[] ReadToArray<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc)
		{
			return cmd.ReadToList<T>(createFunc).ToArray();
		}

		public static T[] ReadToArray<T>(this SqlCommand cmd)
		{
			return cmd.ReadToList<T>().ToArray();
		}

		public static T[] ReadAsArray<T>(this SqlCommand cmd)
		{
			return cmd.ReadAsList<T>().ToArray();
		}
		
		#endregion


		#region [ ReadToEnumerable ]
		
		public static IEnumerable<T> ReadToEnumerableValues<T>(this SqlCommand cmd) 
		{
			var type = typeof(T);
			var isNullableValueType = type.IsNullableValueType();

			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				while (dr.Read())
				{
					if (isNullableValueType)
					{
						var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

						while (dr.Read())
							if (dr.IsDBNull(0))
								yield return default(T);
							else
								yield return SqlDataReaderExtensions.GetValue<T>(dr, underlyingType);
					}
					else
					{
						while (dr.Read())
							yield return SqlDataReaderExtensions.GetValue<T>(dr, type);
					}
				}
			}
		}

		private static IEnumerable<T> ReadToEnumerableOfObjects<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using (var dr = cmd.ExecuteReader(readerFlags))
				while (dr.Read())
					yield return createFunc(dr);
		}

		public static IEnumerable<T> ReadToEnumerable<T>(this SqlCommand cmd) 
		{
			if (typeof(T).IsSimpleType())
				return cmd.ReadToEnumerableValues<T>();

			return cmd.ReadToEnumerableOfObjects(MappingManager.GetCreateObjectFunc<T>());
		}
		
		public static IEnumerable<T> ReadToEnumerable<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc) 
		{
			var isNullableValueType = typeof(T).IsNullableValueType();

			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				if (isNullableValueType)
					while (dr.Read())
						if (dr.IsDBNull(0))
							yield return default(T);
						else
							yield return createFunc(dr);

				else
					while (dr.Read())
						yield return createFunc(dr);
			}
		}

		#endregion


		#region [ ReadToRow(s), ReadAsRow(s) ]

		public static object[] ReadToRow(this SqlCommand cmd, Func<SqlDataReader, object[]> createFunc)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleRow);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				return dr.ReadToRow(createFunc, false);
			}
		}

		public static object[] ReadToRow<T>(this SqlCommand cmd)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleRow);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				return dr.ReadToRow<T>();
			}
		}


		public static Rows ReadToRows(this SqlCommand cmd, Func<SqlDataReader, object[]> createFunc)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleRow);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				return dr.ReadToRows(createFunc, false);
			}
		}

		public static Rows ReadToRows<T>(this SqlCommand cmd)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

            using (var dr = cmd.ExecuteReader(readerFlags))
			{
				return dr.ReadToRows<T>();
			}
		}

		
		public static object[] ReadAsRow(this SqlCommand cmd)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleRow);

            using (var dr = cmd.ExecuteReader(readerFlags))
			{
				return dr.ReadAsRow();
			}
		}
		
		public static Rows ReadAsRows(this SqlCommand cmd)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

            using (var dr = cmd.ExecuteReader(readerFlags))
			{
				return dr.ReadAsRows();
			}
		}

		#endregion
		

		#region [ ReadToDictionary ]

		public static IDictionary<TKey, TValue> ReadToDictionary<TKey, TValue>(this SqlCommand cmd) 
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				return dr.ReadToDictionary<TKey, TValue>();
			}
		}
		
		public static IDictionary<TKey, TValue> ReadToDictionary<TKey, TValue>(this SqlCommand cmd, Func<SqlDataReader, TValue> createFunc) 
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				return dr.ReadToDictionary<TKey, TValue>(createFunc);
			}
		}

		#endregion 
	
	}
}
