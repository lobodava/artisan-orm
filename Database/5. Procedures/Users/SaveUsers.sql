create procedure dbo.SaveUsers
	@Users	dbo.UserTableType	readonly
as
begin
	set nocount on;
	
	declare @UserIds table ( InsertedId int primary key, ParamId int unique)

	
	declare @DataStatus varchar(20);
	declare @DataMessages dbo.DataMessageTableType;
			--create type dbo.DataMessageTableType as table
			--(
			--	Code		varchar(50)		not null	,
			--	Field		varchar(255)	null		,
			--	[Text]		nvarchar(4000)	null		,
			--	SourceId	int				null
			--);



	declare @StartTranCount int;

	begin try
		set @StartTranCount = @@trancount;
		if @StartTranCount = 0 begin transaction;

		begin -- validation

			
			with cte as 
			(
				select
					Id		= coalesce( t.Id, u.Id ),
					[Login] = coalesce( t.[Login], u.[Login]),
					LoginRn = row_number() over(partition by coalesce( t.[Login], u.[Login] ) order by case when t.Id = u.Id or u.Id is null then 0 else 1 end),
					Name	= coalesce( t.Name,	u.Name),
					NameRn	= row_number() over(partition by coalesce(t.Name, u.Name) order by case when t.Id = u.Id or u.Id is null then 0 else 1 end),
					Email	= coalesce( t.Email, u.Email),
					EmailRn	= row_number() over(partition by coalesce(t.Email, u.Email) order by case when t.Id = u.Id or u.Id is null then 0 else 1 end)
				from
					dbo.Users u with (tablockx, holdlock)
					full join @Users t on t.Id = u.Id
			)
			insert into @DataMessages (
				Code		,
				Field		,
				[Text]		,
				SourceId	)

			select 
				Code		=	'NON_UNIQUE_LOGIN',
				Field		=	'Login',
				[Text]		=	'Dublicate value: ' + [Login],
				SourceId	=	Id
			from
				cte
			where
				LoginRn > 1 and Id > 0

			union all

			select 
				Code		=	'NON_UNIQUE_NAME',
				Field		=	'Name',
				[Text]		=	'Dublicate value: ' + Name,
				SourceId	=	Id
			from
				cte
			where
				LoginRn > 1 and Id > 0

			union all

			select 
				Code		=	'NON_UNIQUE_EMAIL',
				Field		=	'Email',
				[Text]		=	'Dublicate value: ' + Email,
				SourceId	=	Id
			from
				cte
			where
				LoginRn > 1 and Id > 0


			if exists(select * from @DataMessages) 
				set @DataStatus = 'Warning';

			
			select DataStatus = @DataStatus where @DataStatus is not null;
						
			if @DataStatus is not null  
			begin
				select * from @DataMessages;

				if @StartTranCount = 0 rollback transaction;
				return;
			end

		end;


		begin -- save Users 

			merge into dbo.Users as target
				using 
				(
					select
						Id		, 
						[Login]	, 
						Name	, 
						Email	
					from
						@Users
				) 
				as source on source.Id = target.Id

			when matched then
				update set				
					[Login]	=	source.[Login]	, 
					Name	=	source.Name		, 
					Email	=	source.Email	

			when not matched by target then													 	
				insert (	
					[Login]	, 
					Name	, 
					Email	)
				values (
					source.[Login]	, 
					source.Name		, 
					source.Email	)
	
			output inserted.Id,	source.Id
			into @UserIds ( InsertedId, ParamId);

		end; 


		begin -- save UserRoles
			
			merge into dbo.UserRoles as target
				using 
				(
					select
						UserId	= ids.InsertedId,
						RoleId	= r.Id
					from 
						@Users u
						inner join @UserIds ids on ids.ParamId = u.Id
						cross apply dbo.SplitTinyIntIds(u.RoleIds) r
				)
				as source on source.UserId = target.UserId and source.RoleId = target.RoleId

			when not matched by target then													 	
				insert (	
					UserId		,
					RoleId	)
				values (
					source.UserId		,
					source.RoleId	)

			when not matched by source and target.UserId in (select InsertedId from @UserIds) then	
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



	begin -- output saved Users

		select
			fn.*
		from
			dbo.vwUsers fn
			inner join @UserIds ids on ids.InsertedId = fn.Id
		order by
			Id;

	end;



end;
