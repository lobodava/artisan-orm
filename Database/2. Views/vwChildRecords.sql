create view dbo.vwChildRecords
as
(
	select
		Id			,
		RecordId	,
		Name		
	from
		dbo.ChildRecords
);