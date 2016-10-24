create view dbo.vwRecordsWithTypes
as
(
	select
		r.Id			,
		GrandRecordId	,
		r.Name			,
		RecordTypeId	,
		Number			,
		[Date]			,
		Amount			,
		IsActive		,
		Comment			,

		rt_Id			=	rt.Id	,
		rt_Code			=	rt.Code	,
		rt_Name			=	rt.Name
		
	from
		dbo.Records r
		left join dbo.RecordTypes rt on rt.Id = r.RecordTypeId
);