# Artisan ORM &#8212; ADO.NET Micro-ORM to SQL Server

Artisan ОRМ was created to meet the following requirements:
* interactions with database should mostly be made through *Stored Procedures*;
* all calls to database should be encapsulated into *Repository Methods*;
* a Repository Method should be able to read or save a *complex object graph* with one Stored Procedure;
* it should work with the highest possible performance, even at the expense of the convenience and development time.

To achieve these goals Artisan ORM uses:
* the *SqlDataReader* as the fastest method of data reading;
* a bunch of its own extensions to ADO.NET [SqlCommand](https://github.com/lobodava/artisan-orm/wiki/SqlCommand-Extensions) and [SqlDataReader](https://github.com/lobodava/artisan-orm/wiki/SqlDataReader-extentions) methods, both synchronous and asynchronous;
* strictly structured static [Mappers](https://github.com/lobodava/artisan-orm/wiki/Mappers);
* [User Defined Table Types](https://github.com/lobodava/artisan-orm/wiki/User-Defined-Table-Types) as a mean of object saving;
* [unique negative identities](https://github.com/lobodava/artisan-orm/blob/master/Artisan.Orm/NegativeIdentity.cs) as a flag of new entities;
* a [special approach](https://github.com/lobodava/artisan-orm/wiki/Negative-identities-and-object-graph-saving) to writing stored procedures for object reading and saving.

Artisan ОRМ is available as [NuGet Package](http://www.nuget.org/packages/Artisan.ORM).

[![NuGet Logo](https://www.nuget.org/Content/Logos/nugetlogo.png)](http://www.nuget.org/packages/Artisan.ORM)

More information is available in Artisan ORM documentation [Wiki](https://github.com/lobodava/artisan-orm/wiki).

## Simple sample

Let's say we have database tables *Users* and *UserRoles* (TSQL):

```C#
create table dbo.Users
(
    Id       int           not null	  identity primary key clustered,
    [Login]  varchar(20)   not null	  ,
    Name     nvarchar(50)  not null	  ,
    Email    varchar(50)   not null
);

create table dbo.UserRoles
(
    UserId   in       not null   foreign key (UserId) references dbo.Users (Id),	
    RoleId   tinyint  not null   foreign key (RoleId) references dbo.Roles (Id),

    primary key clustered (UserId, RoleId)
);

// The Roles table is just a dictionary or lookup table and users don't edit it.
```

And we have a *User* class (C#):

```C#
public class User
{
    public Int32   Id       { get; set; }
    public String  Login    { get; set; }
    public String  Name     { get; set; }
    public String  Email    { get; set; }
    public Byte[]  RoleIds  { get; set; }
}
```

In order to read and save the *User* data in Artisan ORM way it is required to create:
* Mapper Static Class (C#)
* User Defined Table Type (TSQL)
* Stored Procedures (TSQL)

*Mapper Static Class* (C#) is decorated with *MapperFor* attribute and consists of four *static* methods with reserved names:
* CreateObject
* CreateObjectRow
* CreateDataTable
* CreateDataRow

```C#
[MapperFor(typeof(User)]
public static class UserMapper 
{
    public static User CreateObject(SqlDataReader dr)
    {
        var i = 0;

        return new User 
        {
            Id     =  dr.GetInt32(i++)  ,
            Login  =  dr.GetString(i++) ,
            Name   =  dr.GetString(i++) ,
            Email  =  dr.GetString(i++)
        };
    }

    public static Object[] CreateObjectRow(SqlDataReader dr)
    {
        var i = 0;

        return new Object[]
        {
            /* 0 - Id      =  */  dr.GetInt32(i++)  ,
            /* 1 - Login   =  */  dr.GetString(i++) ,
            /* 2 - Name    =  */  dr.GetString(i++) ,
            /* 3 - Email   =  */  dr.GetString(i++)
        };
    }

    public static DataTable CreateDataTable()
    {
        var table = new DataTable("UserTableType");

        table.Columns.Add( "Id"    ,  typeof( Int32  ));
        table.Columns.Add( "Login" ,  typeof( String ));
        table.Columns.Add( "Name"  ,  typeof( String ));
        table.Columns.Add( "Email" ,  typeof( String ));

        return table;
    }

    public static Object[] CreateDataRow(User obj)
    {
        if (obj.Id == 0) 
            obj.Id = Int32NegativeIdentity.Next;

        return new object[]
        {
            obj.Id     ,
            obj.Login  ,
            obj.Name   ,
            obj.Email
        };
    }
}
```

*User Defined Table Type* (TSQL):

```SQL
create type dbo.UserTableType as table
(
    Id       int           not null   primary key clustered,
    [Login]  varchar(20)   not null   ,
    Name     nvarchar(50)  not null   ,
    Email    varchar(50)   not null
);
```

*Stored Procedures* (TSQL):


```SQL
create procedure dbo.GetUserById
    @Id    int
as
begin
    set nocount on;

    -- read User

    select
        Id       ,
        [Login]  ,
        Name     ,
        Email
    from
        dbo.Users
    where
        Id = @Id;
        
    -- read User RoleIds
        
    select
        RoleId
    from
        dbo.UserRoles
    where
        UserId = @Id;
        
end;
```

```SQL
create procedure dbo.SaveUser
    @User     dbo.UserTableType       readonly,
    @RoleIds  dbo.TinyIntIdTableType  readonly
as
begin
    set nocount on;
    
    declare @UserIds table ( InsertedId int primary key, ParamId int unique);

    begin -- save User

        merge into dbo.Users as target
            using 
            (
                select
                    Id       ,
                    [Login]  ,
                    Name     ,
                    Email
                from
                    @User
            ) 
            as source on source.Id = target.Id

        when matched then
            update set
                [Login]  =  source.[Login]  ,
                Name     =  source.Name     ,
                Email    =  source.Email    

        when not matched by target then                                                         
            insert (    
                [Login]  , 
                Name     ,
                Email    )
            values (
                source.[Login]  , 
                source.Name     , 
                source.Email    )
    
        output          inserted.Id , source.Id
        into @UserIds ( InsertedId  , ParamId   );

    end; 

    begin -- save UserRoles
            
        merge into dbo.UserRoles as target
            using 
            (
                select
                    UserId  =  ids.InsertedId,
                    RoleId  =  r.Id
                from 
                    @User u
                    inner join @UserIds ids on ids.ParamId = u.Id
                    cross join @RoleIds r
            )
            as source on source.UserId = target.UserId and source.RoleId = target.RoleId

        when not matched by target then                                                         
            insert (    
                UserId  ,
                RoleId  )
            values (
                source.UserId  ,
                source.RoleId  )

        when not matched by source and target.UserId in (select InsertedId from @UserIds) then    
            delete;
    end;

end;
```

Having prepared *Mapper*, *User Defined Table Type* and *Stored Procedures* we can write the *Repository* (C#):

```C#
public class UserRepository: Artisan.Orm.RepositoryBase
{
    public User GetById(int id)
    {
        return GetByCommand(cmd =>
        {
            cmd.UseProcedure("dbo.GetUserById");

            cmd.AddIntParam("@Id", id);

            return cmd.GetByReader(reader =>
            {
                var user     = reader.ReadTo<User>();
                user.RoleIds = reader.ReadToArray<byte>();            
                
				return user;
            });

        });
    }

    public void Save(User user)
    {
        ExecuteCommand(cmd =>
        {
            cmd.UseProcedure("dbo.SaveUser");

            cmd.AddTableParam( "@User"    , user         );
            cmd.AddTableParam( "@RoleIds" , user.RoleIds );
        });
    }
}
```

More examples of the Artisan ORM usage are available in the [Tests](https://github.com/lobodava/artisan-orm/tree/master/Tests) and [Database](https://github.com/lobodava/artisan-orm/tree/master/Database) projects.

For further reading please visit Artisan ORM documentation [Wiki](https://github.com/lobodava/artisan-orm/wiki).


