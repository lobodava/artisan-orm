create procedure dbo.GetImmediateSubFolders
	@FolderId int
as
begin
	set nocount on;

	select 
		Id			,
		ParentId	,
		[Name]		,
		[Level]		=	Hid.GetLevel(),
		HidCode		=	dbo.GetHidCode(Hid),
		HidPath		=	Hid.ToString()
	from 
		dbo.Folders
	where
		ParentId = @FolderId
	order by
		Hid;

end;
