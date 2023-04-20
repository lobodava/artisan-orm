create procedure dbo.GetFolderById
	@Id int
as
begin
	set nocount on;

	declare @Path nvarchar(4000);

	select
		@Path = concat(coalesce(@Path, ''), a.[Name], ' › ')
	from
		dbo.Folders f
		inner join dbo.Folders a on a.UserId = f.UserId and f.Hid.IsDescendantOf(a.Hid) = 1
	where
		f.Id = @Id and a.Id <> @Id
	order by
		a.Hid;


	select
		Id			,
		ParentId	,
		[Name]		,
		[Level]		=	Hid.GetLevel(),
		HidCode		=	dbo.GetHidCode(Hid),
		HidPath		=	Hid.ToString(),
		[Path]		=	@Path
	from 
		dbo.Folders
	where
		Id = @Id
end;
