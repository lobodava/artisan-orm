create procedure dbo.SaveGrandRecords
	@GrandRecords	dbo.GrandRecordTableType	readonly,
	@Records		dbo.RecordTableType			readonly,
	@ChildRecords	dbo.ChildRecordTableType	readonly
as
begin
	set nocount on;
	
	declare @GrandRecordIds table ( InsertedId int primary key, ParamId int unique)
	declare @RecordIds table ( InsertedId int primary key, ParamId int unique, [Action] nvarchar(10))

	
	declare @StartTranCount int;

	begin try
		set @StartTranCount = @@trancount;
		if @StartTranCount = 0 begin transaction;


		begin -- save GrandRecords 

			merge into dbo.GrandRecords as target
				using 
				(
					select
						Id		, 
						Name	
					from
						@GrandRecords
				) 
				as source on source.Id = target.Id

			when matched then
				update set				
					Name	=	source.Name		

			when not matched by target then													 	
				insert (	
					Name	)
				values (
					source.Name	)

			output inserted.Id,	source.Id
			into @GrandRecordIds ( InsertedId, ParamId);

		end; 


		begin -- save Records 

			merge into dbo.Records as target
			using 
			(
				select
					Id				, 
					GrandRecordId	=	ids.InsertedId, 
					Name			, 
					RecordTypeId	,
					Number			, 
					[Date]			, 
					Amount			, 
					IsActive		, 
					Comment			
				from
					@Records r
					inner join @GrandRecordIds ids on ids.ParamId = r.GrandRecordId
			) 
			as source on source.Id = target.Id

			when matched then
				update set				
					GrandRecordId	=	source.GrandRecordId, 
					Name			=	source.Name			, 
					RecordTypeId	=	source.RecordTypeId	,
					Number			=	source.Number		, 
					[Date]			=	source.[Date]		, 
					Amount			=	source.Amount		, 
					IsActive		=	source.IsActive		, 
					Comment			=	source.Comment			

			when not matched by target then													 	
				insert (	
					GrandRecordId	, 
					Name			,
					RecordTypeId	, 
					Number			, 
					[Date]			, 
					Amount			, 
					IsActive		, 
					Comment			)
				values (
					source.GrandRecordId, 
					source.Name			, 
					source.RecordTypeId	,
					source.Number		, 
					source.[Date]		, 
					source.Amount		, 
					source.IsActive		, 
					source.Comment		)

			when not matched by source and target.GrandRecordId in (select InsertedId from @GrandRecordIds) then
				delete

			output isnull(inserted.Id, deleted.Id),	isnull(source.Id, deleted.Id), $action
			into @RecordIds (InsertedId, ParamId, [Action]);

			
			delete from @RecordIds where [Action] = 'DELETE';
		end;


		begin -- save ChildRecords

			merge into dbo.ChildRecords as target
				using 
				(
					select
						Id			, 
						RecordId	=	ids.InsertedId, 
						Name		
					from
						@ChildRecords cr
						inner join @RecordIds ids on ids.ParamId = cr.RecordId
				) 
				as source on source.Id = target.Id

			when matched then
				update set				
					RecordId	=	source.RecordId	, 
					Name		=	source.Name			

			when not matched by target then													 	
				insert (	
					RecordId	, 
					Name		)
				values (
					source.RecordId	, 
					source.Name		)
		
			when not matched by source and target.RecordId in (select InsertedId from @RecordIds) then
				delete;

		end;


		if @StartTranCount = 0 commit transaction;

	end try
	begin catch
		if xact_state() <> 0 and @StartTranCount = 0 rollback transaction;
		
		declare @ErrorMessage nvarchar(4000) = dbo.GetErrorMessage();
		raiserror (@ErrorMessage, 16, 1);
		return;
	end catch;



	begin -- output saved Grand Records and its descendants

		select
			fn.*	
		from
			dbo.vwGrandRecords fn
			inner join @GrandRecordIds ids on ids.InsertedId = fn.Id
		order by
			Id;
		
			
		select
			fn.*	
		from
			dbo.vwRecordsWithTypes fn
			inner join @RecordIds ids on ids.InsertedId = fn.Id
		order by
			GrandRecordId,
			Id;


		select
			fn.*	
		from
			dbo.vwChildRecords fn
			inner join dbo.Records r on r.Id = fn.RecordId
			inner join @RecordIds ids on ids.InsertedId = r.Id
		order by
			r.GrandRecordId,
			fn.RecordId,
			fn.Id;

	end;



end;
