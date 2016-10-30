using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;


namespace Artisan.Orm
{
	public static partial class DbCommandExtensions
	{
		#region [ private methods ]

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
		

		public static T GetByReader<T>(this SqlCommand cmd,  Func<SqlDataReader, T> func)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd);

            using (var reader = cmd.ExecuteReader(readerFlags))
			{
				return func(reader);
			}
		}

		public static T GetByReader<T>(this SqlCommand cmd,  Func<SqlDataReader, SqlParameter, T> func)
		{
			var returnValueParam = cmd.ReturnValueParam();

			var readerFlags = GetReaderFlagsAndOpenConnection(cmd);

			using (var reader = cmd.ExecuteReader(readerFlags))
			{
				return func(reader, returnValueParam);
			}
		}

		public static int ExecuteReader(this SqlCommand cmd, Action<SqlDataReader> action)
		{
			var returnValueParam = cmd.ReturnValueParam();

			var readerFlags = GetReaderFlagsAndOpenConnection(cmd);

            using (var reader = cmd.ExecuteReader(readerFlags))
			{
				action(reader);
			}

			return (int)returnValueParam.Value;
		}

		public static T ReadAs<T>(this SqlCommand cmd)
		{
			if (typeof(T).IsValueType || typeof(T) == typeof(String))
				return cmd.ReadTo(DataReaderExtensions.GetValue<T>);

			return cmd.ReadTo(DataReaderExtensions.CreateEntity<T>);
		}
		
		public static T ReadTo<T>(this SqlCommand cmd)
		{
			if (typeof(T).IsValueType || typeof(T) == typeof(String))
				return cmd.ReadTo(DataReaderExtensions.GetValue<T>);

			return cmd.ReadTo(MappingManager.GetCreateEntityFunc<T>());
		}

		public static T ReadTo<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleRow);

            using (var reader = cmd.ExecuteReader(readerFlags))
			{
				return reader.Read() ? createFunc(reader) : default(T);
			}
		}
		

		public static IList<T> ReadAsList<T>(this SqlCommand cmd) 
		{
			if (typeof(T).IsValueType || typeof(T) == typeof(String))
				return cmd.ReadToList(DataReaderExtensions.GetValue<T>);

			return cmd.ReadToList(DataReaderExtensions.CreateEntity<T>);
		}

		public static IList<T> ReadToList<T>(this SqlCommand cmd) 
		{
			if (typeof(T).IsValueType || typeof(T) == typeof(String))
				return cmd.ReadToList(DataReaderExtensions.GetValue<T>);

			return cmd.ReadToList(MappingManager.GetCreateEntityFunc<T>());
		}
		
		public static IList<T> ReadToList<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, IList<T> list = null) 
		{
			if (list == null)
				list = new List<T>();

			var type = typeof(T);
			var isNullableScalar = type.IsValueType && Nullable.GetUnderlyingType(type) != null || type == typeof(String);

			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

            using (var dr = cmd.ExecuteReader(readerFlags))
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


		public static T[] ReadAsArray<T>(this SqlCommand cmd)
		{
			if (typeof(T).IsValueType || typeof(T) == typeof(String))
				return cmd.ReadToArray(DataReaderExtensions.GetValue<T>);

			return cmd.ReadToArray(DataReaderExtensions.CreateEntity<T>);
		}

		public static T[] ReadToArray<T>(this SqlCommand cmd)
		{
			if (typeof(T).IsValueType || typeof(T) == typeof(String))
				return cmd.ReadToArray(DataReaderExtensions.GetValue<T>);

			return cmd.ReadToArray(MappingManager.GetCreateEntityFunc<T>());
		}

		public static T[] ReadToArray<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc)
		{
			var list = new List<T>();

			var type = typeof(T);
			var isNullableScalar = type.IsValueType && Nullable.GetUnderlyingType(type) != null || type == typeof(String);

			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using (var dr = cmd.ExecuteReader(readerFlags))
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
		
		public static object[] ReadAsRow(this SqlCommand cmd)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleRow);

            using (var reader = cmd.ExecuteReader(readerFlags))
			{
				return reader.ReadAsRow();
			}
		}

		public static object[] ReadToRow<T>(this SqlCommand cmd)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleRow);

            using (var reader = cmd.ExecuteReader(readerFlags))
			{
				return reader.ReadToRow<T>();
			}
		}

		public static Rows ReadAsRows(this SqlCommand cmd)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

            using (var reader = cmd.ExecuteReader(readerFlags))
			{
				return reader.ReadAsRows();
			}
		}

		public static Rows ReadToRows<T>(this SqlCommand cmd)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

            using (var reader = cmd.ExecuteReader(readerFlags))
			{
				return reader.ReadToRows<T>();
			}
		}

		public static IDictionary<TKey, TValue> ReadToDictionary<TKey, TValue>(this SqlCommand cmd) 
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using (var reader = cmd.ExecuteReader(readerFlags))
			{
				return reader.ReadToDictionary<TKey, TValue>();
			}
		}
		
		
	}
}
