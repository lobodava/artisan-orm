create type dbo.FolderTableType as table
(
	Id			int				not null	primary key clustered,
	ParentId	int				not null	,
	[Name]		nvarchar(50)	not null
);
