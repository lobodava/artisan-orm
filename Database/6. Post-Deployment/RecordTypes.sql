use [$(DatabaseName)];
GO

set nocount on;

print 'Enum Table synchronisation: RecordTypes';


merge into dbo.RecordTypes as target 
using (
	values 

	--	Id	,	Code		,	Name								
	(	1	,	'Type_1'	,	N'Type One'		),
	(	2	,	'Type_2'	,	N'Type Two'		),	
	(	3	,	'Type_3'	,	N'Type Three'	),
	(	4	,	'Type_4'	,	N'Type Four'	),
	(	5	,	'Type_5'	,	N'Type Five'	)		

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