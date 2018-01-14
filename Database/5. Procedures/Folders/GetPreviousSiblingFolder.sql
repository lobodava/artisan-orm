create procedure dbo.GetPreviousSiblingFolder
	@FolderId int
as
begin
	set nocount on;

	declare @PreviousSiblingFolderId int; 
	
	with cte as
	(
		select
			FolderId = s.Id,
			PreviousSiblingFolderId = lag(s.Id) over(order by s.Hid)
		from
			dbo.Folders f
			inner join dbo.Folders s on s.ParentId = f.ParentId
		where
			f.Id = @FolderId
	)
	select
		@PreviousSiblingFolderId = PreviousSiblingFolderId
	from
		cte
	where
		FolderId = @FolderId;	


	exec dbo.GetFolderById @PreviousSiblingFolderId;

end;
