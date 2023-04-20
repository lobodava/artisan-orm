create sequence dbo.FolderId as int minvalue 1 start with 1 increment by 1 no cache;

GO

create table dbo.Folders
(	
	UserId		int				not null	,
	Hid			hierarchyid		not null	,
	Id			int				not null	constraint DF_FolderId default next value for dbo.FolderId,
	ParentId	int					null	,
	[Name]		nvarchar(50)	not null	,

	constraint CU_Folders unique clustered (UserId asc, Hid asc),

	constraint PK_Folders primary key nonclustered (Id asc),	

	constraint FK_Folders_UserId foreign key (UserId) references dbo.Users (Id),

	constraint FK_Folders_ParentId foreign key (ParentId) references dbo.Folders (Id),
	
	constraint CH_Folders_ParentId check (Hid = 0x and ParentId is null or Hid <> 0x and ParentId is not null)
);

GO

create unique nonclustered  index CI_ParentId on dbo.Folders (
	ParentId,
	Name,
	Id
);
