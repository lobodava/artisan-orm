create type dbo.UserTableType as table
(
	Id		int				not null	primary key clustered,
	[Login]	varchar(20)		not null	,
	Name	nvarchar(50)	not null	,
	Email	varchar(50)		not null	,
	RoleIds	varchar(100)		null
);