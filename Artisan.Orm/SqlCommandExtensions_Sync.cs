using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Data.SqlClient;

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
					readerFlags |= CommandBehavior.CloseConnection;
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

					return dr.GetValue<T>();
				}
			}

			return default(T);
		}

		public static T ReadTo<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleRow);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				return dr.Read() ? createFunc(dr) : default(T);
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

			var key = SqlDataReaderExtensions.GetAutoCreateObjectFuncKey<T>(cmd.CommandText);
			var autoMappingFunc = MappingManager.GetAutoCreateObjectFunc<T>(key);

			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleRow);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				return dr.Read() ? dr.CreateObject(autoMappingFunc, key) : default(T);
			}
		}

		public static dynamic ReadDynamic(this SqlCommand cmd)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleRow);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				return dr.Read() ? dr.CreateDynamic() : null;
			}
		}


		private static IList<T> ReadToListOfValues<T>(this SqlCommand cmd, IList<T> list)
		{
			if (list == null)
			{
				list = new List<T>();
			}

			var type = typeof(T);
			var isNullableValueType = type.IsNullableValueType();

			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				if (isNullableValueType)
				{
					var underlyingType = type.GetUnderlyingType();
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
			{
				list = new List<T>();
			}

			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				while (dr.Read())
					list.Add(createFunc(dr));
			}

			return list;
		}


		public static IList<T> ReadToList<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, IList<T> list)
		{
			if (list == null)
			{
				list = new List<T>();
			}

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

		public static IList<dynamic> ReadDynamicList(this SqlCommand cmd, IList<dynamic> list)
		{
			if (list == null)
			{
				list = new List<dynamic>();
			}

			var iList = (IList)list;

			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				while (dr.Read())
				{
					iList.Add(dr.CreateDynamic());
				}
			}

			return list;
		}

		public static IList<dynamic> ReadDynamicList(this SqlCommand cmd)
		{
			return cmd.ReadDynamicList(null);
		}

		public static IList<T> ReadAsList<T>(this SqlCommand cmd, IList<T> list)
		{
			if (typeof(T).IsSimpleType())
				return cmd.ReadToListOfValues<T>(list);

			var key = SqlDataReaderExtensions.GetAutoCreateObjectFuncKey<T>(cmd.CommandText);
			var autoMappingFunc = MappingManager.GetAutoCreateObjectFunc<T>(key);

			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				list = dr.ReadAsList(list, autoMappingFunc, key);
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

		public static IList<dynamic> ReadDynamicArray(this SqlCommand cmd)
		{
			return cmd.ReadDynamicList().ToArray();;
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
					var underlyingType = type.GetUnderlyingType();

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

		private static IEnumerable<T> ReadToEnumerableObjects<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				while (dr.Read())
					yield return createFunc(dr);
			}
		}

		private static IEnumerable<T> ReadAsEnumerableObjects<T>(this SqlCommand cmd)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				var key = SqlDataReaderExtensions.GetAutoCreateObjectFuncKey<T>(dr);
				var autoMappingFunc = MappingManager.GetAutoCreateObjectFunc<T>(key);

				if (autoMappingFunc == null)
				{
					autoMappingFunc = SqlDataReaderExtensions.CreateAutoMappingFunc<T>(dr);
					MappingManager.AddAutoCreateObjectFunc(key, autoMappingFunc);
				}

				while (dr.Read())
					yield return autoMappingFunc(dr);
			}
		}


		public static IEnumerable<T> ReadToEnumerable<T>(this SqlCommand cmd)
		{
			if (typeof(T).IsSimpleType())
				return cmd.ReadToEnumerableValues<T>();

			return cmd.ReadToEnumerableObjects(MappingManager.GetCreateObjectFunc<T>());
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

		public static IEnumerable<T> ReadAsEnumerable<T>(this SqlCommand cmd)
		{
			if (typeof(T).IsSimpleType())
				return cmd.ReadToEnumerableValues<T>();

			return cmd.ReadAsEnumerableObjects<T>();
		}


		#endregion


		#region [ ReadToObjectRow(s), ReadAsObjectRow(s) ]

		public static ObjectRow ReadToObjectRow(this SqlCommand cmd, Func<SqlDataReader, ObjectRow> createFunc)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleRow);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				return dr.ReadToObjectRow(createFunc, false);
			}
		}

		public static ObjectRow ReadToObjectRow<T>(this SqlCommand cmd)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleRow);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				return dr.ReadToObjectRow<T>();
			}
		}


		public static ObjectRows ReadToObjectRows(this SqlCommand cmd, Func<SqlDataReader, ObjectRow> createFunc)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				return dr.ReadToObjectRows(createFunc, false);
			}
		}

		public static ObjectRows ReadToObjectRows<T>(this SqlCommand cmd)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				return dr.ReadToObjectRows<T>();
			}
		}


		public static ObjectRow ReadAsObjectRow(this SqlCommand cmd)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleRow);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				return dr.ReadAsObjectRow();
			}
		}

		public static ObjectRows ReadAsObjectRows(this SqlCommand cmd)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				return dr.ReadAsObjectRows();
			}
		}

		#endregion


		#region [ ReadToDictionary, ReadAsDictionary ]

		public static IDictionary<TKey, TValue> ReadToDictionary<TKey, TValue>(this SqlCommand cmd, Func<SqlDataReader, TValue> createFunc)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				return dr.ReadToDictionary<TKey, TValue>(createFunc);
			}
		}

		public static IDictionary<TKey, TValue> ReadToDictionary<TKey, TValue>(this SqlCommand cmd)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				return dr.ReadToDictionary<TKey, TValue>();
			}
		}

		public static IDictionary<TKey, TValue> ReadAsDictionary<TKey, TValue>(this SqlCommand cmd)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				return dr.ReadAsDictionary<TKey, TValue>();
			}
		}

		#endregion


		#region [ ReadToTree, ReadToTreeList ]

		public static T ReadToTree<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, IList<T> list, bool hierarchicallySorted = false) where T: class, INode<T>
		{
			return cmd.ReadToListOfObjects<T>(createFunc, list).ToTree(hierarchicallySorted);
		}

		public static T ReadToTree<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, bool hierarchicallySorted = false) where T: class, INode<T>
		{
			return cmd.ReadToEnumerableObjects<T>(createFunc).ToTree(hierarchicallySorted);
		}

		public static T ReadToTree<T>(this SqlCommand cmd, IList<T> list, bool hierarchicallySorted = false)  where T: class, INode<T>
		{
			return cmd.ReadToListOfObjects<T>(MappingManager.GetCreateObjectFunc<T>(), list).ToTree(hierarchicallySorted);
		}

		public static T ReadToTree<T>(this SqlCommand cmd, bool hierarchicallySorted = false) where T: class, INode<T>
		{
			return cmd.ReadToEnumerableObjects<T>(MappingManager.GetCreateObjectFunc<T>()).ToTree(hierarchicallySorted);
		}

		public static IList<T> ReadToTreeList<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, IList<T> list, bool hierarchicallySorted = false) where T: class, INode<T>
		{
			return cmd.ReadToListOfObjects<T>(createFunc, list).ToTreeList(hierarchicallySorted);
		}

		public static IList<T> ReadToTreeList<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, bool hierarchicallySorted = false) where T: class, INode<T>
		{
			return cmd.ReadToEnumerableObjects<T>(createFunc).ToTreeList(hierarchicallySorted);
		}

		public static IList<T> ReadToTreeList<T>(this SqlCommand cmd, IList<T> list, bool hierarchicallySorted = false)  where T: class, INode<T>
		{
			return cmd.ReadToListOfObjects<T>(MappingManager.GetCreateObjectFunc<T>(), list).ToTreeList(hierarchicallySorted);
		}

		public static IList<T> ReadToTreeList<T>(this SqlCommand cmd, bool hierarchicallySorted = false) where T: class, INode<T>
		{
			return cmd.ReadToEnumerableObjects<T>(MappingManager.GetCreateObjectFunc<T>()).ToTreeList(hierarchicallySorted);
		}

		#endregion

	}

}
