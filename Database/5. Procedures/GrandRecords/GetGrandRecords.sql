create procedure dbo.GetGrandRecords
as
begin
	set nocount on;

	select
		*	
	from
		dbo.vwGrandRecords
	order by
		Id;
		
			
	select
		*	
	from
		dbo.vwRecordsWithTypes
	order by
		GrandRecordId,
		Id;


	select
		fn.*	
	from
		dbo.vwChildRecords fn
		inner join dbo.Records r on r.Id = fn.RecordId
	order by
		r.GrandRecordId,
		fn.RecordId,
		fn.Id;


end;
