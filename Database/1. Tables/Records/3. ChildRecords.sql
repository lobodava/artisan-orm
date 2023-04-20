create table dbo.ChildRecords
(
	Id			int				not null	identity,
	RecordId	int				not null	,
	[Name]		varchar(30)		not null	,

	constraint PK_ChildRecords primary key clustered (Id),

	constraint FK_ChildRecords_RecordId foreign key (RecordId) references dbo.Records (Id) on delete cascade
);

GO

create nonclustered index IX_RecordId on dbo.ChildRecords
(
	RecordId asc
);
