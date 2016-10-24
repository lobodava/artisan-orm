/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.		
 Use SQLCMD syntax to include a file in the post-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/
:R  "RecordTypes.sql"
:R  "Roles.sql"

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

merge into dbo.Users as target
	using 
	(	
		values 
			('thorin'	, 'Thorin'	, 'thorin@middle.earth'	),
			('balin'	, 'Balin'	, 'balin@middle.earth'	),
			('bifur'	, 'Bifur'	, 'bifur@middle.earth'	),
			('bofur'	, 'Bofur'	, 'bofur@middle.earth'	),
			('bombur'	, 'Bombur'	, 'bombur@middle.earth'	),
			('dori'		, 'Dori'	, 'dori@middle.earth'	),
			('dwalin'	, 'Dwalin'	, 'dwalin@middle.earth'	),
			('fili'		, 'Fili'	, 'fili@middle.earth'	),
			('gloin'	, 'Gloin'	, 'gloin@middle.earth'	),
			('kili'		, 'Kili'	, 'kili@middle.earth'	),
			('nori'		, 'Nori'	, 'nori@middle.earth'	),
			('oin'		, 'Oin'		, 'oin@middle.earth'	),
			('ori'		, 'Ori'		, 'ori@middle.earth'	),
			('bilbo'	, 'Bilbo'	, 'bilbo@middle.earth'	)
	
	) as source ([Login], Name, Email) on source.[Login] = target.[Login]

when matched then
	update set				
		Name	=	source.Name	, 
		Email	=	source.Email	

when not matched by target then													 	
	insert (	
		[Login]	, 
		Name	, 
		Email	)
	values (
		source.[Login], 
		source.Name	, 
		source.Email)
		
when not matched by source then
	delete;

GO


insert into dbo.UserRoles
select
	UserId = u.Id,
	RoleId = r.Id
from 
	(values
		('thorin'	, 'Armorer'		), ('thorin', 'Joiner'		), ('thorin', 'Goldsmith'	),
		('balin'	, 'Blacksmith'	), ('balin'	, 'Cooper'		), ('balin'	, 'Gunsmith'	),
		('bifur'	, 'Bladesmith'	), ('bifur'	, 'Dyer'		), ('bifur'	, 'Hatter'		),
		('bofur'	, 'Joiner'		), ('bofur'	, 'Furrier'		), ('bofur'	, 'Locksmith'	),
		('bombur'	, 'Cooper'		), ('bombur', 'Goldsmith'	), ('bombur', 'Nailsmith'	),
		('dori'		, 'Dyer'		), ('dori'	, 'Gunsmith'	), ('dori'	, 'Potter'		),
		('dwalin'	, 'Furrier'		), ('dwalin', 'Hatter'		), ('dwalin', 'Ropemaker'	),
		('fili'		, 'Goldsmith'	), ('fili'	, 'Locksmith'	), ('fili'	, 'Saddler'		),
		('gloin'	, 'Gunsmith'	), ('gloin'	, 'Nailsmith'	), ('gloin'	, 'Shoemaker'	),
		('kili'		, 'Hatter'		), ('kili'	, 'Potter'		), ('kili'	, 'Stonemason'	),
		('nori'		, 'Locksmith'	), ('nori'	, 'Ropemaker'	), ('nori'	, 'Tailor'		),
		('oin'		, 'Nailsmith'	), ('oin'	, 'Saddler'		), ('oin'	, 'Tanner'		),
		('ori'		, 'Potter'		), ('ori'	, 'Shoemaker'	), ('ori'	, 'Weaver'		),
		('bilbo'	, 'Ropemaker'	), ('bilbo'	, 'Stonemason'	), ('bilbo'	, 'Wheelwright'	)
	) t (UserLogin, RoleCode)
	inner join dbo.Users u on u.[Login] = t.UserLogin
	inner join dbo.Roles r on r.Code = t.RoleCode
	left join dbo.UserRoles ur on ur.UserId = u.Id and ur.RoleId = r.Id
where
	ur.UserId is null;

GO


--insert into dbo.ExtraPermits 
--	(UserId	, Permit1	, Permit2	, Permit3	, Permit4	, Permit5	, Permit6	, Permit7	, Permit8	)
--values
--	(	1	,	0		,	0		,	0		,	0		,	0		,	0		,	0		,	0		),
--	(	2	,	0		,	0		,	0		,	0		,	0		,	0		,	0		,	0		),
--	(	3	,	0		,	0		,	0		,	0		,	0		,	0		,	0		,	0		),
--	(	4	,	0		,	0		,	0		,	0		,	0		,	0		,	0		,	0		),
--	(	5	,	0		,	0		,	0		,	0		,	0		,	0		,	0		,	0		),
--	(	6	,	0		,	0		,	0		,	0		,	0		,	0		,	0		,	0		),
--	(	7	,	0		,	0		,	0		,	0		,	0		,	0		,	0		,	0		),
--	(	8	,	0		,	0		,	0		,	0		,	0		,	0		,	0		,	0		),
--	(	9	,	0		,	0		,	0		,	0		,	0		,	0		,	0		,	0		),
--	(	10	,	0		,	0		,	0		,	0		,	0		,	0		,	0		,	0		),
--	(	11	,	0		,	0		,	0		,	0		,	0		,	0		,	0		,	0		),
--	(	12	,	0		,	0		,	0		,	0		,	0		,	0		,	0		,	0		),
--	(	13	,	0		,	0		,	0		,	0		,	0		,	0		,	0		,	0		),
--	(	14	,	0		,	0		,	0		,	0		,	0		,	0		,	0		,	0		);
