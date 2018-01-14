create procedure dbo.GetNextSiblingFolder
	@FolderId int
as
begin
	set nocount on;

	declare @NextSiblingFolderId int; 
	
	with cte as
	(
		select
			FolderId = s.Id,
			NextSiblingFolderId = lead(s.Id) over(order by s.Hid)
		from
			dbo.Folders f
			inner join dbo.Folders s on s.ParentId = f.ParentId
		where
			f.Id = @FolderId
	)
	select
		@NextSiblingFolderId = NextSiblingFolderId
	from
		cte
	where
		FolderId = @FolderId;	


	exec dbo.GetFolderById @NextSiblingFolderId;

end;
