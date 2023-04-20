namespace Artisan.Orm;

public static class MergeJoinExtensions
{
	/// <summary>
	/// <para>Iterate once for Master and  Detail lists.</para>
	/// <para>For equally sorted lists only!</para>
	/// </summary>
	/// <typeparam name="TMaster"></typeparam>
	/// <typeparam name="TDetail"></typeparam>
	/// <param name="masterList"></param>
	/// <param name="detailList"></param>
	/// <param name="isMasterDetailLink"></param>
	/// <param name="action"></param>
	/// <example> 
	/// This sample shows how to call the MergeJoin method for Master and  Detail lists.
	/// <code>
	/// grandRecords.MergeJoin(
	///	 records, 
	///	 (gr, r) => gr.Id == r.GrandRecordId,
	///	 (gr, r) => {r.GrandRecord = gr; gr.Records.Add(r);}
	/// );
	/// </code>
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

	/// <summary>
	/// <para>Iterate once for Master and Detail lists, with Action for each Master item.</para>
	/// <para>For equally sorted lists only!</para>
	/// </summary>
	/// <typeparam name="TMaster"></typeparam>
	/// <typeparam name="TDetail"></typeparam>
	/// <param name="masterList"></param>
	/// <param name="eachMasterAction"></param> 
	/// <param name="detailList"></param>
	/// <param name="isMasterDetailLink"></param>
	/// <param name="joinedMasterDetailAction"></param>
	/// <example> 
	/// This sample shows how to call the MergeJoin method for Master and Detail lists, with Action for each Master item.
	/// <code>
	/// grandRecord.Records.MergeJoin(
	///	 r => { r.GrandRecord = grandRecord; },
	///	 childRecords, 
	///	 (r, cr) => r.Id == cr.RecordId,
	///	 (r, cr) => {cr.Record = r; r.ChildRecords.Add(cr);}
	/// );
	/// </code>
	/// </example>		
	public static void MergeJoin<TMaster, TDetail>
	( 
		this IEnumerable<TMaster> masterList,
		Action<TMaster> eachMasterAction,

		IEnumerable<TDetail> detailList,
		Func<TMaster, TDetail, bool> isMasterDetailLink,
		Action<TMaster, TDetail> joinedMasterDetailAction
	)	
		where TMaster :class 
		where TDetail :class
	{
		var enumerator = detailList.GetEnumerator();
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

	/// <summary>
	/// <para>Iterate once for Master, First Detail and Second Detail lists.</para>
	/// <para>For equally sorted lists only!</para>
	/// </summary>
	/// <typeparam name="TMaster"></typeparam>
	/// <typeparam name="TFirstDetail"></typeparam>
	/// <typeparam name="TSecondDetail"></typeparam>
	/// <param name="masterList"></param>
	/// <param name="firstDetailList"></param>
	/// <param name="isMasterFirstDetailLink"></param>
	/// <param name="masterFirstDetailAction"></param>
	/// <param name="secondDetailList"></param>
	/// <param name="isMasterSecondDetailLink"></param>
	/// <param name="masterSecondDetailAction"></param>
	/// <example> 
	/// This sample shows how to call the MergeJoin method for Master, First Detail and Second Detail lists.
	/// <code>
	///	grandRecords.MergeJoin(
	///	 records, 
	///	 (gr, r) => gr.Id == r.GrandRecordId,
	///	 (gr, r) => { r.GrandRecord = gr; gr.Records.Add(r); },
	///	 
	///	 payments,
	///	 (gr, p) => gr.Id == p.GrandRecordId,
	///	 (gr, p) => { p.GrandRecord = gr; gr.Payements.Add(p); }
	///	);
	/// </code>
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


	/// <summary>
	/// <para>Iterate once for Master, First Detail and Second Detail lists, with Action for each Master item.</para>
	/// <para>For equally sorted lists only!</para>
	/// </summary>
	/// <typeparam name="TMaster"></typeparam>
	/// <typeparam name="TFirstDetail"></typeparam>
	/// <typeparam name="TSecondDetail"></typeparam>
	/// <param name="masterList"></param>
	/// <param name="eachMasterAction"></param> 
	/// <param name="firstDetailList"></param>
	/// <param name="isMasterFirstDetailLink"></param>
	/// <param name="masterFirstDetailAction"></param>
	/// <param name="secondDetailList"></param>
	/// <param name="isMasterSecondDetailLink"></param>
	/// <param name="masterSecondDetailAction"></param>
	/// <example> 
	/// This sample shows how to call the MergeJoin method for Master, First Detail and Second Detail lists, with Action for each Master item.
	/// <code>
	///	grandRecords.MergeJoin(
	///	 gr => { if (gr.Records == null) gr.Records = new List&lt;Record&gt;(); },
	///  
	///	 records, 
	///	 (gr, r) => gr.Id == r.GrandRecordId,
	///	 (gr, r) => { r.GrandRecord = gr; gr.Records.Add(r); },
	///	 
	///	 payments,
	///	 (gr, p) => gr.Id == p.GrandRecordId,
	///	 (gr, p) => { p.GrandRecord = gr; gr.Payements.Add(p); }
	///	);
	/// </code>
	/// </example>	
	public static void MergeJoin<TMaster, TFirstDetail, TSecondDetail>
	( 
		this IEnumerable<TMaster> masterList, 
		Action<TMaster> eachMasterAction,

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
		var firstDetailEnumerator = firstDetailList.GetEnumerator();
		var firstDetail = firstDetailEnumerator.MoveNext() ? firstDetailEnumerator.Current : null;

		var secondDetailEnumerator = secondDetailList.GetEnumerator();
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
	

	/// <summary>
	/// <para>Iterate once for Master, Detail and Sub Detail lists.</para>
	/// <para>For equally sorted lists only!</para>
	/// </summary>
	/// <typeparam name="TMaster"></typeparam>
	/// <typeparam name="TDetail"></typeparam>
	/// <typeparam name="TSubDetail"></typeparam>
	/// <param name="masterList"></param>
	/// <param name="detailList"></param>
	/// <param name="isMasterDetailLink"></param>
	/// <param name="detailAction"></param>
	/// <param name="subDetailList"></param>
	/// <param name="isDetailSubDetailLink"></param>
	/// <param name="subDetailAction"></param>
	/// <example> 
	/// This sample shows how to call the MergeJoin method for Master, Detail and Sub Detail lists.
	/// <code>
	///	grandRecords.MergeJoin(
	///	records, 
	///	(gr, r) => gr.Id == r.GrandRecordId,
	///	(gr, r) => { r.GrandRecord = gr;  gr.Records.Add(r); },
	///	
	///	childRecords,
	///	(r, cr) => r.Id == cr.RecordId,
	///	(r, cr) => { cr.Record = r;  r.ChildRecords.Add(cr); }
	///	);
	/// </code>
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

	/// <summary>
	/// <para>Iterate once for Master, Detail and Sub Detail lists, with Action for each Master item.</para>
	/// <para>For equally sorted lists only!</para>
	/// </summary>
	/// <typeparam name="TMaster"></typeparam>
	/// <typeparam name="TDetail"></typeparam>
	/// <typeparam name="TSubDetail"></typeparam>
	/// <param name="masterList"></param>
	/// <param name="eachMasterAction"></param> 
	/// <param name="detailList"></param>
	/// <param name="isMasterDetailLink"></param>
	/// <param name="detailAction"></param>
	/// <param name="subDetailList"></param>
	/// <param name="isDetailSubDetailLink"></param>
	/// <param name="subDetailAction"></param>
	/// <example> 
	/// This sample shows how to call the MergeJoin method for Master, Detail and Sub Detail lists, with Action for each Master item.
	/// <code>
	///	grandRecords.MergeJoin(
	///	 gr => { if (gr.Records == null) gr.Records = new List&lt;Record&gt;(); },
	/// 
	///	 records, 
	///	 (gr, r) => gr.Id == r.GrandRecordId,
	///	 (gr, r) => { r.GrandRecord = gr;  gr.Records.Add(r); },
	///	
	///	 childRecords,
	///	 (r, cr) => r.Id == cr.RecordId,
	///	 (r, cr) => { cr.Record = r;  r.ChildRecords.Add(cr); }
	///	);
	/// </code>
	/// </example>	
	public static void MergeJoin<TMaster, TDetail, TSubDetail>
	( 
		this IEnumerable<TMaster> masterList, 
		Action<TMaster> eachMasterAction,

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
		var detailEnumerator = detailList.GetEnumerator();
		var detail = detailEnumerator.MoveNext() ? detailEnumerator.Current : null;

		var subDetailEnumerator = subDetailList.GetEnumerator();
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
