create table dbo.RecordTypes
(
	Id		tinyint			not null	,	
	Code	varchar(30)		not null	,
	Name	nvarchar(50)	not null	,	

	constraint PK_RecordTypes primary key clustered (Id),
	
	constraint UQ_RecordTypes_Code unique (Code),

	constraint UQ_RecordTypes_Name unique (Name),
);

GO 