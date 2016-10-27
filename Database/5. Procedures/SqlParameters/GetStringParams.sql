create procedure dbo.GetStringParams

	@Char					char			,
	@CharNull				char			,
	@CharNullable			char			,

	@NChar					nchar			,
	@NCharNull				nchar			,
	@NCharNullable			nchar			,

	@Varchar				varchar(8000)	,
	@VarcharNull			varchar(8000)	,

	@NVarchar				nvarchar(4000)	,
	@NVarcharNull			nvarchar(4000)	,

	@VarcharMax				varchar(max)	,
	@VarcharMaxNull			varchar(max)	,

	@NVarcharMax			nvarchar(max)	,
	@NVarcharMaxNull		nvarchar(max)	
as
begin
	set nocount on;
	
	select

		@Char					,	
		@CharNull				,
		@CharNullable			,

		@NChar					,
		@NCharNull				,
		@NCharNullable			,

		@Varchar				,
		@VarcharNull			,

		@NVarchar				,
		@NVarcharNull			,

		@VarcharMax				,
		@VarcharMaxNull			,

		@NVarcharMax			,
		@NVarcharMaxNull		;

end