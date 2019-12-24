/*		Code Generation via SQL!  :)
	
	1.	Open the script in Sql Server Management Studio or Visual Studio.
	2.	Set a query window to use your database.
	3.	Change the declared variables with the values you need.
	4.	Boldly execute the script - it changes nothing!
	5.	The generated code should appear in the message panel below.
	6.	After a short revision the code can be used in the real case. 
*/ 
 
declare @NameSpacePrefix sysname = N'Tests.DAL';
declare @Schema			 sysname = N'dbo';

declare @TableName		 sysname = N'Records';	 -- existing table ov view name

declare @ObjectName		 sysname = N'Record';

declare @PluralName		 sysname = @TableName;

set nocount on;


begin -- декларация переменных 

	declare @ColumnCount int = 0;

	declare @ColumnTabCount int;
	declare @SqlTypeTabCount int;
	declare @MethodTabCount int;
	declare @C#TypeTabCount int;

	declare @SchemaTableName sysname = @Schema + '.' + @TableName;

	declare @Tab varchar(2) = N'	';
	declare @2Tabs varchar(3) = N'		';
	declare @3Tabs varchar(3) = N'			';
	declare @4Tabs varchar(10) = N'				';
	declare @5Tabs varchar(10) = N'					';
	declare @Caret nvarchar(2) = N'
	';

	declare @ToPrint nvarchar(4000);
	declare @n int;


	declare @LineSource table 
	(
		ColumnNo		int				,	
		ColumnName		sysname			,
		SqlType			varchar(20)		,
		Method			nvarchar(500)	,
		C#Type			nvarchar(30)	,
		IsNullable		varchar(10)		,
		Constrain		varchar(30)		,
		Comma			varchar(1)		,
		ClassPropLine	nvarchar(4000)	,
		ToObjLine		nvarchar(4000)	,
		ToObjRowLine	nvarchar(4000)	,
		DataTableLine	nvarchar(4000)	,
		TableTypeLine	nvarchar(4000)	,
		ToObjectLine	nvarchar(4000)	,
		FunctionLine	nvarchar(4000)	
	);
	
end;

begin -- наполнение @LineSource 

	with DataTypes (SqlType, Method, C#Type) as  (
		
		select	'bit'		,	'GetBoolean'	,	'Boolean'	union
		select	'bigint'	,	'GetInt64'		,	'Int64'		union
		select	'int'		,	'GetInt32'		,	'Int32'		union	
		select	'smallint'	,	'GetInt16'		,	'Int16'		union
		select	'tinyint'	,	'GetByte'		,	'Byte'		union
		select	'decimal'	,	'GetDecimal'	,	'Decimal'	union
		select	'float'		,	'GetDouble'		,	'Double'	union
		select	'varchar'	,	'GetString'		,	'String'	union
		select	'nvarchar'	,	'GetString'		,	'String'	union
		select	'datetime'	,	'GetDateTime'	,	'DateTime'	union
		select	'datetime2'	,	'GetDateTime'	,	'DateTime'	union
		select	'date'		,	'GetDateTime'	,	'DateTime'	union
		select	'timestamp',	'GetRowVersion'	,	'Binary'
	)
	insert into @LineSource (
		ColumnNo	,
		ColumnName	,
		SqlType		,
		Method		,
		C#Type		,
		IsNullable	,
		Constrain	,
		Comma		)	
	select
		ColumnNo	=	ORDINAL_POSITION,
		ColumnName	=	COLUMN_NAME,
		SqlType		=	case 
							when DATA_TYPE in ('decimal', 'numeric', 'float') then DATA_TYPE + '(' + cast(NUMERIC_PRECISION as varchar(3)) + ',' + cast(NUMERIC_SCALE as varchar(3)) +')'
							when DATA_TYPE in ('varchar', 'nvarchar')  then DATA_TYPE + '(' + case CHARACTER_MAXIMUM_LENGTH when -1 then 'max' else  cast(CHARACTER_MAXIMUM_LENGTH as varchar(5)) end +')'
						else
							DATA_TYPE
						end,
						
		Method		=	'dr.' +  isnull((select Method from DataTypes where SqlType = DATA_TYPE), '###' + DATA_TYPE  + '###')
					+ case IS_NULLABLE when 'YES' then 'Nullable' else '' end
					/*+ '(i++)'*/,	
		
		C#Type		=	isnull((select C#Type from DataTypes where SqlType = DATA_TYPE), '###' + DATA_TYPE  + '###'),	
		
		IsNullable	= case IS_NULLABLE when 'YES' then 'null' else 'not null' end,
		
		Constrain	=	'',
		
		Comma		=	','
	from 
		INFORMATION_SCHEMA.COLUMNS
	where 
		TABLE_SCHEMA = @Schema
	and TABLE_NAME = @TableName;
	
	
	select @ColumnCount = count(*) from @LineSource
	
	update @LineSource set Comma = '' where ColumnNo = @ColumnCount;
end;

begin -- расстановка ключей (constraint) 

	declare @TableKeys table (	
		CONSTRAINT_TYPE		sysname,
		COLUMN_NAME			sysname,
		ORDINAL_POSITION	int
	)

		
	insert into @TableKeys
	select 
		constr.CONSTRAINT_TYPE, 
		col.COLUMN_NAME, 
		col.ORDINAL_POSITION
	from 
		INFORMATION_SCHEMA.TABLE_CONSTRAINTS constr
		inner join INFORMATION_SCHEMA.KEY_COLUMN_USAGE col on col.TABLE_NAME = constr.TABLE_NAME and col.CONSTRAINT_NAME = constr.CONSTRAINT_NAME
	where
		constr.CONSTRAINT_TYPE in ('PRIMARY KEY', 'UNIQUE')
		and constr.TABLE_NAME = @TableName
		
	--select * from @TableKeys

	if (select count(*) from @TableKeys where CONSTRAINT_TYPE = 'PRIMARY KEY') = 1
	begin
		update @LineSource set Constrain = 'primary key clustered' 
		where ColumnName = (select COLUMN_NAME from @TableKeys where CONSTRAINT_TYPE = 'PRIMARY KEY');		
	end
	
	if (select count(*) from @TableKeys where CONSTRAINT_TYPE = 'UNIQUE') = 1
	begin
		update @LineSource set Constrain = 'unique' 
		where ColumnName = (select COLUMN_NAME from @TableKeys where CONSTRAINT_TYPE = 'UNIQUE');		
	end

end;

begin -- составление строк 

	select 
		@ColumnTabCount = max(len(ColumnName) + 2) / 4 + case when (max(len(ColumnName) + 2) % 4) = 0 then 0 else 1 end,
		@SqlTypeTabCount = max(len(SqlType) + 2) / 4 + case when (max(len(SqlType) + 2) % 4) = 0 then 0 else 1 end,
		@MethodTabCount = max(len(Method) + 2) / 4 + case when (max(len(Method) + 2) % 4) = 0 then 0 else 1 end,
		@C#TypeTabCount = max(len(C#Type) + 2) / 4 + case when (max(len(C#Type) + 2) % 4) = 0 then 0 else 1 end
	from 
		@LineSource;


	update @LineSource set

		--ClassPropLine = @2Tabs
		--	+ 'public' + @Tab + C#Type + case when IsNullable = 'null' and C#Type <> 'String'  then '?' else '' end
		--	+ replicate ( @Tab , (@C#TypeTabCount - len(C#Type +  + case when IsNullable = 'null' and C#Type <> 'String'  then '?' else '' end) / 4 ) )
		--	+ ColumnName + replicate ( @Tab , (@ColumnTabCount - len(ColumnName) / 4 ) )
		--	+ '{ get; set; }',

		ClassPropLine = @2Tabs
			+ 'public ' 
			+ case C#Type when 'Binary' then 'String' else C#Type end
			+ case when IsNullable = 'null' and C#Type <> 'String'  then '?' else '' end
			+ ' ' + ColumnName + ' { get; set; }',

		ToObjLine = @4Tabs
			+ ColumnName + replicate ( @Tab , (@ColumnTabCount - len(ColumnName) / 4 ) )
			+ '=	'
			+ Method + replicate ( @Tab , (@MethodTabCount - len(Method) / 4 ) )
			+ case ColumnNo when 1 then '(i)' + @Tab else '(++i)' end
			+ case when ColumnNo < @ColumnCount then @Tab + ',' else '' end,

		ToObjRowLine = @4Tabs
			+ '/*' + @Tab + ColumnName + replicate ( @Tab , (@ColumnTabCount - len(ColumnName) / 4 ) ) +  cast(ColumnNo - 1 as varchar(2)) + @Tab +  '*/' + @Tab 
			+ Method + replicate ( @Tab , (@MethodTabCount - len(Method) / 4 ) )
			+ case ColumnNo when 1 then '(i)' + @Tab else '(++i)' end
			+ case when ColumnNo < @ColumnCount then @Tab + ',' else '' end,


		TableTypeLine = @Tab
			+ ColumnName + replicate ( @Tab , (@ColumnTabCount - len(ColumnName) / 4 ) )
			+ SqlType	 + replicate ( @Tab , (@SqlTypeTabCount - len(SqlType) / 4 ) )
			+ replicate ( @Tab , (2 - len(IsNullable) / 4 ) ) + IsNullable + @Tab 
			+ Constrain
			+ Comma,

		--DataTableLine = @3Tabs
		--	+ 'table.Columns.Add(	"' + ColumnName + '"' + replicate ( @Tab , (@ColumnTabCount + 0 - (len(ColumnName) + 2) / 4 ) ) 
		--	+ ',	typeof( ' + C#Type + replicate ( @Tab , (@C#TypeTabCount + 2 - (len(C#Type) + 8 ) / 4 ) ) + '));',

		DataTableLine = @4Tabs
			+ '.AddColumn< '+ C#Type + replicate ( @Tab , (@C#TypeTabCount + 2 - (len(C#Type) + 8 ) / 4 ) ) + '>(	"' 
			+ ColumnName + '"' + replicate ( @Tab , (@ColumnTabCount + 0 - (len(ColumnName) + 2) / 4 ) ) + ')'
			+ case when ColumnNo = @ColumnCount then ';' else '' end,

		ToObjectLine = @4Tabs
			+ 'obj.' + ColumnName + replicate ( @Tab , (@ColumnTabCount + 1 - (len('obj.' + ColumnName) ) / 4 ) ) + Comma,
			
		FunctionLine = @2Tabs
			+ ColumnName + replicate ( @Tab , (@ColumnTabCount - len(ColumnName) / 4 ) ) + Comma;
end;


raiserror(	-- namespaces & header 
'using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Artisan.Orm;

namespace %s.%s.Models
{
	public class %s
	{'
,0,1, @NameSpacePrefix, @PluralName, @ObjectName);


set @n = 1;
while (1=1)
begin
	select @ToPrint = ClassPropLine, @n = @n + 1 from @LineSource where ColumnNo = @n;
	if @@rowcount = 0 break; 
	if @n > 2 raiserror ('
',0,1);					
	raiserror (@ToPrint,0,1);
end;

raiserror(	
'	}

'
,0,1);
	

raiserror(	-- MapperFor
'
	[MapperFor(typeof(%s), RequiredMethod.All)]
	public static class %sMapper 
	{
		public static %s CreateObject(SqlDataReader dr)
		{
			var i = 0;
			
			return new %s 
			{
'
,0,1, @ObjectName, @ObjectName, @ObjectName, @ObjectName);

set @n = 1;
while (1=1)
begin
	select @ToPrint = ToObjLine, @n = @n + 1 from @LineSource where ColumnNo = @n;
	if @@rowcount = 0 break; 	
	raiserror (@ToPrint,0,1);
end;	

raiserror(	
'
				//ExtraInfo = ++i < dr.FieldCount ? dr.GetStringNullable(i) : null
			};
		}
'
,0,1);



raiserror(	-- CreateObjectRow
'
		public static ObjectRow CreateObjectRow(SqlDataReader dr)
		{
			var i = 0;
			
			return new ObjectRow(%d) 
			{
'
,0,1, @ColumnCount );

set @n = 1;
while (1=1)
begin
	select @ToPrint = ToObjRowLine, @n = @n + 1 from @LineSource where ColumnNo = @n;
	if @@rowcount = 0 break; 	
	raiserror (@ToPrint,0,1);
end;	

raiserror(	
'			};
		}
'
,0,1);



raiserror(	-- Additional Create and  CreateDataTable header 
'	
		public static DataTable CreateDataTable()
		{
			return new DataTable("%sTableType")
			
'
,0,1, @ObjectName, @ObjectName);

set @n = 1;
while (1=1)
begin
	select @ToPrint = DataTableLine, @n = @n + 1 from @LineSource where ColumnNo = @n;
	if @@rowcount = 0 break; 					
	raiserror (@ToPrint,0,1);
end;	
 
raiserror(	-- CreateDataRow
'
		}

		public static Object[] CreateDataRow(%s obj)
		{
			return new object[]
			{'
,0,1, @ObjectName); 

set @n = 1;
while (1=1)
begin
	select @ToPrint = ToObjectLine, @n = @n + 1 from @LineSource where ColumnNo = @n;
	if @@rowcount = 0 break; 					
	raiserror (@ToPrint,0,1);
end;


raiserror(	-- Tail
'			};
		}

	}
}

'
,0,1);

begin -- Вывод TableType 

raiserror(	
'/*
create type %s.%sTableType as table
(
'
,0,1, @Schema, @ObjectName)


set @n = 1;
while (1=1)
begin
	select @ToPrint = TableTypeLine, @n = @n + 1 from @LineSource where ColumnNo = @n;
	if @@rowcount = 0 break; 					
	raiserror (@ToPrint,0,1);
end;	

raiserror(
');

GO
*/
'
,0,1);

end;


begin -- view 

raiserror('
/*
create view %s.vw%s
as
(
	select'
,0,1, @Schema, @TableName);
 
  
set @n = 1;
while (1=1)
begin
	select @ToPrint = FunctionLine, @n = @n + 1 from @LineSource where ColumnNo = @n;
	if @@rowcount = 0 break; 					
	raiserror (@ToPrint,0,1);
end;	

		
raiserror(
'	from
		%s
);

GO
*/'
,0,1, @SchemaTableName);
end;


begin -- function 

raiserror('
/*
create function %s.fn%s
(	
	@Id int = null
)
returns table
as
return 
(
	select'
,0,1, @Schema, @TableName);
 
  
set @n = 1;
while (1=1)
begin
	select @ToPrint = FunctionLine, @n = @n + 1 from @LineSource where ColumnNo = @n;
	if @@rowcount = 0 break; 					
	raiserror (@ToPrint,0,1);
end;	

		
raiserror(
'	from
		%s
	where
		( @Id = Id or @Id is null )
);

GO
*/'
,0,1, @SchemaTableName);
end;

