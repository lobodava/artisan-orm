# Choosing RepositoryBase, SqlCommand, or SqlDataReader

Start with a `RepositoryBase` convenience method when it expresses the operation directly. Ordinary lookups, lists, arrays, dictionaries, trees, multiple lists, streaming, and bulk inserts do not inherently require a hand-written command/reader callback. Use the existing application's registered or automatic mapping choice at any level.

| Level | Choose it when | What it manages |
| --- | --- | --- |
| `repo.ReadTo<T>`, `repo.ReadAsList<T>`, `repo.ReadToLists<T1,T2>`, and related methods | A standard result operation is enough | Creates/disposes the command, attaches the repository transaction, delegates execution and reading |
| `cmd.ReadTo<T>`, `cmd.ReadAsList<T>`, `cmd.GetByReader(...)` | You need command access, a custom projection, output parameters, or explicit execution configuration | Artisan command read methods open a closed connection, execute SQL, and dispose the reader; the caller owns the command |
| `dr.ReadTo<T>`, `dr.ReadToList<T>`, and related methods | You already have a reader or must compose a mixed sequence of result sets | Reads the current result and normally advances to the next one; it does not execute SQL or own the connection/command |

All three levels share the same mapper contracts. A lower level gives access to more execution details; it does not make mapping inherently more correct. `RepositoryBase` can be used directly or inherited by an application repository.

## One lookup at three levels

This complete example assumes `dbo.GetProductById(@Id int)` returns zero or one row with `Id int NOT NULL, Name nvarchar(100) NOT NULL`, in that order. The caller owns `repo` and keeps it alive until the operation finishes. The first method is the normal starting point; the other two demonstrate the equivalent lower levels.

```csharp
using System.Threading;
using System.Threading.Tasks;
using Artisan.Orm;
using Microsoft.Data.SqlClient;

public sealed class LookupProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

[MapperFor(typeof(LookupProduct), RequiredMethod.CreateObject)]
public static class LookupProductMapper
{
    public static LookupProduct CreateObject(SqlDataReader dr) => new LookupProduct
    {
        Id = dr.GetInt32(0),
        Name = dr.GetString(1)
    };
}

public static class LookupExamples
{
    public static LookupProduct? ViaRepository(RepositoryBase repo, int id) =>
        repo.ReadTo<LookupProduct>("dbo.GetProductById",
            cmd => cmd.AddIntParam("@Id", id));

    public static Task<LookupProduct?> ViaRepositoryAsync(
        RepositoryBase repo, int id, CancellationToken cancellationToken) =>
        repo.ReadToAsync<LookupProduct>("dbo.GetProductById",
            cmd => cmd.AddIntParam("@Id", id), cancellationToken);

    public static LookupProduct? ViaCommand(RepositoryBase repo, int id) =>
        repo.GetByCommand(cmd =>
        {
            cmd.UseProcedure("dbo.GetProductById");
            cmd.AddIntParam("@Id", id);
            return cmd.ReadTo<LookupProduct>();
        });

    public static LookupProduct? ViaReader(RepositoryBase repo, int id) =>
        repo.GetByCommand(cmd =>
        {
            cmd.UseProcedure("dbo.GetProductById");
            cmd.AddIntParam("@Id", id);
            return cmd.GetByReader(dr => dr.ReadTo<LookupProduct>());
        });
}

public sealed class LookupRepository : RepositoryBase
{
    public LookupRepository(string connectionString) : base(connectionString) { }

    public LookupRepository(SqlTransaction transaction, string connectionString)
        : base(transaction, connectionString) { }

    public Task<LookupProduct?> FindAsync(int id, CancellationToken cancellationToken) =>
        ReadToAsync<LookupProduct>("dbo.GetProductById",
            cmd => cmd.AddIntParam("@Id", id), cancellationToken);
}
```

These are alternatives, not three calls to make for one lookup. No row returns null for this class. Scalar types instead follow their own `default(T)` and SQL NULL conversion rules; use a nullable scalar when absence must be distinguished from zero.

`RepositoryBase` has no parameterless constructor. A complete derived repository must forward a connection string or the external-transaction constructor as above. Reuse an existing application's constructor when adding a method; a helper accepting an existing `RepositoryBase` instance does not need a new subclass.

Repository convenience overloads accept either `SqlParameter[]` or a synchronous `Action<SqlCommand>` for configuration. Typed `Add*Param` helpers work inside that action. Setting a timeout or adding parameters alone is not a reason to expand a simple lookup into nested callbacks. Async convenience methods execute asynchronously after configuration; do not pass an `async` lambda to their `Action<SqlCommand>` overload.

For single-object reads, the actual call shapes are:

| Call | Arguments |
| --- | --- |
| `repo.ReadTo<T>(sql, configure)` / `repo.ReadAs<T>(sql, configure)` | SQL text or procedure name, then `Action<SqlCommand>` |
| `repo.ReadTo<T>(sql, parameters)` / `repo.ReadAs<T>(sql, parameters)` | SQL text or procedure name, then `params SqlParameter[]` |
| `repo.ReadToAsync<T>(sql, configure, token)` / `repo.ReadAsAsync<T>(sql, configure, token)` | Configuration action before the cancellation token |
| `repo.ReadToAsync<T>(sql, token, parameters)` / `repo.ReadAsAsync<T>(sql, token, parameters)` | Cancellation token before the parameter array |

Single-object repository `ReadTo<T>` / `ReadToAsync<T>` have no inline-factory overload. For a custom factory, use `repo.GetByCommand(cmd => ...)` and `cmd.ReadTo(factory)`, or the token-aware async wrapper and `cmd.ReadToAsync(factory, token)`. See the complete [date/time example](parameters-and-types.md#dateonly-and-timeonly). Do not invent `repo.ReadTo(factory, configure)` or omit the SQL/procedure argument. Other families do expose selected repository factory overloads, including arrays, compact rows, and async trees; inspect the specific signature instead of assuming all `ReadTo*` methods have identical overloads.

## SQL text and procedure names

The convenience methods classify the supplied string using a keyword heuristic: `select`, `insert`, `update`, `delete`, or `merge` indicates SQL text; otherwise it is treated as a procedure name. This is not a SQL parser. For an unusual batch such as `EXEC ...` without those keywords, explicitly call `cmd.UseSql(batch)` in the configuration action, or configure it through `GetByCommand`. Use `cmd.UseProcedure(name)` to force a procedure call. Keep values in parameters rather than interpolating them into SQL.

## Drop to a lower level for a concrete need

- Use `repo.ReadToLists<T1,T2>` through four result types when the result is simply several complete lists. Use `cmd.GetByReader` with `dr.ReadTo*` when combining a single row, lists, and an optional DataReply header in a specific sequence.
- Use `repo.GetByCommand` for a custom inline projection not exposed by a particular repository shortcut, or to read output parameters after a command-level reader has closed. The repository owns and disposes the new command; it initially binds only the connection and transaction. The callback supplies SQL/parameters and executes it. `RunCommand` likewise does not execute a merely configured command.
- Use `repo.Execute` / `ExecuteAsync` for the simple execution shortcut or `ExecuteCommand` / `ExecuteCommandAsync` for configuration callbacks. Both families return the T-SQL `RETURN` value, not affected rows. If affected rows are required, own the open connection scope and use SqlClient `ExecuteNonQuery*`, or return an explicit count from SQL.
- Use `repo.CreateCommand` when the command must outlive a callback, with `using` around the caller-owned command. It attaches the active repository transaction but does not execute the command or open the connection. Raw SqlClient execution requires an open connection; Artisan command read extensions handle opening for their own calls.

Do not return a lazy sequence backed by a reader from `GetByReader` or by a command from `GetByCommand` after their callback ends. Prefer repository streaming shortcuts when they express the stream you need, and keep the repository alive through enumeration. Reader materializers normally advance results; reader async streams do not. See [cancellation and streaming lifetimes](reading-and-mapping.md#cancellation-and-streaming-lifetimes).

`GetByReaderAsync` has a synchronous projection callback. It is not an async reader-processing scope; returning a task from that callback lets the reader be disposed too early. Use an async materializer, stream, or explicitly owned reader instead.

Calling a Task-returning command/repository operation starts it; `await` observes completion rather than starting execution. Keep its resources alive from invocation through completion. This differs from lazy enumerable and async-enumerable APIs, whose work starts during enumeration.

Sources: `Artisan.Orm/RepositoryBase.cs`, `Artisan.Orm/SqlCommandExtensions_Sync.cs`, `Artisan.Orm/SqlCommandExtensions_Async.cs`, `Artisan.Orm/SqlCommandExtensions_Parameters.cs`, and `Artisan.Orm/SqlDataReaderExtensions_Read.cs`.
