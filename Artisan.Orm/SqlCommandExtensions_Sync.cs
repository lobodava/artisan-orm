using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;

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

		/// <summary>Opens the connection if needed, executes the command, and projects the open
		/// <see cref="SqlDataReader"/> through <paramref name="func"/>. The reader is disposed on exit;
		/// the connection is closed automatically if it was opened by this call.</summary>
		public static T GetByReader<T>(this SqlCommand cmd,  Func<SqlDataReader, T> func)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd);

			using var dr = cmd.ExecuteReader(readerFlags);
			return func(dr);
		}

		/// <summary>Same as <see cref="GetByReader{T}(SqlCommand, Func{SqlDataReader, T})"/>, but also
		/// surfaces the auto-added <c>@ReturnValue</c> output parameter so callers can read TSQL <c>RETURN</c>
		/// values alongside the result set.</summary>
		public static T GetByReader<T>(this SqlCommand cmd,  Func<SqlDataReader, SqlParameter, T> func)
		{
			var returnValueParam = cmd.ReturnValueParam();

			var readerFlags = GetReaderFlagsAndOpenConnection(cmd);

			using var dr = cmd.ExecuteReader(readerFlags);
			return func(dr, returnValueParam);
		}

		/// <summary>Executes the command, lets <paramref name="action"/> walk the open reader,
		/// and returns the TSQL <c>RETURN</c> value (the auto-added <c>@ReturnValue</c> parameter).</summary>
		public static int ExecuteReader(this SqlCommand cmd, Action<SqlDataReader> action)
		{
			var returnValueParam = cmd.ReturnValueParam();

			var readerFlags = GetReaderFlagsAndOpenConnection(cmd);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				action(dr);
			}

			return (int)returnValueParam.Value!;  // ReturnValueParam() always returns non-null; Value is set by SQL Server
		}

		#endregion


		#region [ ReadTo, ReadAs ]

		private static T? ReadToValue<T>(this SqlCommand cmd)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleRow);

			using var dr = cmd.ExecuteReader(readerFlags);
			if (dr.Read())
			{
				if (typeof(T).IsNullableValueType() && dr.IsDBNull(0))
					return default;

				return dr.GetValue<T>();
			}

			return default;
		}

		/// <summary>Reads a single row and projects it through <paramref name="createFunc"/>.
		/// Returns <c>default</c> when the result set is empty.</summary>
		public static T? ReadTo<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleRow);

			using var dr = cmd.ExecuteReader(readerFlags);
			return dr.Read() ? createFunc(dr) : default;
		}

		/// <summary>Reads a single row using the registered mapper for <typeparamref name="T"/>
		/// (or scalar conversion for simple types). Returns <c>default</c> when the result set is empty.</summary>
		/// <remarks>
		/// Use this when you have a hand-written <c>CreateObject</c> mapper marked with
		/// <c>[MapperFor(typeof(T))]</c> — the mapper is invoked directly with no reflection.
		/// Use <see cref="ReadAs{T}(SqlCommand)"/> instead when you don't want to write a mapper
		/// and are happy with reflection-based property-by-name binding (cached after the first call).
		/// </remarks>
		/// <example>
		/// <code><![CDATA[
		/// using var cmd = repo.CreateCommand();
		/// cmd.UseProcedure("dbo.GetUserById");
		/// cmd.AddIntParam("@Id", 1);
		///
		/// User? user = cmd.ReadTo<User>();
		/// ]]></code>
		/// </example>
		public static T? ReadTo<T>(this SqlCommand cmd)
		{
			if (typeof(T).IsSimpleType())
				return cmd.ReadToValue<T>();

			return cmd.ReadTo(MappingManager.GetCreateObjectFunc<T>());
		}


		/// <summary>Reads a single row using auto-mapping (reflection-based, cached after first call)
		/// for <typeparamref name="T"/>, or scalar conversion for simple types.
		/// Returns <c>default</c> when the result set is empty.</summary>
		/// <remarks>
		/// <para>The mapping is built once per (<typeparamref name="T"/>, result-set shape) pair via
		/// <see cref="System.Linq.Expressions.Expression"/> trees, then cached, so the per-call cost after warmup
		/// is comparable to a hand-written mapper.</para>
		/// <para>Prefer <see cref="ReadTo{T}(SqlCommand)"/> when a registered mapper exists for
		/// <typeparamref name="T"/> — it skips the reflection step entirely and is friendlier to AOT/trimming.</para>
		/// <para>Column-to-property matching is by name, case-insensitive. Columns without a matching property are ignored.</para>
		/// </remarks>
		/// <example>
		/// <code><![CDATA[
		/// // No [MapperFor] attribute on User needed — columns map to properties by name.
		/// using var cmd = repo.CreateCommand();
		/// cmd.UseSql("select Id, Login, Name, Email from dbo.Users where Id = @Id");
		/// cmd.AddIntParam("@Id", 1);
		///
		/// User? user = cmd.ReadAs<User>();
		/// ]]></code>
		/// </example>
		public static T? ReadAs<T>(this SqlCommand cmd)
		{
			if (typeof(T).IsSimpleType())
				return cmd.ReadToValue<T>();

			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleRow);

			using var dr = cmd.ExecuteReader(readerFlags);
			return dr.Read() ? dr.CreateObject<T>() : default;
		}

		/// <summary>Reads a single row into an <see cref="System.Dynamic.ExpandoObject"/>
		/// whose properties match the result-set columns. Returns <c>null</c> when empty.</summary>
		public static dynamic? ReadDynamic(this SqlCommand cmd)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleRow);

			using var dr = cmd.ExecuteReader(readerFlags);
			return dr.Read() ? dr.CreateDynamic() : null;
		}
	

		#endregion


		#region [ ReadToList, ReadAsList, ReadToArray, ReadAsArray ]

		private static IList<T> ReadToListOfValues<T>(this SqlCommand cmd, IList<T>? list)
		{
			list ??= new List<T>();

			var type = typeof(T);
			var isNullableValueType = type.IsNullableValueType();

			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				if (isNullableValueType)
				{
					var underlyingType = type.GetUnderlyingType();
					while (dr.Read())
						list.Add(dr.IsDBNull(0) ? default! : SqlDataReaderExtensions.GetValue<T>(dr, underlyingType));  // default! — T is Nullable<X> here, null is valid
				}
				else
				{
					while (dr.Read())
						list.Add(SqlDataReaderExtensions.GetValue<T>(dr, type));
				}
			}

			return list;
		}

		private static IList<T> ReadToListOfObjects<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, IList<T>? list)
		{
			list ??= new List<T>();

			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using (var dr = cmd.ExecuteReader(readerFlags))
				while (dr.Read())
					list.Add(createFunc(dr));

			return list;
		}
	

		/// <summary>Reads all rows of the first result set into <paramref name="list"/> (or a new list if <c>null</c>),
		/// projecting each row through <paramref name="createFunc"/>, including rows with <c>NULL</c> values.</summary>
		public static IList<T> ReadToList<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, IList<T>? list)
		{
			return cmd.ReadToListOfObjects(createFunc, list);
		}

		/// <inheritdoc cref="ReadToList{T}(SqlCommand, Func{SqlDataReader, T}, IList{T})"/>
		public static IList<T> ReadToList<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc)
		{
			return cmd.ReadToListOfObjects<T>(createFunc, null);
		}

		/// <summary>Reads all rows of the first result set into <paramref name="list"/> (or a new list if <c>null</c>)
		/// using the registered mapper for <typeparamref name="T"/> (or scalar conversion for simple types).</summary>
		public static IList<T> ReadToList<T>(this SqlCommand cmd, IList<T>? list)
		{
			if (typeof(T).IsSimpleType())
				return cmd.ReadToListOfValues<T>(list);

			return cmd.ReadToListOfObjects<T>(MappingManager.GetCreateObjectFunc<T>(), list);
		}

		/// <summary>Reads all rows of the first result set into a new <see cref="IList{T}"/> using the
		/// registered mapper for <typeparamref name="T"/> (or scalar conversion for simple types).</summary>
		/// <remarks>Hand-written mapper path — pairs with <see cref="ReadAsList{T}(SqlCommand)"/>
		/// (auto-mapping), which is cheaper to introduce but slightly more expensive at warmup.</remarks>
		/// <example>
		/// <code><![CDATA[
		/// using var cmd = repo.CreateCommand();
		/// cmd.UseProcedure("dbo.GetUsers");
		///
		/// IList<User> users = cmd.ReadToList<User>();
		/// ]]></code>
		/// </example>
		public static IList<T> ReadToList<T>(this SqlCommand cmd)
		{
			if (typeof(T).IsSimpleType())
				return cmd.ReadToListOfValues<T>(null);

			return cmd.ReadToListOfObjects<T>(MappingManager.GetCreateObjectFunc<T>(), null);
		}

		/// <summary>Reads all rows into <paramref name="list"/> (or a new list if <c>null</c>),
		/// where each row becomes an <see cref="System.Dynamic.ExpandoObject"/>.</summary>
		public static IList<dynamic> ReadDynamicList(this SqlCommand cmd, IList<dynamic>? list)
		{
			list ??= new List<dynamic>();

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

		/// <inheritdoc cref="ReadDynamicList(SqlCommand, IList{dynamic})"/>
		public static IList<dynamic> ReadDynamicList(this SqlCommand cmd)
		{
			return cmd.ReadDynamicList(null);
		}

		/// <summary>Reads all rows of the first result set into <paramref name="list"/> (or a new list if <c>null</c>)
		/// using auto-mapping (reflection-based, cached after first call).</summary>
		/// <remarks>Use <see cref="ReadToList{T}(SqlCommand, IList{T})"/> when a registered mapper exists for
		/// <typeparamref name="T"/>; it skips the reflection step.</remarks>
		public static IList<T> ReadAsList<T>(this SqlCommand cmd, IList<T>? list)
		{
			if (typeof(T).IsSimpleType())
				return cmd.ReadToListOfValues<T>(list);

			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using (var dr = cmd.ExecuteReader(readerFlags))
			{
				list = dr.ReadAsList<T>(list, getNextResult: false);
			}

			return list;
		}

		/// <summary>Reads all rows of the first result set into a new <see cref="IList{T}"/> using auto-mapping
		/// (reflection-based, cached after first call).</summary>
		/// <remarks>Reflection-built mapping is cached per (<typeparamref name="T"/>, column shape), so
		/// repeated calls with the same SQL run at hand-mapper speed. Prefer <see cref="ReadToList{T}(SqlCommand)"/>
		/// when a <c>[MapperFor]</c> mapper exists.</remarks>
		/// <example>
		/// <code><![CDATA[
		/// using var cmd = repo.CreateCommand();
		/// cmd.UseSql("select Id, Login, Name, Email from dbo.Users");
		///
		/// IList<User> users = cmd.ReadAsList<User>();
		/// ]]></code>
		/// </example>
		public static IList<T> ReadAsList<T>(this SqlCommand cmd)
		{
			if (typeof(T).IsSimpleType())
				return cmd.ReadToListOfValues<T>(null);

			return cmd.ReadAsList<T>(null);
		}


		/// <summary>Reads all rows into a <typeparamref name="T"/>-typed array, projecting through
		/// <paramref name="createFunc"/>. Convenience wrapper around <see cref="ReadToList{T}(SqlCommand, Func{SqlDataReader, T})"/>.</summary>
		public static T[] ReadToArray<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc)
		{
			return cmd.ReadToList<T>(createFunc).ToArray();
		}

		/// <summary>Reads all rows into a <typeparamref name="T"/>-typed array using the registered mapper.</summary>
		public static T[] ReadToArray<T>(this SqlCommand cmd)
		{
			return cmd.ReadToList<T>().ToArray();
		}

		/// <summary>Reads all rows into a <typeparamref name="T"/>-typed array using auto-mapping.</summary>
		public static T[] ReadAsArray<T>(this SqlCommand cmd)
		{
			return cmd.ReadAsList<T>().ToArray();
		}

		/// <summary>Reads all rows into an array of <see cref="System.Dynamic.ExpandoObject"/>.</summary>
		public static IList<dynamic> ReadDynamicArray(this SqlCommand cmd)
		{
			return cmd.ReadDynamicList().ToArray();;
		}
	
		#endregion


		#region [ ReadToEnumerable ]

		/// <summary>Streams the first result set as an <see cref="IEnumerable{T}"/> of scalar values
		/// (one per row, column 0). The underlying reader stays open until enumeration completes
		/// or the enumerator is disposed.</summary>
		public static IEnumerable<T> ReadToEnumerableValues<T>(this SqlCommand cmd)
		{
			var type = typeof(T);
			var isNullableValueType = type.IsNullableValueType();

			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using var dr = cmd.ExecuteReader(readerFlags);

			if (isNullableValueType)
			{
				var underlyingType = type.GetUnderlyingType();

				while (dr.Read())
					if (dr.IsDBNull(0))
						yield return default!;  // default! — T is Nullable<X> here, null is valid
					else
						yield return SqlDataReaderExtensions.GetValue<T>(dr, underlyingType);
			}
			else
			{
				while (dr.Read())
					yield return SqlDataReaderExtensions.GetValue<T>(dr, type);
			}
		}

		private static IEnumerable<T> ReadToEnumerableObjects<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using var dr = cmd.ExecuteReader(readerFlags);
			while (dr.Read())
				yield return createFunc(dr);
		}

		private static IEnumerable<T> ReadAsEnumerableObjects<T>(this SqlCommand cmd)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using var dr = cmd.ExecuteReader(readerFlags);
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


		/// <summary>Streams the first result set lazily through the registered mapper for <typeparamref name="T"/>
		/// (or scalar conversion for simple types). Rows are yielded one at a time — no buffering.
		/// The reader remains open until enumeration completes or the enumerator is disposed.</summary>
		public static IEnumerable<T> ReadToEnumerable<T>(this SqlCommand cmd)
		{
			if (typeof(T).IsSimpleType())
				return cmd.ReadToEnumerableValues<T>();

			return cmd.ReadToEnumerableObjects(MappingManager.GetCreateObjectFunc<T>());
		}

		/// <summary>Streams the first result set lazily, projecting each row through <paramref name="createFunc"/>.</summary>
		public static IEnumerable<T> ReadToEnumerable<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using var dr = cmd.ExecuteReader(readerFlags);

			while (dr.Read())
				yield return createFunc(dr);
		}

		/// <summary>Streams the first result set lazily using auto-mapping (reflection-based, cached per
		/// reader column shape). Use <see cref="ReadToEnumerable{T}(SqlCommand)"/> when a registered mapper
		/// exists for <typeparamref name="T"/>.</summary>
		public static IEnumerable<T> ReadAsEnumerable<T>(this SqlCommand cmd)
		{
			if (typeof(T).IsSimpleType())
				return cmd.ReadToEnumerableValues<T>();

			return cmd.ReadAsEnumerableObjects<T>();
		}


		#endregion


		#region [ ReadToObjectRow(s), ReadAsObjectRow(s) ]

		/// <summary>Reads a single row into an <see cref="ObjectRow"/> via <paramref name="createFunc"/>.
		/// Returns <c>null</c> when the result set is empty.</summary>
		public static ObjectRow? ReadToObjectRow(this SqlCommand cmd, Func<SqlDataReader, ObjectRow> createFunc)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleRow);

			using var dr = cmd.ExecuteReader(readerFlags);
			return dr.ReadToObjectRow(createFunc, false);
		}

		/// <summary>Reads a single row into an <see cref="ObjectRow"/> using the registered
		/// <c>CreateObjectRow</c> mapper for <typeparamref name="T"/>. Returns <c>null</c> when empty.</summary>
		public static ObjectRow? ReadToObjectRow<T>(this SqlCommand cmd)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleRow);

			using var dr = cmd.ExecuteReader(readerFlags);
			return dr.ReadToObjectRow<T>();
		}


		/// <summary>Reads all rows into an <see cref="ObjectRows"/> collection via <paramref name="createFunc"/>.</summary>
		public static ObjectRows ReadToObjectRows(this SqlCommand cmd, Func<SqlDataReader, ObjectRow> createFunc)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using var dr = cmd.ExecuteReader(readerFlags);
			return dr.ReadToObjectRows(createFunc, false);
		}

		/// <summary>Reads all rows into an <see cref="ObjectRows"/> collection using the registered
		/// <c>CreateObjectRow</c> mapper for <typeparamref name="T"/>.</summary>
		public static ObjectRows ReadToObjectRows<T>(this SqlCommand cmd)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using var dr = cmd.ExecuteReader(readerFlags);
			return dr.ReadToObjectRows<T>();
		}

	
		/// <summary>Reads a single row into an <see cref="ObjectRow"/> using auto-mapping by column name.
		/// Returns <c>null</c> when the result set is empty.</summary>
		public static ObjectRow? ReadAsObjectRow(this SqlCommand cmd)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleRow);

			using var dr = cmd.ExecuteReader(readerFlags);
			return dr.ReadAsObjectRow();
		}
	
		/// <summary>Reads all rows into an <see cref="ObjectRows"/> collection using auto-mapping by column name.</summary>
		public static ObjectRows ReadAsObjectRows(this SqlCommand cmd)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using var dr = cmd.ExecuteReader(readerFlags);
			return dr.ReadAsObjectRows();
		}

		#endregion
	

		#region [ ReadToDictionary, ReadAsDictionary ]

		/// <summary>Reads the first result set into a dictionary keyed by column 0; values are produced
		/// by <paramref name="createFunc"/>.</summary>
		public static IDictionary<TKey, TValue> ReadToDictionary<TKey, TValue>(this SqlCommand cmd, Func<SqlDataReader, TValue> createFunc) where TKey : notnull
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using var dr = cmd.ExecuteReader(readerFlags);
			return dr.ReadToDictionary<TKey, TValue>(createFunc);
		}

		/// <summary>Reads the first result set into a dictionary keyed by column 0; values are produced
		/// by the registered mapper for <typeparamref name="TValue"/>.</summary>
		public static IDictionary<TKey, TValue> ReadToDictionary<TKey, TValue>(this SqlCommand cmd) where TKey : notnull
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using var dr = cmd.ExecuteReader(readerFlags);
			return dr.ReadToDictionary<TKey, TValue>();
		}

		/// <summary>Reads the first result set into a dictionary keyed by column 0; values are produced
		/// by auto-mapping (reflection-based, cached after first call).</summary>
		public static IDictionary<TKey, TValue> ReadAsDictionary<TKey, TValue>(this SqlCommand cmd) where TKey : notnull
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd, CommandBehavior.SingleResult);

			using var dr = cmd.ExecuteReader(readerFlags);
			return dr.ReadAsDictionary<TKey, TValue>();
		}

		#endregion 


		#region [ ReadToTree, ReadToTreeList ]

		/// <summary>Reads a flat <see cref="INode{T}"/> result set, projects each row through
		/// <paramref name="createFunc"/> into <paramref name="list"/>, then assembles the rows into a tree
		/// by linking each node to its parent and returns the root.</summary>
		/// <remarks>
		/// <para><typeparamref name="T"/> must implement <see cref="INode{T}"/>, which means each row exposes
		/// <c>Id</c>, <c>ParentId</c> and a <c>Children</c> collection. Rows whose <c>ParentId</c> is <c>null</c>
		/// are treated as roots; everything else is attached to its parent's <c>Children</c>.</para>
		/// <para>Pass <paramref name="hierarchicallySorted"/>=<c>true</c> when the SQL already returns rows in
		/// pre-order traversal (e.g. <c>order by hierarchyid</c> or <c>order by Path</c>) — that switches the
		/// builder to a faster single-pass mode that walks the list as a stack.</para>
		/// <para>If multiple roots are present, only the first is returned. Use <see cref="ReadToTreeList{T}(SqlCommand, Func{SqlDataReader, T}, IList{T}, bool)"/>
		/// when the result set may contain a forest.</para>
		/// </remarks>
		/// <example>
		/// <code><![CDATA[
		/// public class Folder : INode<Folder>
		/// {
		///     public int Id { get; set; }
		///     public int? ParentId { get; set; }
		///     public string Name { get; set; }
		///     public IList<Folder>? Children { get; set; }
		/// }
		///
		/// // SQL returns Id, ParentId, Name ordered hierarchically.
		/// var root = repo.GetByCommand(cmd =>
		/// {
		///     cmd.UseProcedure("dbo.GetFolderTree");
		///     cmd.AddIntParam("@RootFolderId", 1);
		///     return cmd.ReadToTree<Folder>(hierarchicallySorted: true);
		/// });
		/// ]]></code>
		/// </example>
		public static T? ReadToTree<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, IList<T>? list, bool hierarchicallySorted = false) where T : class, INode<T>
		{
			return cmd.ReadToListOfObjects<T>(createFunc, list).ToTree(hierarchicallySorted);
		}

		/// <inheritdoc cref="ReadToTree{T}(SqlCommand, Func{SqlDataReader, T}, IList{T}, bool)"/>
		public static T? ReadToTree<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, bool hierarchicallySorted = false) where T : class, INode<T>
		{
			return cmd.ReadToEnumerableObjects<T>(createFunc).ToTree(hierarchicallySorted);
		}

		/// <summary>Reads a flat <see cref="INode{T}"/> result set using the registered mapper for
		/// <typeparamref name="T"/> and assembles a tree, returning the root.</summary>
		/// <remarks>Pass <paramref name="hierarchicallySorted"/>=<c>true</c> when the SQL already returns rows in
		/// pre-order traversal — that enables a faster single-pass build.</remarks>
		public static T? ReadToTree<T>(this SqlCommand cmd, IList<T>? list, bool hierarchicallySorted = false) where T : class, INode<T>
		{
			return cmd.ReadToListOfObjects<T>(MappingManager.GetCreateObjectFunc<T>(), list).ToTree(hierarchicallySorted);
		}

		/// <inheritdoc cref="ReadToTree{T}(SqlCommand, IList{T}, bool)"/>
		public static T? ReadToTree<T>(this SqlCommand cmd, bool hierarchicallySorted = false) where T : class, INode<T>
		{
			return cmd.ReadToEnumerableObjects<T>(MappingManager.GetCreateObjectFunc<T>()).ToTree(hierarchicallySorted);
		}

		/// <summary>Reads a flat <see cref="INode{T}"/> result set, links nodes into a forest, and returns
		/// the list of root nodes.</summary>
		/// <remarks>Same algorithm as <see cref="ReadToTree{T}(SqlCommand, Func{SqlDataReader, T}, IList{T}, bool)"/>,
		/// but returns every <c>ParentId == null</c> row as an independent root instead of just the first one.
		/// Use this when the query may legitimately produce multiple top-level nodes (e.g. all top-level
		/// folders for several users).</remarks>
		public static IList<T> ReadToTreeList<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, IList<T>? list, bool hierarchicallySorted = false) where T : class, INode<T>
		{
			return cmd.ReadToListOfObjects<T>(createFunc, list).ToTreeList(hierarchicallySorted);
		}

		/// <inheritdoc cref="ReadToTreeList{T}(SqlCommand, Func{SqlDataReader, T}, IList{T}, bool)"/>
		public static IList<T> ReadToTreeList<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, bool hierarchicallySorted = false) where T : class, INode<T>
		{
			return cmd.ReadToEnumerableObjects<T>(createFunc).ToTreeList(hierarchicallySorted);
		}

		/// <inheritdoc cref="ReadToTreeList{T}(SqlCommand, Func{SqlDataReader, T}, IList{T}, bool)"/>
		public static IList<T> ReadToTreeList<T>(this SqlCommand cmd, IList<T>? list, bool hierarchicallySorted = false) where T : class, INode<T>
		{
			return cmd.ReadToListOfObjects<T>(MappingManager.GetCreateObjectFunc<T>(), list).ToTreeList(hierarchicallySorted);
		}

		/// <inheritdoc cref="ReadToTreeList{T}(SqlCommand, Func{SqlDataReader, T}, IList{T}, bool)"/>
		public static IList<T> ReadToTreeList<T>(this SqlCommand cmd, bool hierarchicallySorted = false) where T : class, INode<T>
		{
			return cmd.ReadToEnumerableObjects<T>(MappingManager.GetCreateObjectFunc<T>()).ToTreeList(hierarchicallySorted);
		}

		#endregion 

	}

}
