# Bulk copy

Use bulk copy for inserting many rows into an existing SQL Server table. Apply the [version guidance](../SKILL.md#version-and-source-compatibility) and inspect the destination schema first. These APIs do not invoke a stored procedure, create a table or UDTT, perform an upsert, or return generated identities.

## Choose the mapping path

| API on `SqlConnection` or `RepositoryBase` | Source conversion |
| --- | --- |
| `BulkCopy` / `BulkCopyAsync` | Registered `CreateDataTable` and `CreateDataRow`; `RequiredMethod.BothForDataTable` is a suitable write-only declaration |
| `BulkCopyAs` / `BulkCopyAsAsync` | Automatic DataTable mapping from public readable simple properties |

`BulkCopyAs` has no `columnNames` argument. Project to an import DTO or anonymous type containing exactly the destination columns, or use a registered mapper for a custom shape. A hand-written mapper may rename, compute, omit, or flatten model values.

All four methods add source-to-destination column mappings **by name**. The DataTable column order need not match the physical SQL table order; each `CreateDataRow` array must still match its own DataTable order. This differs from a TVP's ordinal contract. Every mapped name must exist in the destination with a compatible type and nullability.

The `destinationTable` argument selects the actual table. A mapper's `DataTable.TableName` is not a required SQL type name here. Do not install a UDTT solely to use bulk copy, or substitute a UDTT name for the destination table.

## Complete mapped and automatic examples

Assume `dbo.ImportProducts` already exists with `Name nvarchar(100) NOT NULL`, `IsActive bit NOT NULL`, and `Id int IDENTITY NOT NULL`, in that physical order. This import explicitly preserves source IDs; the application has checked their uniqueness and the destination's constraints. The model uses different property names to demonstrate deliberate mapping.

```csharp
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Artisan.Orm;
using Microsoft.Data.SqlClient;

public sealed class BulkImportProduct
{
    public int SourceId { get; set; }
    public string Label { get; set; } = "";
    public bool Enabled { get; set; }
}

[MapperFor(typeof(BulkImportProduct), RequiredMethod.BothForDataTable)]
public static class BulkImportProductMapper
{
    public static DataTable CreateDataTable() => new DataTable()
        .AddColumn<int>("Id", isNullable: false)
        .AddColumn<string>("Name", isNullable: false)
        .AddColumn<bool>("IsActive", isNullable: false);

    public static object[] CreateDataRow(BulkImportProduct row) =>
        new object[] { row.SourceId, row.Label, row.Enabled };
}

public static class BulkImportExamples
{
    public static int CopyMapped(
        SqlConnection connection, IEnumerable<BulkImportProduct> rows,
        SqlTransaction? transaction = null)
    {
        return connection.BulkCopy(rows, "dbo.ImportProducts",
            transaction: transaction,
            options: SqlBulkCopyOptions.KeepIdentity |
                SqlBulkCopyOptions.CheckConstraints,
            batchSize: 1000, timeout: 60);
    }

    public static Task<int> CopyAutomaticAsync(
        RepositoryBase repo, IEnumerable<BulkImportProduct> rows,
        CancellationToken cancellationToken)
    {
        var projection = rows.Select(row => new
        {
            Id = row.SourceId,
            Name = row.Label,
            IsActive = row.Enabled
        });
        return repo.BulkCopyAsAsync(projection, "dbo.ImportProducts",
            cancellationToken: cancellationToken,
            options: SqlBulkCopyOptions.KeepIdentity |
                SqlBulkCopyOptions.CheckConstraints,
            batchSize: 1000, timeout: 60);
    }

    public static async Task<int> CopyAtomicallyAsync(
        RepositoryBase repo, IEnumerable<BulkImportProduct> rows,
        CancellationToken cancellationToken)
    {
        var count = 0;
        await repo.RunInTransactionAsync(IsolationLevel.ReadCommitted,
            async (_, token) =>
            {
                count = await repo.BulkCopyAsync(rows, "dbo.ImportProducts",
                    cancellationToken: token,
                    options: SqlBulkCopyOptions.KeepIdentity |
                        SqlBulkCopyOptions.CheckConstraints,
                    batchSize: 1000, timeout: 60);
            }, cancellationToken);
        return count;
    }
}
```

The first two helpers participate in a supplied/attached transaction but do not start or commit one. The third starts a repository transaction and commits on normal completion; call it only when the repository has no transaction already attached. For an existing caller-owned transaction, attach it to the repository or pass it to the connection extension instead. See [transaction ownership](writes-and-transactions.md#transaction-ownership).

The synchronous automatic equivalent is `repo.BulkCopyAs(projection, destinationTable, ...)`. For asynchronous mapped copying on a raw connection, use `connection.BulkCopyAsync(rows, destinationTable, cancellationToken: token, transaction: transaction, ...)`; the full parameter names match the examples above. Repository wrappers forward their `Connection` and current `Transaction` automatically.

## Options and commit boundaries

- `KeepIdentity` preserves supplied identity values. Without it, the destination assigns identity values. The return value is a row count, not the generated IDs, and input objects are not updated with those IDs.
- Choose `CheckConstraints`, `FireTriggers`, and `KeepNulls` according to the import contract. Default bulk-copy options do not reproduce every ordinary INSERT behavior; for example, preserving nulls rather than applying destination defaults requires `KeepNulls`.
- `TableLock` requests a bulk update lock; it does not promise ordering or freedom from deadlocks.
- `UseInternalTransaction` makes each batch a separate transaction. Do not combine it with a supplied transaction, including one forwarded by a repository. Use one external transaction for all-or-nothing behavior across the entire import; `batchSize` alone does not provide that guarantee.

These options are passed to SqlClient unchanged. Consult [SqlBulkCopyOptions](https://learn.microsoft.com/en-us/dotnet/api/microsoft.data.sqlclient.sqlbulkcopyoptions) when selecting nondefault behavior; do not add every flag indiscriminately.

The connection extension closes a connection that it opened for the copy, and leaves an already open connection open. It does not dispose the caller's connection or commit/roll back the supplied transaction. The transaction must belong to that same live connection. Repository disposal follows the repository ownership rules, separately from the bulk-copy call.

## Memory, batching, and cancellation

The wrappers first enumerate the entire input into a DataTable synchronously. They then open the connection if needed and call `WriteToServer` or `WriteToServerAsync`. This is not a streaming import API, even when the source is lazy or the method name ends in `Async`.

`batchSize` is passed to SqlClient's `BatchSize` and defaults to zero, which sends the copy as one batch; it does not cap the in-memory DataTable. `timeout` is passed to `BulkCopyTimeout` in seconds and defaults to 30; it is not a timeout on the earlier enumeration or a repository command timeout. See [SqlClient batching](https://learn.microsoft.com/en-us/dotnet/api/microsoft.data.sqlclient.sqlbulkcopy.batchsize).

Async methods pass the token to connection opening and the actual bulk write. They do not poll it while enumerating input or running mapper code. If the preparation itself must be cancellable, make the input enumeration or mapper cooperate with cancellation. For bounded-memory streaming, explicitly own a SqlClient reader-based bulk-copy implementation rather than claiming these wrappers stream.

On successful completion the method returns `dataTable.Rows.Count`, after any filtering performed by DataTable conversion. Failure or cancellation throws; it does not return a partial count. Whether earlier writes remain depends on the transaction policy. A successful copy inside a transaction is not yet a committed import.

Sources: `Artisan.Orm/SqlConnectionExtensions_BulkCopy.cs`, the bulk-copy wrappers in `Artisan.Orm/RepositoryBase.cs`, `Artisan.Orm/DataTableExtensions.cs`, and `Tests/Tests/BulkCopyTest.cs`.
