create procedure dbo.GetRecordById
	@Id	int
as
begin
	set nocount on;

	select
		*	
	from
		dbo.vwRecords
	where
		Id = @Id;
end;
