create procedure dbo.SaveRecords
	@Records dbo.RecordTableType readonly
as
begin
	set nocount on;
	

	declare @RecordIds table ( InsertedId int, ParamId int)


	merge into dbo.Records as target
		using 
		(
			select
				Id				, 
				GrandRecordId	, 
				Name			, 
				RecordTypeId	,
				Number			, 
				[Date]			, 
				Amount			, 
				IsActive		, 
				Comment			
			from
				@Records
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

	output inserted.Id,	source.Id
	into @RecordIds ( InsertedId, ParamId);



	select
		fn.*
	from
		dbo.vwRecords fn
		inner join @RecordIds ids on ids.InsertedId = fn.Id;


end;
