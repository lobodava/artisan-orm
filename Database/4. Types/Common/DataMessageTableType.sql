create type dbo.DataReplyMessageTableType as table
(
	Code		varchar(50)		not null	,
	[Text]		nvarchar(4000)	null		,
	Id			bigint			null		,
	[Value]		sql_variant		null		,

	unique (Code, Id)
);
