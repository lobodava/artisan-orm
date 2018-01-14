create procedure dbo.SaveFolder
	@Folder			dbo.FolderTableType	readonly,
	@WithHidReculc	bit	= 0
as
begin
	set nocount on;
		
	begin -- variable declaration 
	
		declare 
			@ParamId		 int			,
			@ParentId		 int			,
			@UserId			 int			,
			@ParentHid		 hierarchyid	,
			@ParentHidStr	 varchar(1000)	,
			@StartTranCount  int			,
			@OldParentId	 int			,
			@OldParentHid	 hierarchyid	,
			@OldHid			 hierarchyid	,
			@NewHid			 hierarchyid	;		


		declare @FolderIds  table ( 
			InsertedId		int			not null,
			OldParentId		int				null,
			OldParentHid	hierarchyid		null,	
			OldHid			hierarchyid		null,
			NewHid			hierarchyid		null	
		)

	end;


	begin try
		set @StartTranCount = @@trancount;
		if @StartTranCount = 0 
		begin
			set transaction isolation level serializable;
			begin transaction;
		end;
		
		begin -- init variables and lock parent for update 

			select @ParamId = Id, @ParentId = ParentId from @Folder;
			
			select
				@UserId			= UserId,
				@ParentHid		= Hid	,
				@ParentHidStr	= cast(Hid as varchar(1000))		
			from
				dbo.Folders
			where
				Id = @ParentId;
		end;
			
		if @WithHidReculc	= 1
		begin -- merge calculated hierarchical data with existing folders 

			merge into dbo.Folders as target
				using 
				(
					select 
						Hid			=	cast(concat(@ParentHidStr, -1, '/') as varchar(1000)),
						Id			,
						ParentId	,
						[Name]
					from 
						@Folder
				) 
				as source on source.Id = target.Id 

			when matched and target.UserId = @UserId then
				update set
					ParentId	=	source.ParentId	,						
					[Name]		=	source.[Name]					

			when not matched by target and source.Id = 0  then													 	
				insert (
					UserId		,
					Hid			,
					ParentId	,
					Name		)
				values (
					@UserId			,
					source.Hid		,
					source.ParentId	,
					source.Name		)
			output
				inserted.Id,
				deleted.ParentId,
				deleted.Hid.GetAncestor(1)				
			into 
				@FolderIds (
					InsertedId		,
					OldParentId		,
					OldParentHid	);


			select top 1
				@OldParentId	=	OldParentId		,
				@OldParentHid	=	OldParentHid	
			from 
				@FolderIds

			--exec dbo.ResetSubFolderHids @ParentId, @ParentHid, @UserId;

			exec dbo.ReculcSubFolderHids @UserId, @ParentId, @ParentHid, @OldParentId, @OldParentHid ;

		end
		else
		begin

			merge into dbo.Folders as target
				using 
				(
					select		
						Hid		 =	@ParentHid.GetDescendant(
										LAG (case when t.Id is null then f.Hid end) over(order by coalesce(t.[Name], f.[Name])),
										LEAD(case when t.Id is null then f.Hid end) over(order by coalesce(t.[Name], f.[Name]))
									),
						Id		 =	coalesce( t.Id		 , f.Id		  ),
						ParentId =	coalesce( t.ParentId , f.ParentId ),
						[Name]	 =	coalesce( t.[Name]	 , f.[Name]	  )
					from 
						(select * from dbo.Folders where ParentId = @ParentId)  f
						full join @Folder t on t.Id = f.Id
				) 
				as source on source.Id = @ParamId and source.Id = target.Id 

			when matched and target.UserId = @UserId then
				update set
					Hid			=	source.Hid		,
					[Name]		=	source.[Name]	,
					ParentId	=	source.ParentId

			when not matched by target and source.Id = 0  then													 	
				insert (
					UserId		,
					Hid			,
					ParentId	,
					Name		)
				values (
					@UserId			,
					source.Hid		,
					source.ParentId	,
					source.Name		)
			output  
				inserted.Id		,
				deleted.Hid		,
				inserted.Hid
			into 
				@FolderIds		(
					InsertedId	,
					OldHid		,
					NewHid		);


			select top 1
				@OldHid	=	OldHid	,
				@NewHid	=	NewHid	
			from 
				@FolderIds;


			if @OldHid <> @NewHid 
				update dbo.Folders set
					Hid = Hid.GetReparentedValue(@OldHid, @NewHid) 
				where
					UserId = @UserId
					and Hid.IsDescendantOf(@OldHid) = 1;
		end;


		if @StartTranCount = 0 commit transaction;

	end try
	begin catch
		if xact_state() <> 0 and @StartTranCount = 0 rollback transaction;
		
		declare @ErrorMessage nvarchar(4000) = dbo.GetErrorMessage();
		raiserror (@ErrorMessage, 16, 1);
		return;
	end catch;


 
	begin  -- output of the saved Folder

		declare @FolderId int = coalesce
		(
			(select InsertedId from @FolderIds),
			(select Id from @Folder),
			0
		);

		exec dbo.GetFolderById @Id = @FolderId;
	end;	

end;