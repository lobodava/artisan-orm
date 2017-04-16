create table dbo.Users
(
	Id				int				not null	identity,
	[Login]			varchar(20)		not null	,
	Name			nvarchar(50)	not null	,
	Email			varchar(50)		not null	,
	[RowVersion] 	rowversion		not null	,

	
	constraint PK_Users primary key clustered (Id),

	constraint UQ_Users_Login unique ([Login]),

	constraint UQ_Users_Email unique (Email),
);