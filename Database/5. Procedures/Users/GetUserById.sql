create procedure dbo.GetUserById
	@Id	int
as
begin
	set nocount on;

	select
		*
	from
		dbo.vwUsers
	where
		Id = @Id;

end;
