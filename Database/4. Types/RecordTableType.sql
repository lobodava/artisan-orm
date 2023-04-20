create type dbo.RecordTableType as table
(
	Id				int				not null	primary key clustered,
	GrandRecordId	int				not null	,
	[Name]			varchar(30)		not null	,
	RecordTypeId	tinyint				null	,
	Number			smallint			null	,
	[Date]			datetime2			null	,
	Amount			decimal(19,2)		null	,
	IsActive		bit					null	,
	Comment			nvarchar(500)		null
);
