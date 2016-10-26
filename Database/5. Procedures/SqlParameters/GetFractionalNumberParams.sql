create procedure dbo.GetFractionalNumberParams

	@Decimal			decimal(29,0)	,
	@DecimalNull		decimal(29,0)	,
	@DecimalNullable	decimal(29,0)	,

	@SmallMoney			smallmoney		,
	@SmallMoneyNull		smallmoney		,
	@SmallMoneyNullable	smallmoney		,

	@Money				money			,
	@MoneyNull			money			,
	@MoneyNullable		money			,

	@Real				real			,
	@RealNull			real			,
	@RealNullable		real			,

	@Float				float(53)		,
	@FloatNull			float(53)		,
	@FloatNullable		float(53)
as
begin
	set nocount on;

	
	select	

		@Decimal			,
		@DecimalNull		,
		@DecimalNullable	,

		@SmallMoney			,
		@SmallMoneyNull		,
		@SmallMoneyNullable	,

		@Money				,
		@MoneyNull			,
		@MoneyNullable		,

		@Real				,
		@RealNull			,
		@RealNullable		,

		@Float				,
		@FloatNull			,
		@FloatNullable		;	

end
