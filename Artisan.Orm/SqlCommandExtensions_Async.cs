using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Data.SqlClient;

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

		public static async Task<T> GetByReaderAsync<T>(this SqlCommand cmd,  Func<SqlDataReader, T> func, CancellationToken cancellationToken = default(CancellationToken))
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false))
			{
				return func(dr);
			}
		}

		public static async Task<T> GetByReaderAsync<T>(this SqlCommand cmd,  Func<SqlDataReader, SqlParameter, T> func, CancellationToken cancellationToken = default(CancellationToken))
		{
			var returnValueParam = cmd.ReturnValueParam();

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false))
			{
				return func(dr, returnValueParam);
			}
		}

		public static async Task<int> ExecuteReaderAsync(this SqlCommand cmd, Action<SqlDataReader> action, CancellationToken cancellationToken = default(CancellationToken))
		{
			var returnValueParam = cmd.ReturnValueParam();

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false))
			{
				action(dr);
			}

			return (int)returnValueParam.Value;
		}

		#endregion


		#region [ ReadToAsync, ReadAsAsync ]

		private static async Task<T> ReadToValueAsync<T>(this SqlCommand cmd, CancellationToken cancellationToken = default(CancellationToken))
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false))
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


		public static async Task<T> ReadToAsync<T>(this SqlCommand cmd,  Func<SqlDataReader, T> createFunc, CancellationToken cancellationToken = default(CancellationToken))
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false))
			{
				return dr.Read() ? createFunc(dr) : default(T);
			}
		}

		public static async Task<T> ReadToAsync<T>(this SqlCommand cmd, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (typeof(T).IsSimpleType())
				return await cmd.ReadToValueAsync<T>(cancellationToken);

			return await cmd.ReadToAsync(MappingManager.GetCreateObjectFunc<T>(), cancellationToken);
		}


		public static async Task<T> ReadAsAsync<T>(this SqlCommand cmd, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (typeof(T).IsSimpleType())
				return await cmd.ReadToValueAsync<T>(cancellationToken);

			var key = SqlDataReaderExtensions.GetAutoCreateObjectFuncKey<T>(cmd.CommandText);
			var autoMappingFunc = MappingManager.GetAutoCreateObjectFunc<T>(key);

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false))
			{
				return dr.Read() ? dr.CreateObject(autoMappingFunc, key) : default(T);
			}
		}

		private static async Task<IList<T>> ReadToListOfValuesAsync<T>(this SqlCommand cmd, IList<T> list, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (list == null)
			{
				list = new List<T>();
			}

			var type = typeof(T);
			var isNullableValueType = type.IsNullableValueType();

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false))
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

		private static async Task<IList<T>> ReadToListOfObjectsAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, IList<T> list, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (list == null)
			{
				list = new List<T>();
			}

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false))
			{
				while (dr.Read())
					list.Add(createFunc(dr));
			}

			return list;
		}


		public static async Task<IList<T>> ReadToListAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, IList<T> list, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (list == null)
			{
				list = new List<T>();
			}

			var isNullableValueType = typeof(T).IsNullableValueType();

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false))
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

		public static async Task<IList<T>> ReadToListAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (typeof(T).IsSimpleType())
				return await cmd.ReadToListOfValuesAsync<T>(null, cancellationToken);

			return await cmd.ReadToListOfObjectsAsync<T>(createFunc, null, cancellationToken);
		}

		public static async Task<IList<T>> ReadToListAsync<T>(this SqlCommand cmd, IList<T> list, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (typeof(T).IsSimpleType())
				return await cmd.ReadToListOfValuesAsync<T>(list, cancellationToken);

			return await cmd.ReadToListOfObjectsAsync<T>(MappingManager.GetCreateObjectFunc<T>(), list, cancellationToken);
		}

		public static async Task<IList<T>> ReadToListAsync<T>(this SqlCommand cmd, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (typeof(T).IsSimpleType())
				return await cmd.ReadToListOfValuesAsync<T>(null, cancellationToken);

			return await cmd.ReadToListOfObjectsAsync<T>(MappingManager.GetCreateObjectFunc<T>(), null, cancellationToken);
		}


		public static async Task<IList<T>> ReadAsListAsync<T>(this SqlCommand cmd, IList<T> list, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (typeof(T).IsSimpleType())
				return await cmd.ReadToListOfValuesAsync<T>(list);

			var key = SqlDataReaderExtensions.GetAutoCreateObjectFuncKey<T>(cmd.CommandText);
			var autoMappingFunc = MappingManager.GetAutoCreateObjectFunc<T>(key);

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false))
			{
				list = dr.ReadAsList(list, autoMappingFunc, key);
			}

			return list;
		}

		public static async Task<IList<T>> ReadAsListAsync<T>(this SqlCommand cmd, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (typeof(T).IsSimpleType())
				return await cmd.ReadToListOfValuesAsync<T>(null, cancellationToken);

			return await cmd.ReadAsListAsync<T>(null, cancellationToken);
		}


		public static async Task<T[]> ReadToArrayAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, CancellationToken cancellationToken = default(CancellationToken))
		{
			return (await cmd.ReadToListAsync<T>(createFunc, cancellationToken)).ToArray();
		}

		public static async Task<T[]> ReadToArrayAsync<T>(this SqlCommand cmd, CancellationToken cancellationToken = default(CancellationToken))
		{
			return (await cmd.ReadToListAsync<T>(cancellationToken)).ToArray();
		}

		public static async Task<T[]> ReadAsArrayAsync<T>(this SqlCommand cmd, CancellationToken cancellationToken = default(CancellationToken))
		{
			return (await cmd.ReadAsListAsync<T>(cancellationToken)).ToArray();
		}

		#endregion


		#region [ ReadToObjectRow(s)Async, ReadAsObjectRow(s)Async ]

		public static async Task<ObjectRow> ReadToObjectRowAsync(this SqlCommand cmd, Func<SqlDataReader, ObjectRow> createFunc, CancellationToken cancellationToken = default(CancellationToken))
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false))
			{
				return dr.ReadToObjectRow(createFunc, false);
			}
		}

		public static async Task<ObjectRow> ReadToObjectRowAsync<T>(this SqlCommand cmd, CancellationToken cancellationToken = default(CancellationToken))
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false))
			{
				return dr.ReadToObjectRow<T>();
			}
		}


		public static async Task<ObjectRows> ReadToObjectRowsAsync(this SqlCommand cmd, Func<SqlDataReader, ObjectRow> createFunc, CancellationToken cancellationToken = default(CancellationToken))
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false))
			{
				return dr.ReadToObjectRows(createFunc, false);
			}
		}

		public static async Task<ObjectRows> ReadAsObjectRowsAsync(this SqlCommand cmd, CancellationToken cancellationToken = default(CancellationToken))
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false))
			{
				return dr.ReadAsObjectRows();
			}
		}


		public static async Task<ObjectRow> ReadAsObjectRowAsync(this SqlCommand cmd, CancellationToken cancellationToken = default(CancellationToken))
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false))
			{
				return dr.ReadAsObjectRow();
			}
		}

		public static async Task<ObjectRows> ReadToObjectRowsAsync<T>(this SqlCommand cmd, CancellationToken cancellationToken = default(CancellationToken))
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false))
			{
				return dr.ReadToObjectRows<T>();
			}
		}

		#endregion


		#region [ ReadToDictionaryAsync, ReadAsDictionaryAsync ]

		public static async Task<IDictionary<TKey, TValue>> ReadToDictionaryAsync<TKey, TValue>(this SqlCommand cmd, Func<SqlDataReader, TValue> createFunc, CancellationToken cancellationToken = default(CancellationToken))
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false))
			{
				return dr.ReadToDictionary<TKey, TValue>(createFunc);
			}
		}


		public static async Task<IDictionary<TKey, TValue>> ReadToDictionaryAsync<TKey, TValue>(this SqlCommand cmd, CancellationToken cancellationToken = default(CancellationToken))
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false))
			{
				return dr.ReadToDictionary<TKey, TValue>();
			}
		}

		public static async Task<IDictionary<TKey, TValue>> ReadAsDictionaryAsync<TKey, TValue>(this SqlCommand cmd, CancellationToken cancellationToken = default(CancellationToken))
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false))
			{
				return dr.ReadAsDictionary<TKey, TValue>();
			}
		}

		#endregion


		#region [ ReadToTree, ReadToTreeList ]

		public static async Task<T> ReadToTreeAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, IList<T> list, bool hierarchicallySorted = false, CancellationToken cancellationToken = default(CancellationToken)) where T: class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(createFunc, list, cancellationToken)).ToTree(hierarchicallySorted);
		}

		public static async Task<T> ReadToTreeAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, bool hierarchicallySorted = false, CancellationToken cancellationToken = default(CancellationToken)) where T: class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(createFunc, null, cancellationToken)).ToTree(hierarchicallySorted);
		}

		public static async Task<T> ReadToTreeAsync<T>(this SqlCommand cmd, IList<T> list, bool hierarchicallySorted = false, CancellationToken cancellationToken = default(CancellationToken))  where T: class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(MappingManager.GetCreateObjectFunc<T>(), list, cancellationToken)).ToTree(hierarchicallySorted);
		}

		public static async Task<T> ReadToTreeAsync<T>(this SqlCommand cmd, bool hierarchicallySorted = false, CancellationToken cancellationToken = default(CancellationToken)) where T: class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(MappingManager.GetCreateObjectFunc<T>(), null, cancellationToken)).ToTree(hierarchicallySorted);
		}

		public static async Task<IList<T>> ReadToTreeListAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, IList<T> list, bool hierarchicallySorted = false, CancellationToken cancellationToken = default(CancellationToken)) where T: class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(createFunc, list, cancellationToken)).ToTreeList(hierarchicallySorted);
		}

		public static async Task<IList<T>> ReadToTreeListAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, bool hierarchicallySorted = false, CancellationToken cancellationToken = default(CancellationToken)) where T: class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(createFunc, null, cancellationToken)).ToTreeList(hierarchicallySorted);
		}

		public static async Task<IList<T>> ReadToTreeListAsync<T>(this SqlCommand cmd, IList<T> list, bool hierarchicallySorted = false, CancellationToken cancellationToken = default(CancellationToken))  where T: class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(MappingManager.GetCreateObjectFunc<T>(), list, cancellationToken)).ToTreeList(hierarchicallySorted);
		}

		public static async Task<IList<T>> ReadToTreeListAsync<T>(this SqlCommand cmd, bool hierarchicallySorted = false, CancellationToken cancellationToken = default(CancellationToken)) where T: class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(MappingManager.GetCreateObjectFunc<T>(), null, cancellationToken)).ToTreeList(hierarchicallySorted);
		}

		#endregion

	}

}
