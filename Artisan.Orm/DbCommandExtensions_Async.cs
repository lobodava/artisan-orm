using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Artisan.Orm
{
	public static partial class DbCommandExtensions
	{
		#region [ private methods ]

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


        public static async Task<T> GetByReaderAsync<T>(this SqlCommand cmd,  Func<SqlDataReader, T> func)
        {
	        var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd);

	        using (var reader = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
			{
				return func(reader);
			}
        }

		public static async Task<T> GetByReaderAsync<T>(this SqlCommand cmd,  Func<SqlDataReader, SqlParameter, T> func)
		{
			var returnValueParam = cmd.ReturnValueParam();

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd);

			using (var reader = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
			{
				return func(reader, returnValueParam);
			}
		}

		public static async Task<int> ExecuteReaderAsync(this SqlCommand cmd, Action<SqlDataReader> action)
		{
			var returnValueParam = cmd.ReturnValueParam();

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd);

            using (var reader = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
			{
				action(reader);
			}

			return (int)returnValueParam.Value;
		}

		public static async Task<T> ReadAsAsync<T>(this SqlCommand cmd)
		{
			if (typeof(T).IsValueType || typeof(T) == typeof(String))
				return await cmd.ReadToAsync(DataReaderExtensions.GetValue<T>);

			return await cmd.ReadToAsync(DataReaderExtensions.CreateObject<T>);
		}


		public static async Task<T> ReadToAsync<T>(this SqlCommand cmd)
		{
			if (typeof(T).IsValueType || typeof(T) == typeof(String))
				return await cmd.ReadToAsync(DataReaderExtensions.GetValue<T>);

			return await cmd.ReadToAsync(MappingManager.GetCreateObjectFunc<T>());
		}

		public static async Task<T> ReadToAsync<T>(this SqlCommand cmd,  Func<SqlDataReader, T> createFunc)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow);

            using (var reader = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
			{
				return reader.Read() ? createFunc(reader) : default(T);
			}
		}

		public static async Task<IList<T>> ReadAsListAsync<T>(this SqlCommand cmd) 
		{
			if (typeof(T).IsValueType || typeof(T) == typeof(String))
				return await cmd.ReadToListAsync(DataReaderExtensions.GetValue<T>);

			return await cmd.ReadAsListAsync<T>(null);
		}

		public static async Task<IList<T>> ReadToListAsync<T>(this SqlCommand cmd) 
		{
			if (typeof(T).IsValueType || typeof(T) == typeof(String))
				return await cmd.ReadToListAsync(DataReaderExtensions.GetValue<T>);

			return await cmd.ReadToListAsync(MappingManager.GetCreateObjectFunc<T>());
		}
		
		public static async Task<IList<T>> ReadToListAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, IList<T> list = null) 
		{
			if (list == null)
				list = new List<T>();

			var type = typeof(T);
			var isNullableScalar = type.IsValueType && Nullable.GetUnderlyingType(type) != null || type == typeof(String);

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

            using (var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
			{
				while (dr.Read())
				{
					if (isNullableScalar && dr.IsDBNull(0))
						list.Add(default(T));
					else if (!dr.IsDBNull(0))
						list.Add(createFunc(dr));
				}
			}

			return list;
		}

		public static async Task<IList<T>> ReadAsListAsync<T>(this SqlCommand cmd, IList<T> list) 
		{
			if (list == null)
				list = new List<T>();

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

            using (var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
			{
				dr.ReadAsList<T>(list, false);
			}

			return list;
		}
		

		public static async Task<T[]> ReadAsArrayAsync<T>(this SqlCommand cmd)
		{
			if (typeof(T).IsValueType || typeof(T) == typeof(String))
				return await cmd.ReadToArrayAsync(DataReaderExtensions.GetValue<T>);

			var list = await cmd.ReadAsListAsync<T>(null);
			return list.ToArray();
		}

		public static async Task<T[]> ReadToArrayAsync<T>(this SqlCommand cmd)
		{
			if (typeof(T).IsValueType || typeof(T) == typeof(String))
				return await cmd.ReadToArrayAsync(DataReaderExtensions.GetValue<T>);

			return await cmd.ReadToArrayAsync(MappingManager.GetCreateObjectFunc<T>());
		}

		public static async Task<T[]> ReadToArrayAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc)
		{
			var list = new List<T>();

			var type = typeof(T);
			var isNullableScalar = type.IsValueType && Nullable.GetUnderlyingType(type) != null || type == typeof(String);

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
			{
				while (dr.Read())
				{
					if (isNullableScalar && dr.IsDBNull(0))
						list.Add(default(T));
					else if (!dr.IsDBNull(0))
						list.Add(createFunc(dr));
				}
			}

			return list.ToArray();
		}
		
		public static async Task<object[]> ReadAsRowAsync(this SqlCommand cmd)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow);

            using (var reader = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
			{
				return reader.ReadAsRow();
			}
		}

		public static async Task<object[]> ReadToRowAsync<T>(this SqlCommand cmd)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow);

            using (var reader = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
			{
				return reader.ReadToRow<T>();
			}
		}


		public static async Task<Rows> ReadAsRowsAsync(this SqlCommand cmd)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

            using (var reader = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
			{
				return reader.ReadAsRows();
			}
		}

		public static async Task<Rows> ReadToRowsAsync<T>(this SqlCommand cmd)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

            using (var reader = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
			{
				return reader.ReadToRows<T>();
			}
		}

		public static async Task<IDictionary<TKey, TValue>> ReadToDictionaryAsync<TKey, TValue>(this SqlCommand cmd) 
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult);

			 using (var reader = await cmd.ExecuteReaderAsync(readerFlags).ConfigureAwait(false))
			{
				return reader.ReadToDictionary<TKey, TValue>();
			}
		}

	}
}

