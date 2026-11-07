# Combination of Id-ParentId and HierarchyId Approaches to Hierarchical Data

> **Republication note.** Originally published on [CodeProject in July 2017](https://www.codeproject.com/Articles/1192607/Combination-of-Id-ParentId-and-HierarchyId).
> CodeProject has since gone read-only and PDFs were the only surviving
> snapshot. This re-publication preserves the historical explanation; a handful of
> typos and PDF extraction artifacts have been cleaned up, and the SQL is
> formatted for modern Markdown rendering. `hierarchyid` was introduced in SQL
> Server 2008, but the examples here use `LAG`, `LEAD` and `CONCAT` and require
> SQL Server 2012 or later; see the [LAG version requirement](https://learn.microsoft.com/en-us/sql/t-sql/functions/lag-transact-sql).
>
> A working implementation lives in the Artisan.Orm sample repository:
> [`Database/`](../Database/) for the schema, procedures and SQL functions;
> [`Tests/DAL/Folders/`](../Tests/DAL/Folders/) for the C# repository code.
> The standalone schemas and alternative procedure names below are historical;
> the 4.1.0 compatibility notes identify the current entry points. The SQL examples
> include corrections to sibling numbering and the full-width HidCode conversion.
>
> — *Vadim Loboda, originally 17 Jul 2017*

Combine Id-ParentId and HierarchyId approaches to keep and fetch the data in hierarchical and alphabetical order.

## Introduction

### Hierarchical and Alphabetical Sorting

To sort the data hierarchically and alphabetically means sorting a tree using a depth-first search algorithm and selecting nodes with the same parent in the alphabetical order of their names.

The hierarchical sorting together with the level of nesting is a good way to emulate an expanded tree or a table of book contents using just a flat list of objects without the necessity to build a tree of objects.

Just fetch the records, sort them hierarchically and alphabetically, indent each record _n_ times — where _n_ is the level of nesting — and you get the expanded tree for UI and reports.

### Two Approaches

There are several approaches to structuring a relational database to store hierarchical data. The one that is used most frequently is the **Id-ParentId** approach.

The Id-ParentId approach has a few drawbacks that make it necessary to seek alternative methods. The most difficult and expensive tasks in Id-ParentId are those that require recursion, for example:

- find all descendants
- find all ancestors
- calculate the level of nesting
- sort flat records hierarchically and then alphabetically

In response to these difficulties, Microsoft added the **HierarchyId** data type to SQL Server starting from version 2008. HierarchyId:

- helps finding descendants and ancestors without recursion
- has a fast `GetLevel()` method
- contains the information about a node's position in the hierarchy
- contains the information about a node's order within its parent's children

If you build a clustered index on HierarchyId, all node-records are physically stored in the order in which they are most frequently fetched.

> **Ordering clarification for 4.1.0.** The historical wording describes the useful depth-first organization of clustered keys, not guaranteed page contiguity, insertion order, or result order. Use `ORDER BY UserId, Hid` (or `ORDER BY Hid` within one user) when returning a hierarchy. The index supports that access pattern; the explicit order defines the result contract. See the [ordering discussion in Article 1](01-object-graph-saving.md#410-clarification-what-the-indexes-actually-guarantee).

But HierarchyId still has drawbacks — trade-offs for its useful properties:

- it does not automatically enforce parent existence or a tree structure. A persisted computed parent column based on `GetAncestor(1)` can participate in a self-referencing foreign key, as shown in [Microsoft's tree-constraint example](https://learn.microsoft.com/en-us/sql/relational-databases/hierarchical-data-sql-server#enforce-a-tree). Stable integer IDs remain useful when paths change.
- difficult ways to generate HierarchyId values for added and changed records to keep the required order — SQL Server does not do this automatically; it is up to the programmer to manage it.
- difficult use of HierarchyId as a primary or foreign key: it may regularly change.
- it has no corresponding type in JavaScript (for example).

So what to choose: Id-ParentId or HierarchyId?

An acceptable solution to this dilemma is **the combination of two approaches**: Id-ParentId and HierarchyId. This combination will allow us to:

- use `Id` as an integer, ever-increasing, unchangeable primary key
- use `ParentId` as a self-referencing foreign key
- use `Hid` (HierarchyId) to keep the records physically sorted in hierarchical and alphabetical order

This article describes the way to unite the both approaches and ensure their normal operation.

## Some Facts about HierarchyId

### Column and Field Names in this Article

- `Hid` — column of HierarchyId data type
- `HidPath` — string column for human-readable path of HierarchyId
- `HidCode` — a binary representation of HierarchyId converted to a string and without the leading `0x`

### To Human-Readable Format

The HierarchyId data type is a materialized path encoded into binary format. It can be decoded to a human-readable string path with the `ToString()` method:

```sql
select Hid, Hid.ToString() as HidPath, Hid.GetLevel() as Level
from   Folders
order by Hid;
```

```text
Hid           HidPath        Level
-----------   --------------  ------
0x            /               0
0x58          /1/             1
0x5AC0        /1/1/           2
0x5AD6        /1/1/1/         3
0x5AD6B0      /1/1/1/1/       4
0x5AD6B580    /1/1/1/1/1/     5
0x5AD6B680    /1/1/1/1/2/     5
0x5AD6B780    /1/1/1/1/3/     5
0x5AD6D0      /1/1/1/2/       4
0x5AD6D580    /1/1/1/2/1/     5
0x5AD6D680    /1/1/1/2/2/     5
0x5AD6D780    /1/1/1/2/3/     5
...
```

> Note: `ToString()` and `GetLevel()` are case-sensitive!

### From Human-Readable Format

The opposite operation — from a string path to the binary format — works as well:

```sql
declare @Hid hierarchyid = '/1/1/1/1/3/';
select @Hid;
-- 0x5AD6B780
```

The above is the implicit conversion. HierarchyId has the explicit `Parse()` method, although the call format is a little strange:

```sql
declare @Hid hierarchyid = hierarchyid::Parse('/1/1/1/1/3/');  -- Parse() is case-sensitive!
select @Hid;
-- 0x5AD6B780
```

### Tricky Numbering System of Paths

The interesting thing about HierarchyId is the way it manages the numeration of a node inserted between two other nodes.

To get a Hid for a new node, HierarchyId uses the `GetDescendant()` method on the parent Hid and the two Hids between which the new one has to be inserted:

```sql
declare @Parent hierarchyid = 0x;

print @Parent.GetDescendant('/1/',   '/2/'  ).ToString();   -- /1.1/
print @Parent.GetDescendant('/1/',   '/1.1/').ToString();   -- /1.0/
print @Parent.GetDescendant('/1/',   '/1.0/').ToString();   -- /1.-1/
print @Parent.GetDescendant('/1/',   '/1.-1/').ToString();  -- /1.-2/
print @Parent.GetDescendant('/1.1/', '/2/'  ).ToString();   -- /1.2/
print @Parent.GetDescendant('/1.2/', '/2/'  ).ToString();   -- /1.3/
print @Parent.GetDescendant('/1.3/', '/2/'  ).ToString();   -- /1.4/
print @Parent.GetDescendant('/1.3/', '/1.4/').ToString();   -- /1.3.1/

print @Parent.GetDescendant('/1.2.3.4.5.6.7.8/', '/1.2.3.4.5.6.7.9/').ToString();
-- /1.2.3.4.5.6.7.8.1/

-- by the way
declare @Hid hierarchyid = '/1.2.3.4.5.6.7.8.1/';
select @Hid;  -- 0x63A08A49A85258

set @Hid = '/-1.-2.-3.-4.-5.-6.-7.-8.-1234567890/';
select @Hid;  -- 0x41F8F87A3C1D8E87216D9A81A73A

-- special cases with null
print @Parent.GetDescendant(null,  null ).ToString();  -- /1/
print @Parent.GetDescendant('/1/', null ).ToString();  -- /2/
print @Parent.GetDescendant(null,  '/1/').ToString();  -- /0/
```

### Why Need Binary Encoding?

If we can build a string path, why do we need this HierarchyId binary encoding? Could we just sort the hierarchical data by this string path?

Look at this example:

```sql
select hierarchyid::Parse('/1/')  as Hid, '/1/'  as HidPath union all
select hierarchyid::Parse('/2/')        , '/2/'             union all
select hierarchyid::Parse('/10/')       , '/10/'
order by HidPath;
```

```text
Hid     HidPath
-----   --------
0x58    /1/
0xAA    /10/
0x68    /2/
```

The same query but ordered by `Hid` produces the right sorting:

```text
Hid     HidPath
-----   --------
0x58    /1/
0x68    /2/
0xAA    /10/
```

Microsoft uses a sophisticated algorithm in HierarchyId to encode the string path so that it can be used for hierarchical sorting just by the `Hid` column value.

## The Solution

### Master and Slave

So let's combine the Id-ParentId self-referencing approach with HierarchyId.

The Id-ParentId part will be responsible for the `Id` primary key and the self-referencing `ParentId` foreign key — and will be the leading, or **master**.

The HierarchyId part will be a calculated field — the **slave**.

Of course, `Hid` as a persistent calculated field is denormalization. But this is a conscious step and a compromise for the advantages HierarchyId brings.

The best place and moment to calculate the HierarchyId and keep it in a coordinated state is a stored procedure, while saving the node.

### User Catalog for Experiments

Let's imagine a multi-user application where each user keeps its own catalog. This catalog stores information in hierarchical folders. The table for these folders might look like this:

This is the article's standalone teaching schema. The current [sample Folders table](../Database/1.%20Tables/Users/4.%20Folders.sql) uses the `dbo.FolderId` sequence and `DEFAULT NEXT VALUE FOR` instead of `IDENTITY`; the integer key, ParentId relationship and `(UserId, Hid)` clustered key serve the same roles.

```sql
create table dbo.Folders
(
    UserId    int           not null,
    Hid       hierarchyid   not null,
    Id        int           not null  identity,
    ParentId  int               null,
    Name      nvarchar(50)  not null,

    constraint CU_Folders        unique    clustered    (UserId asc, Hid asc),
    constraint PK_Folders        primary key nonclustered (Id asc),
    constraint FK_Folders_UserId foreign key (UserId)   references dbo.Users   (Id),
    constraint FK_Folders_ParentId foreign key (ParentId) references dbo.Folders (Id),

    constraint CH_Folders_ParentId check
        (Hid = 0x  and ParentId is null
         or Hid <> 0x and ParentId is not null)
);
```

Note that the **clustered index is built on `(UserId, Hid)`**, but the **primary key is on `Id`**. This allows us to:

- organize each user's folder records by the hierarchical clustered key, supporting depth-first access,
- use the integer `Id` as a primary key — and as a foreign key for other tables and DTOs.

The application models one catalog root per user. The unique `(UserId, Hid)` key permits at most one stored `/` root for each user; it does not require that a root exists. `hierarchyid` values alone do not enforce a complete tree.

The application's initialization must create each user's root as a system record that users do not edit. The table permits `ParentId = NULL` for that root. The single-folder save contract below accepts only non-root nodes, so its TVP requires a non-null ParentId. The foreign key checks parent existence, but does not by itself prevent cycles, cross-user parenting, or disagreement between ParentId and Hid.

### The Best Place to Make the Calculation

As Microsoft states it: "It is up to the application to generate and assign HierarchyId values in such a way that the desired relationship between rows is reflected in the values."

The best way to create and keep the integrity and consistency of hierarchical data, in our case, is to always use the same stored procedure to insert and update a Folder node.

A user-defined table type that represents the saving Folder may serve as a parameter to the `SaveFolder` stored procedure:

```sql
create type dbo.FolderTableType as table
(
    Id        int           not null  primary key clustered,
    ParentId  int           not null,
    Name      nvarchar(50)  not null
);
```

And here is the historical `SaveFolder` stored procedure itself. It illustrates the incremental path algorithm. The current [SaveFolder procedure](../Database/5.%20Procedures/Folders/SaveFolder.sql) combines this and the recalculation alternative behind `@WithHidReculc`, and also returns the saved folder.

```sql
create procedure dbo.SaveFolder
    @Folder dbo.FolderTableType readonly
as
begin
    set nocount on;

    begin -- variable declaration

        declare
            @ParamId         int         ,
            @ParentId        int         ,
            @UserId          int         ,
            @ParentHid       hierarchyid ,
            @StartTranCount  int         ,
            @OldHid          hierarchyid ,
            @NewHid          hierarchyid ;

        declare @FolderIds table (
            InsertedId  int          not null,
            OldHid      hierarchyid      null,
            NewHid      hierarchyid      null
        );
    end;

    begin try
        set @StartTranCount = @@trancount;
        if @StartTranCount = 0
        begin
            set transaction isolation level serializable;
            begin transaction;
        end;

        begin -- init variables and lock parent for update

            select
                @ParamId  = Id,
                @ParentId = ParentId
            from
                @Folder;

            select
                @UserId    = UserId,
                @ParentHid = Hid
            from
                dbo.Folders
            where
                Id = @ParentId;
        end;

        begin -- save the @Folder
            merge into dbo.Folders as target
                using
                (
                    -- full join in this 'select' allows to see the picture
                    -- as if the change had already applied;
                    -- coalesce(t.x, f.x) takes the new value if t.x exists
                    -- and the old value if not.
                    select
                        -- LAG and LEAD functions help find the previous and next
                        -- Hid values when sorting by Name
                        Hid       =  @ParentHid.GetDescendant(
                                        LAG (case when t.Id is null then f.Hid end)
                                            over (order by coalesce(t.Name, f.Name)),
                                        LEAD(case when t.Id is null then f.Hid end)
                                            over (order by coalesce(t.Name, f.Name))
                                     ),
                        Id        =  coalesce(t.Id      , f.Id      ),
                        ParentId  =  coalesce(t.ParentId, f.ParentId),
                        Name      =  coalesce(t.Name    , f.Name    )
                    from
                        (select * from dbo.Folders where ParentId = @ParentId) f
                        full join @Folder t on t.Id = f.Id
                )
                -- For LAG and LEAD we need all children of @ParentId,
                -- but for MERGE we want only the Folder where source.Id = @ParamId.
                as source on source.Id = @ParamId and source.Id = target.Id

            -- target.UserId = @UserId here just to make sure that
            -- we do not reparent a Folder to another user.
            when matched and target.UserId = @UserId then
                update set
                    Hid       = source.Hid,
                    Name      = source.Name,
                    ParentId  = source.ParentId

            -- source.Id = 0 here just to make sure that we insert a new Folder,
            -- not an already-deleted one. A not-matched Id can be deleted by
            -- another user, and we can try to add it again.
            when not matched by target and source.Id = 0 then
                insert (UserId, Hid, ParentId, Name)
                values (@UserId, source.Hid, source.ParentId, source.Name)

            output
                inserted.Id,
                deleted.Hid,
                inserted.Hid
            into @FolderIds (InsertedId, OldHid, NewHid);
        end;

        begin -- reparent children
            -- Saving a folder might change its ParentId. In that case the
            -- Hid of all its children should reflect the change as well.
            select top 1
                @OldHid = OldHid, @NewHid = NewHid
            from @FolderIds;

            if @OldHid <> @NewHid
                update dbo.Folders set
                    Hid = Hid.GetReparentedValue(@OldHid, @NewHid)
                where
                    UserId = @UserId
                    and Hid.IsDescendantOf(@OldHid) = 1;
        end;

        if @StartTranCount = 0 commit transaction;
    end try
    begin catch
        if xact_state() <> 0 and @StartTranCount = 0 rollback transaction;

        declare @ErrorMessage nvarchar(4000) = dbo.GetErrorMessage();
        raiserror(@ErrorMessage, 16, 1);
        return;
    end catch;
end;
```

Using one save path helps keep ParentId and Hid coordinated, provided its input and transaction preconditions hold. Supply one non-root folder, an existing parent belonging to the intended user, and reject moves beneath the node itself or its descendants. The sample procedure does not implement all of those validations. It sets SERIALIZABLE only when it starts its own transaction; a caller supplying an ambient transaction must choose suitable isolation and locking. The comment about locking the parent is not a substitute for that policy.

## Fetch a Folder With Its Descendants in Hierarchical Order

The following stored procedure fetches a Folder with all its descendants and sorts them hierarchically and then alphabetically — as if they are an expanded tree:

```sql
create procedure dbo.GetFolderWithSubFolders
    @FolderId int
as
begin
    set nocount on;

    select
        d.Id       ,
        d.ParentId ,
        d.Name     ,
        [Level]  =  d.Hid.GetLevel(),
        HidCode  =  dbo.GetHidCode(d.Hid),
        HidPath  =  d.Hid.ToString()
    from
        dbo.Folders f
        inner join dbo.Folders d
                on d.UserId = f.UserId
               and d.Hid.IsDescendantOf(f.Hid) = 1
    where
        f.Id = @FolderId
    order by
        d.Hid;
end;
```

For example:

```sql
exec dbo.GetFolderWithSubFolders @FolderId = 40;
```

```text
Id    ParentId  Name  Level  HidCode    HidPath
----- --------- ----- ------ ---------- ------------
40    13        3A    3      5AD6       /1/1/1/
121   40        4A    4      5AD6B0     /1/1/1/1/
364   121       5A    5      5AD6B580   /1/1/1/1/1/
365   121       5B    5      5AD6B680   /1/1/1/1/2/
366   121       5C    5      5AD6B780   /1/1/1/1/3/
122   40        4B    4      5AD6D0     /1/1/1/2/
367   122       5A    5      5AD6D580   /1/1/1/2/1/
368   122       5B    5      5AD6D680   /1/1/1/2/2/
369   122       5C    5      5AD6D780   /1/1/1/2/3/
123   40        4C    4      5AD6F0     /1/1/1/3/
370   123       5A    5      5AD6F580   /1/1/1/3/1/
371   123       5B    5      5AD6F680   /1/1/1/3/2/
372   123       5C    5      5AD6F780   /1/1/1/3/3/
```

### What is the Use of `HidCode`?

As stated above, `HidCode` is a binary representation of HierarchyId converted to varchar, with the leading `0x` stripped.

Here is the `dbo.GetHidCode` scalar function:

```sql
create function dbo.GetHidCode (@Hid hierarchyid)
returns varchar(1784)
as
begin
    return convert(varchar(1784), cast(@Hid as varbinary(892)), 2);
end;
```

A hierarchyid can occupy [up to 892 bytes](https://learn.microsoft.com/en-us/sql/t-sql/data-types/hierarchyid-data-type-method-reference), so its full hexadecimal representation can require 1784 characters. Conversion style 2 already omits `0x`. Keep that capacity in transport fields and client storage too; the former 1000-character version could truncate valid long paths. This width describes HidCode, not the separate human-readable HidPath buffers used by the historical procedures.

The `HidCode` string can be used on the client side to sort a flat list of Folders hierarchically — convenient when you want to keep the heavy lifting in SQL but ship a flat list to JavaScript and let the UI re-sort on the fly.

## Chapter for the Perfectionists

The above method of `Hid` calculation works well, but it has a tiny drawback.

```text
-- imagine you have two sibling folders
Id    ParentId  Name  Level  HidCode    HidPath
----- --------- ----- ------ ---------- ------------
364   121       5A    5      5AD6B580   /1/1/1/1/1/
365   121       5B    5      5AD6B680   /1/1/1/1/2/

-- and you want to insert a new folder
Id    ParentId  Name
----- --------- -----
0     121       5Aaa

-- the result will be
Id    ParentId  Name  Level  HidCode    HidPath
----- --------- ----- ------ ---------- ------------
364   121       5A    5      5AD6B580   /1/1/1/1/1/
1234  121       5Aaa  5      5AD6B62C   /1/1/1/1/1.1/
365   121       5B    5      5AD6B680   /1/1/1/1/2/
```

See this sequence in `HidPath`: `1 → 1.1 → 2` instead of `1 → 2 → 3`. In time, this ugly sequence may become uglier still.

If you are OK with that — skip this chapter. But for the rest of us, here is one more option for keeping the hierarchical data in a clean state.

The following historical alternative recalculates HierarchyId so sibling ordinals form a continuous sequence. `SaveFolderWithHidReculc` is an illustrative standalone name, not a procedure installed by the current sample project. For 4.1.0, call `dbo.SaveFolder` with `@WithHidReculc = 1`, or the C# sample's `SaveFolder(folder, withHidReorder: true)`.

```sql
create procedure dbo.SaveFolderWithHidReculc
    @Folder dbo.FolderTableType readonly
as
begin
    set nocount on;

    begin -- variable declaration

        declare
            @ParentId        int            ,
            @UserId          int            ,
            @ParentHid       hierarchyid    ,
            @ParentHidStr    varchar(1000)  ,
            @StartTranCount  int            ,
            @OldParentId     int            ,
            @OldParentHid    hierarchyid    ;

        declare @FolderIds table (
            InsertedId    int          not null,
            OldParentId   int              null,
            OldParentHid  hierarchyid      null
        );
    end;

    begin try
        set @StartTranCount = @@trancount;
        if @StartTranCount = 0
        begin
            set transaction isolation level serializable;
            begin transaction;
        end;

        begin -- init variables and lock parent for update
            select @ParentId = ParentId from @Folder;

            select
                @UserId       = UserId,
                @ParentHid    = Hid,
                @ParentHidStr = cast(Hid as varchar(1000))
            from
                dbo.Folders
            where
                Id = @ParentId;
        end;

        begin -- merge calculated hierarchical data with existing folders
            merge into dbo.Folders as target
                using
                (
                    select
                        Hid       =  cast(concat(@ParentHidStr, -1, '/') as varchar(1000)),
                        Id        ,
                        ParentId  ,
                        Name
                    from
                        @Folder
                )
                as source on source.Id = target.Id

            when matched and target.UserId = @UserId then
                update set
                    ParentId = source.ParentId,
                    Name     = source.Name

            when not matched by target and source.Id = 0 then
                insert (UserId, Hid, ParentId, Name)
                values (@UserId, source.Hid, source.ParentId, source.Name)

            output
                inserted.Id,
                deleted.ParentId,
                deleted.Hid.GetAncestor(1)
            into @FolderIds (InsertedId, OldParentId, OldParentHid);
        end;

        begin -- recalculate sub-folder Hids
            select top 1
                @OldParentId  = OldParentId,
                @OldParentHid = OldParentHid
            from @FolderIds;

            exec dbo.RecalcSubFolderHids
                @UserId, @ParentId, @ParentHid, @OldParentId, @OldParentHid;
        end;

        if @StartTranCount = 0 commit transaction;
    end try
    begin catch
        if xact_state() <> 0 and @StartTranCount = 0 rollback transaction;

        declare @ErrorMessage nvarchar(4000) = dbo.GetErrorMessage();
        raiserror(@ErrorMessage, 16, 1);
        return;
    end catch;
end;
```

The trick is in the call to `RecalcSubFolderHids` after the `@Folder` save. That is the current SQL object name; its [source filename](../Database/5.%20Procedures/Folders/ReculcSubFolderHids.sql) retains the older spelling.

```sql
create procedure dbo.RecalcSubFolderHids
    @UserId        int,
    @ParentId      int,
    @ParentHid     hierarchyid,
    @OldParentId   int          = null,
    @OldParentHid  hierarchyid  = null
as
begin
    declare @ParentHidStr    varchar(1000) = cast(@ParentHid    as varchar(1000));
    declare @OldParentHidStr varchar(1000) = cast(@OldParentHid as varchar(1000));

    with Recursion as
    (
        select
            Id        ,
            ParentId  ,
            Name      ,
            OldHid    =  cast(Hid as varchar(1000)),
            NewHid    =  cast(
                            concat(
                                case when ParentId = @ParentId
                                     then @ParentHidStr
                                     else @OldParentHidStr
                                end,
                                row_number() over (partition by ParentId order by Name, Id),
                                '/'
                            )
                            as varchar(1000)
                         )
        from
            dbo.Folders
        where
            ParentId in (@ParentId, @OldParentId)

        union all

        select
            Id        =  f.Id,
            ParentId  =  f.ParentId,
            Name      =  f.Name,
            OldHid    =  cast(f.Hid as varchar(1000)),
            NewHid    =  cast(
                             concat(
                                 r.NewHid,
                                 row_number() over (partition by f.ParentId order by f.Name, f.Id),
                                 '/'
                             )
                             as varchar(1000)
                         )
        from
            Recursion r
            inner join dbo.Folders f on f.ParentId = r.Id
        where
            r.OldHid <> r.NewHid
    )
    update f set
        Hid = r.NewHid
    from
        dbo.Folders f
        inner join Recursion r on r.Id = f.Id and f.Hid <> r.NewHid
    where
        f.UserId = @UserId;
end;
```

The anchor numbers siblings separately for each ParentId, including both the old and new parents after a move. The old path prefix comes from `@OldParentHid`, not the integer `@OldParentId`. These details keep each affected sibling group in its own continuous sequence:

```text
-- Result of SaveFolderWithHidReculc on the previous example
Id    ParentId  Name  Level  HidCode    HidPath
----- --------- ----- ------ ---------- ------------
364   121       5A    5      5AD6B580   /1/1/1/1/1/
1234  121       5Aaa  5      5AD6B680   /1/1/1/1/2/
365   121       5B    5      5AD6B780   /1/1/1/1/3/
```

The original measurements suggested a cost trade-off: recalculating about 10,000 nodes took under one second, while 100,000 nodes could take 15 seconds or more. These are historical observations, not 4.1.0 performance guarantees; measure the relevant schema, hardware, tree depth and workload. The sample also uses bounded path strings and SQL Server's default recursive-CTE depth limit, so validate unusually deep or long paths before relying on it.

## Read a Tree in C#

The implemented Id-ParentId reference allows us to build a tree of Folders in C#. Moreover, once the Folders are sorted hierarchically, the tree can be built in a single pass through the flat list.

Suppose we have a `Folder` class:

```csharp
public class Folder
{
    public int             Id          { get; set; }
    public int?            ParentId    { get; set; }
    public string          Name        { get; set; } = null!;
    public IList<Folder>?  SubFolders  { get; set; }
}
```

The following method builds a tree in one pass through a hierarchically sorted Folder list, using a stack to track the current parent chain:

```csharp
public static IList<Folder> ConvertHierarchicallySortedFolderListToTrees(
    IEnumerable<Folder> folders)
{
    var parentStack = new Stack<Folder>();
    var parent      = default(Folder);
    var prevNode    = default(Folder);
    var rootNodes   = new List<Folder>();

    foreach (var folder in folders)
    {
        if (parent == null || folder.ParentId == null)
        {
            rootNodes.Add(folder);
            parent = folder;
        }
        else if (folder.ParentId == parent.Id)
        {
            parent.SubFolders ??= new List<Folder>();
            parent.SubFolders.Add(folder);
        }
        else if (folder.ParentId == prevNode.Id)
        {
            parentStack.Push(parent);
            parent = prevNode;
            parent.SubFolders ??= new List<Folder>();
            parent.SubFolders.Add(folder);
        }
        else
        {
            var parentFound = false;
            while (parentStack.Count > 0 && parentFound == false)
            {
                parent = parentStack.Pop();
                if (folder.ParentId != null && folder.ParentId.Value == parent.Id)
                {
                    parent.SubFolders!.Add(folder);
                    parentFound = true;
                }
            }
            if (parentFound == false)
            {
                rootNodes.Add(folder);
                parent = folder;
            }
        }

        prevNode = folder;
    }

    return rootNodes;
}
```

In the original measurements, the sorted-list version ran about twice as fast as the unsorted-list version below, which builds a dictionary first. This is not a guaranteed ratio: both approaches are linear for valid input, with different allocation and traversal costs.

```csharp
public static IList<Folder> ConvertHierarchicallyUnsortedFolderListToTrees(
    IEnumerable<Folder> folders)
{
    var dictionary  = folders.ToDictionary(n => n.Id, n => n);
    var rootFolders = new List<Folder>();

    foreach (var folder in dictionary.Select(item => item.Value))
    {
        if (folder.ParentId.HasValue
            && dictionary.TryGetValue(folder.ParentId.Value, out var parent))
        {
            parent.SubFolders ??= new List<Folder>();
            parent.SubFolders.Add(folder);
        }
        else
        {
            rootFolders.Add(folder);
        }
    }

    return rootFolders;
}
```

Both methods can build multiple roots, so the return type is `IList<Folder>`. That is useful when you need to fetch several branches detached from the tree, or several independent trees.

## Where This Lives in Artisan.Orm

The patterns above became part of the Artisan.Orm sample database and library:

- The [Folders table](../Database/1.%20Tables/Users/4.%20Folders.sql), [SaveFolder](../Database/5.%20Procedures/Folders/SaveFolder.sql), [RecalcSubFolderHids](../Database/5.%20Procedures/Folders/ReculcSubFolderHids.sql), [GetFolderWithSubFolders](../Database/5.%20Procedures/Folders/GetFolderWithSubFolders.sql) and [GenerateFolders](../Database/5.%20Procedures/Folders/GenerateFolders.sql) live in the Database project. `SaveFolder` selects its algorithm through `@WithHidReculc`; there is no separate `SaveFolderWithHidReculc` object in this project.
- The C# repository, including `SaveFolder(folder, withHidReorder: true)`, lives in [Tests/DAL/Folders/Repository.cs](../Tests/DAL/Folders/Repository.cs).
- Artisan.Orm's [`INode<T>`](https://github.com/lobodava/artisan-orm/wiki/INode-Interface-and-ToTree-Methods) interface generalizes the tree-building algorithm. With a registered mapper and `INode<Folder>`, use `ReadToTreeList<Folder>(..., hierarchicallySorted: true)` to preserve the forest returned by the two converters. Use the default unsorted path when the query does not guarantee depth-first order. `ReadToTree<Folder>` returns only the first root and is appropriate for an explicitly single-root result. The current [Folder model and mapper](../Tests/DAL/Folders/Models/Folder.cs) supply both contracts.

The integration tests in [`Tests/Tests/FolderTests.cs`](../Tests/Tests/FolderTests.cs) demonstrate the full flow.

The 4.1.0 regressions in [`FolderSqlRegressionTest.cs`](../Tests/Tests/FolderSqlRegressionTest.cs) also check moving a folder between nonempty sibling groups, updating descendant paths, and preserving distinct complete HidCode values for long hierarchyids.

## About the Source Code

The original article shipped with a standalone Hierarchy solution containing two projects: a `Hierarchy.DB` SSDT project and a `Hierarchy.Tests` test project. That code has been folded into the Artisan.Orm repository — see the links in the previous section.

The Database project runs `dbo.GenerateFolders` during its post-deployment script. It truncates `dbo.Folders` before generating sample data, so publish this project to a disposable sample/test database, not a database whose folder data must be retained.

## See Also

- Wiki page: [INode Interface and ToTree Methods](https://github.com/lobodava/artisan-orm/wiki/INode-Interface-and-ToTree-Methods) — how to consume hierarchical data through Artisan.Orm's high-level API.
- Other articles in this series: [`README.md`](README.md).

---

*Originally published as [Combination of Id-ParentId and HierarchyId Approaches to Hierarchical Data](https://www.codeproject.com/Articles/1192607/Combination-of-Id-ParentId-and-HierarchyId) on CodeProject, 17 July 2017. CodeProject went read-only in late 2024; this version preserves the article for posterity. Re-published under [CC-BY-SA-4.0](LICENSE).*
