create procedure dbo.GenerateFolders

as 
begin

	declare @Letters table (Letter char(1));

	declare @ParentId bigint = null;


		declare @PowerIndex int = 3; -- 1,092 records

		--declare @PowerIndex int = 5; -- 19,530 records

		--declare @PowerIndex int = 10; -- 1,111,110 records, ~ 2 minutes


	insert into @Letters
	select top (@PowerIndex) l 
	from (values
		('A'),('B'),('C'),('D'),('E')
		,('F'),('G'),('H'),('I'),('J')
		,('K'),('L'),('M'),('N'),('O')
		,('P'),('Q'),('R'),('S'),('T')
		,('U'),('V'),('W'),('X'),('Y'),('Z')
	) L(l);

	drop table if exists #Hierarchy ;

	create table #Hierarchy 
	(
		UserId		int				not null	,
		Hid			hierarchyid			null	,
		Id			int				not null	identity,
		ParentId	int					null	,
		Name		nvarchar(50)	not null	,
		Level		tinyint			not null
	)

	insert into #Hierarchy (
		UserId		,
		Hid			,
		Name		,
		Level		)
	select 
		UserId		=	u.Id	, 
		Hid			=	'/'		,
		Name		=	'<root>',
		Level		=	0
	from 
		dbo.Users u
	where
		u.Id <= @PowerIndex;


	insert into #Hierarchy (
		ParentId	,
		UserId		,
		Name		,
		Level		)
	select 
		ParentId	=	h.Id,
		UserId		=	u.Id, 
		Name		=	'1' + l.Letter,
		Level		=	1
	from 
		#Hierarchy h		
		inner join dbo.Users u on u.Id = h.UserId
		cross join @Letters l
	where
		h.Level = 0
		and u.Id <= @PowerIndex;


	insert into #Hierarchy (
		ParentId	,
		UserId		,
		Name		,
		Level		)
	select 
		ParentId	=	h.Id,
		UserId		=	u.Id, 
		Name		=	'2' + l.Letter,
		Level		=	2
	from
		#Hierarchy h		
		inner join dbo.Users u on u.Id = h.UserId
		cross join @Letters l
	where
		h.Level = 1
		and u.Id <= @PowerIndex;



	insert into #Hierarchy (
		ParentId	,
		UserId		,
		Name		,
		Level		)
	select 
		ParentId	=	h.Id,
		UserId		=	u.Id, 
		Name		=	'3' + l.Letter,
		Level		=	3
	from
		#Hierarchy h		
		inner join dbo.Users u on u.Id = h.UserId
		cross join @Letters l
	where
		h.Level = 2
		and u.Id <= @PowerIndex;


	insert into #Hierarchy (
		ParentId	,
		UserId		,
		Name		,
		Level		)
	select 
		ParentId	=	h.Id,
		UserId		=	u.Id, 
		Name		=	'4' + l.Letter,
		Level		=	4
	from
		#Hierarchy h		
		inner join dbo.Users u on u.Id = h.UserId
		cross join @Letters l
	where
		h.Level = 3
		and u.Id <= @PowerIndex;



	insert into #Hierarchy (
		ParentId	,
		UserId		,
		Name		,
		Level		)
	select 
		ParentId	=	h.Id,
		UserId		=	u.Id, 
		Name		=	'5' + l.Letter,
		Level		=	5
	from
		#Hierarchy h		
		inner join dbo.Users u on u.Id = h.UserId
		cross join @Letters l
	where
		h.Level = 4
		and u.Id <= @PowerIndex;

	alter table #Hierarchy add constraint PK_Hierarchy primary key clustered (Id);

	create unique index UX_ParentId on #Hierarchy (
		UserId,
		ParentId,
		Name,
		Id
	);


	with Recursion as
	(
		select   
			UserId		=	UserId		,
			Hid			=	cast('/' as varchar(1000)),
			Id			=	Id			,
			ParentId	=	ParentId	,		
			[Name]		=	[Name]
		from
			#Hierarchy h
		where
			h.ParentId is null

		union all 

		select 
			UserId		=	h.UserId	,
			Hid			=	cast(concat(r.Hid, row_number() over (partition by h.ParentId order by h.Name, h.Id), '/') as varchar(1000)),
			Id			=	h.Id			,
			ParentId	=	h.ParentId		,		
			[Name]		=	h.[Name]	
		from 
			Recursion r		
			inner join #Hierarchy h on h.UserId = r.UserId and h.ParentId = r.Id
	)
	update h set
		Hid = r.Hid
	from
		#Hierarchy h
		inner join Recursion r on r.Id = h.Id
	where
		h.Hid is null or h.Hid <> r.Hid;

	alter table #Hierarchy drop constraint PK_Hierarchy;

	create unique clustered index CI_HId on #Hierarchy (
		UserId,
		HId
	);

	alter table #Hierarchy add constraint PK_Hierarchy primary key nonclustered (Id);


	truncate table dbo.Folders;

	insert into dbo.Folders (
		UserId		, 
		Hid			, 
		Id			, 
		ParentId	, 
		Name
	)
	select 
		UserId		,
		Hid			,
		Id			,
		ParentId	,
		Name
	from
		#Hierarchy;


	declare @MaxId int, @q nvarchar(500);
	set @MaxId = isnull((select max(Id) from dbo.Folders), 0) + 1;
	set @q = concat('alter sequence dbo.FolderId restart with ',  @MaxId);
	exec (@q);
	
end;

GO

--exec dbo.ResetFolders;

--select *, cast(Hid as varchar(1000)) StrHid from dbo.Folders order by UserId, Hid; -- UserId, ParentId, Name;
