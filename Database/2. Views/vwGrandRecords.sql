create view dbo.vwGrandRecords
as
(
	select
		Id		,
		Name	
	from
		dbo.GrandRecords
);