create procedure dbo.SaveUser
	@User		dbo.UserTableType		readonly,
	@RoleIds	dbo.TinyIntIdTableType	readonly
as
begin
	set nocount on;
	
	if (select count(*) from @User) > 1
	begin		
		raiserror ('Procedure dbo.SaveUser supports saving one User at a time only.', 16, 1);
		return;
	end

	declare @Login varchar(20);
	declare @Name nvarchar(50);
	declare @Email varchar(50);


	declare @DataReplyStatus varchar(20);
	declare @DataReplyMessages dbo.DataReplyMessageTableType;

	declare @UserIds table ( InsertedId int primary key, ParamId int unique)

	
	declare @StartTranCount int;

	begin try
		set @StartTranCount = @@trancount;
		if @StartTranCount = 0 begin transaction;


		if exists -- concurrency 
		(
			select
				*
			from
				dbo.Users u with (tablockx, holdlock)
				inner join @User t on t.Id = u.Id and t.[RowVersion] <> u.[RowVersion]
		)
		begin
			
			select DataReplyStatus = 'Concurrency';

			if @StartTranCount = 0 rollback transaction;
			return;
		end

	
		
		begin -- validation

			begin -- check User.Login uniqueness
				select top 1 
					@Login = u.[Login]
				from
					dbo.Users u
					inner join @User t on t.[Login] = u.[Login] and t.Id <> u.Id;

				if @Login is not null 
				begin 
					set @DataReplyStatus = 'Validation';

					insert into @DataReplyMessages (Code, Value) 
					select Code ='NON_UNIQUE_LOGIN', @Login;
				end;
			end;
			
			begin -- check User.Name uniqueness
				select top 1 
					@Name = u.Name
				from
					dbo.Users u
					inner join @User t on t.Name = u.Name and t.Id <> u.Id

				if @Name is not null 
				begin 
					set @DataReplyStatus = 'Validation';

					insert into @DataReplyMessages (Code, Value) 
					select Code ='NON_UNIQUE_NAME', @Name;
				end;
			end;

			begin -- check User.Email uniqueness
				select top 1 
					@Email = u.Email
				from
					dbo.Users u
					inner join @User t on t.Email = u.Email and t.Id <> u.Id

				if @Email is not null 
				begin 
					set @DataReplyStatus = 'Validation';

					insert into @DataReplyMessages (Code, Value) 
					select Code ='NON_UNIQUE_EMAIL', @Email;
				end;
			end;
			
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
						@User
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
						@User u
						inner join @UserIds ids on ids.ParamId = u.Id
						cross join @RoleIds r
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
