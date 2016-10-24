create procedure dbo.GetGrandRecordById
	@Id	int
as
begin
	set nocount on;

	select
		*	
	from
		dbo.vwGrandRecords
	where
		Id = @Id;
		
			
	select
		*	
	from
		dbo.vwRecordsWithTypes
	where
		GrandRecordId = @Id
	order by
		Id;


	select
		fn.*	
	from
		dbo.vwChildRecords fn
		inner join dbo.Records r on r.Id = fn.RecordId
	where
		GrandRecordId = @Id
	order by
		fn.RecordId,
		fn.Id;


end;
