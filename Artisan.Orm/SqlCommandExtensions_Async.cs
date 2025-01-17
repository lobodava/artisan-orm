using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Artisan.Orm
{ 

	public static partial class SqlCommandExtensions
	{
		#region [ GetReaderFlagsAndOpenConnectionAsync ]

		private static async Task<CommandBehavior> GetReaderFlagsAndOpenConnectionAsync(SqlCommand cmd, CommandBehavior defaultReaderFlag = CommandBehavior.Default)
		{
			var readerFlags = defaultReaderFlag;

			if (cmd.Connection.State == ConnectionState.Closed)
			{
				await cmd.Connection.OpenAsync().ConfigureAwait(false);

				if (defaultReaderFlag == CommandBehavior.Default)
					readerFlags = CommandBehavior.CloseConnection;
				else
					readerFlags |= CommandBehavior.CloseConnection;
			}

			return readerFlags;
		}
	
		#endregion


		#region [ GetByReaderAsync, ExecuteReaderAsync ]

		public static async Task<T> GetByReaderAsync<T>(this SqlCommand cmd,  Func<SqlDataReader, T> func)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd);

			using var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false);
			return func(dr);
		}

		public static async Task<T> GetByReaderAsync<T>(this SqlCommand cmd,  Func<SqlDataReader, SqlParameter, T> func)
		{
			var returnValueParam = cmd.ReturnValueParam();

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd);

			using var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false);
			return func(dr, returnValueParam);
		}

		public static async Task<int> ExecuteReaderAsync(this SqlCommand cmd, Action<SqlDataReader> action)
		{
			var returnValueParam = cmd.ReturnValueParam();

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
			{
				action(dr);
			}

			return (int)returnValueParam.Value;
		}

		#endregion


		#region [ ReadToAsync, ReadAsAsync ]
	
		private static async Task<T> ReadToValueAsync<T>(this SqlCommand cmd)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow);

			using var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false);
			if (dr.Read())
			{
				if (typeof(T).IsNullableValueType() && dr.IsDBNull(0))
					return default;

				return dr.GetValue<T>();
			}

			return default;
		}
	

		public static async Task<T> ReadToAsync<T>(this SqlCommand cmd,  Func<SqlDataReader, T> createFunc)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow);

			using var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false);
			return dr.Read() ? createFunc(dr) : default;
		}
	
		public static async Task<T> ReadToAsync<T>(this SqlCommand cmd)
		{
			if (typeof(T).IsSimpleType())
				return await cmd.ReadToValueAsync<T>();

			return await cmd.ReadToAsync(MappingManager.GetCreateObjectFunc<T>());
		}


		public static async Task<T> ReadAsAsync<T>(this SqlCommand cmd)
		{
			if (typeof(T).IsSimpleType())
				return await cmd.ReadToValueAsync<T>();

			var key = SqlDataReaderExtensions.GetAutoCreateObjectFuncKey<T>(cmd.CommandText);
			var autoMappingFunc = MappingManager.GetAutoCreateObjectFunc<T>(key);

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow);

			using var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false);
			return dr.Read() ? dr.CreateObject(autoMappingFunc, key) : default;
		}

		#endregion


		#region [ ReadToListAsync, ReadAsListAsync, ReadToArrayAsync, ReadAsArrayAsync ]
	
		private static async Task<IList<T>> ReadToListOfValuesAsync<T>(this SqlCommand cmd, IList<T> list)
		{
			list ??= new List<T>();

			var type = typeof(T);
			var isNullableValueType = type.IsNullableValueType();
		
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
			{
				if (isNullableValueType)
				{
					var underlyingType = type.GetUnderlyingType();
					while (dr.Read())
						list.Add(dr.IsDBNull(0) ? default : SqlDataReaderExtensions.GetValue<T>(dr, underlyingType));
				}
				else
				{
					while (dr.Read())
						list.Add(SqlDataReaderExtensions.GetValue<T>(dr, type));
				}
			}

			return list;
		}
	
		private static async Task<IList<T>> ReadToListOfObjectsAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, IList<T> list)
		{
			list ??= new List<T>();

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
				while (dr.Read())
					list.Add(createFunc(dr));

			return list;
		}
	

		public static async Task<IList<T>> ReadToListAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, IList<T> list) 
		{
			list ??= new List<T>();

			var isNullableValueType = typeof(T).IsNullableValueType();

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
			{
				while (dr.Read())
				{
					if (isNullableValueType && dr.IsDBNull(0))
						list.Add(default);
					else 
						list.Add(createFunc(dr));
				}
			}

			return list;
		}

		public static async Task<IList<T>> ReadToListAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc) 
		{
			if (typeof(T).IsSimpleType())
				return await cmd.ReadToListOfValuesAsync<T>(null);

			return await cmd.ReadToListOfObjectsAsync<T>(createFunc, null);
		}

		public static async Task<IList<T>> ReadToListAsync<T>(this SqlCommand cmd, IList<T> list) 
		{
			if (typeof(T).IsSimpleType())
				return await cmd.ReadToListOfValuesAsync<T>(list);

			return await cmd.ReadToListOfObjectsAsync<T>(MappingManager.GetCreateObjectFunc<T>(), list);
		}
	
		public static async Task<IList<T>> ReadToListAsync<T>(this SqlCommand cmd) 
		{
			if (typeof(T).IsSimpleType())
				return await cmd.ReadToListOfValuesAsync<T>(null);

			return await cmd.ReadToListOfObjectsAsync<T>(MappingManager.GetCreateObjectFunc<T>(), null);
		}


		public static async Task<IList<T>> ReadAsListAsync<T>(this SqlCommand cmd, IList<T> list) 
		{
			if (typeof(T).IsSimpleType())
				return await cmd.ReadToListOfValuesAsync<T>(list);

			var key = SqlDataReaderExtensions.GetAutoCreateObjectFuncKey<T>(cmd.CommandText);
			var autoMappingFunc = MappingManager.GetAutoCreateObjectFunc<T>(key);

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
			{
				list = dr.ReadAsList(list, autoMappingFunc, key);
			}

			return list;
		}

		public static async Task<IList<T>> ReadAsListAsync<T>(this SqlCommand cmd) 
		{
			if (typeof(T).IsSimpleType())
				return await cmd.ReadToListOfValuesAsync<T>(null);

			return await cmd.ReadAsListAsync<T>(null);
		}
	

		public static async Task<T[]> ReadToArrayAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc)
		{
			return (await cmd.ReadToListAsync<T>(createFunc)).ToArray();
		}

		public static async Task<T[]> ReadToArrayAsync<T>(this SqlCommand cmd)
		{
			return (await cmd.ReadToListAsync<T>()).ToArray();
		}
	
		public static async Task<T[]> ReadAsArrayAsync<T>(this SqlCommand cmd)
		{
			return (await cmd.ReadAsListAsync<T>()).ToArray();
		}

		#endregion

	
		#region [ ReadToObjectRow(s)Async, ReadAsObjectRow(s)Async ]

		public static async Task<ObjectRow> ReadToObjectRowAsync(this SqlCommand cmd, Func<SqlDataReader, ObjectRow> createFunc)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow);

			using var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false);
			return dr.ReadToObjectRow(createFunc, false);
		}

		public static async Task<ObjectRow> ReadToObjectRowAsync<T>(this SqlCommand cmd)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow);

			using var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false);
			return dr.ReadToObjectRow<T>();
		}


		public static async Task<ObjectRows> ReadToObjectRowsAsync(this SqlCommand cmd, Func<SqlDataReader, ObjectRow> createFunc)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

			using var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false);
			return dr.ReadToObjectRows(createFunc, false);
		}

		public static async Task<ObjectRows> ReadAsObjectRowsAsync(this SqlCommand cmd)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

			using var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false);
			return dr.ReadAsObjectRows();
		}


		public static async Task<ObjectRow> ReadAsObjectRowAsync(this SqlCommand cmd)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow);

			using var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false);
			return dr.ReadAsObjectRow();
		}

		public static async Task<ObjectRows> ReadToObjectRowsAsync<T>(this SqlCommand cmd)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

			using var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false);
			return dr.ReadToObjectRows<T>();
		}
	
		#endregion


		#region [ ReadToDictionaryAsync, ReadAsDictionaryAsync ]

		public static async Task<IDictionary<TKey, TValue>> ReadToDictionaryAsync<TKey, TValue>(this SqlCommand cmd, Func<SqlDataReader, TValue> createFunc) 
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

			using var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false);
			return dr.ReadToDictionary<TKey, TValue>(createFunc);
		}


		public static async Task<IDictionary<TKey, TValue>> ReadToDictionaryAsync<TKey, TValue>(this SqlCommand cmd) 
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

			using var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false);
			return dr.ReadToDictionary<TKey, TValue>();
		}

		public static async Task<IDictionary<TKey, TValue>> ReadAsDictionaryAsync<TKey, TValue>(this SqlCommand cmd) 
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

			using var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false);
			return dr.ReadAsDictionary<TKey, TValue>();
		}

		#endregion 


		#region [ ReadToTree, ReadToTreeList ]
	
		public static async Task<T> ReadToTreeAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, IList<T> list, bool hierarchicallySorted = false) where T: class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(createFunc, list)).ToTree(hierarchicallySorted);
		}

		public static async Task<T> ReadToTreeAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, bool hierarchicallySorted = false) where T: class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(createFunc, null)).ToTree(hierarchicallySorted);
		}

		public static async Task<T> ReadToTreeAsync<T>(this SqlCommand cmd, IList<T> list, bool hierarchicallySorted = false)  where T: class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(MappingManager.GetCreateObjectFunc<T>(), list)).ToTree(hierarchicallySorted);
		}
	
		public static async Task<T> ReadToTreeAsync<T>(this SqlCommand cmd, bool hierarchicallySorted = false) where T: class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(MappingManager.GetCreateObjectFunc<T>(), null)).ToTree(hierarchicallySorted);
		}
	
		public static async Task<IList<T>> ReadToTreeListAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, IList<T> list, bool hierarchicallySorted = false) where T: class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(createFunc, list)).ToTreeList(hierarchicallySorted);
		}

		public static async Task<IList<T>> ReadToTreeListAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, bool hierarchicallySorted = false) where T: class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(createFunc, null)).ToTreeList(hierarchicallySorted);
		}

		public static async Task<IList<T>> ReadToTreeListAsync<T>(this SqlCommand cmd, IList<T> list, bool hierarchicallySorted = false)  where T: class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(MappingManager.GetCreateObjectFunc<T>(), list)).ToTreeList(hierarchicallySorted);
		}
	
		public static async Task<IList<T>> ReadToTreeListAsync<T>(this SqlCommand cmd, bool hierarchicallySorted = false) where T: class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(MappingManager.GetCreateObjectFunc<T>(), null)).ToTreeList(hierarchicallySorted);
		}

		#endregion

	}

}
