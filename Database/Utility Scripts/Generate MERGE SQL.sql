declare @Schema sysname = N'dbo';
declare @TableName sysname = N'Users';
declare @ParamTable sysname = N'@Users'
declare @LinkColumn sysname = N'Id'



set nocount on;

declare @MaxTabCount int;

declare @SelectColumns varchar(8000);
declare @UpdateColumns varchar(8000);
declare @InsertToColumns varchar(8000);
declare @InsertFromColumns varchar(8000);

declare @SchemaTableName sysname = @Schema + '.' + @TableName;

declare @Tab varchar(2) = N'	';
declare @2Tabs varchar(3) = N'		';
declare @3Tabs varchar(3) = N'			';
declare @Caret nvarchar(2) = N'
';


select 
	@MaxTabCount = max(len(COLUMN_NAME) + 2) / 4 + case when (max(len(COLUMN_NAME) + 2) % 4) = 0 then 0 else 1 end
FROM 
	INFORMATION_SCHEMA.COLUMNS
WHERE 
	TABLE_SCHEMA = @Schema
	and TABLE_NAME = @TableName;	
	
	
select 
	@SelectColumns = 
		case 
			when @SelectColumns is null 
				then COLUMN_NAME + replicate ( @Tab , (@MaxTabCount - len(COLUMN_NAME) / 4 ) ) 
			else
				@SelectColumns + ', ' + @Caret + @3Tabs + COLUMN_NAME + replicate ( @Tab , (@MaxTabCount - len(COLUMN_NAME) / 4 ) )
		end
from 
	INFORMATION_SCHEMA.COLUMNS
where 
	TABLE_SCHEMA = @Schema
	and TABLE_NAME = @TableName;
	
	
select 
	@UpdateColumns = 
		case 
			when @UpdateColumns is null 
				then COLUMN_NAME + replicate ( @Tab , (@MaxTabCount - len(COLUMN_NAME) / 4 ) ) + '=	source.' + COLUMN_NAME + replicate ( @Tab , (@MaxTabCount + 2  - (len(COLUMN_NAME) + 11) / 4 ) )
			else
				@UpdateColumns + ', ' + @Caret + @2Tabs + COLUMN_NAME + replicate ( @Tab , (@MaxTabCount - len(COLUMN_NAME) / 4 ) ) + '=	source.' + COLUMN_NAME + replicate ( @Tab , (@MaxTabCount + 2 - (len(COLUMN_NAME) + 11) / 4 ) )
		end,
		
	@InsertToColumns =
		case 
			when @InsertToColumns is null 
				then COLUMN_NAME + replicate ( @Tab , (@MaxTabCount - len(COLUMN_NAME) / 4 ) ) 
			else
				@InsertToColumns + ', ' + @Caret + @2Tabs + COLUMN_NAME + replicate ( @Tab , (@MaxTabCount - len(COLUMN_NAME) / 4 ) )
		end,
	
	
	@InsertFromColumns = 
		case 
			when @InsertFromColumns is null 
				then 'source.' + COLUMN_NAME + replicate ( @Tab , (@MaxTabCount + 1  - (len(COLUMN_NAME) + 7) / 4 ) )
			else
				@InsertFromColumns + ', ' + @Caret + @2Tabs + 'source.' + COLUMN_NAME + replicate ( @Tab , (@MaxTabCount + 1 - (len(COLUMN_NAME) + 7) / 4 ) )
		end	
from 
	INFORMATION_SCHEMA.COLUMNS
where 
	TABLE_SCHEMA = @Schema
	and TABLE_NAME = @TableName
	and COLUMN_NAME <> @LinkColumn;	

raiserror(	
'
merge into %s as target
	using 
	(
		select
			%s
		from
			%s
	) 
	as source on source.%s = target.%s
'
,0,1, @SchemaTableName, @SelectColumns, @ParamTable, @LinkColumn, @LinkColumn); 


raiserror(	
'
when matched then
	update set				
		%s	
'
,0,1, @UpdateColumns); 

raiserror(	
'
when not matched by target then
	insert (	
		%s)
	values (
		%s)
		
when not matched by source then
	delete

output inserted.Id,	source.Id
into @%sIds ( InsertedId, ParamId);
'
,0,1, @InsertToColumns, @InsertFromColumns, @TableName); 
