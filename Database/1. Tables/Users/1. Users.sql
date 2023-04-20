create sequence dbo.UserId as int minvalue 1 start with 1 increment by 1 no cache;

GO

create table dbo.Users
(
	Id				int				not null	constraint DF_UserId default next value for dbo.UserId,
	[Login]			varchar(20)		not null	,
	[Name]			nvarchar(50)	not null	,
	Email			varchar(50)		not null	,
	[RowVersion]	rowversion		not null	,

	constraint PK_Users primary key clustered (Id),

	constraint UQ_Users_Login unique ([Login]),

	constraint UQ_Users_Email unique (Email),
);
