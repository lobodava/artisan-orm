create view dbo.vwUsers
as
(
	select
		Id				,
		[Login]			,
		Name			,
		Email			,
		[RowVersion]	,
		RoleIds			=  dbo.GetUserRoleIds(Id)
	from
		dbo.Users u
);