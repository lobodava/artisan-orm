# Artisan.Orm — How to Reinvent the Wheel

> **Republication note.** Originally published on [CodeProject in November 2016](https://www.codeproject.com/Articles/1155836/Artisan-Orm-or-How-to-Reinvent-the-Wheel) and last revised June 2020. CodeProject has since gone read-only; this re-publication preserves the historical explanation. A handful of small fixes were applied during conversion:
> - `(string)drr["Name"]` had a typo `drr` — fixed to `dr`;
> - `[MapperFor(typeof(User)]` was missing its closing parenthesis — fixed;
> - `cmd.ReadTo<User>;` was missing parentheses — fixed to `cmd.ReadTo<User>()`;
> - `public <T> GetByCommand<T>(...)` was missing its return type — fixed to `public T GetByCommand<T>(...)`;
> - the C# samples that use plain primitives have been switched from `Int32` / `String` to `int` / `string` for readability. The `MapperFor` examples keep `Int32` / `String` because that is how the library's `DataTableExtensions.AddColumn<T>` API is still presented.
>
> **4.1.0 compatibility.** The Read-method table is a **v1.x snapshot**, and the User and graph examples use historical teaching schemas. They are not deployment scripts for the current sample database. The API calls remain useful, but use the linked current models, SQL types and procedures together when running against this repository. Modern notes below identify the differences; see [Read Methods Understanding](https://github.com/lobodava/artisan-orm/wiki/Read-Methods-Understanding) for the expanded API.
>
> Current C# examples use `Microsoft.Data.SqlClient`, `Artisan.Orm` and `System.Data`. The compatibility notes describe the changes being prepared for 4.1.0; they do not announce a published release.
>
> — *Vadim Loboda, originally Nov 2016, revised Jun 2020*

ADO.NET Micro-ORM to SQL Server.

In this article you will read about a tiny micro-ORM that I wrote, which only works with SQL Server and has basic functionality for storing and reading data. But at least it performs what is expected and does it well! This article is about why I did that, and how.

This article is a continuation and development of:

- [How to Save an Object Graph in Master-Detail Relationship with One Stored Procedure](01-object-graph-saving.md)

The next articles:

- [The Artisan Way of Data Reply](03-data-reply.md)
- [Combination of Id-ParentId and HierarchyId Approaches to Hierarchical Data](04-hierarchyid-combination.md)

## Introduction

Have you read the article *Don't write your own ORM*? I have. And I was very depressed by the end of it. But not for too long — only until the first comment underneath:

> Coders: if you want to write an ORM please do so!

Nicholas de Lioncourt, you are my inspirer and hero!

So I did. I wrote my own ORM. The wheel was reinvented again!

Of course, it is not a full and sophisticated ORM like EF; it is just a tiny micro-ORM, which only works with SQL Server and has basic functionality for storing and reading data. But at least it performs what is expected, and does it well.

This article is about why I did that and how.

## Why?

Mostly because I have not found an elegant way to do what I want using the existing ORM frameworks. I am conscious that I may have missed something out of sight.

**My main requirement to the ORM is performance.** I have heard the opinion that the speed of development is more important than a slight subsidence of system performance. And I have seen the consequences: "Our system has matured and become slow." So I cannot relax until everything is done to ensure that the best performance is achieved. Let it be a little more work for my fingers — but I will be able to sleep well afterwards.

**The second** — neither database-first approach nor code-first is good enough; the second part usually suffers. The best solution is to leave an application domain model to the OOP world, a database to the relational world, and let the ORM eliminate the difference.

**The third** — I still believe that an application should work with a database through stored procedures. Yes, I know that we are at the end of 2016, but old school is cool! There are many reasons for that: performance, security, maintainability. Note: I did not say a word about keeping business logic in stored procedures. And one more reason — a stored procedure is an efficient way to save and retrieve object graphs at once.

**The fourth** — in continuation of the previous paragraph — I believe CRUD commands as SQL text in the application code are evil. Not the absolute evil, of course, but at least a terrible sin. The right place to query a database is a repository. And even there — call a stored procedure! That's enough for my auto-da-fé, I guess.

**The fifth** — existing ORMs usually suppose that an application domain model and a database are developed synchronously from the beginning, so objects match tables, property names and types correspond to column names and types. But in the world where I live, it is not always so. Rather often, I find myself in a situation where the application domain model is already created, has a complex OO structure, and it is necessary to make persistent storage for it. Or the other case — when a new UI is being created for a very old legacy database. In such cases I would like to have an instrument that helps me to integrate two different parts with less effort.

The above assertions are my point of view, of course not the ultimate truth. You are free to have another opinion — even the opposite one. :)

To make a long story short, here are my initial targets:

- The best performance
- Full control over data transformation in mappers
- Preferably the repository pattern
- Database interactions mostly through stored procedures
- Convenient multiple-result-sets reading
- Easy table-valued parameter creation
- Overall code reduction

## The Way to My Own ORM

So I started from the beginning — pure ADO.NET. For example, here is how to get a `User` by Id with ADO.NET:

```csharp
public static User GetUserById(int id)
{
    using (SqlConnection connection = new SqlConnection(connectionString))
    {
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "select Id, Login, Name, Email from dbo.Users where Id = @Id";

            cmd.Parameters.Add(new SqlParameter
            {
                ParameterName = "@Id",
                Direction     = ParameterDirection.Input,
                SqlDbType     = SqlDbType.Int,
                Value         = id
            });

            User user = null;

            connection.Open();
            using (var dr = cmd.ExecuteReader())
            {
                if (dr.Read())
                {
                    user = new User
                    {
                        Id    = (int)dr["Id"],
                        Login = (string)dr["Login"],
                        Name  = (string)dr["Name"],
                        Email = (string)dr["Email"]
                    };
                }
            }
            connection.Close();

            return user;
        }
    }
}
```

There is too much code for such a simple sample. Imagine what it would be for a whole object-graph saving!

## Data-to-Object Mapping

Almost the whole code can be refactored into extension methods on `SqlConnection`, `SqlCommand` and `SqlDataReader` — except for the part inside the `using ... ExecuteReader` block:

```csharp
using (var dr = cmd.ExecuteReader())
{
    if (dr.Read())
    {
        user = new User
        {
            Id    = (int)dr["Id"],
            Login = (string)dr["Login"],
            Name  = (string)dr["Name"],
            Email = (string)dr["Email"]
        };
    }
}
```

What if we refactor it a little and pull out this `Func` delegate?

```csharp
Func<SqlDataReader, User> createUser = dr =>
{
    return new User
    {
        Id    = (int)dr["Id"],
        Login = (string)dr["Login"],
        Name  = (string)dr["Name"],
        Email = (string)dr["Email"]
    };
};
```

Then a simplified code for the `using ... ExecuteReader` block turns into:

```csharp
using (var dr = cmd.ExecuteReader())
{
    if (dr.Read())
        user = createUser(dr);
}
```

Why is this important? Because ORMs are usually based on how to create and use that `Func<SqlDataReader, T>` delegate to create a `T` object.

There is the article *The Tale of Three Monkeys and A Wolf* that demonstrates three main approaches to that task. Dapper, for example, generates that `Func` delegate with `DynamicMethod` and keeps it in cache. This allows the performance to match a hand-written `Func` delegate. It is the best solution for auto-mapping, I think.

But I have a paranoia about black boxes for such critical things as data. And I want to have more power and control over how the data is transformed.

Therefore, for my ORM, I prefer the hand-written `Func` delegate. That means I prepare and keep somewhere the code for data-to-object transformation, while some other ORMs autogenerate it.

Also for `SqlDataReader`, I prefer **ordinal access** rather than access by column name:

```csharp
Func<SqlDataReader, User> createUser = dr =>
{
    var i = 0;
    return new User
    {
        Id    = dr.GetInt32  (i)   ,
        Login = dr.GetString (++i) ,
        Name  = dr.GetString (++i) ,
        Email = dr.GetString (++i)
    };
};
```

Why ordinal access? Because it is faster. And because in that case, the column names don't even matter.

Of course there is always a risk of mixing up the order, but to be on the safe side you can create, for example, a view:

```sql
create view dbo.vwUsers
as
(
    select
        Id      ,
        [Login] ,
        Name    ,
        Email
    from
        dbo.Users
);
```

…and always use `select * from vwUsers` when fetching the User. Yes, I know that `select *` is bad practice — but we are here to break the rules, dude.

## Object-to-Data Mapping

Besides data-to-object mapping, I need the reverse direction — object-to-data.

The technique of object-graph saving means creating user-defined data types for each application domain type that is to be persisted. So I have got two more mapping delegates for a domain type: one to create a `DataTable`, and another to transform an object to a `DataRow`.

## Static Mapper Class

Where to keep those `Func` delegates? My solution is to make a static class with static methods and decorate it with the custom `MapperFor` attribute, like this:

The following mapper belongs to the article's four-column User schema. The current sample's [User mapper](../Tests/DAL/Users/Models/User.cs) and [SQL UserTableType](../Database/4.%20Types/UserTableType.sql) also include RowVersion and RoleIds; use those together with the current procedures.

```csharp
[MapperFor(typeof(User))]
public static class UserMapper
{
    public static User CreateObject(SqlDataReader dr)
    {
        var i = 0;
        return new User
        {
            Id    =  dr.GetInt32  (i)   ,
            Login =  dr.GetString (++i) ,
            Name  =  dr.GetString (++i) ,
            Email =  dr.GetString (++i)
        };
    }

    public static DataTable CreateDataTable()
    {
        return new DataTable("UserTableType")
            .AddColumn< Int32  >( "Id"    )
            .AddColumn< String >( "Login" )
            .AddColumn< String >( "Name"  )
            .AddColumn< String >( "Email" );
    }

    public static object[] CreateDataRow(User obj)
    {
        return new object[]
        {
            obj.Id    ,
            obj.Login ,
            obj.Name  ,
            obj.Email
        };
    }
}
```

`MappingManager` discovers `MapperFor` attributes, converts the static methods to delegates, and caches them by mapped type. In the current implementation this is a one-time lazy scan on the first registered-mapper operation, rather than an automatic scan at application startup.

For 4.1.0, this three-method mapper can declare its intended contract explicitly:

```csharp
[MapperFor(typeof(User), RequiredMethod.AllMain)]
```

`AllMain` requires `CreateObject`, `CreateDataTable` and `CreateDataRow`. This makes accidental removal of either the read or write side visible to the Roslyn analyzer and the runtime checks. `All` additionally requires `CreateObjectRow`, which this example does not implement. The original attribute without a second argument remains valid; it does not declare a required-method set. The analyzer checks registrations and operations at build time, while runtime discovery remains in place. See [compile-time mapper validation](../docs/analyzers.md).

## Extension Methods

Once there is a cache with `Func` delegates in the application, and it is possible to fetch a delegate by type, all the routine calls of `SqlCommand` or `SqlDataReader` execution can be refactored into generic extension methods:

```csharp
cmd.ReadTo<User>();
cmd.ReadToList<User>();
cmd.AddTableParam("@Records", records);

dr.ReadTo<string>();
dr.ReadToArray<int>();
dr.ReadToDictionary<int, User>();
```

## Auto-Mapping

When property/column names, quantity and types of an object and a database query match, it is safe to use auto-mapping and not write a static mapper. My ORM has several extension methods for those cases:

```csharp
cmd.ReadAs<User>();
cmd.ReadAsList<User>();
dr.ReadAsArray<Record>();
```

The `ReadAs` methods use auto-mapping based on expression-trees compilation:

- When `SqlDataReader` reads the first row from a result set, it calls the expression-trees builder which uses the API.
- The expression-trees builder looks for column names in `SqlDataReader` and matches them to target object properties.
- The expression trees are then compiled into a `Func<SqlDataReader, T>` mapping delegate.
- This mapping delegate is cached into a dictionary with a composite string key.
- The composite key consists of the `SqlCommand` text and the full name of the target object type.
- Therefore every next `SqlDataReader` reading uses the compiled and cached mapping delegate.

> **Current cache behavior.** The command-text key above describes the historical implementation. In 4.1.0, the cache uses the actual target `Type` identity and the reader shape: ordered column names and field types. Queries with the same shape can reuse a mapper without matching SQL text. Registered mapper discovery still uses reflection; hand-written mappers are not an automatic Native AOT or trimming guarantee.

So auto-mapping performance is almost the same as the performance of hand-written mappers.

> Damn it. I became a creator of a black box. Am I slipping down to the dark side?

## Read-Methods

The most frequent interaction with a database is data fetching. So the collection of mapper-required and auto-mapping `Read`-methods became the main part of my ORM:

| Read-method | Description |
|---|---|
| `ReadTo<T>` | to read a single value or object using an existing mapper |
| `ReadToAsync<T>` | to read a single value or object asynchronously using an existing mapper |
| `ReadAs<T>` | to read a single object using auto-mapping |
| `ReadAsAsync<T>` | to read a single object asynchronously using auto-mapping |
| `ReadToList<T>` | to read a list of values or objects using an existing mapper |
| `ReadToListAsync<T>` | to read a list of values or objects asynchronously using an existing mapper |
| `ReadAsList<T>` | to read a list of objects using auto-mapping |
| `ReadAsListAsync<T>` | to read a list of objects asynchronously using auto-mapping |
| `ReadToArray<T>` | to read an array of values or objects using an existing mapper |
| `ReadToArrayAsync<T>` | to read an array of values or objects asynchronously using an existing mapper |
| `ReadAsArray<T>` | to read an array of objects using auto-mapping |
| `ReadAsArrayAsync<T>` | to read an array of objects asynchronously using auto-mapping |
| `ReadToObjectRow<T>` | to read a single `ObjectRow` using an existing mapper |
| `ReadToObjectRowAsync<T>` | to read a single `ObjectRow` asynchronously using an existing mapper |
| `ReadAsObjectRows` | to read `ObjectRows` using auto-mapping |
| `ReadAsObjectRowsAsync` | to read `ObjectRows` asynchronously using auto-mapping |
| `ReadToDictionary<TKey, TValue>` | to read a dictionary of objects with the first column as a key, using an existing mapper |
| `ReadToDictionaryAsync<TKey, TValue>` | …asynchronously |
| `ReadAsDictionary<TKey, TValue>` | …using auto-mapping |
| `ReadAsDictionaryAsync<TKey, TValue>` | …asynchronously |
| `ReadToEnumerable<T>` | to read an `IEnumerable` of objects using an existing mapper (sync method only) |
| `ReadAsEnumerable<T>` | to read an `IEnumerable` of objects using auto-mapping (sync method only) |

> The list above is the **v1.x snapshot**. Since then it has been extended with multi-result-set tuple reads (`ReadToLists<T1, T2[, T3[, T4]]>`), `IAsyncEnumerable` streaming (`ReadToAsyncEnumerable<T>` / `ReadAsAsyncEnumerable<T>`), tree builders (`ReadToTree<T>` / `ReadToTreeList<T>`), `dynamic` reads, async tree variants, and `IList<T>?` overloads for pre-populated lists. See [Read Methods Understanding](https://github.com/lobodava/artisan-orm/wiki/Read-Methods-Understanding) on the wiki for the current list.

## Inline Mappers

All the above `ReadTo` extension methods accept `Func<SqlDataReader, T>` as a parameter, so besides separate mappers and auto-mapping, these methods can be used with so-called **inline mappers**:

```csharp
var user = cmd.ReadTo(dr => new User
{
    Id    = dr.GetInt32  (0) ,
    Login = dr.GetString (1) ,
    Name  = dr.GetString (2) ,
    Email = dr.GetString (3)
});
```

## Repository Base

Because I decided to use a repository pattern, I created a base class for all custom repositories. This `RepositoryBase` class can take responsibility for the connection, command creation, and transaction (if the latter is required). One repository can have one connection and one transaction at a time, and create many commands.

In order to encapsulate all the logic of disposing the expensive resources (closing the connection and disposing the command), `RepositoryBase` has methods that create a `SqlCommand` and pass it as an argument to a `Func` or `Action` parameter:

```csharp
public T   GetByCommand<T>(Func<SqlCommand, T> func)
public void RunCommand   (Action<SqlCommand>   action)
public int  ExecuteCommand(Action<SqlCommand>  action)
```

In the current API, `GetByCommand` and `RunCommand` own the command lifetime; the read/execute operation inside the callback opens and closes the connection when needed. They do not automatically open a connection for arbitrary raw ADO.NET calls. A repository created from a connection string owns its connection. A repository attached to an external transaction borrows that transaction's existing connection, and disposing it leaves both resources with the caller. Keep commands and readers alive while consuming deferred results, and do not use one repository concurrently.

## Repository Methods

Inheriting from `RepositoryBase`, having hand-written mappers and the extension methods for `SqlCommand` and `SqlDataReader` allow making the repository methods more compact, the code more readable, and self-explanatory.

```csharp
public User GetUserById(int id)
{
    return ReadTo<User>("dbo.GetUserById", new SqlParameter("Id", id));
}
```

This is how to save a User and read it back:

This historical example assumes the four-column TVP above and a procedure whose first result contains the saved user. The current [SaveUser procedure](../Database/5.%20Procedures/Users/SaveUser.sql) has a different contract: its User TVP has six columns, role IDs are passed separately, and a DataReply status result precedes the payload. The current [repository example](../Tests/DAL/Users/Repository.cs) supplies those parameters and consumes the status before reading the user. Updating only this mapper or only this call is not sufficient to target the current sample database.

```csharp
public User SaveUser(User user)
{
    return GetByCommand(cmd =>
    {
        cmd.UseProcedure("dbo.SaveUser");
        cmd.AddTableRowParam("@User", user);
        return cmd.ReadTo<User>();
    });
}
```

And here is the example from the [previous article](01-object-graph-saving.md), rewritten for ORM use:

```csharp
public IList<GrandRecord> SaveGrandRecords(IList<GrandRecord> grandRecords)
{
    var records      = grandRecords.SelectMany(gr => gr.Records);
    var childRecords = records.SelectMany(r => r.ChildRecords);

    return GetByCommand(cmd =>
    {
        cmd.UseProcedure("dbo.SaveGrandRecords");
        cmd.AddTableParam("@GrandRecords", grandRecords);
        cmd.AddTableParam("@Records",      records);
        cmd.AddTableParam("@ChildRecords", childRecords);

        return cmd.GetByReader(dr =>
        {
            var grandRecords = dr.ReadToList<GrandRecord>();
            var records      = dr.ReadToList<Record>();
            var childRecords = dr.ReadToList<ChildRecord>();
            dr.Close();

            grandRecords.MergeJoin(
                records,
                (gr, r) => gr.Id == r.GrandRecordId,
                (gr, r) => { r.GrandRecord = gr; gr.Records.Add(r); },

                childRecords,
                (r, cr) => r.Id == cr.RecordId,
                (r, cr) => { cr.Record = r; r.ChildRecords.Add(cr); }
            );

            return grandRecords;
        });
    });
}
```

For the graph example above, the three result sets must follow the same parent traversal order: grand records by Id, records by GrandRecordId then Id, and child records by GrandRecordId then RecordId then Id. Every detail must have a parent in the supplied lists; an unmatched detail can prevent later matches. The current [SaveGrandRecords procedure](../Database/5.%20Procedures/GrandRecords/SaveGrandRecords.sql) supplies that order. Use its matching [mappers and repository](../Tests/DAL/GrandRecords/) when running against the current sample schema.

## About Artisan.Orm

The above approach has been tested in several projects and proved its efficiency. So I decided to create a separate library.

I named my project **Artisan.Orm**, because it allows you to neatly and accurately tune up the DAL.

> Ford's conveyor is cool — but Ferraris are assembled by hand. :)

If you are interested in the project, please visit the [Artisan.Orm GitHub page](https://github.com/lobodava/artisan-orm) and its [documentation wiki](https://github.com/lobodava/artisan-orm/wiki). Artisan.Orm is also available as a [NuGet package](https://www.nuget.org/packages/Artisan.ORM/).

## About the Source Code

The original article shipped with a Visual Studio 2015 solution containing three projects:

- **Artisan.Orm** — DLL project containing the Artisan.Orm classes
- **Database** — SSDT project to create the database for SQL Server (used as test data)
- **Tests** — test project with examples of the code in use

The source has moved on through several major versions. The 4.1.0 preparation tree targets `netstandard2.1`, `net8.0` and `net10.0`, and includes the analyzer and analyzer-test projects in addition to the original three. See [What's New in v4](https://github.com/lobodava/artisan-orm/wiki/Whats-New-in-v4) and [CHANGELOG](../CHANGELOG.md).

## Multiple Exception Output

When an error or exception occurs in a single insert/update/delete statement, it is enough to raise an error and throw an exception with a single error code or message.

It is quite a different challenge when it is required to collect several issues while saving an object graph and transmit those details to an application client. Artisan.Orm offers an optional status-and-message protocol for this task, described in [The Artisan Way of Data Reply](03-data-reply.md). It is not required for ordinary reads and writes or a replacement for SQL exceptions.

## Original Publication History

- **16 November 2016** — initial publication.
- **23 November 2016** — added section about auto-mapping via expression trees. Source code updated to v1.0.5.
- **30 December 2016** — added section about Read-methods. Source code updated to v1.0.7.
- **31 January 2017** — added `ReadToDictionary` and `ReadAsDictionary` extension methods. Source code updated to v1.0.8.
- **13 June 2017** — added paragraph about Inline Mappers. Source code updated to v1.1.0. Added link to the article *Artisan Way of Data Reply*. Added paragraph about Multiple Exception Output.
- **4 February 2018** — added methods for working with hierarchies. .NET Framework source code updated to v1.1.3. Implemented as a .NET Standard 2.0 library; added .NET Standard source code v2.0.0 with tests for .NET Core.
- **15 June 2020** — final revision on CodeProject.

## What Has Happened Since

The 4.1.0 preparation tree targets `netstandard2.1`, `net8.0` and `net10.0`. Highlights since this article was first published:

- Async read/write variants and `IAsyncEnumerable<T>` streaming were added. Some materializing helpers still perform synchronous row reads, connection opening in read helpers does not forward cancellation, and `GetByReaderAsync` accepts a synchronous projection. Use explicit async reader traversal when those boundaries must be asynchronous and cancellable.
- A `BulkCopy<T>` / `BulkCopyAs<T>` family was added on top of `SqlBulkCopy`.
- Multi-result-set reads via tuple-returning `ReadToLists<T1, T2[, T3[, T4]]>` paired with `MergeJoin` for one-pass parent-child graph assembly.
- Streaming via `ReadToAsyncEnumerable<T>` for queries that don't fit in memory.
- JSON parameters for `OPENJSON`-based round-trips, plus `DateOnly` / `TimeOnly` (NET6+).
- Output-parameter helpers for the nine common SQL types.
- `IAsyncDisposable` support and proper async transactions (`RunInTransactionAsync`).
- Roslyn mapper validation checks the declared `RequiredMethod` contract and mapper-dependent operations during a normal build. Runtime checks remain as a second line of validation.

The full picture lives in [What's New in v4](https://github.com/lobodava/artisan-orm/wiki/Whats-New-in-v4) on the wiki.

---

*Originally published as [Artisan.Orm or How to Reinvent the Wheel](https://www.codeproject.com/Articles/1155836/Artisan-Orm-or-How-to-Reinvent-the-Wheel) on CodeProject, November 2016, with revisions through June 2020. CodeProject went read-only in late 2024; this version preserves the article for posterity. Re-published under [CC-BY-SA-4.0](LICENSE).*
