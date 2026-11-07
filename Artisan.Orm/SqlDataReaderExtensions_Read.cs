using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Data.SqlClient;

namespace Artisan.Orm
{ 

	public static partial class SqlDataReaderExtensions
	{
		/// <summary>If the reader has at least one row in the current result set, hands it to <paramref name="action"/>.
		/// When <paramref name="getNextResult"/> is <c>true</c> (default), advances to the next result set on exit
		/// — convenient for chaining multiple <c>Read*</c> calls on a multi-result-set <see cref="SqlDataReader"/>.</summary>
		public static void Read(this SqlDataReader dr, Action<SqlDataReader> action, bool getNextResult = true)
		{
			if (dr.Read())
				action(dr);

			if (getNextResult) dr.NextResult();
		}


		#region [ ReadTo, ReadAs ]

		private static T? ReadToValue<T>(this SqlDataReader dr, bool getNextResult = true)
		{
			T? obj;

			if (dr.Read())
				if (typeof(T).IsNullableValueType() && dr.IsDBNull(0))
					obj = default;
				else
					obj = dr.GetValue<T>();
			else
				obj = default;

			if (getNextResult) dr.NextResult();

			return obj;
		}


		/// <summary>Reads a single row, projects it through <paramref name="createFunc"/>, and optionally
		/// advances to the next result set. Returns <c>default</c> when the current set is empty.</summary>
		public static T? ReadTo<T>(this SqlDataReader dr, Func<SqlDataReader, T> createFunc, bool getNextResult = true)
		{
			var obj = dr.Read() ? createFunc(dr) : default;

			if (getNextResult) dr.NextResult();

			return obj;
		}

		/// <summary>Reads a single row using the registered mapper for <typeparamref name="T"/>
		/// (or scalar conversion for simple types). Optionally advances to the next result set.</summary>
		public static T? ReadTo<T>(this SqlDataReader dr, bool getNextResult = true)
		{
			if (typeof(T).IsSimpleType())
				return dr.ReadToValue<T>(getNextResult);

			return dr.ReadTo(MappingManager.GetCreateObjectFunc<T>(), getNextResult);
		}


		/// <summary>Reads a single row using auto-mapping (reflection-based, cached after first call) for
		/// <typeparamref name="T"/>. Optionally advances to the next result set.</summary>
		public static T? ReadAs<T>(this SqlDataReader dr, bool getNextResult = true)
		{
			if (typeof(T).IsSimpleType())
				return dr.ReadToValue<T>(getNextResult);

			return dr.ReadTo(CreateObject<T>, getNextResult);
		}

		/// <summary>Reads a single row into an <see cref="System.Dynamic.ExpandoObject"/>; returns <c>null</c> when empty.</summary>
		public static dynamic? ReadDynamic(this SqlDataReader dr, bool getNextResult = true)
		{
			var obj = dr.Read() ? dr.CreateDynamic() : null;

			if (getNextResult) dr.NextResult();

			return obj;
		}

		#endregion
	

		#region [ ReadToList, ReadAsList, ReadToArray, ReadAsArray ]
	
		private static IList<T> ReadToListOfValues<T>(this SqlDataReader dr, IList<T>? list, bool getNextResult = true)
		{
			list ??= new List<T>();

			var type = typeof(T);
			var isNullableValueType = type.IsNullableValueType();

			if (isNullableValueType)
			{
				var underlyingType = type.GetUnderlyingType();
				while (dr.Read())
					list.Add(dr.IsDBNull(0) ? default! : GetValue<T>(dr, underlyingType));  // default! — T is Nullable<X> here, null is valid
			}
			else
			{
				while (dr.Read())
					list.Add(GetValue<T>(dr, type));
			}

			if (getNextResult) dr.NextResult();

			return list;
		}

		private static IList<T> ReadToListOfObjects<T>(this SqlDataReader dr, Func<SqlDataReader, T> createFunc, IList<T>? list, bool getNextResult = true)
		{
			list ??= new List<T>();

			while (dr.Read())
				list.Add(createFunc(dr));

			if (getNextResult) dr.NextResult();

			return list;
		}

		/// <summary>Reads all rows of the current result set into <paramref name="list"/> (or a new list
		/// if <c>null</c>) via <paramref name="createFunc"/>. Optionally advances to the next result set.</summary>
		public static IList<T> ReadToList<T>(this SqlDataReader dr, Func<SqlDataReader, T> createFunc, IList<T>? list, bool getNextResult = true)
		{
			return dr.ReadToListOfObjects(createFunc, list, getNextResult);
		}

		/// <inheritdoc cref="ReadToList{T}(SqlDataReader, Func{SqlDataReader, T}, IList{T}, bool)"/>
		public static IList<T> ReadToList<T>(this SqlDataReader dr, Func<SqlDataReader, T> createFunc, bool getNextResult = true)
		{
			return dr.ReadToListOfObjects<T>(createFunc, null, getNextResult);
		}

		/// <summary>Reads all rows of the current result set into <paramref name="list"/> (or a new list
		/// if <c>null</c>) using the registered mapper for <typeparamref name="T"/>.</summary>
		public static IList<T> ReadToList<T>(this SqlDataReader dr, IList<T>? list, bool getNextResult = true)
		{
			if (typeof(T).IsSimpleType())
				return dr.ReadToListOfValues<T>(list, getNextResult);

			return dr.ReadToListOfObjects<T>(MappingManager.GetCreateObjectFunc<T>(), list, getNextResult);
		}

		/// <inheritdoc cref="ReadToList{T}(SqlDataReader, IList{T}, bool)"/>
		public static IList<T> ReadToList<T>(this SqlDataReader dr, bool getNextResult = true)
		{
			if (typeof(T).IsSimpleType())
				return dr.ReadToListOfValues<T>(null, getNextResult);

			return dr.ReadToListOfObjects<T>(MappingManager.GetCreateObjectFunc<T>(), null, getNextResult);
		}

		/// <summary>Reads all rows of the current result set into <paramref name="list"/> (or a new list
		/// if <c>null</c>) using auto-mapping (reflection-based, cached after first call).</summary>
		public static IList<T> ReadAsList<T>(this SqlDataReader dr, IList<T>? list, bool getNextResult = true)
		{
			if (typeof(T).IsSimpleType())
				return dr.ReadToListOfValues<T>(list, getNextResult);

			var key = GetAutoCreateObjectFuncKey<T>(dr);
			var autoMappingFunc = MappingManager.GetAutoCreateObjectFunc<T>(key);

			list = dr.ReadAsList(list, autoMappingFunc, key);

			if (getNextResult) dr.NextResult();

			return list;
		}

		internal static IList<T> ReadAsList<T>(this SqlDataReader dr, IList<T>? list, Func<SqlDataReader, T>? autoMappingFunc, string key)
		{
			list ??= new List<T>();

			while (dr.Read())
			{
				if (autoMappingFunc == null)
				{
					autoMappingFunc = CreateAutoMappingFunc<T>(dr);
					MappingManager.AddAutoCreateObjectFunc(key, autoMappingFunc);
				}

				list.Add(autoMappingFunc(dr));
			}

			return list;
		}

		/// <inheritdoc cref="ReadAsList{T}(SqlDataReader, IList{T}, bool)"/>
		public static IList<T> ReadAsList<T>(this SqlDataReader dr, bool getNextResult = true)
		{
			if (typeof(T).IsSimpleType())
				return dr.ReadToListOfValues<T>(null, getNextResult);

			return dr.ReadAsList<T>(null, getNextResult);
		}

		/// <summary>Reads all rows into <paramref name="list"/> (or a new list if <c>null</c>) where each
		/// row becomes an <see cref="System.Dynamic.ExpandoObject"/>.</summary>
		public static IList<dynamic> ReadDynamicList(this SqlDataReader dr, IList<dynamic>? list, bool getNextResult = true)
		{
			list ??= new List<dynamic>();

			while (dr.Read())
			{
				list.Add(dr.CreateDynamic());
			}

			if (getNextResult) dr.NextResult();

			return list;
		}

		/// <inheritdoc cref="ReadDynamicList(SqlDataReader, IList{dynamic}, bool)"/>
		public static IList<dynamic> ReadDynamicList(this SqlDataReader dr, bool getNextResult = true)
		{
			return dr.ReadDynamicList(null, getNextResult);
		}

		/// <summary>Reads all rows into a <typeparamref name="T"/>-typed array via <paramref name="createFunc"/>.</summary>
		public static T[] ReadToArray<T>(this SqlDataReader dr, Func<SqlDataReader, T> createFunc, bool getNextResult = true)
		{
			return dr.ReadToList<T>(createFunc, getNextResult).ToArray();
		}

		/// <summary>Reads all rows into a <typeparamref name="T"/>-typed array using the registered mapper.</summary>
		public static T[] ReadToArray<T>(this SqlDataReader dr, bool getNextResult = true)
		{
			return dr.ReadToList<T>(getNextResult).ToArray();
		}

		/// <summary>Reads all rows into a <typeparamref name="T"/>-typed array using auto-mapping.</summary>
		public static T[] ReadAsArray<T>(this SqlDataReader dr, bool getNextResult = true)
		{
			return dr.ReadAsList<T>(getNextResult).ToArray();
		}

		/// <summary>Reads all rows into an array of <see cref="System.Dynamic.ExpandoObject"/>.</summary>
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
					yield return dr.IsDBNull(0) ? default! : GetValue<T>(dr, underlyingType);  // default! — T is Nullable<X> here, null is valid
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
	
		/// <summary>Streams rows lazily through <paramref name="createFunc"/>. Note: the reader stays
		/// positioned on the current result set until enumeration completes — combining this with
		/// <paramref name="getNextResult"/>=<c>true</c> only triggers <c>NextResult()</c> after the consumer drains
		/// the iterator.</summary>
		public static IEnumerable<T> ReadToEnumerable<T>(this SqlDataReader dr, Func<SqlDataReader, T> createFunc, bool getNextResult = true)
		{
			return dr.ReadToEnumerableOfObjects<T>(createFunc, getNextResult);
		}
	
		/// <summary>Streams rows lazily using the registered mapper for <typeparamref name="T"/>.</summary>
		public static IEnumerable<T> ReadToEnumerable<T>(this SqlDataReader dr, bool getNextResult = true)
		{
			if (typeof(T).IsSimpleType())
				return dr.ReadToEnumerableOfValues<T>(getNextResult);

			return dr.ReadToEnumerableOfObjects<T>(MappingManager.GetCreateObjectFunc<T>(), getNextResult);
		}
	
		/// <summary>Streams rows lazily using auto-mapping (reflection-based, cached after first call).</summary>
		public static IEnumerable<T> ReadAsEnumerable<T>(this SqlDataReader dr, bool getNextResult = true)
		{
			if (typeof(T).IsSimpleType())
				return dr.ReadToEnumerableOfValues<T>(getNextResult);

			var key = GetAutoCreateObjectFuncKey<T>(dr);
			var autoMappingFunc = MappingManager.GetAutoCreateObjectFunc<T>(key);

			if (autoMappingFunc == null)
			{
				autoMappingFunc = CreateAutoMappingFunc<T>(dr);
				MappingManager.AddAutoCreateObjectFunc(key, autoMappingFunc);
			}

			return dr.ReadToEnumerableOfObjects<T>(autoMappingFunc, getNextResult);
		}
	
		#endregion
	


		#region [ ReadToObjectRow(s), ReadAsObjectRow(s) ]

		/// <summary>Reads a single row into an <see cref="ObjectRow"/> via <paramref name="createFunc"/>.</summary>
		public static ObjectRow? ReadToObjectRow(this SqlDataReader dr, Func<SqlDataReader, ObjectRow> createFunc, bool getNextResult = true)
		{
			var objectRow = dr.Read() ? createFunc(dr) : null;

			if (getNextResult) dr.NextResult();

			return objectRow;
		}

		/// <summary>Reads a single row into an <see cref="ObjectRow"/> using the registered
		/// <c>CreateObjectRow</c> mapper for <typeparamref name="T"/>.</summary>
		public static ObjectRow? ReadToObjectRow<T>(this SqlDataReader dr, bool getNextResult = true)
		{
			return dr.ReadToObjectRow(MappingManager.GetCreateObjectRowFunc<T>(), getNextResult);
		}
	

		/// <summary>Reads all rows into an <see cref="ObjectRows"/> collection via <paramref name="createFunc"/>.</summary>
		public static ObjectRows ReadToObjectRows(this SqlDataReader dr, Func<SqlDataReader, ObjectRow> createFunc, bool getNextResult = true)
		{
			var objectRows = new ObjectRows();

			while (dr.Read())
				objectRows.Add(createFunc(dr));

			if (getNextResult) dr.NextResult();

			return objectRows;
		}
	
		/// <summary>Reads all rows into an <see cref="ObjectRows"/> collection using the registered
		/// <c>CreateObjectRow</c> mapper for <typeparamref name="T"/>.</summary>
		public static ObjectRows ReadToObjectRows<T>(this SqlDataReader dr, bool getNextResult = true)
		{
			return dr.ReadToObjectRows(MappingManager.GetCreateObjectRowFunc<T>(), getNextResult);
		}


		/// <summary>Reads a single row into an <see cref="ObjectRow"/> using auto-mapping by column name.</summary>
		public static ObjectRow? ReadAsObjectRow(this SqlDataReader dr, bool getNextResult = true)
		{
			ObjectRow? objectRow = null;

			if (dr.Read())
			{
				objectRow = new ObjectRow(dr.FieldCount);

				for (var i = 0; i < dr.FieldCount; i++)
					objectRow.Add(dr.GetValue(i));
			}

			if (getNextResult) dr.NextResult();

			return objectRow;
		}

		/// <summary>Reads all rows into an <see cref="ObjectRows"/> collection using auto-mapping by column name.</summary>
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

		private static Dictionary<TKey, TValue> ReadToDictionaryOfValues<TKey, TValue>(SqlDataReader dr, Type underlyingTypeOfValue) where TKey : notnull
		{
			var dictionary = new Dictionary<TKey, TValue>();

			var keyType = typeof(TKey);

			while (dr.Read())
			{
				if (!dr.IsDBNull(0))
				{
					var key = (TKey)ConvertValue(dr.GetValue(0), keyType);
					var value = (TValue)(dr.IsDBNull(1) ? (object?)null : ConvertValue(dr.GetValue(1), underlyingTypeOfValue))!;
					dictionary.Add(key, value);
				}
			}

			return dictionary;
		}

		private static Dictionary<TKey, TObject> ReadToDictionaryOfObjects<TKey, TObject>(SqlDataReader dr, Func<SqlDataReader, TObject> createFunc) where TKey : notnull
		{
			var dictionary = new Dictionary<TKey, TObject>();

			var keyType = typeof(TKey);

			while (dr.Read())
			{
				if (!dr.IsDBNull(0))
				{
					var key = (TKey)ConvertValue(dr.GetValue(0), keyType);
					var objectValue = createFunc(dr);
					dictionary.Add(key, objectValue);
				}
			}

			return dictionary;
		}

		/// <summary>Reads the current result set into a dictionary keyed by column 0; values are produced
		/// by <paramref name="createFunc"/>.</summary>
		public static Dictionary<TKey, TValue> ReadToDictionary<TKey, TValue>(this SqlDataReader dr, Func<SqlDataReader, TValue> createFunc, bool getNextResult = true) where TKey : notnull
		{
			var dictionary = new Dictionary<TKey, TValue>();

			var keyType = typeof(TKey);

			while (dr.Read())
			{
				if (!dr.IsDBNull(0))
				{
					var key = (TKey)ConvertValue(dr.GetValue(0), keyType);
					var value = createFunc(dr);
					dictionary.Add(key, value);
				}
			}

			if (getNextResult) dr.NextResult();

			return dictionary;
		}

		/// <summary>Reads the current result set into a dictionary keyed by column 0; values are produced
		/// by the registered mapper for <typeparamref name="TValue"/>.</summary>
		public static Dictionary<TKey, TValue> ReadToDictionary<TKey, TValue>(this SqlDataReader dr, bool getNextResult = true) where TKey : notnull
		{
			Dictionary<TKey, TValue> dictionary;

			var underlyingType = typeof(TValue).GetUnderlyingType();

			if (underlyingType.IsSimpleType())
				dictionary = ReadToDictionaryOfValues<TKey, TValue>(dr, underlyingType);
			else
				dictionary = ReadToDictionaryOfObjects<TKey, TValue>(dr, MappingManager.GetCreateObjectFunc<TValue>());

			if (getNextResult) dr.NextResult();

			return dictionary;
		}

		/// <summary>Reads the current result set into a dictionary keyed by column 0; values are produced
		/// by auto-mapping (reflection-based, cached after first call).</summary>
		public static Dictionary<TKey, TValue> ReadAsDictionary<TKey, TValue>(this SqlDataReader dr, bool getNextResult = true) where TKey : notnull
		{
			Dictionary<TKey, TValue> dictionary;

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
	

		#region [ ReadToAsyncEnumerable, ReadAsAsyncEnumerable ]

		private static async IAsyncEnumerable<T> ReadToAsyncEnumerableValues<T>(
			this SqlDataReader dr,
			[EnumeratorCancellation] CancellationToken ct = default)
		{
			var type = typeof(T);
			var isNullableValueType = type.IsNullableValueType();

			if (isNullableValueType)
			{
				var underlyingType = type.GetUnderlyingType();
				while (await dr.ReadAsync(ct).ConfigureAwait(false))
					yield return dr.IsDBNull(0) ? default! : GetValue<T>(dr, underlyingType);  // default! — T is Nullable<X> here, null is valid
			}
			else
			{
				while (await dr.ReadAsync(ct).ConfigureAwait(false))
					yield return GetValue<T>(dr, type);
			}
		}

		private static async IAsyncEnumerable<T> ReadToAsyncEnumerableObjects<T>(
			this SqlDataReader dr,
			Func<SqlDataReader, T> createFunc,
			[EnumeratorCancellation] CancellationToken ct = default)
		{
			while (await dr.ReadAsync(ct).ConfigureAwait(false))
				yield return createFunc(dr);
		}

		private static async IAsyncEnumerable<T> ReadAsAsyncEnumerableObjects<T>(
			this SqlDataReader dr,
			[EnumeratorCancellation] CancellationToken ct = default)
		{
			var key = GetAutoCreateObjectFuncKey<T>(dr);
			var autoMappingFunc = MappingManager.GetAutoCreateObjectFunc<T>(key);

			if (autoMappingFunc == null)
			{
				autoMappingFunc = CreateAutoMappingFunc<T>(dr);
				MappingManager.AddAutoCreateObjectFunc(key, autoMappingFunc);
			}

			while (await dr.ReadAsync(ct).ConfigureAwait(false))
				yield return autoMappingFunc(dr);
		}


		/// <summary>
		/// Streams rows from the current result set as an <see cref="IAsyncEnumerable{T}"/>,
		/// using <paramref name="createFunc"/> to map each row.
		/// The caller is responsible for the reader's connection lifetime.
		/// Does <b>not</b> call <c>NextResult</c>.
		/// </summary>
		public static IAsyncEnumerable<T> ReadToAsyncEnumerable<T>(
			this SqlDataReader dr,
			Func<SqlDataReader, T> createFunc,
			CancellationToken ct = default)
		{
			return dr.ReadToAsyncEnumerableObjects(createFunc, ct);
		}

		/// <summary>
		/// Streams rows from the current result set as an <see cref="IAsyncEnumerable{T}"/>,
		/// using a registered mapping function for <typeparamref name="T"/> (or scalar conversion for simple types).
		/// The caller is responsible for the reader's connection lifetime.
		/// Does <b>not</b> call <c>NextResult</c>.
		/// </summary>
		public static IAsyncEnumerable<T> ReadToAsyncEnumerable<T>(
			this SqlDataReader dr,
			CancellationToken ct = default)
		{
			if (typeof(T).IsSimpleType())
				return dr.ReadToAsyncEnumerableValues<T>(ct);

			return dr.ReadToAsyncEnumerableObjects(MappingManager.GetCreateObjectFunc<T>(), ct);
		}

		/// <summary>
		/// Streams rows from the current result set as an <see cref="IAsyncEnumerable{T}"/>,
		/// using auto-mapping (reflection-based, result cached after first execution).
		/// The caller is responsible for the reader's connection lifetime.
		/// Does <b>not</b> call <c>NextResult</c>.
		/// </summary>
		public static IAsyncEnumerable<T> ReadAsAsyncEnumerable<T>(
			this SqlDataReader dr,
			CancellationToken ct = default)
		{
			if (typeof(T).IsSimpleType())
				return dr.ReadToAsyncEnumerableValues<T>(ct);

			return dr.ReadAsAsyncEnumerableObjects<T>(ct);
		}

		#endregion


		#region [ ReadToTree, ReadToTreeList ]

		/// <inheritdoc cref="SqlCommandExtensions.ReadToTree{T}(SqlCommand, Func{SqlDataReader, T}, IList{T}, bool)"/>
		public static T? ReadToTree<T>(this SqlDataReader dr, Func<SqlDataReader, T> createFunc, IList<T>? list, bool getNextResult = true, bool hierarchicallySorted = false) where T : class, INode<T>
		{
			return dr.ReadToListOfObjects<T>(createFunc, list, getNextResult).ToTree(hierarchicallySorted);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToTree{T}(SqlCommand, Func{SqlDataReader, T}, IList{T}, bool)"/>
		public static T? ReadToTree<T>(this SqlDataReader dr, Func<SqlDataReader, T> createFunc, bool getNextResult = true, bool hierarchicallySorted = false) where T : class, INode<T>
		{
			return dr.ReadToEnumerableOfObjects<T>(createFunc, getNextResult).ToTree(hierarchicallySorted);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToTree{T}(SqlCommand, IList{T}, bool)"/>
		public static T? ReadToTree<T>(this SqlDataReader dr, bool getNextResult = true, bool hierarchicallySorted = false) where T : class, INode<T>
		{
			return dr.ReadToEnumerableOfObjects<T>(MappingManager.GetCreateObjectFunc<T>(), getNextResult).ToTree(hierarchicallySorted);
		}


		/// <inheritdoc cref="SqlCommandExtensions.ReadToTreeList{T}(SqlCommand, Func{SqlDataReader, T}, IList{T}, bool)"/>
		public static IList<T> ReadToTreeList<T>(this SqlDataReader dr, Func<SqlDataReader, T> createFunc, IList<T>? list, bool getNextResult = true, bool hierarchicallySorted = false) where T : class, INode<T>
		{
			return dr.ReadToListOfObjects<T>(createFunc, list, getNextResult).ToTreeList(hierarchicallySorted);
		}
	
		/// <inheritdoc cref="SqlCommandExtensions.ReadToTreeList{T}(SqlCommand, Func{SqlDataReader, T}, IList{T}, bool)"/>
		public static IList<T> ReadToTreeList<T>(this SqlDataReader dr, Func<SqlDataReader, T> createFunc, bool getNextResult = true, bool hierarchicallySorted = false) where T: class, INode<T>
		{
			return dr.ReadToEnumerableOfObjects<T>(createFunc, getNextResult).ToTreeList(hierarchicallySorted);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToTreeList{T}(SqlCommand, Func{SqlDataReader, T}, IList{T}, bool)"/>
		public static IList<T> ReadToTreeList<T>(this SqlDataReader dr, bool getNextResult = true, bool hierarchicallySorted = false) where T: class, INode<T>
		{
			return dr.ReadToEnumerableOfObjects<T>(MappingManager.GetCreateObjectFunc<T>(), getNextResult).ToTreeList(hierarchicallySorted);
		}

	
		#endregion 

	}

}
