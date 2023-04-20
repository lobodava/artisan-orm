create procedure dbo.GetUserRootFolder
	@UserId int
as
begin
	set nocount on;
	
	declare @RootHid hierarchyid = 0x;
	

	if not exists (select * from dbo.Folders where UserId = @UserId and Hid = @RootHid)
	begin

		insert into dbo.Folders (
			UserId		,
			Hid			,
			ParentId	,
			[Name]		)
		values (
			@UserId		,
			@RootHid	,
			null		,
			'<root>'	);
	end;


	select 
		Id			,
		ParentId	,
		[Name]		,
		[Level]		=	Hid.GetLevel(),
		HidCode		=	'',
		HidPath		=	Hid.ToString(),
		[Path]		=	''
	from 
		dbo.Folders
	where
		UserId = @UserId and Hid = @RootHid
end;
