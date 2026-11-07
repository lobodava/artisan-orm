using System;
using System.Collections.Generic;

namespace Artisan.Orm
{

	/// <summary>
	/// In-memory merge-join helpers for stitching together multiple flat result sets into an
	/// object graph (master / detail / sub-detail) without an O(N&#215;M) nested loop or a hash map.
	/// </summary>
	/// <remarks>
	/// <para><b>What it does.</b> Walks the master and detail enumerables in lock-step a single time
	/// (O(N+M) total work, no lookup collection), and calls your <c>action</c> delegate for each
	/// linked pair. Pair this with <see cref="SqlCommandExtensions.ReadToLists{T1, T2}(Microsoft.Data.SqlClient.SqlCommand)"/>
	/// to assemble parent-child graphs from a single round-trip.</para>
	///
	/// <para><b>Critical precondition.</b> Both lists must be <b>sorted by the same join key, in the
	/// same direction</b>, with details grouped in the order their masters are visited. Every detail
	/// must have a matching master in this traversal. An unmatched detail is not skipped: it holds
	/// the cursor in place and can prevent all subsequent details from being joined. The predicate
	/// only reports equality and cannot tell the algorithm which cursor should advance.</para>
	///
	/// <para><b>When to use this vs LINQ <c>GroupJoin</c>.</b> <c>GroupJoin</c> allocates a lookup
	/// dictionary keyed by the master id, which is O(N) memory and a per-key hash. <c>MergeJoin</c>
	/// uses constant auxiliary space; enumerators and caller callbacks may still allocate. The cost
	/// is the ordering and matching-parent preconditions. Every acquired enumerator is disposed.</para>
	/// </remarks>
	public static class MergeJoinExtensions
	{
		/// <summary>Walks <paramref name="masterList"/> and <paramref name="detailList"/> once in parallel,
		/// invoking <paramref name="action"/> on every (master, detail) pair for which
		/// <paramref name="isMasterDetailLink"/> returns <c>true</c>.</summary>
		/// <typeparam name="TMaster">Master entity type.</typeparam>
		/// <typeparam name="TDetail">Detail entity type.</typeparam>
		/// <param name="masterList">Master list, sorted by the join key.</param>
		/// <param name="detailList">Detail list, sorted by the same join key as <paramref name="masterList"/>.</param>
		/// <param name="isMasterDetailLink">Predicate that returns <c>true</c> when a detail belongs to a master
		/// (typically <c>(m, d) =&gt; m.Id == d.MasterId</c>).</param>
		/// <param name="action">Called for each linked pair — wire up navigation properties here.</param>
		/// <example>
		/// <code><![CDATA[
		/// // Stored procedure returns: select * from GrandRecords order by Id;
		/// //                           select * from Records       order by GrandRecordId;
		/// var (grandRecords, records) = repo.GetByCommand(cmd =>
		/// {
		///     cmd.UseProcedure("dbo.GetGrandRecordsWithRecords");
		///     return cmd.ReadToLists<GrandRecord, Record>();
		/// });
		///
		/// grandRecords.MergeJoin(
		///     records,
		///     (gr, r) => gr.Id == r.GrandRecordId,
		///     (gr, r) => { r.GrandRecord = gr; gr.Records.Add(r); });
		/// ]]></code>
		/// </example>
		public static void MergeJoin<TMaster, TDetail>
		(
			this IEnumerable<TMaster> masterList,

			IEnumerable<TDetail> detailList,
			Func<TMaster, TDetail, bool> isMasterDetailLink,
			Action<TMaster, TDetail> action
		)
			where TMaster :class
			where TDetail :class
		{
			masterList.MergeJoin(null, detailList, isMasterDetailLink, action);
		}

		/// <summary>Same as <see cref="MergeJoin{TMaster, TDetail}(IEnumerable{TMaster}, IEnumerable{TDetail}, Func{TMaster, TDetail, bool}, Action{TMaster, TDetail})"/>,
		/// but additionally invokes <paramref name="eachMasterAction"/> on every master — handy when each master
		/// needs initialization (e.g. allocating an empty <c>Children</c> list) before details are attached.</summary>
		/// <example>
		/// <code><![CDATA[
		/// grandRecords.MergeJoin(
		///     gr => { gr.Records ??= new List<Record>(); },
		///     records,
		///     (gr, r) => gr.Id == r.GrandRecordId,
		///     (gr, r) => { r.GrandRecord = gr; gr.Records.Add(r); });
		/// ]]></code>
		/// </example>
		public static void MergeJoin<TMaster, TDetail>
		(
			this IEnumerable<TMaster> masterList,
			Action<TMaster>? eachMasterAction,

			IEnumerable<TDetail> detailList,
			Func<TMaster, TDetail, bool> isMasterDetailLink,
			Action<TMaster, TDetail> joinedMasterDetailAction
		)
			where TMaster :class
			where TDetail :class
		{
			using var enumerator = detailList.GetEnumerator();
			var detail = enumerator.MoveNext() ? enumerator.Current : null;

			foreach (var master in masterList)
			{
				eachMasterAction?.Invoke(master);

				while (detail != null && isMasterDetailLink(master, detail))
				{
					joinedMasterDetailAction(master, detail);

					detail = enumerator.MoveNext() ? enumerator.Current : null;
				}
			}
		}

		/// <summary>Walks <paramref name="masterList"/> and two <em>sibling</em> detail lists in parallel —
		/// both linked to the master by independent predicates. Use when a master has two unrelated
		/// child collections (e.g. an order has both line-items and payments).</summary>
		/// <remarks>The first detail list is consumed against <paramref name="isMasterFirstDetailLink"/>,
		/// then the second against <paramref name="isMasterSecondDetailLink"/>, all within a single pass
		/// over masters. The two detail lists do not need to be linked to each other — only each must be
		/// sorted by the foreign key it shares with the master.</remarks>
		/// <example>
		/// <code><![CDATA[
		/// // Two flat lists, both sorted by GrandRecordId; we attach each to its parent.
		/// grandRecords.MergeJoin(
		///     records,
		///     (gr, r) => gr.Id == r.GrandRecordId,
		///     (gr, r) => { r.GrandRecord = gr; gr.Records.Add(r); },
		///
		///     payments,
		///     (gr, p) => gr.Id == p.GrandRecordId,
		///     (gr, p) => { p.GrandRecord = gr; gr.Payments.Add(p); });
		/// ]]></code>
		/// </example>
		public static void MergeJoin<TMaster, TFirstDetail, TSecondDetail>
		(
			this IEnumerable<TMaster> masterList,

			IEnumerable<TFirstDetail> firstDetailList,
			Func<TMaster, TFirstDetail, bool> isMasterFirstDetailLink,
			Action<TMaster, TFirstDetail> masterFirstDetailAction,

			IEnumerable<TSecondDetail> secondDetailList,
			Func<TMaster, TSecondDetail, bool> isMasterSecondDetailLink,
			Action<TMaster, TSecondDetail> masterSecondDetailAction
		)
			where TMaster :class
			where TFirstDetail :class
			where TSecondDetail : class
		{
			masterList.MergeJoin(null, firstDetailList, isMasterFirstDetailLink, masterFirstDetailAction, secondDetailList, isMasterSecondDetailLink, masterSecondDetailAction);
		}


		/// <inheritdoc cref="MergeJoin{TMaster, TFirstDetail, TSecondDetail}(IEnumerable{TMaster}, IEnumerable{TFirstDetail}, Func{TMaster, TFirstDetail, bool}, Action{TMaster, TFirstDetail}, IEnumerable{TSecondDetail}, Func{TMaster, TSecondDetail, bool}, Action{TMaster, TSecondDetail})"/>
		/// <remarks>This overload also invokes <paramref name="eachMasterAction"/> per master before either
		/// detail list is processed — useful for initializing both child collections in one place.</remarks>
		public static void MergeJoin<TMaster, TFirstDetail, TSecondDetail>
		(
			this IEnumerable<TMaster> masterList,
			Action<TMaster>? eachMasterAction,

			IEnumerable<TFirstDetail> firstDetailList,
			Func<TMaster, TFirstDetail, bool> isMasterFirstDetailLink,
			Action<TMaster, TFirstDetail> masterFirstDetailAction,

			IEnumerable<TSecondDetail> secondDetailList,
			Func<TMaster, TSecondDetail, bool> isMasterSecondDetailLink,
			Action<TMaster, TSecondDetail> masterSecondDetailAction
		)
			where TMaster :class
			where TFirstDetail :class
			where TSecondDetail : class
		{
			using var firstDetailEnumerator = firstDetailList.GetEnumerator();
			var firstDetail = firstDetailEnumerator.MoveNext() ? firstDetailEnumerator.Current : null;

			using var secondDetailEnumerator = secondDetailList.GetEnumerator();
			var secondDetail = secondDetailEnumerator.MoveNext() ? secondDetailEnumerator.Current : null;

			foreach (var master in masterList)
			{
				eachMasterAction?.Invoke(master);

				while (firstDetail != null && isMasterFirstDetailLink(master, firstDetail))
				{
					masterFirstDetailAction(master, firstDetail);

					firstDetail = firstDetailEnumerator.MoveNext() ? firstDetailEnumerator.Current : null;
				}

				while (secondDetail != null &&  isMasterSecondDetailLink(master, secondDetail))
				{
					masterSecondDetailAction(master, secondDetail);

					secondDetail = secondDetailEnumerator.MoveNext() ? secondDetailEnumerator.Current : null;
				}
			}
		}


		/// <summary>Walks Master &#8594; Detail &#8594; SubDetail in a single pass — three nested levels
		/// of merge-join, suitable for hierarchies like <c>GrandRecord &#8594; Record &#8594; ChildRecord</c>.</summary>
		/// <remarks>
		/// <para>The link predicates form a chain: <paramref name="isMasterDetailLink"/> tests
		/// (master, detail) and <paramref name="isDetailSubDetailLink"/> tests (detail, subDetail) — note
		/// that the third level is keyed on <em>detail</em>, not master.</para>
		/// <para>Detail groups must follow the actual master traversal. Sub-detail groups must follow
		/// the actual detail traversal, which need not be sorted by detail ID. Sorting sub-details by
		/// detail-FK alone is insufficient when the detail IDs are visited in another order. Every
		/// detail and sub-detail must have its parent in the corresponding input.</para>
		/// </remarks>
		/// <example>
		/// <code><![CDATA[
		/// var (grandRecords, records, childRecords) =
		///     repo.ReadToLists<GrandRecord, Record, ChildRecord>("dbo.GetThreeLevelGraph");
		///
		/// grandRecords.MergeJoin(
		///     records,
		///     (gr, r) => gr.Id == r.GrandRecordId,
		///     (gr, r) => { r.GrandRecord = gr; gr.Records.Add(r); },
		///
		///     childRecords,
		///     (r, cr) => r.Id == cr.RecordId,
		///     (r, cr) => { cr.Record = r; r.ChildRecords.Add(cr); });
		/// ]]></code>
		/// </example>
		public static void MergeJoin<TMaster, TDetail, TSubDetail>
		(
			this IEnumerable<TMaster> masterList,

			IEnumerable<TDetail> detailList,
			Func<TMaster, TDetail, bool> isMasterDetailLink,
			Action<TMaster, TDetail> detailAction,

			IEnumerable<TSubDetail> subDetailList,
			Func<TDetail, TSubDetail, bool> isDetailSubDetailLink,
			Action<TDetail, TSubDetail> subDetailAction
		)
			where TMaster :class
			where TDetail :class
			where TSubDetail : class
		{
			masterList.MergeJoin(null, detailList, isMasterDetailLink, detailAction, subDetailList, isDetailSubDetailLink, subDetailAction);
		}

		/// <inheritdoc cref="MergeJoin{TMaster, TDetail, TSubDetail}(IEnumerable{TMaster}, IEnumerable{TDetail}, Func{TMaster, TDetail, bool}, Action{TMaster, TDetail}, IEnumerable{TSubDetail}, Func{TDetail, TSubDetail, bool}, Action{TDetail, TSubDetail})"/>
		/// <remarks>This overload additionally invokes <paramref name="eachMasterAction"/> once per master
		/// — typically used to initialize the master's children collection (<c>gr.Records ??= new List&lt;Record&gt;()</c>)
		/// before the inner walk fills it.</remarks>
		public static void MergeJoin<TMaster, TDetail, TSubDetail>
		(
			this IEnumerable<TMaster> masterList,
			Action<TMaster>? eachMasterAction,

			IEnumerable<TDetail> detailList,
			Func<TMaster, TDetail, bool> isMasterDetailLink,
			Action<TMaster, TDetail> detailAction,

			IEnumerable<TSubDetail> subDetailList,
			Func<TDetail, TSubDetail, bool> isDetailSubDetailLink,
			Action<TDetail, TSubDetail> subDetailAction
		)
			where TMaster :class
			where TDetail :class
			where TSubDetail : class
		{
			using var detailEnumerator = detailList.GetEnumerator();
			var detail = detailEnumerator.MoveNext() ? detailEnumerator.Current : null;

			using var subDetailEnumerator = subDetailList.GetEnumerator();
			var subDetail = subDetailEnumerator.MoveNext() ? subDetailEnumerator.Current : null;

			foreach (var master in masterList)
			{
				eachMasterAction?.Invoke(master);

				while (detail != null && isMasterDetailLink(master, detail))
				{
					while (subDetail != null &&  isDetailSubDetailLink(detail, subDetail))
					{
						subDetailAction(detail, subDetail);

						subDetail = subDetailEnumerator.MoveNext() ? subDetailEnumerator.Current : null;
					}

					detailAction(master, detail);

					detail = detailEnumerator.MoveNext() ? detailEnumerator.Current : null;
				}
			}
		}

	}

}
