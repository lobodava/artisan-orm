use [$(DatabaseName)];
GO

print 'Records generation...';
GO

-- data generation for dbo.GrandRecords table

insert into GrandRecords ([Name])
select l
from (values 		
		('(A)'),('(B)'),('(C)'),('(D)'),('(E)'),('(F)'),('(G)'),('(H)'),('(I)'),('(J)'),('(K)'),('(L)'),('(M)'),
		('(N)'),('(O)'),('(P)'),('(Q)'),('(R)'),('(S)'),('(T)'),('(U)'),('(V)'),('(W)'),('(X)'),('(Y)'),('(Z)')
	) t(l)
where
	t.l not in (select [Name] from GrandRecords);


-- data generation for dbo.Records table	

with E as 
(
    select [Name] from (values 
		('A'),('B'),('C'),('D'),('E'),('F'),('G'),('H'),('I'),('J'),('K'),('L'),('M'),
		('N'),('O'),('P'),('Q'),('R'),('S'),('T'),('U'),('V'),('W'),('X'),('Y'),('Z')
	) t([Name])    
) 
insert into dbo.Records (
	GrandRecordId	,
	[Name]			,
	RecordTypeId	,
	Number			,
	[Date]			,
	Amount			,
	IsActive		,
	Comment			)
select
	GrandRecordId	=	G.Id,
	[Name]			=	E.[Name],
	RecordTypeId /* tinyint			*/	= case when abs(checksum(newid())) % 10 = 0 then null else	abs(checksum(newid())) % 5 + 1 end,
	Number		 /* smallint		*/	= case when abs(checksum(newid())) % 10 = 0 then null else	abs(checksum(newid())) % 1000 end,
	[Date]		 /* datetime2(0)	*/	= case when abs(checksum(newid())) % 15 = 0 then null else	dateadd(day, -1 * (abs(checksum(newid())) % 365), getdate()) end,
	Amount		 /* decimal(19,2)	*/	= case when abs(checksum(newid())) % 20 = 0 then null else	abs(checksum(newid())) % 1000.05 end,
	IsActive	 /* bit				*/	= case when abs(checksum(newid())) % 30 = 0 then null else	abs(checksum(newid())) % 2 end,
	Comment		 /* nvarchar(500)	*/	= case when abs(checksum(newid())) % 35 = 0 then null else	concat('Comment # ', G.Id, '-',  abs(checksum(newid())) % 1000) end	
from
	GrandRecords G, E
where
	not exists (select * from Records ee where ee.GrandRecordId = G.Id and ee.[Name] = E.[Name] );


-- data generation for dbo.Records table	

with C as 
(
    select [Name] from (values 
		('a'),('b'),('c'),('d'),('e'),('f'),('g'),('h'),('i'),('j'),('k'),('l'),('m'),
		('n'),('o'),('p'),('q'),('r'),('s'),('t'),('u'),('v'),('w'),('x'),('y'),('z')
	) t([Name])    
) 
insert into dbo.ChildRecords (
	RecordId	,
	[Name]		)
select
	RecordId	=	E.Id,
	[Name]		=	C.[Name]
from
	Records E, C
where
	not exists (select * from ChildRecords cc where cc.RecordId = E.Id and cc.[Name] = C.[Name] );

GO


GO

print 'OK';
GO