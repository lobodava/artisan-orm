using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

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
					readerFlags = readerFlags | CommandBehavior.CloseConnection;
			}

			return readerFlags;
		}
		
		#endregion


		#region [ GetByReaderAsync, ExecuteReaderAsync ]

		public static async Task<T> GetByReaderAsync<T>(this SqlCommand cmd,  Func<SqlDataReader, T> func)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
			{
				return func(dr);
			}
		}

		public static async Task<T> GetByReaderAsync<T>(this SqlCommand cmd,  Func<SqlDataReader, SqlParameter, T> func)
		{
			var returnValueParam = cmd.ReturnValueParam();

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
			{
				return func(dr, returnValueParam);
			}
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

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
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
		

		public static async Task<T> ReadToAsync<T>(this SqlCommand cmd,  Func<SqlDataReader, T> createFunc)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
			{
				if (dr.Read()) {
					if (typeof(T).IsNullableValueType() & dr.IsDBNull(0))
						return default(T);

					return createFunc(dr);
				}

				return default(T);
			}
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

			return await cmd.ReadToAsync(SqlDataReaderExtensions.CreateObject<T>);
		}

		#endregion


		#region [ ReadToListAsync, ReadAsListAsync, ReadToArrayAsync, ReadAsArrayAsync ]
		
		private static async Task<IList<T>> ReadToListOfValuesAsync<T>(this SqlCommand cmd, IList<T> list)
		{
			if (list == null)
				list = new List<T>();

			var type = typeof(T);
			var isNullableValueType = type.IsNullableValueType();
			
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
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
		
		private static async Task<IList<T>> ReadToListOfObjectsAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, IList<T> list)
		{
			if (list == null)
				list = new List<T>();

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
				while (dr.Read())
					list.Add(createFunc(dr));

			return list;
		}
		
	
		public static async Task<IList<T>> ReadToListAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, IList<T> list) 
		{
			if (list == null)
				list = new List<T>();

			var isNullableValueType = typeof(T).IsNullableValueType();

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
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
			if (list == null)
				list = new List<T>();
			else if (typeof(T).IsSimpleType())
				return await cmd.ReadToListOfValuesAsync<T>(list);

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
			{
				dr.ReadAsList<T>(list, false);
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

		
		#region [ ReadToRow(s)Async, ReadAsRow(s)Async ]

		public static async Task<object[]> ReadToRowAsync(this SqlCommand cmd, Func<SqlDataReader, object[]> createFunc)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
			{
				return dr.ReadToRow(createFunc, false);
			}
		}

		public static async Task<object[]> ReadToRowAsync<T>(this SqlCommand cmd)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
			{
				return dr.ReadToRow<T>();
			}
		}


		public static async Task<Rows> ReadToRowsAsync(this SqlCommand cmd, Func<SqlDataReader, object[]> createFunc)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
			{
				return dr.ReadToRows(createFunc, false);
			}
		}

		public static async Task<Rows> ReadAsRowsAsync(this SqlCommand cmd)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
			{
				return dr.ReadAsRows();
			}
		}


		public static async Task<object[]> ReadAsRowAsync(this SqlCommand cmd)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
			{
				return dr.ReadAsRow();
			}
		}

		public static async Task<Rows> ReadToRowsAsync<T>(this SqlCommand cmd)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
			{
				return dr.ReadToRows<T>();
			}
		}
		
		#endregion


		#region [ ReadToDictionary ]

		public static async Task<IDictionary<TKey, TValue>> ReadToDictionaryAsync<TKey, TValue>(this SqlCommand cmd) 
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

			 using (var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
			{
				return dr.ReadToDictionary<TKey, TValue>();
			}
		}

		public static async Task<IDictionary<TKey, TValue>> ReadToDictionaryAsync<TKey, TValue>(this SqlCommand cmd, Func<SqlDataReader, TValue> createFunc) 
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
			{
				return dr.ReadToDictionary<TKey, TValue>(createFunc);
			}
		}

		#endregion 

	}
}

