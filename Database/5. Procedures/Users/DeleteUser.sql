create procedure dbo.DeleteUser
	@UserId	int
as
begin
	set nocount on;
	
	if @UserId between 1 and 14 -- Heros can not be deleted
		return 1;

	delete from dbo.Users where	Id = @UserId;

	if @@rowcount = 0
		return 2;


end;
