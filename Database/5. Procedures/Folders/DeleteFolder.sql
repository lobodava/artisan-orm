create procedure dbo.DeleteFolder
	@FolderId		int,
	@WithHidReculc	bit	= 0
as
begin
	set nocount on;

	declare 
		@ParentId		 int		 ,
		@UserId			 int		 ,
		@ParentHid		 hierarchyid ,
		@StartTranCount  int		 ;

	begin try
		set @StartTranCount = @@trancount;
		if @StartTranCount = 0 
		begin
			set transaction isolation level serializable;
			begin transaction;
		end;


		begin -- init variables

			select 
				@ParentId	= ParentId ,
				@UserId		= UserId
			from
				dbo.Folders
			where
				Id = @FolderId;

			select				
				@ParentHid	= Hid	
			from
				dbo.Folders
			where
				Id = @ParentId;

		end;


		begin -- delete folder and its descendants

			delete d
			from 
				dbo.Folders f
				inner join dbo.Folders d on d.UserId = @UserId and d.Hid.IsDescendantOf(f.Hid) = 1
			where 
				f.Id = @FolderId;


			if @WithHidReculc = 1
				exec dbo.RecalcSubFolderHids @UserId, @ParentId, @ParentHid;

		end;

		if @StartTranCount = 0 commit transaction;

	end try
	begin catch
		if xact_state() <> 0 and @StartTranCount = 0 rollback transaction;
		
		declare @ErrorMessage nvarchar(4000) = dbo.GetErrorMessage();
		raiserror (@ErrorMessage, 16, 1);
		return;
	end catch;

end;
