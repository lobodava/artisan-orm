create function SplitTinyIntIds ( --[dbo].[DelimitedSplit8K] -- http://www.sqlservercentral.com/articles/Tally+Table/72993/
        @Ids varchar(1000)
)
returns table with schemabinding as
return
(
	with N as
	(
		select n from (values (1),(2),(3),(4),(5),(6),(7),(8),(9),(10)) t(n)
	), 
	E3 as
	( 
		select-- top 8000
			row_number() over(order by (select null)) as number
		from 
			N n1, N n2, N n3--, N n4
	),
	cteTally(N) as 
	(
		select top (isnull(datalength(@Ids), 0)) row_number() over (order by (select null)) from E3
	),
	cteStart(N1) as
	(
		select 1 union all
		select t.N + 1 from cteTally t where substring(@Ids, t.N, 1) = ','
	),
	cteLen(N1,L1) as 
	(
		select 
			s.N1,
			isnull(nullif(charindex(',', @Ids, s.N1), 0) - s.N1, 1000)
		from 
			cteStart s
	)
	select distinct
		Id = cast(substring(@Ids, l.N1, l.L1) as tinyint)
	from 
		cteLen l
	where
		len(substring(@Ids, l.N1, l.L1)) > 0
);

