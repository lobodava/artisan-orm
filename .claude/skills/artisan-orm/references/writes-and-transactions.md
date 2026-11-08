# Writes, transactions, and reply contracts

Use this reference for write operations, graph saving, and procedures that return a DataReply status. Apply the [version and source compatibility](../SKILL.md#version-and-source-compatibility) guidance before copying an API. Resolve documentation differences against the target version's implementation and tests. Source paths are relative to the ArtisanOrm repository root and apply when that checkout is available.

C# method fragments below belong inside an application class. They use `System`, `System.Collections.Generic`, `System.Data`, `System.Linq`, `System.Threading`, `System.Threading.Tasks`, `Artisan.Orm`, and `Microsoft.Data.SqlClient`.

## Transaction ownership

| API | Normal return | Responsibility inside the callback |
| --- | --- | --- |
| `BeginTransaction` | Uncommitted work rolls back on disposal | Call `transaction.Commit()` to persist |
| `RunInTransaction` | Commits automatically | Do not commit or roll back manually |
| `BeginTransactionAsync` | Uncommitted work rolls back on disposal | Call `await transaction.CommitAsync(ct)` to persist |
| `RunInTransactionAsync` | Commits automatically | Do not commit or roll back manually |

These helpers open a closed connection and close it afterward; a connection already opened by the caller remains open. Commands created through the same repository attach its active transaction. Do not assume a second repository instance shares that transaction or connection. These helpers are not a nested-transaction/savepoint abstraction.

If partial rollback is required, the transaction owner must manage savepoints explicitly through `SqlTransaction.Save(name)` / `Rollback(name)` or T-SQL. A savepoint does not create an independent transaction, and rollback to it requires a committable transaction. See [SqlTransaction](https://learn.microsoft.com/en-us/dotnet/api/microsoft.data.sqlclient.sqltransaction) and [SAVE TRANSACTION](https://learn.microsoft.com/en-us/sql/t-sql/language-elements/save-transaction-transact-sql).

The 4.1.0 constructor `RepositoryBase(SqlTransaction? transaction, string connectionString)` borrows the supplied transaction's existing connection, even if the string names another connection. The caller owns both resources: disposing the repository neither commits nor rolls back the transaction and neither closes nor disposes its connection. A completed/disposed transaction is rejected. Pass the same live transaction explicitly to each repository that must participate; a matching connection string alone does not share it.

Starting `BeginTransaction*` or `RunInTransaction*` while `Transaction` is attached is rejected. Complete and detach it before starting a new one; do not treat these methods as nested transactions. Regressions: `Tests/Tests/ExternalTransactionTest.cs`.

For an existing `dbo.Items(Id int, Name nvarchar(100))` table, automatic commit looks like this:

```csharp
public static Task RenameItemAsync(
    RepositoryBase repo, int id, string name, CancellationToken cancellationToken)
{
    return repo.RunInTransactionAsync(IsolationLevel.ReadCommitted, async (_, token) =>
    {
        await repo.ExecuteCommandAsync(cmd =>
        {
            cmd.UseSql("UPDATE dbo.Items SET Name = @Name WHERE Id = @Id;");
            cmd.AddIntParam("@Id", id);
            cmd.AddNVarcharParam("@Name", 100, name);
        }, token);
    }, cancellationToken);
}
```

`ExecuteCommandAsync` takes a synchronous `Action<SqlCommand>` that configures the command; it executes and awaits the SQL itself. Its result is the T-SQL `RETURN` value, not the number of affected rows. `GetByCommandAsync` and `RunCommandAsync` instead accept asynchronous callbacks; their token-aware overloads pass the supplied token to that callback, which must pass it to the read/execute operation.

If a normal return should discard the work, use the manual-commit form:

```csharp
public static void RenameItemConditionally(RepositoryBase repo, int id, string name, bool persist)
{
    repo.BeginTransaction(transaction =>
    {
        repo.ExecuteCommand(cmd =>
        {
            cmd.UseSql("UPDATE dbo.Items SET Name = @Name WHERE Id = @Id;");
            cmd.AddIntParam("@Id", id);
            cmd.AddNVarcharParam("@Name", 100, name);
        });
        if (persist)
            transaction.Commit();
    });
}
```

The async manual form takes `Func<SqlTransaction, CancellationToken, Task>` and a token, optionally preceded by `IsolationLevel`. Use an `async` callback and `await transaction.CommitAsync(token)`; do not pass an async lambda to the synchronous `Action<SqlTransaction>` overload.

Source: `Artisan.Orm/RepositoryBase.cs`. Background: [Transactions wiki](https://github.com/lobodava/artisan-orm/wiki/Transactions).

### A procedure must respect an outer transaction

Remember whether the procedure started the transaction. Checking only `@@TRANCOUNT > 0` before `COMMIT` would also commit a transaction owned by its caller. This method-body fragment assumes an `@Id int` and `@Name nvarchar(100)` parameter and the `dbo.Items` table above:

```sql
DECLARE @StartTranCount int = @@TRANCOUNT;
BEGIN TRY
    IF @StartTranCount = 0 BEGIN TRANSACTION;

    UPDATE dbo.Items SET Name = @Name WHERE Id = @Id;

    IF @StartTranCount = 0 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @StartTranCount = 0 AND XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
```

An error under an outer transaction is rethrown to its owner, which must decide whether to roll back. This template does not implement partial rollback through savepoints.

## Saving a graph with temporary identities

Negative identities are a strategy for graphs whose new integer-keyed entities use `Id == 0`. They are not required for every mapper or schema. Keep existing nonzero IDs. Assign each new parent its temporary ID and propagate it to its children before converting the child rows to a TVP.

With the `Id == 0` guard, preparing the same object graph again preserves its assigned temporary IDs. Rebuilding the graph or resetting its IDs can allocate new ones; merely retrying TVP construction from the same prepared objects does not.

When existing `CreateDataRow` mappers perform that preparation, create parent TVPs before child TVPs: `AddTableParam` converts and enumerates each collection immediately. The repository examples do this explicitly. Choose one preparation strategy rather than assigning identities in several places.

This small alternative prepares two levels before constructing their TVPs. Its inline table/row delegates avoid needing registered mappers for the illustrative draft types:

```csharp
public sealed class ParentDraft
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<ChildDraft> Children { get; set; } = new List<ChildDraft>();
}

public sealed class ChildDraft
{
    public int Id { get; set; }
    public int ParentId { get; set; }
    public string Name { get; set; } = "";
}

public static void ConfigureDraftSave(SqlCommand cmd, IEnumerable<ParentDraft> roots)
{
    var parents = roots.ToList();
    foreach (var parent in parents)
    {
        if (parent.Id == 0) parent.Id = Int32NegativeIdentity.Next;
        foreach (var child in parent.Children)
        {
            if (child.Id == 0) child.Id = Int32NegativeIdentity.Next;
            child.ParentId = parent.Id;
        }
    }

    var parentRows = parents.ToDataTable(
        () => new DataTable("dbo.ParentDraftTableType")
            .AddColumn<int>("Id", false).AddColumn<string>("Name", false),
        parent => new object[] { parent.Id, parent.Name });
    var childRows = parents.SelectMany(parent => parent.Children).ToDataTable(
        () => new DataTable("dbo.ChildDraftTableType")
            .AddColumn<int>("Id", false).AddColumn<int>("ParentId", false)
            .AddColumn<string>("Name", false),
        child => new object[] { child.Id, child.ParentId, child.Name });

    cmd.UseProcedure("dbo.SaveDraftGraph");
    cmd.AddTableParam("@Parents", parentRows);
    cmd.AddTableParam("@Children", childRows);
}

// Optional alternative for a schema extended with SortOrder; prepare IDs first.
public static DataTable CreateOrderedChildRows(IEnumerable<ParentDraft> parents)
{
    var rows = parents.SelectMany(parent => parent.Children.Select((child, index) => new
    {
        child.Id,
        ParentId = parent.Id,
        child.Name,
        SortOrder = index
    }));
    return rows.ToDataTable(
        () => new DataTable("dbo.OrderedChildDraftTableType")
            .AddColumn<int>("Id", false).AddColumn<int>("ParentId", false)
            .AddColumn<string>("Name", false).AddColumn<int>("SortOrder", false),
        row => new object[] { row.Id, row.ParentId, row.Name, row.SortOrder });
}
```

The matching UDTTs must have the same column order and compatible types: `dbo.ParentDraftTableType(Id int, Name nvarchar(100))` and `dbo.ChildDraftTableType(Id int, ParentId int, Name nvarchar(100))`. Use non-null columns and unique `Id` keys for this example. The stored procedure parameters must be declared `READONLY`. Adding a TVP does not execute the command.

### Translate IDs through a join; never update the TVP

This procedure-body fragment assumes `@Parents dbo.ParentDraftTableType READONLY`, `@Children dbo.ChildDraftTableType READONLY`, and existing tables `dbo.ParentRecords(Id int IDENTITY PRIMARY KEY, Name nvarchar(100))` and `dbo.ChildRecords(Id int IDENTITY PRIMARY KEY, ParentId int, Name nvarchar(100))`, with the appropriate foreign key. Enclose both merges in one transaction using the ownership pattern above.

```sql
DECLARE @ParentIds table (ParamId int PRIMARY KEY, SavedId int NOT NULL);

MERGE dbo.ParentRecords AS target
USING @Parents AS source ON target.Id = source.Id
WHEN MATCHED THEN UPDATE SET Name = source.Name
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Name) VALUES (source.Name)
OUTPUT source.Id, inserted.Id INTO @ParentIds (ParamId, SavedId);

MERGE dbo.ChildRecords AS target
USING (
    SELECT child.Id, ids.SavedId AS ParentId, child.Name
    FROM @Children AS child
    INNER JOIN @ParentIds AS ids ON ids.ParamId = child.ParentId
) AS source ON target.Id = source.Id
WHEN MATCHED THEN UPDATE SET ParentId = source.ParentId, Name = source.Name
WHEN NOT MATCHED BY TARGET THEN
    INSERT (ParentId, Name) VALUES (source.ParentId, source.Name);
```

The join replaces temporary foreign keys without modifying either TVP. This example upserts supplied rows; it does not delete omitted children. For replacement semantics, explicitly scope deletion to the supplied parent IDs. Returning saved entities or ID mappings is also an explicit SQL/C# contract: sending TVPs does not automatically replace temporary IDs in the original objects.

The current sample's `SaveGrandRecords` uses replacement semantics for supplied branches: omitted records/children under supplied parents are deleted; absent top-level grand records remain. It returns three ordered result sets that are assembled into a new saved graph. Use the returned graph for generated positive IDs; the input objects are not rewritten with those final IDs.

### Preserve relationships and application order separately

Each `OUTPUT source.Id, inserted.Id` row pairs keys for the same affected row. Join those pairs by key; never zip input and output by array position or infer IDs from a presumed insertion sequence. Neither a clustered join index nor increasing negative temporary IDs guarantees `MERGE` insertion order. `OUTPUT` row order is also unspecified. These facts do not break the graph algorithm, because key translation is independent of order.

When collection order is business data, assign a per-parent `SortOrder` before building TVPs, persist it through the matching UDTT/table/mapper/save contract, and read with an explicit `ORDER BY ParentId, SortOrder, Id`. The final key breaks ties. This is an application extension, not a column already present in the sample schema.

`CreateDataRow(T)` receives only the entity, not its collection index. The collection owner must enumerate with an index and prepare a projection or sidecar lookup for the row writer. A `SortOrder` property on the entity is not required. The optional `CreateOrderedChildRows` helper above uses an indexed projection and inline delegates; it resets the index for each parent's children and leaves the model unchanged. Its separate `dbo.OrderedChildDraftTableType(Id int, ParentId int, Name nvarchar(100), SortOrder int)` requires matching non-null UDTT columns and corresponding table/save changes before use.

For an insert-only batch, SQL Server documents a narrower guarantee: `INSERT ... SELECT ... ORDER BY` controls how identity values are computed, but does not guarantee physical insertion order. Do not transfer that guarantee to `MERGE`, treat identity as global business chronology, or infer gap-free numbering. Keep an outer `ORDER BY` on reads even when an index supports that order.

See [INSERT ordering](https://learn.microsoft.com/en-us/sql/t-sql/statements/insert-transact-sql#limitations-and-restrictions) and [OUTPUT ordering](https://learn.microsoft.com/en-us/sql/t-sql/queries/output-clause-transact-sql#remarks). The original article's stronger assertion is preserved with a correction in `articles/01-object-graph-saving.md`; do not repeat the historical assertion as current guidance.

### Design a deadlock policy explicitly

Use this sequence when diagnosing deadlocks or planning a retry policy:

1. Obtain the captured deadlock graph and identify the participating statements, resources, and lock/access order. On SQL Server, the built-in `system_health` Extended Events session captures `xml_deadlock_report`; use it or an equivalent capture appropriate to the deployment. For a planning-only task, identify the evidence needed instead of claiming to have inspected a database.
2. Choose changes from that evidence: index join columns for matching performance, keep competing access order consistent, shorten transactions, and use isolation appropriate to the required consistency. These measures reduce risk; neither indexes nor an access-order convention guarantees deadlock freedom.
3. Decide whether the application needs bounded whole-transaction retries and whether external side effects can safely repeat. A retry policy does not replace investigating a recurring deadlock.

If the application requires retries, retry the whole transaction after rollback, with a bounded policy and safe handling of external side effects. Artisan.Orm does not add automatic retries. Preserve the original error number for classification: the sample's message-based `RAISERROR` catch wrappers re-emit number 50000. In a SQL Server 2012+ procedure, the ownership-aware cleanup above followed by `THROW;` preserves the original exception, including deadlock number 1205. Do not implement a retry that assumes every current sample wrapper already preserves 1205.

See [deadlock capture](https://learn.microsoft.com/en-us/sql/relational-databases/sql-server-deadlocks-guide#deadlock-extended-event), [SQL Server deadlock mitigation](https://learn.microsoft.com/en-us/sql/relational-databases/sql-server-deadlocks-guide#minimize-deadlocks), [RAISERROR numbers](https://learn.microsoft.com/en-us/sql/t-sql/language-elements/raiserror-transact-sql#arguments), and [THROW](https://learn.microsoft.com/en-us/sql/t-sql/language-elements/throw-transact-sql#b-use-throw-to-raise-an-exception-again).

In 4.1.0, transaction helpers preserve the original callback/commit failure if cleanup also fails, including after an explicit commit or cancellation. Cleanup does not use the canceled operation token; it disposes unfinished transactions and closes connections opened by the helper. An already committed transaction cannot be undone. This is error-preserving cleanup, not an automatic retry policy.

Source examples: `Tests/DAL/GrandRecords/Models/`, `Tests/DAL/GrandRecords/Repository.cs`, `Database/5. Procedures/GrandRecords/SaveGrandRecords.sql`, and `Artisan.Orm/DataTableExtensions.cs`. Background: [graph-saving wiki](https://github.com/lobodava/artisan-orm/wiki/Negative-identities-and-object-graph-saving), [Microsoft MERGE guidance](https://learn.microsoft.com/en-us/sql/t-sql/statements/merge-transact-sql).

`Int16NegativeIdentity`, `Int32NegativeIdentity`, and `Int64NegativeIdentity` allocate from the type minimum through -1, then permanently throw `InvalidOperationException` on exhaustion. They never wrap to zero/positive IDs or restart. Int16 has only 32,768 values per generator lifetime; choose a larger type for larger workloads. Temporary IDs correlate graph rows and are not persisted business ordering values.

## DataReply is an explicit result-set protocol

DataReply is optional. Use it when SQL-side business validation needs structured statuses and multiple message details; ordinary reads/writes can return data directly, and applications may use their own result or HTTP error contract. It does not replace SQL exceptions or require all repository operations to use an envelope.

Call `RepositoryBase.CheckForDataReplyException(reader)` only for a procedure that emits the status result set. Calling it on an ordinary entity result set would consume actual data as a status.

The explicit-`Ok` fix is implemented in `RepositoryBase.CheckForDataReplyException` in `Artisan.Orm/RepositoryBase.cs`, with regressions in `Tests/Tests/DataReplyTest.cs`. The presence of `Ok` in the enum alone does not prove that an older package handles it correctly. Verify that helper in the matching version; do not claim to inspect a source checkout that is unavailable.

This helper is synchronous; the 4.1.0 contract has no `CheckForDataReplyExceptionAsync`. If status and payload traversal must also be asynchronous and cancellable, explicitly own the reader and implement that protocol using its async methods. Do not put an async callback into `GetByReaderAsync`'s synchronous projection; see [reader lifetime guidance](reading-and-mapping.md#cancellation-and-streaming-lifetimes).

The 4.1.0 contract is:

- First result set: one string status in its first row/column.
- `Ok` (case-insensitive), null, or a blank status: advance once to the first payload result set and continue reading there.
- A non-OK status: read the next result set as messages when present, then throw `DataReplyException`; the status is preserved even when no messages follow.
- Unknown status names throw `InvalidCastException`; they do not become `Fail`.

Supported names are `Ok`, `Fail`, `Missing`, `Validation`, `Concurrency`, `Denial`, and `Error`. Do not assume the enum maps directly to HTTP status codes; that mapping belongs to the application.

Send one defined enum name. In 4.1.0, `ParseStatus` rejects undefined numeric values and comma-separated combinations, even combinations that happen to equal a defined value. Defined numeric values remain accepted for compatibility. A status-only `DataReplyException.Message` includes its status. Extend message `Code` values for application-specific details instead of inventing new packaged enum names.

Message rows require four columns in this order: `Code` (string), `Text` (nullable string), `Id` (nullable `bigint`), `Value` (nullable value). Include typed nulls for unused fields:

```sql
SELECT N'Validation';
SELECT N'INVALID_NAME' AS Code,
       N'Name is required' AS Text,
       CAST(NULL AS bigint) AS Id,
       CAST(NULL AS sql_variant) AS Value;
```

A successful status is followed directly by payload; it does not require an empty message result set:

```sql
SELECT N'Ok';
SELECT CAST(42 AS int) AS Value;
```

For that success contract, a repository method can read the payload without an extra `NextResult()`:

```csharp
public static int ReadStatusAndValue(RepositoryBase repo)
{
    return repo.GetByCommand(cmd =>
    {
        cmd.UseSql("SELECT N'Ok'; SELECT CAST(42 AS int) AS Value;");
        return cmd.GetByReader(reader =>
        {
            RepositoryBase.CheckForDataReplyException(reader);
            return reader.ReadTo<int>();
        });
    });
}
```

Constructors such as `new DataReply<int>(DataReplyStatus.Ok, 42)`, `new DataReply<int>(DataReplyStatus.Missing)`, and `new DataReply(DataReplyStatus.Validation, "INVALID_NAME", "Name is required")` are valid. C# may throw `new DataReplyException(DataReplyStatus.Validation, "INVALID_NAME", "Name is required")`; SQL communicates the status and message rows rather than instantiating a C# exception.

### JSON and HTTP are application contracts

`DataReplyMessage` has public fields. System.Text.Json defaults omit them, and the historical DataMember attributes do not configure that serializer. Map to application response DTOs, or explicitly configure `IncludeFields = true` for the relevant serializer. The latter affects other public fields in that serializer's scope too.

To use the article's JSON shape, configure camel-case property naming, `JsonStringEnumConverter(JsonNamingPolicy.CamelCase)`, and `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`. Standalone System.Text.Json defaults preserve property names; ASP.NET Core's web defaults already use camel case. Inspect the actual endpoint options before diagnosing a naming mismatch. Field inclusion, enum conversion, and null omission still need their own configuration. See [JSON naming defaults](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/customize-properties).

Those options produce lowercase enum strings and omit null fields/properties; they do not omit `DataReply<int>.Data == 0`. Message IDs are JSON numbers. Keep the top-level reply shape consistent across controller and client, without inventing an extra `dataReply` wrapper.

Choose HTTP status mappings at the application boundary and have the client inspect both HTTP status and the structured body. A fetch promise does not reject solely for HTTP 4xx/5xx. Messages may be absent even for failure; guard their enumeration. Log unexpected exceptions server-side, send a safe client message, and let cancellation propagate. The test-only DataServiceBase now propagates cancellation and bounds nullable stack traces safely, but still includes diagnostic details for tests. Do not copy that disclosure policy into a public application.

Current serializer, service, controller, and client examples are in `articles/03-data-reply.md`. They are application integration examples, not new serialization or retry behavior built into the library.

Sources: `Artisan.Orm/DataReply.cs`, `Artisan.Orm/DataReplyState.cs`, `Artisan.Orm/DataReplyMessage.cs`, `Artisan.Orm/RepositoryBase.cs`, and `Tests/Tests/DataReplyTest.cs`. Background: [DataReply wiki](https://github.com/lobodava/artisan-orm/wiki/Artisan-Way-of-Data-Reply).
