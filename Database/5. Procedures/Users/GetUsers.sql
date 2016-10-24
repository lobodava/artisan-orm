create procedure dbo.GetUsers

as
begin
	set nocount on;


	select
		fn.*
	from
		dbo.vwUsers fn
	order by
		Id;

end;
