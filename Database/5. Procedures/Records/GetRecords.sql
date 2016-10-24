create procedure dbo.GetRecords
as
begin
	set nocount on;

	select
		*	
	from
		dbo.vwRecords
	order by
		Id;
end;
