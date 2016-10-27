create procedure dbo.GetGuidAndRowVersionParams

	@Guid						uniqueidentifier	,
	@GuidNull					uniqueidentifier	,
	@GuidNullable				uniqueidentifier	,

	@RowVersion					binary(8)			,
	@RowVersionNull				binary(8)			,

	@RowVersionInt64			binary(8)			,
	@RowVersionInt64Null		binary(8)			,
	@RowVersionInt64Nullable	binary(8)			,
	
	@RowVersionBase64			binary(8)			,
	@RowVersionBase64Null		binary(8)

as
begin
	set nocount on;
	
	declare @t table (Id int, Rv rowversion)
	insert into @t (Id) values (1);

	select

		@Guid						,
		@GuidNull					,
		@GuidNullable				,

		@RowVersion					,
		@RowVersionNull				,
		
		@RowVersionInt64			,
		@RowVersionInt64Null		,
		@RowVersionInt64Nullable	,

		@RowVersionBase64			,
		@RowVersionBase64Null		,
		
		(select top 1 Rv from @t)	;

end