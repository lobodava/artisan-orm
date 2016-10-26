create procedure dbo.GetWholeNumberParams

	@Bit				bit				,
	@BitNull			bit				,
	@BitNullable		bit				,

	@TinyInt			tinyint			,
	@TinyIntNull		tinyint			,
	@TinyIntNullable	tinyint			,

	@SmallInt			smallint		,
	@SmallIntNull		smallint		,
	@SmallIntNullable	smallint		,

	@Int				int				,
	@IntNull			int				,
	@IntNullable		int				,

	@BigInt				bigint			,
	@BigIntNull			bigint			,
	@BigIntNullable		bigint
	
as
begin
	set nocount on;

	
	select	
		@Bit				,
		@BitNull			,
		@BitNullable		,

		@TinyInt			,
		@TinyIntNull		,
		@TinyIntNullable	,

		@SmallInt			,
		@SmallIntNull		,
		@SmallIntNullable	,

		@Int				,
		@IntNull			,
		@IntNullable		,

		@BigInt				,
		@BigIntNull			,
		@BigIntNullable		;	

end
