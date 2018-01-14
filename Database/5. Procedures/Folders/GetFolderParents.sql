create procedure dbo.GetFolderWithParents
	@FolderId int
as
begin
	set nocount on;

	select 
		a.Id		,
		a.ParentId	,
		a.Name		,
		[Level]		=	a.Hid.GetLevel(),
		HidCode		=	dbo.GetHidCode(a.Hid),
		HidPath		=	a.Hid.ToString()
	from 
		dbo.Folders f
		inner join dbo.Folders a on f.UserId = a.UserId and f.Hid.IsDescendantOf(a.Hid) = 1
	where
		f.Id = @FolderId
	order by
		a.Hid;

end;
