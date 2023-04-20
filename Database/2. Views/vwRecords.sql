create view dbo.vwRecords
as
(
	select
		Id				,
		GrandRecordId	,
		[Name]			,
		RecordTypeId	,
		Number			,
		[Date]			,
		Amount			,
		IsActive		,
		Comment
	from
		dbo.Records
);
