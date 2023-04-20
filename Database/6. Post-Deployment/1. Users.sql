use [$(DatabaseName)];
GO

print 'dbo.Users table update...';
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
		[Name]	=	source.[Name]	,
		Email	=	source.Email

when not matched by target then
	insert (	
		[Login]	,
		[Name]	,
		Email	)
	values (
		source.[Login],
		source.[Name]	,
		source.Email)
		
when not matched by source then
	delete;

GO

print 'OK';
GO