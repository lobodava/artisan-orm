create procedure dbo.RecalcSubFolderHids
	@UserId			int				,
	@ParentId		int				,
	@ParentHid		hierarchyid		,
	@OldParentId	int				=	null,
	@OldParentHid	hierarchyid		=	null
as 
begin

	declare @ParentHidStr varchar(1000)	= cast(@ParentHid as varchar(1000));
	declare @OldParentHidStr varchar(1000)	= cast(@OldParentId as varchar(1000));

	with Recursion as
	(
		select					
			Id			,
			ParentId	,		
			[Name]		,
			OldHid		=	cast(Hid as varchar(1000)),
			NewHid		=	cast(
								concat( 
									case when ParentId = @ParentId then @ParentHidStr else @OldParentHidStr end,
									row_number() over (order by [Name], Id),
									'/'
								) 
								as varchar(1000)
							)
		from
			dbo.Folders
		where
			ParentId in (@ParentId, @OldParentId)

		union all 

		select					
			Id			=	f.Id			,
			ParentId	=	f.ParentId		,		
			[Name]		=	f.[Name]		,
			OldHid		=	cast(f.Hid as varchar(1000)),
			NewHid		=	cast(concat(r.NewHid, row_number() over (partition by f.ParentId order by f.Name, f.Id), '/') as varchar(1000))					
		from 
			Recursion r		
			inner join dbo.Folders f on f.ParentId = r.Id 
		where
			r.OldHid <> r.NewHid
	)
	update f set 
		Hid = r.NewHid
	from
		dbo.Folders f
		inner join Recursion r on r.Id = f.Id and f.Hid <> r.NewHid
	where
		f.UserId = @UserId
				
	option (recompile);
	
end;
