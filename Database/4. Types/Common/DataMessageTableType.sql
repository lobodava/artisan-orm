create type dbo.DataMessageTableType as table
(
	Code		varchar(50)		not null	,
	Field		varchar(255)	null		,
	[Text]		nvarchar(4000)	null		,
	SourceId	int				null

	--primary key clustered (Code)
);