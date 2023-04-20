create table dbo.UserRoles
(
	UserId			int		not null,
	RoleId		tinyint		not null,

	constraint PK_UserRoles primary key clustered (UserId, RoleId),

	constraint FK_UserRoles_UserId foreign key (UserId) references dbo.Users (Id) on delete cascade,

	constraint FK_UserRoles_RoleId foreign key (RoleId) references dbo.Roles (Id) on delete cascade
);
