create table dbo.Roles
(
	Id		tinyint			not null	,	
	Code	varchar(30)		not null	,
	Name	nvarchar(50)	not null	,	

	constraint PK_Roles primary key clustered (Id),
	
	constraint UQ_Roles_Code unique (Code),

	constraint UQ_Roles_Name unique (Name),
);
