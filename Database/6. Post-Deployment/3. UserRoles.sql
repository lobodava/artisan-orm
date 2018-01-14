use [$(DatabaseName)];
GO

print 'dbo.UserRoles table update...';
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

print 'OK';
GO