create type dbo.GrandRecordTableType as table
(
	Id		int				not null	primary key clustered,
	[Name]	varchar(30)		not null	
);
