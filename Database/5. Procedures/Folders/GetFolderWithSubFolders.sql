create procedure dbo.GetFolderWithSubFolders
	@FolderId int
as
begin
	set nocount on;

	select 
		d.Id		,
		d.ParentId	,
		d.Name		,
		[Level]		=	d.Hid.GetLevel(),
		HidCode		=	dbo.GetHidCode(d.Hid),
		HidPath		=	d.Hid.ToString()
	from 
		dbo.Folders f
		inner join dbo.Folders d on d.UserId = f.UserId and d.Hid.IsDescendantOf(f.Hid) = 1
	where
		f.Id = @FolderId
	order by
		d.Hid;
end;
