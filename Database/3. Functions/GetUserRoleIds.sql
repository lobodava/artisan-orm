create function dbo.GetUserRoleIds
(
    @UserId int
)
returns varchar(100)
as
begin

	declare @UserRoleIds varchar(100);

	select
		@UserRoleIds = concat(coalesce(@UserRoleIds + ',', ''), RoleId) 
	from	
		dbo.UserRoles
	where
		UserId = @UserId;

    return @UserRoleIds;    
end
