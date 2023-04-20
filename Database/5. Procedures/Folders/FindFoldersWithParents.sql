create procedure dbo.FindFoldersWithParents
	@UserId		int,
	@Level		smallint	 =	null, 
	@FolderName	nvarchar(50) =	null	
as
begin
	set nocount on;

	declare @Hids table (Hid hierarchyid primary key);
	
	insert into @Hids
	select 
		Hid
	from
		dbo.Folders
	where
		UserId = @UserId
		and (@Level is null or Hid.GetLevel() = @Level)
		and (@FolderName is null or Name like '%' + @FolderName + '%');


	select distinct
		d.Id		,
		d.ParentId	,
		d.[Name]	,
		[Level]		=	d.Hid.GetLevel(),
		HidCode		=	dbo.GetHidCode(d.Hid),
		HidPath		=	d.Hid.ToString()
	from 
		@Hids h
		inner join dbo.Folders d on h.Hid.IsDescendantOf(d.Hid) = 1
	where
		UserId = @UserId
	order by 
		HidCode;

end;
