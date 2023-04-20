create table dbo.Records
(
	Id				int				not null	identity,
	GrandRecordId	int				not null	,
	[Name]			varchar(30)		not null	,
	RecordTypeId	tinyint				null	,	
	Number			smallint			null	,
	[Date]			datetime2(0)		null	,
	Amount			decimal(19,2)		null	,
	IsActive		bit					null	,
	Comment			nvarchar(500)		null	,

	constraint PK_Records primary key clustered (Id),

	constraint FK_GrandRecords_GrandRecordId foreign key (GrandRecordId) references dbo.GrandRecords (Id) on delete cascade,

	constraint FK_GrandRecords_RecordTypeId foreign key (RecordTypeId) references dbo.RecordTypes (Id)
);

GO

create nonclustered index IX_GrandRecordId on dbo.Records
(
	GrandRecordId asc
);
