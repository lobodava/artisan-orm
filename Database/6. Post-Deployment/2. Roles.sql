use [$(DatabaseName)];
GO

set nocount on;

print 'Enum Table synchronisation: Roles';


merge into dbo.Roles as target 
using (
	values 

	--	Id	,	Code			,	Name

	(	 1	,	'Armorer'		,	'Armorer'		),
	(	 2	,	'Blacksmith'	,	'Blacksmith'	),
	(	 3	,	'Bladesmith'	,	'Bladesmith'	),
	(	 4	,	'Joiner'		,	'Joiner'		),
	(	 5	,	'Cooper'		,	'Cooper'		),
	(	 6	,	'Dyer'			,	'Dyer'			),
	(	 7	,	'Furrier'		,	'Furrier'		),
	(	 8	,	'Goldsmith'		,	'Goldsmith'		),
	(	 9	,	'Gunsmith'		,	'Gunsmith'		),
	(	10	,	'Hatter'		,	'Hatter'		),
	(	11	,	'Locksmith'		,	'Locksmith'		),
	(	12	,	'Nailsmith'		,	'Nailsmith'		),
	(	13	,	'Potter'		,	'Potter'		),
	(	14	,	'Ropemaker'		,	'Ropemaker'		),
	(	15	,	'Saddler'		,	'Saddler'		),
	(	16	,	'Shoemaker'		,	'Shoemaker'		),
	(	17	,	'Stonemason'	,	'Stonemason'	),
	(	18	,	'Tailor'		,	'Tailor'		),
	(	19	,	'Tanner'		,	'Tanner'		),
	(	20	,	'Weaver'		,	'Weaver'		),
	(	21	,	'Wheelwright'	,	'Wheelwright'	)

) 	as source (Id, Code, Name) on target.Code = source.Code
 
when matched then 
	update set 
		Id		= source.Id	,
		Name	= source.Name	
		 
when not matched by target then 
	insert (Id, Code, Name) 
	values (Id, Code, Name) 

when not matched by source then 
	delete;


print 'OK';
GO