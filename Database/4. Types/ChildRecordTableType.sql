create type dbo.ChildRecordTableType as table
(
	Id			int				not null	primary key clustered,
	RecordId	int				not null	,
	Name		varchar(30)		not null	
);