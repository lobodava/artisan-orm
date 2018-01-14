use [$(DatabaseName)];
GO

create procedure #ResetSequentialId
	@TableName		sysname	,
	@SequenceName	sysname	,
	@IdName			sysname =	'Id',
	@IdValue		int		=	null
as
begin

	declare @MaxId int, @q nvarchar(500), @p nvarchar(500)

	if @IdValue is null 
	begin

		set @q = concat('set @MaxIdOutput = isnull((select max(', @IdName, ') from dbo.', @TableName, '), 0) + 1;');
		set @p = '@MaxIdOutput int output';

		execute sp_executesql @q, @p, @MaxIdOutput = @MaxId output;
	end
	else
	begin
		
		set @MaxId = @IdValue;
	end

	set @q = concat('alter sequence dbo.', @SequenceName, ' restart with ',  @MaxId);
	execute (@q);

	select @MaxId;

end;


GO

-- execute #ResetSequentialId @TableName = 'Users', @SequenceName = 'UserId', @IdName = 'Id', @IdValue = 136 

--							@TableName	,	@SequenceName	,	 @IdName	,	IdValue

execute #ResetSequentialId	'Users'		,	'UserId'		;
execute #ResetSequentialId	'Folders'	,	'FolderId'		;


GO


