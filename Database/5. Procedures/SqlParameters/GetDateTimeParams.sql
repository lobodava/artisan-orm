create procedure dbo.GetDateTimeParams
	
	@Date					date				,
	@DateNull				date				,
	@DateNullable			date				,
	
	@Time					time(0)				,
	@TimeNull				time(0)				,
	@TimeNullable			time(0)				,	
	
	@SmallDateTime			smalldatetime		,
	@SmallDateTimeNull		smalldatetime		,
	@SmallDateTimeNullable	smalldatetime		,

	@DateTime				datetime			,
	@DateTimeNull			datetime			,
	@DateTimeNullable		datetime			,

	@DateTime2				datetime2(0)		,
	@DateTime2Null			datetime2(0)		,
	@DateTime2Nullable		datetime2(0)		,

	@DateTimeOffset			datetimeoffset(0)	,
	@DateTimeOffsetNull		datetimeoffset(0)	,
	@DateTimeOffsetNullable	datetimeoffset(0)

as
begin
	set nocount on;
	
	select

		@Date					,
		@DateNull				,
		@DateNullable			,

		@Time					,
		@TimeNull				,
		@TimeNullable			,

		@SmallDateTime			,
		@SmallDateTimeNull		,
		@SmallDateTimeNullable	,
		
		@DateTime				,
		@DateTimeNull			,
		@DateTimeNullable		,
		
		@DateTime2				,
		@DateTime2Null			,
		@DateTime2Nullable		,

		@DateTimeOffset			,
		@DateTimeOffsetNull		,
		@DateTimeOffsetNullable	;

end