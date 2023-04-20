create table dbo.GrandRecords
(
	Id		int				not null	identity,
	[Name]	varchar(30)		not null	,

	constraint PK_Grandparents primary key clustered (Id),
);
