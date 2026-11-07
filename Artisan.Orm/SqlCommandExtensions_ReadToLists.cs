using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Artisan.Orm
{

	public static partial class SqlCommandExtensions
	{

		#region [ ReadToLists — private async helpers ]

		private static async Task<IList<T>> ReadResultSetAsync<T>(SqlDataReader dr, bool advance, CancellationToken ct)
		{
			var list = new List<T>();

			if (typeof(T).IsSimpleType())
			{
				var type = typeof(T);
				var isNullableValueType = type.IsNullableValueType();

				if (isNullableValueType)
				{
					var underlyingType = type.GetUnderlyingType();
					while (await dr.ReadAsync(ct).ConfigureAwait(false))
						list.Add(dr.IsDBNull(0) ? default! : SqlDataReaderExtensions.GetValue<T>(dr, underlyingType));  // default! — T is Nullable<X> here, null is valid
				}
				else
				{
					while (await dr.ReadAsync(ct).ConfigureAwait(false))
						list.Add(SqlDataReaderExtensions.GetValue<T>(dr, type));
				}
			}
			else
			{
				var createFunc = MappingManager.GetCreateObjectFunc<T>();
				while (await dr.ReadAsync(ct).ConfigureAwait(false))
					list.Add(createFunc(dr));
			}

			if (advance)
				await dr.NextResultAsync(ct).ConfigureAwait(false);

			return list;
		}

		private static async Task<IList<T>> ReadResultSetAsync<T>(SqlDataReader dr, Func<SqlDataReader, T> createFunc, bool advance, CancellationToken ct)
		{
			var list = new List<T>();

			while (await dr.ReadAsync(ct).ConfigureAwait(false))
				list.Add(createFunc(dr));

			if (advance)
				await dr.NextResultAsync(ct).ConfigureAwait(false);

			return list;
		}

		#endregion


		#region [ ReadToLists<T1,T2> ]

		/// <summary>
		/// Reads two result sets from a single query into two lists,
		/// using registered mappers for <typeparamref name="T1"/> and <typeparamref name="T2"/>.
		/// </summary>
		/// <remarks>
		/// Saves a round-trip when you need two related shapes from one stored procedure or batch.
		/// The first result set must contain rows matching <typeparamref name="T1"/>, the second
		/// matching <typeparamref name="T2"/>. Use C# tuple deconstruction on the returned value.
		/// </remarks>
		/// <example>
		/// <code><![CDATA[
		/// // dbo.GetUsersAndRoles selects from dbo.Users, then from dbo.Roles.
		/// using var cmd = repo.CreateCommand();
		/// cmd.UseProcedure("dbo.GetUsersAndRoles");
		///
		/// var (users, roles) = cmd.ReadToLists<User, Role>();
		/// ]]></code>
		/// </example>
		public static (IList<T1>, IList<T2>) ReadToLists<T1, T2>(this SqlCommand cmd)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd);

			using var dr = cmd.ExecuteReader(readerFlags);
			var list1 = dr.ReadToList<T1>(getNextResult: true);
			var list2 = dr.ReadToList<T2>(getNextResult: false);

			return (list1, list2);
		}

		/// <summary>
		/// Reads two result sets from a single query into two lists,
		/// using the supplied factory functions.
		/// </summary>
		public static (IList<T1>, IList<T2>) ReadToLists<T1, T2>(
			this SqlCommand cmd,
			Func<SqlDataReader, T1> createFunc1,
			Func<SqlDataReader, T2> createFunc2)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd);

			using var dr = cmd.ExecuteReader(readerFlags);
			var list1 = dr.ReadToList<T1>(createFunc1, getNextResult: true);
			var list2 = dr.ReadToList<T2>(createFunc2, getNextResult: false);

			return (list1, list2);
		}

		/// <summary>
		/// Asynchronously reads two result sets from a single query into two lists,
		/// using registered mappers for <typeparamref name="T1"/> and <typeparamref name="T2"/>.
		/// </summary>
		public static async Task<(IList<T1>, IList<T2>)> ReadToListsAsync<T1, T2>(
			this SqlCommand cmd,
			CancellationToken cancellationToken = default)
		{
			using var dr = await ExecuteReaderAndOpenConnectionAsync(cmd, CommandBehavior.Default, cancellationToken).ConfigureAwait(false);
			var list1 = await ReadResultSetAsync<T1>(dr, advance: true,  ct: cancellationToken).ConfigureAwait(false);
			var list2 = await ReadResultSetAsync<T2>(dr, advance: false, ct: cancellationToken).ConfigureAwait(false);

			return (list1, list2);
		}

		/// <summary>
		/// Asynchronously reads two result sets from a single query into two lists,
		/// using the supplied factory functions.
		/// </summary>
		public static async Task<(IList<T1>, IList<T2>)> ReadToListsAsync<T1, T2>(
			this SqlCommand cmd,
			Func<SqlDataReader, T1> createFunc1,
			Func<SqlDataReader, T2> createFunc2,
			CancellationToken cancellationToken = default)
		{
			using var dr = await ExecuteReaderAndOpenConnectionAsync(cmd, CommandBehavior.Default, cancellationToken).ConfigureAwait(false);
			var list1 = await ReadResultSetAsync<T1>(dr, createFunc1, advance: true,  ct: cancellationToken).ConfigureAwait(false);
			var list2 = await ReadResultSetAsync<T2>(dr, createFunc2, advance: false, ct: cancellationToken).ConfigureAwait(false);

			return (list1, list2);
		}

		#endregion


		#region [ ReadToLists<T1,T2,T3> ]

		/// <summary>
		/// Reads three result sets from a single query into three lists,
		/// using registered mappers.
		/// </summary>
		public static (IList<T1>, IList<T2>, IList<T3>) ReadToLists<T1, T2, T3>(this SqlCommand cmd)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd);

			using var dr = cmd.ExecuteReader(readerFlags);
			var list1 = dr.ReadToList<T1>(getNextResult: true);
			var list2 = dr.ReadToList<T2>(getNextResult: true);
			var list3 = dr.ReadToList<T3>(getNextResult: false);

			return (list1, list2, list3);
		}

		/// <summary>
		/// Reads three result sets from a single query into three lists,
		/// using the supplied factory functions.
		/// </summary>
		public static (IList<T1>, IList<T2>, IList<T3>) ReadToLists<T1, T2, T3>(
			this SqlCommand cmd,
			Func<SqlDataReader, T1> createFunc1,
			Func<SqlDataReader, T2> createFunc2,
			Func<SqlDataReader, T3> createFunc3)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd);

			using var dr = cmd.ExecuteReader(readerFlags);
			var list1 = dr.ReadToList<T1>(createFunc1, getNextResult: true);
			var list2 = dr.ReadToList<T2>(createFunc2, getNextResult: true);
			var list3 = dr.ReadToList<T3>(createFunc3, getNextResult: false);

			return (list1, list2, list3);
		}

		/// <summary>
		/// Asynchronously reads three result sets from a single query into three lists,
		/// using registered mappers.
		/// </summary>
		public static async Task<(IList<T1>, IList<T2>, IList<T3>)> ReadToListsAsync<T1, T2, T3>(
			this SqlCommand cmd,
			CancellationToken cancellationToken = default)
		{
			using var dr = await ExecuteReaderAndOpenConnectionAsync(cmd, CommandBehavior.Default, cancellationToken).ConfigureAwait(false);
			var list1 = await ReadResultSetAsync<T1>(dr, advance: true,  ct: cancellationToken).ConfigureAwait(false);
			var list2 = await ReadResultSetAsync<T2>(dr, advance: true,  ct: cancellationToken).ConfigureAwait(false);
			var list3 = await ReadResultSetAsync<T3>(dr, advance: false, ct: cancellationToken).ConfigureAwait(false);

			return (list1, list2, list3);
		}

		/// <summary>
		/// Asynchronously reads three result sets from a single query into three lists,
		/// using the supplied factory functions.
		/// </summary>
		public static async Task<(IList<T1>, IList<T2>, IList<T3>)> ReadToListsAsync<T1, T2, T3>(
			this SqlCommand cmd,
			Func<SqlDataReader, T1> createFunc1,
			Func<SqlDataReader, T2> createFunc2,
			Func<SqlDataReader, T3> createFunc3,
			CancellationToken cancellationToken = default)
		{
			using var dr = await ExecuteReaderAndOpenConnectionAsync(cmd, CommandBehavior.Default, cancellationToken).ConfigureAwait(false);
			var list1 = await ReadResultSetAsync<T1>(dr, createFunc1, advance: true,  ct: cancellationToken).ConfigureAwait(false);
			var list2 = await ReadResultSetAsync<T2>(dr, createFunc2, advance: true,  ct: cancellationToken).ConfigureAwait(false);
			var list3 = await ReadResultSetAsync<T3>(dr, createFunc3, advance: false, ct: cancellationToken).ConfigureAwait(false);

			return (list1, list2, list3);
		}

		#endregion


		#region [ ReadToLists<T1,T2,T3,T4> ]

		/// <summary>
		/// Reads four result sets from a single query into four lists,
		/// using registered mappers.
		/// </summary>
		public static (IList<T1>, IList<T2>, IList<T3>, IList<T4>) ReadToLists<T1, T2, T3, T4>(this SqlCommand cmd)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd);

			using var dr = cmd.ExecuteReader(readerFlags);
			var list1 = dr.ReadToList<T1>(getNextResult: true);
			var list2 = dr.ReadToList<T2>(getNextResult: true);
			var list3 = dr.ReadToList<T3>(getNextResult: true);
			var list4 = dr.ReadToList<T4>(getNextResult: false);

			return (list1, list2, list3, list4);
		}

		/// <summary>
		/// Reads four result sets from a single query into four lists,
		/// using the supplied factory functions.
		/// </summary>
		public static (IList<T1>, IList<T2>, IList<T3>, IList<T4>) ReadToLists<T1, T2, T3, T4>(
			this SqlCommand cmd,
			Func<SqlDataReader, T1> createFunc1,
			Func<SqlDataReader, T2> createFunc2,
			Func<SqlDataReader, T3> createFunc3,
			Func<SqlDataReader, T4> createFunc4)
		{
			var readerFlags = GetReaderFlagsAndOpenConnection(cmd);

			using var dr = cmd.ExecuteReader(readerFlags);
			var list1 = dr.ReadToList<T1>(createFunc1, getNextResult: true);
			var list2 = dr.ReadToList<T2>(createFunc2, getNextResult: true);
			var list3 = dr.ReadToList<T3>(createFunc3, getNextResult: true);
			var list4 = dr.ReadToList<T4>(createFunc4, getNextResult: false);

			return (list1, list2, list3, list4);
		}

		/// <summary>
		/// Asynchronously reads four result sets from a single query into four lists,
		/// using registered mappers.
		/// </summary>
		public static async Task<(IList<T1>, IList<T2>, IList<T3>, IList<T4>)> ReadToListsAsync<T1, T2, T3, T4>(
			this SqlCommand cmd,
			CancellationToken cancellationToken = default)
		{
			using var dr = await ExecuteReaderAndOpenConnectionAsync(cmd, CommandBehavior.Default, cancellationToken).ConfigureAwait(false);
			var list1 = await ReadResultSetAsync<T1>(dr, advance: true,  ct: cancellationToken).ConfigureAwait(false);
			var list2 = await ReadResultSetAsync<T2>(dr, advance: true,  ct: cancellationToken).ConfigureAwait(false);
			var list3 = await ReadResultSetAsync<T3>(dr, advance: true,  ct: cancellationToken).ConfigureAwait(false);
			var list4 = await ReadResultSetAsync<T4>(dr, advance: false, ct: cancellationToken).ConfigureAwait(false);

			return (list1, list2, list3, list4);
		}

		/// <summary>
		/// Asynchronously reads four result sets from a single query into four lists,
		/// using the supplied factory functions.
		/// </summary>
		public static async Task<(IList<T1>, IList<T2>, IList<T3>, IList<T4>)> ReadToListsAsync<T1, T2, T3, T4>(
			this SqlCommand cmd,
			Func<SqlDataReader, T1> createFunc1,
			Func<SqlDataReader, T2> createFunc2,
			Func<SqlDataReader, T3> createFunc3,
			Func<SqlDataReader, T4> createFunc4,
			CancellationToken cancellationToken = default)
		{
			using var dr = await ExecuteReaderAndOpenConnectionAsync(cmd, CommandBehavior.Default, cancellationToken).ConfigureAwait(false);
			var list1 = await ReadResultSetAsync<T1>(dr, createFunc1, advance: true,  ct: cancellationToken).ConfigureAwait(false);
			var list2 = await ReadResultSetAsync<T2>(dr, createFunc2, advance: true,  ct: cancellationToken).ConfigureAwait(false);
			var list3 = await ReadResultSetAsync<T3>(dr, createFunc3, advance: true,  ct: cancellationToken).ConfigureAwait(false);
			var list4 = await ReadResultSetAsync<T4>(dr, createFunc4, advance: false, ct: cancellationToken).ConfigureAwait(false);

			return (list1, list2, list3, list4);
		}

		#endregion

	}

}
