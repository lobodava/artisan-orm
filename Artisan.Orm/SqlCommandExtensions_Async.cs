using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Artisan.Orm
{ 

	public static partial class SqlCommandExtensions
	{
		#region [ GetReaderFlagsAndOpenConnectionAsync ]

		private static async Task<CommandBehavior> GetReaderFlagsAndOpenConnectionAsync(SqlCommand cmd, CommandBehavior defaultReaderFlag, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var readerFlags = defaultReaderFlag;

			if (cmd.Connection.State == ConnectionState.Closed)
			{
				await cmd.Connection.OpenAsync(cancellationToken).ConfigureAwait(false);

				if (defaultReaderFlag == CommandBehavior.Default)
					readerFlags = CommandBehavior.CloseConnection;
				else
					readerFlags |= CommandBehavior.CloseConnection;
			}

			return readerFlags;
		}
	
		private static async Task<SqlDataReader> ExecuteReaderAndOpenConnectionAsync(SqlCommand cmd, CommandBehavior behavior, CancellationToken cancellationToken)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, behavior, cancellationToken).ConfigureAwait(false);
			try
			{
				return await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false);
			}
			catch
			{
				// No reader exists to apply CloseConnection when execution fails.
				if ((readerFlags & CommandBehavior.CloseConnection) != 0)
				{
					try { await cmd.Connection.CloseAsync().ConfigureAwait(false); }
					catch { /* Preserve the command failure. */ }
				}
				throw;
			}
		}

		#endregion


		#region [ GetByReaderAsync, ExecuteReaderAsync ]

		/// <inheritdoc cref="GetByReader{T}(SqlCommand, Func{SqlDataReader, T})"/>
		/// <remarks>Opening and command execution are asynchronous and cancellable. The supplied callback
		/// is synchronous; it controls its own row traversal and cancellation checks.</remarks>
		public static async Task<T> GetByReaderAsync<T>(this SqlCommand cmd,  Func<SqlDataReader, T> func, CancellationToken cancellationToken = default)
		{
			using var dr = await ExecuteReaderAndOpenConnectionAsync(cmd, CommandBehavior.Default, cancellationToken).ConfigureAwait(false);
			return func(dr);
		}

		/// <inheritdoc cref="GetByReader{T}(SqlCommand, Func{SqlDataReader, SqlParameter, T})"/>
		/// <remarks>Opening and command execution are asynchronous and cancellable. The supplied callback
		/// is synchronous; it controls its own row traversal and cancellation checks.</remarks>
		public static async Task<T> GetByReaderAsync<T>(this SqlCommand cmd,  Func<SqlDataReader, SqlParameter, T> func, CancellationToken cancellationToken = default)
		{
			var returnValueParam = cmd.ReturnValueParam();

			using var dr = await ExecuteReaderAndOpenConnectionAsync(cmd, CommandBehavior.Default, cancellationToken).ConfigureAwait(false);
			return func(dr, returnValueParam);
		}

		/// <inheritdoc cref="ExecuteReader(SqlCommand, Action{SqlDataReader})"/>
		/// <remarks>Opening and command execution are asynchronous and cancellable. The supplied callback
		/// is synchronous; it controls its own row traversal and cancellation checks.</remarks>
		public static async Task<int> ExecuteReaderAsync(this SqlCommand cmd, Action<SqlDataReader> action, CancellationToken cancellationToken = default)
		{
			var returnValueParam = cmd.ReturnValueParam();

			using (var dr = await ExecuteReaderAndOpenConnectionAsync(cmd, CommandBehavior.Default, cancellationToken).ConfigureAwait(false))
			{
				action(dr);
			}

			return (int)returnValueParam.Value!;  // ReturnValueParam() always returns non-null; Value is set by SQL Server
		}

		#endregion


		#region [ ReadToAsync, ReadAsAsync ]
	
		private static async Task<T?> ReadToValueAsync<T>(this SqlCommand cmd, CancellationToken cancellationToken = default)
		{
			using var dr = await ExecuteReaderAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
			if (await dr.ReadAsync(cancellationToken).ConfigureAwait(false))
			{
				if (typeof(T).IsNullableValueType() && dr.IsDBNull(0))
					return default;

				return dr.GetValue<T>();
			}

			return default;
		}


		/// <inheritdoc cref="ReadTo{T}(SqlCommand, Func{SqlDataReader, T})"/>
		public static async Task<T?> ReadToAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, CancellationToken cancellationToken = default)
		{
			using var dr = await ExecuteReaderAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
			return await dr.ReadAsync(cancellationToken).ConfigureAwait(false) ? createFunc(dr) : default;
		}

		/// <inheritdoc cref="ReadTo{T}(SqlCommand)"/>
		public static async Task<T?> ReadToAsync<T>(this SqlCommand cmd, CancellationToken cancellationToken = default)
		{
			if (typeof(T).IsSimpleType())
				return await cmd.ReadToValueAsync<T>(cancellationToken).ConfigureAwait(false);

			return await cmd.ReadToAsync(MappingManager.GetCreateObjectFunc<T>(), cancellationToken).ConfigureAwait(false);
		}


		/// <inheritdoc cref="ReadAs{T}(SqlCommand)"/>
		public static async Task<T?> ReadAsAsync<T>(this SqlCommand cmd, CancellationToken cancellationToken = default)
		{
			if (typeof(T).IsSimpleType())
				return await cmd.ReadToValueAsync<T>(cancellationToken).ConfigureAwait(false);

			using var dr = await ExecuteReaderAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
			return await dr.ReadAsync(cancellationToken).ConfigureAwait(false) ? dr.CreateObject<T>() : default;
		}

		#endregion


		#region [ ReadToListAsync, ReadAsListAsync, ReadToArrayAsync, ReadAsArrayAsync ]
	
		private static async Task<IList<T>> ReadToListOfValuesAsync<T>(this SqlCommand cmd, IList<T>? list, CancellationToken cancellationToken = default)
		{
			list ??= new List<T>();

			var type = typeof(T);
			var isNullableValueType = type.IsNullableValueType();

			using (var dr = await ExecuteReaderAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult, cancellationToken).ConfigureAwait(false))
			{
				if (isNullableValueType)
				{
					var underlyingType = type.GetUnderlyingType();
					while (await dr.ReadAsync(cancellationToken).ConfigureAwait(false))
						list.Add(dr.IsDBNull(0) ? default! : SqlDataReaderExtensions.GetValue<T>(dr, underlyingType));  // default! — T is Nullable<X> here, null is valid
				}
				else
				{
					while (await dr.ReadAsync(cancellationToken).ConfigureAwait(false))
						list.Add(SqlDataReaderExtensions.GetValue<T>(dr, type));
				}
			}

			return list;
		}

		private static async Task<IList<T>> ReadToListOfObjectsAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, IList<T>? list, CancellationToken cancellationToken = default)
		{
			list ??= new List<T>();

			using (var dr = await ExecuteReaderAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult, cancellationToken).ConfigureAwait(false))
				while (await dr.ReadAsync(cancellationToken).ConfigureAwait(false))
					list.Add(createFunc(dr));

			return list;
		}
	

		/// <inheritdoc cref="ReadToList{T}(SqlCommand, Func{SqlDataReader, T}, IList{T})"/>
		public static async Task<IList<T>> ReadToListAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, IList<T>? list, CancellationToken cancellationToken = default)
		{
			return await cmd.ReadToListOfObjectsAsync(createFunc, list, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="ReadToList{T}(SqlCommand, Func{SqlDataReader, T})"/>
		public static async Task<IList<T>> ReadToListAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, CancellationToken cancellationToken = default)
		{
			return await cmd.ReadToListOfObjectsAsync<T>(createFunc, null, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="ReadToList{T}(SqlCommand, IList{T})"/>
		public static async Task<IList<T>> ReadToListAsync<T>(this SqlCommand cmd, IList<T>? list, CancellationToken cancellationToken = default)
		{
			if (typeof(T).IsSimpleType())
				return await cmd.ReadToListOfValuesAsync<T>(list, cancellationToken).ConfigureAwait(false);

			return await cmd.ReadToListOfObjectsAsync<T>(MappingManager.GetCreateObjectFunc<T>(), list, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="ReadToList{T}(SqlCommand)"/>
		public static async Task<IList<T>> ReadToListAsync<T>(this SqlCommand cmd, CancellationToken cancellationToken = default)
		{
			if (typeof(T).IsSimpleType())
				return await cmd.ReadToListOfValuesAsync<T>(null, cancellationToken).ConfigureAwait(false);

			return await cmd.ReadToListOfObjectsAsync<T>(MappingManager.GetCreateObjectFunc<T>(), null, cancellationToken).ConfigureAwait(false);
		}


		/// <inheritdoc cref="ReadAsList{T}(SqlCommand, IList{T})"/>
		public static async Task<IList<T>> ReadAsListAsync<T>(this SqlCommand cmd, IList<T>? list, CancellationToken cancellationToken = default)
		{
			if (typeof(T).IsSimpleType())
				return await cmd.ReadToListOfValuesAsync<T>(list, cancellationToken).ConfigureAwait(false);

			using (var dr = await ExecuteReaderAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult, cancellationToken).ConfigureAwait(false))
			{
				list ??= new List<T>();
				Func<SqlDataReader, T>? createFunc = null;
				while (await dr.ReadAsync(cancellationToken).ConfigureAwait(false))
				{
					createFunc ??= GetAutoMappingFunc<T>(dr);
					list.Add(createFunc(dr));
				}
			}

			return list;
		}

		/// <inheritdoc cref="ReadAsList{T}(SqlCommand)"/>
		public static async Task<IList<T>> ReadAsListAsync<T>(this SqlCommand cmd, CancellationToken cancellationToken = default)
		{
			if (typeof(T).IsSimpleType())
				return await cmd.ReadToListOfValuesAsync<T>(null, cancellationToken).ConfigureAwait(false);

			return await cmd.ReadAsListAsync<T>(null, cancellationToken).ConfigureAwait(false);
		}


		/// <inheritdoc cref="ReadToArray{T}(SqlCommand, Func{SqlDataReader, T})"/>
		public static async Task<T[]> ReadToArrayAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, CancellationToken cancellationToken = default)
		{
			return (await cmd.ReadToListAsync<T>(createFunc, cancellationToken).ConfigureAwait(false)).ToArray();
		}

		/// <inheritdoc cref="ReadToArray{T}(SqlCommand)"/>
		public static async Task<T[]> ReadToArrayAsync<T>(this SqlCommand cmd, CancellationToken cancellationToken = default)
		{
			return (await cmd.ReadToListAsync<T>(cancellationToken).ConfigureAwait(false)).ToArray();
		}

		/// <inheritdoc cref="ReadAsArray{T}(SqlCommand)"/>
		public static async Task<T[]> ReadAsArrayAsync<T>(this SqlCommand cmd, CancellationToken cancellationToken = default)
		{
			return (await cmd.ReadAsListAsync<T>(cancellationToken).ConfigureAwait(false)).ToArray();
		}


		/// <inheritdoc cref="ReadDynamic(SqlCommand)"/>
		public static async Task<dynamic?> ReadDynamicAsync(this SqlCommand cmd, CancellationToken cancellationToken = default)
		{
			using var dr = await ExecuteReaderAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
			return await dr.ReadAsync(cancellationToken).ConfigureAwait(false) ? dr.CreateDynamic() : null;
		}

		/// <inheritdoc cref="ReadDynamicList(SqlCommand, IList{dynamic})"/>
		public static async Task<IList<dynamic>> ReadDynamicListAsync(this SqlCommand cmd, IList<dynamic>? list, CancellationToken cancellationToken = default)
		{
			list ??= new List<dynamic>();

			var iList = (System.Collections.IList)list;

			using (var dr = await ExecuteReaderAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult, cancellationToken).ConfigureAwait(false))
			{
				while (await dr.ReadAsync(cancellationToken).ConfigureAwait(false))
					iList.Add(dr.CreateDynamic());
			}

			return list;
		}

		/// <inheritdoc cref="ReadDynamicList(SqlCommand)"/>
		public static Task<IList<dynamic>> ReadDynamicListAsync(this SqlCommand cmd, CancellationToken cancellationToken = default)
		{
			return cmd.ReadDynamicListAsync(null, cancellationToken);
		}

		/// <inheritdoc cref="ReadDynamicArray(SqlCommand)"/>
		public static async Task<dynamic[]> ReadDynamicArrayAsync(this SqlCommand cmd, CancellationToken cancellationToken = default)
		{
			return (await cmd.ReadDynamicListAsync(null, cancellationToken).ConfigureAwait(false)).ToArray();
		}

		#endregion


		private static Func<SqlDataReader, T> GetAutoMappingFunc<T>(SqlDataReader dr)
		{
			var key = SqlDataReaderExtensions.GetAutoCreateObjectFuncKey<T>(dr);
			var createFunc = MappingManager.GetAutoCreateObjectFunc<T>(key);
			if (createFunc == null)
			{
				createFunc = SqlDataReaderExtensions.CreateAutoMappingFunc<T>(dr);
				MappingManager.AddAutoCreateObjectFunc(key, createFunc);
			}
			return createFunc;
		}

		#region [ ReadToAsyncEnumerable, ReadAsAsyncEnumerable ]

		private static async IAsyncEnumerable<T> ReadToAsyncEnumerableValues<T>(
			this SqlCommand cmd,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var type = typeof(T);
			var isNullableValueType = type.IsNullableValueType();

			await using var dr = await ExecuteReaderAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult, cancellationToken).ConfigureAwait(false);

			if (isNullableValueType)
			{
				var underlyingType = type.GetUnderlyingType();
				while (await dr.ReadAsync(cancellationToken).ConfigureAwait(false))
					yield return dr.IsDBNull(0) ? default! : SqlDataReaderExtensions.GetValue<T>(dr, underlyingType);  // default! — T is Nullable<X> here, null is valid
			}
			else
			{
				while (await dr.ReadAsync(cancellationToken).ConfigureAwait(false))
					yield return SqlDataReaderExtensions.GetValue<T>(dr, type);
			}
		}

		private static async IAsyncEnumerable<T> ReadToAsyncEnumerableObjects<T>(
			this SqlCommand cmd,
			Func<SqlDataReader, T> createFunc,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await using var dr = await ExecuteReaderAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult, cancellationToken).ConfigureAwait(false);

			while (await dr.ReadAsync(cancellationToken).ConfigureAwait(false))
				yield return createFunc(dr);
		}

		private static async IAsyncEnumerable<T> ReadAsAsyncEnumerableObjects<T>(
			this SqlCommand cmd,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await using var dr = await ExecuteReaderAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult, cancellationToken).ConfigureAwait(false);

			var autoMappingFunc = GetAutoMappingFunc<T>(dr);

			while (await dr.ReadAsync(cancellationToken).ConfigureAwait(false))
				yield return autoMappingFunc(dr);
		}


		/// <summary>
		/// Streams rows from the SQL result set as an <see cref="IAsyncEnumerable{T}"/>,
		/// using <paramref name="createFunc"/> to map each row.
		/// Rows are yielded one at a time — no buffering into a list.
		/// </summary>
		public static IAsyncEnumerable<T> ReadToAsyncEnumerable<T>(
			this SqlCommand cmd,
			Func<SqlDataReader, T> createFunc,
			CancellationToken cancellationToken = default)
		{
			return cmd.ReadToAsyncEnumerableObjects(createFunc, cancellationToken);
		}

		/// <summary>
		/// Streams rows from the SQL result set as an <see cref="IAsyncEnumerable{T}"/>,
		/// using a registered mapping function for <typeparamref name="T"/> (or scalar conversion for simple types).
		/// Rows are yielded one at a time — no buffering into a list.
		/// </summary>
		public static IAsyncEnumerable<T> ReadToAsyncEnumerable<T>(
			this SqlCommand cmd,
			CancellationToken cancellationToken = default)
		{
			if (typeof(T).IsSimpleType())
				return cmd.ReadToAsyncEnumerableValues<T>(cancellationToken);

			return cmd.ReadToAsyncEnumerableObjects(MappingManager.GetCreateObjectFunc<T>(), cancellationToken);
		}

		/// <summary>
		/// Streams rows from the SQL result set as an <see cref="IAsyncEnumerable{T}"/>,
		/// using auto-mapping (reflection-based, result cached after first execution).
		/// Rows are yielded one at a time — no buffering into a list.
		/// </summary>
		public static IAsyncEnumerable<T> ReadAsAsyncEnumerable<T>(
			this SqlCommand cmd,
			CancellationToken cancellationToken = default)
		{
			if (typeof(T).IsSimpleType())
				return cmd.ReadToAsyncEnumerableValues<T>(cancellationToken);

			return cmd.ReadAsAsyncEnumerableObjects<T>(cancellationToken);
		}

		#endregion


		private static ObjectRow CreateObjectRowFromValues(SqlDataReader dr)
		{
			var row = new ObjectRow(dr.FieldCount);
			for (var i = 0; i < dr.FieldCount; i++)
				row.Add(dr.GetValue(i));
			return row;
		}

		#region [ ReadToObjectRow(s)Async, ReadAsObjectRow(s)Async ]

		/// <inheritdoc cref="ReadToObjectRow(SqlCommand, Func{SqlDataReader, ObjectRow})"/>
		public static async Task<ObjectRow?> ReadToObjectRowAsync(this SqlCommand cmd, Func<SqlDataReader, ObjectRow> createFunc, CancellationToken cancellationToken = default)
		{
			using var dr = await ExecuteReaderAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
			return await dr.ReadAsync(cancellationToken).ConfigureAwait(false) ? createFunc(dr) : null;
		}

		/// <inheritdoc cref="ReadToObjectRow{T}(SqlCommand)"/>
		public static async Task<ObjectRow?> ReadToObjectRowAsync<T>(this SqlCommand cmd, CancellationToken cancellationToken = default)
		{
			return await cmd.ReadToObjectRowAsync(MappingManager.GetCreateObjectRowFunc<T>(), cancellationToken).ConfigureAwait(false);
		}


		/// <inheritdoc cref="ReadToObjectRows(SqlCommand, Func{SqlDataReader, ObjectRow})"/>
		public static async Task<ObjectRows> ReadToObjectRowsAsync(this SqlCommand cmd, Func<SqlDataReader, ObjectRow> createFunc, CancellationToken cancellationToken = default)
		{
			using var dr = await ExecuteReaderAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult, cancellationToken).ConfigureAwait(false);
			var rows = new ObjectRows();
			while (await dr.ReadAsync(cancellationToken).ConfigureAwait(false))
				rows.Add(createFunc(dr));
			return rows;
		}

		/// <inheritdoc cref="ReadAsObjectRows(SqlCommand)"/>
		public static async Task<ObjectRows> ReadAsObjectRowsAsync(this SqlCommand cmd, CancellationToken cancellationToken = default)
		{
			return await cmd.ReadToObjectRowsAsync(CreateObjectRowFromValues, cancellationToken).ConfigureAwait(false);
		}


		/// <inheritdoc cref="ReadAsObjectRow(SqlCommand)"/>
		public static async Task<ObjectRow?> ReadAsObjectRowAsync(this SqlCommand cmd, CancellationToken cancellationToken = default)
		{
			return await cmd.ReadToObjectRowAsync(CreateObjectRowFromValues, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="ReadToObjectRows{T}(SqlCommand)"/>
		public static async Task<ObjectRows> ReadToObjectRowsAsync<T>(this SqlCommand cmd, CancellationToken cancellationToken = default)
		{
			return await cmd.ReadToObjectRowsAsync(MappingManager.GetCreateObjectRowFunc<T>(), cancellationToken).ConfigureAwait(false);
		}
	
		#endregion


		private static TValue ReadDictionaryValue<TValue>(SqlDataReader dr)
		{
			return dr.IsDBNull(1) ? (TValue)(object?)null! : dr.GetValue<TValue>(1);
		}

		private static async Task<IDictionary<TKey, TValue>> ReadDictionaryAsync<TKey, TValue>(SqlDataReader dr, Func<SqlDataReader, TValue> createFunc, CancellationToken cancellationToken) where TKey : notnull
		{
			var dictionary = new Dictionary<TKey, TValue>();
			while (await dr.ReadAsync(cancellationToken).ConfigureAwait(false))
			{
				if (!dr.IsDBNull(0))
					dictionary.Add(dr.GetValue<TKey>(0), createFunc(dr));
			}
			return dictionary;
		}

		#region [ ReadToDictionaryAsync, ReadAsDictionaryAsync ]

		/// <inheritdoc cref="ReadToDictionary{TKey, TValue}(SqlCommand, Func{SqlDataReader, TValue})"/>
		public static async Task<IDictionary<TKey, TValue>> ReadToDictionaryAsync<TKey, TValue>(this SqlCommand cmd, Func<SqlDataReader, TValue> createFunc, CancellationToken cancellationToken = default) where TKey : notnull
		{
			using var dr = await ExecuteReaderAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult, cancellationToken).ConfigureAwait(false);
			return await ReadDictionaryAsync<TKey, TValue>(dr, createFunc, cancellationToken).ConfigureAwait(false);
		}


		/// <inheritdoc cref="ReadToDictionary{TKey, TValue}(SqlCommand)"/>
		public static async Task<IDictionary<TKey, TValue>> ReadToDictionaryAsync<TKey, TValue>(this SqlCommand cmd, CancellationToken cancellationToken = default) where TKey : notnull
		{
			using var dr = await ExecuteReaderAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult, cancellationToken).ConfigureAwait(false);
			Func<SqlDataReader, TValue> createFunc = typeof(TValue).IsSimpleType()
				? ReadDictionaryValue<TValue>
				: MappingManager.GetCreateObjectFunc<TValue>();
			return await ReadDictionaryAsync<TKey, TValue>(dr, createFunc, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="ReadAsDictionary{TKey, TValue}(SqlCommand)"/>
		public static async Task<IDictionary<TKey, TValue>> ReadAsDictionaryAsync<TKey, TValue>(this SqlCommand cmd, CancellationToken cancellationToken = default) where TKey : notnull
		{
			using var dr = await ExecuteReaderAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult, cancellationToken).ConfigureAwait(false);
			Func<SqlDataReader, TValue> createFunc = typeof(TValue).IsSimpleType()
				? ReadDictionaryValue<TValue>
				: GetAutoMappingFunc<TValue>(dr);
			return await ReadDictionaryAsync<TKey, TValue>(dr, createFunc, cancellationToken).ConfigureAwait(false);
		}

		#endregion 


		#region [ ReadToTree, ReadToTreeList ]
	
		/// <inheritdoc cref="ReadToTree{T}(SqlCommand, Func{SqlDataReader, T}, IList{T}, bool)"/>
		public static async Task<T?> ReadToTreeAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, IList<T>? list, bool hierarchicallySorted = false, CancellationToken cancellationToken = default) where T : class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(createFunc, list, cancellationToken).ConfigureAwait(false)).ToTree(hierarchicallySorted);
		}

		/// <inheritdoc cref="ReadToTree{T}(SqlCommand, Func{SqlDataReader, T}, IList{T}, bool)"/>
		public static async Task<T?> ReadToTreeAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, bool hierarchicallySorted = false, CancellationToken cancellationToken = default) where T : class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(createFunc, null, cancellationToken).ConfigureAwait(false)).ToTree(hierarchicallySorted);
		}

		/// <inheritdoc cref="ReadToTree{T}(SqlCommand, IList{T}, bool)"/>
		public static async Task<T?> ReadToTreeAsync<T>(this SqlCommand cmd, IList<T>? list, bool hierarchicallySorted = false, CancellationToken cancellationToken = default) where T : class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(MappingManager.GetCreateObjectFunc<T>(), list, cancellationToken).ConfigureAwait(false)).ToTree(hierarchicallySorted);
		}

		/// <inheritdoc cref="ReadToTree{T}(SqlCommand, IList{T}, bool)"/>
		public static async Task<T?> ReadToTreeAsync<T>(this SqlCommand cmd, bool hierarchicallySorted = false, CancellationToken cancellationToken = default) where T : class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(MappingManager.GetCreateObjectFunc<T>(), null, cancellationToken).ConfigureAwait(false)).ToTree(hierarchicallySorted);
		}

		/// <inheritdoc cref="ReadToTreeList{T}(SqlCommand, Func{SqlDataReader, T}, IList{T}, bool)"/>
		public static async Task<IList<T>> ReadToTreeListAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, IList<T>? list, bool hierarchicallySorted = false, CancellationToken cancellationToken = default) where T : class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(createFunc, list, cancellationToken).ConfigureAwait(false)).ToTreeList(hierarchicallySorted);
		}

		/// <inheritdoc cref="ReadToTreeList{T}(SqlCommand, Func{SqlDataReader, T}, IList{T}, bool)"/>
		public static async Task<IList<T>> ReadToTreeListAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, bool hierarchicallySorted = false, CancellationToken cancellationToken = default) where T : class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(createFunc, null, cancellationToken).ConfigureAwait(false)).ToTreeList(hierarchicallySorted);
		}

		/// <inheritdoc cref="ReadToTreeList{T}(SqlCommand, Func{SqlDataReader, T}, IList{T}, bool)"/>
		public static async Task<IList<T>> ReadToTreeListAsync<T>(this SqlCommand cmd, IList<T>? list, bool hierarchicallySorted = false, CancellationToken cancellationToken = default) where T : class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(MappingManager.GetCreateObjectFunc<T>(), list, cancellationToken).ConfigureAwait(false)).ToTreeList(hierarchicallySorted);
		}

		/// <inheritdoc cref="ReadToTreeList{T}(SqlCommand, Func{SqlDataReader, T}, IList{T}, bool)"/>
		public static async Task<IList<T>> ReadToTreeListAsync<T>(this SqlCommand cmd, bool hierarchicallySorted = false, CancellationToken cancellationToken = default) where T : class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(MappingManager.GetCreateObjectFunc<T>(), null, cancellationToken).ConfigureAwait(false)).ToTreeList(hierarchicallySorted);
		}

		#endregion

	}

}
