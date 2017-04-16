create procedure dbo.SaveUsers
	@Users	dbo.UserTableType	readonly
as
begin
	set nocount on;
	
	declare @UserIds table ( InsertedId int primary key, ParamId int unique)

	
	declare @DataReplyStatus varchar(20);
	declare @DataReplyMessages dbo.DataReplyMessageTableType;
				--create type  dbo.DataReplyMessageTableType as table
				--(
				--	Code		varchar(50)		not null	,
				--	[Text]		nvarchar(4000)	null		,
				--	Id			bigint			null		,
				--	[Value]		sql_variant		null
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
			insert into @DataReplyMessages (
				Code	 ,
				[Text]	 ,
				Id		 ,
				[Value]	 )

			select 
				Code	 =	'NON_UNIQUE_LOGIN',
				[Text]	 =	'User login is not unique',
				Id		 =	Id,
				[Value]	 =	[Login]
			from
				cte
			where
				LoginRn > 1 and Id > 0

			union all

			select 
				Code	 =	'NON_UNIQUE_NAME',
				[Text]	 =	'User name is not unique',
				Id		 =	Id,
				[Value]	 =	Name
			from
				cte
			where
				LoginRn > 1 and Id > 0

			union all

			select 
				Code	 =	'NON_UNIQUE_EMAIL',
				[Text]	 =	'User email is not unique',
				Id		 =	Id,
				[Value]	 =	Email
			from
				cte
			where
				LoginRn > 1 and Id > 0


			if exists(select * from @DataReplyMessages) 
				set @DataReplyStatus = 'Validation';

			
			select DataReplyStatus = @DataReplyStatus;
						
			if @DataReplyStatus is not null  
			begin
				select * from @DataReplyMessages;

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
