# Dynamic rows, dictionaries, and compact rows

Use this reference when the result should be a dynamic record, a key-based lookup, or a compact positional row rather than an entity list. These are materializing operations. For the 4.1.0 contract and mapper selection, read the relevant parts of [reading and mapping](reading-and-mapping.md); the examples below are independent of its `User` declarations.

## Dynamic results

`ReadDynamic`, `ReadDynamicList`, and `ReadDynamicArray` build `ExpandoObject` records. Each result column becomes a member with its exact column name; SQL NULL becomes C# null. No entity class or registered mapper is required. Alias duplicate column names before reading: members are inserted with dictionary `Add`, so duplicate names throw rather than overwrite. Use the dictionary view for names that are not convenient C# member identifiers.

`ReadDynamic` returns null for no row. List and array operations return an empty collection. Command and repository async counterparts accept a cancellation token. The synchronous command/repository `ReadDynamicArray` signature returns `IList<dynamic>` even though its implementation creates an array; the reader method and async counterparts return `dynamic[]`. Use the declared return type or `var`.

This complete example returns a materialized report and shows how to inspect a dynamic record without assuming its member type:

```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Artisan.Orm;

public static class DynamicReportExamples
{
    public static Task<IList<dynamic>> ReadAsync(
        RepositoryBase repository, CancellationToken cancellationToken)
    {
        return repository.ReadDynamicListAsync(
            "SELECT CAST(7 AS int) AS ProductId, " +
            "CAST(N'Widget' AS nvarchar(50)) AS ProductName, " +
            "CAST(NULL AS nvarchar(100)) AS Note;", cancellationToken);
    }

    public static bool HasNullNote(object row)
    {
        var fields = (IDictionary<string, object?>)row;
        return fields.TryGetValue("Note", out var value) && value is null;
    }
}
```

Dynamic access moves name/type checks to runtime; it does not validate a changing report contract. Prefer a typed mapper when callers need compile-time member names.

## Dictionaries

All dictionary variants take the key from **column 0**, converted to `TKey`. There is no key-selector overload. Reorder the SELECT list or use an application projection/grouping when another key policy is required.

| Value choice | Method and interpretation |
| --- | --- |
| Simple scalar | `ReadToDictionary<TKey, TValue>` or `ReadAsDictionary<TKey, TValue>` reads the value from column 1 |
| Registered entity | `ReadToDictionary<TKey, TValue>` uses `TValue`'s `CreateObject` on the entire current row |
| Automatic entity | `ReadAsDictionary<TKey, TValue>` maps matching writable properties from the entire row |
| Explicit projection | `ReadToDictionary<TKey, TValue>(reader => ...)` controls the value, including scalar values |

Entity/factory ordinals are not shifted past the key: column 0 remains available to the value mapper. Rows with a SQL NULL key are skipped. Duplicate keys throw `ArgumentException`; there is no automatic last-wins or grouping policy. Use nullable value types when scalar column 1 can be SQL NULL; a non-nullable value type cannot accept null. A dictionary is a lookup, not a result-order contract.

The following standalone example uses the same `Id int NOT NULL, Name nvarchar(50) NOT NULL` shape for scalar, registered, automatic, and explicit-projection paths:

```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Artisan.Orm;
using Microsoft.Data.SqlClient;

public sealed class DictionaryProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

[MapperFor(typeof(DictionaryProduct), RequiredMethod.CreateObject)]
public static class DictionaryProductMapper
{
    public static DictionaryProduct CreateObject(SqlDataReader reader) => new DictionaryProduct
    {
        Id = reader.GetInt32(0),
        Name = reader.GetString(1)
    };
}

public static class DictionaryExamples
{
    private const string Sql =
        "SELECT CAST(7 AS int) AS Id, CAST(N'Widget' AS nvarchar(50)) AS Name;";

    public static IDictionary<int, string> ReadNames(RepositoryBase repository) =>
        repository.ReadToDictionary<int, string>(Sql);

    public static IDictionary<int, DictionaryProduct> ReadMapped(RepositoryBase repository) =>
        repository.ReadToDictionary<int, DictionaryProduct>(Sql);

    public static Task<IDictionary<int, DictionaryProduct>> ReadAutomaticAsync(
        RepositoryBase repository, CancellationToken cancellationToken) =>
        repository.ReadAsDictionaryAsync<int, DictionaryProduct>(Sql, cancellationToken);

    public static Task<IDictionary<int, string>> ReadProjectedAsync(
        RepositoryBase repository, CancellationToken cancellationToken)
    {
        return repository.GetByCommandAsync((command, token) =>
        {
            command.UseSql(Sql);
            return command.ReadToDictionaryAsync<int, string>(
                reader => reader.GetString(1).ToUpperInvariant(), token);
        }, cancellationToken);
    }
}
```

The last method returns the key 7 with value `WIDGET`; a scalar `TValue` does not override the supplied factory. `ReadToDictionaryAsync` and `ReadAsDictionaryAsync` exist on commands and repositories; do not invent reader-level async dictionary extensions.

## ObjectRow and ObjectRows

`ObjectRow` is a `List<object>` and `ObjectRows` is a list of those rows. They carry values only: no column names, SQL types, or embedded schema. Keep a separate ordered column contract when exposing compact rows to another component. For example, a client needs to know that `["Widget", 7, null]` means Name, Id, Email.

- `ReadToObjectRow<T>` / `ReadToObjectRows<T>` invoke the registered `CreateObjectRow(SqlDataReader)` for `T`. The type selects a mapper; the result is not a `T` instance. Use `RequiredMethod.CreateObjectRow` when that is the mapper's only intended capability.
- Their explicit-factory overloads accept `Func<SqlDataReader, ObjectRow>` without a registration. A hand-written row can reorder, omit, or transform SQL columns; it need not mirror C# properties.
- `ReadAsObjectRow()` / `ReadAsObjectRows()` have no entity type argument. They copy `GetValue(i)` in SELECT order and retain `DBNull.Value` for SQL NULL. They do not use property-name mapping or normalize nulls for JSON.

This raw `DBNull.Value` behavior belongs to `ReadAsObjectRow(s)`, not to all `ReadAs*` methods or every ObjectRow mapper. Automatic entity mapping preserves null strings and nullable value types; a custom compact-row mapper chooses its own representation.

This standalone example deliberately converts `Id, Name, Email` into `Name, Id, Email`. The mapper normalizes SQL NULL for the application's compact-row contract. The null-forgiving operator reflects the library's `List<object>` annotation; a list element can still be null at runtime.

```csharp
using System.Threading;
using System.Threading.Tasks;
using Artisan.Orm;
using Microsoft.Data.SqlClient;

public sealed class CompactProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
}

[MapperFor(typeof(CompactProduct), RequiredMethod.CreateObjectRow)]
public static class CompactProductMapper
{
    public static ObjectRow CreateObjectRow(SqlDataReader reader) => new ObjectRow(3)
    {
        reader.GetString(1),
        reader.GetInt32(0),
        reader.IsDBNull(2) ? null! : reader.GetString(2)
    };
}

public sealed class CompactProductReport
{
    public string[] Columns { get; } = new[] { "Name", "Id", "Email" };
    public ObjectRows Rows { get; set; } = new ObjectRows();
}

public static class CompactProductExamples
{
    public static async Task<CompactProductReport> ReadAsync(
        RepositoryBase repository, CancellationToken cancellationToken)
    {
        var rows = await repository.ReadToObjectRowsAsync<CompactProduct>(
            "SELECT CAST(7 AS int) AS Id, " +
            "CAST(N'Widget' AS nvarchar(50)) AS Name, " +
            "CAST(NULL AS nvarchar(100)) AS Email;", cancellationToken);
        return new CompactProductReport { Rows = rows };
    }
}
```

A single-row operation returns null when empty; an `ObjectRows` operation returns an empty collection. Command/repository async variants include both `ReadToObjectRow(s)Async` and `ReadAsObjectRow(s)Async` with cancellation tokens. The mapper callbacks remain synchronous.

## Result advancement and ownership

For all three families, synchronous reader-level materializers consume the current result and call `NextResult()` by default, including after an empty result. Pass `getNextResult: false` if the caller will advance explicitly; otherwise do not add another `NextResult()`. Start with a reader positioned before its first row, not one already advanced by `Read()`.

Command operations execute and dispose their own reader, returning materialized values. Keep the command and repository alive until an async operation completes. Pass the token to a repository shortcut; when using a callback wrapper, forward it to the command operation too. These results can be used after the reader has closed; they are not lazy reader-dependent sequences. For reading multiple result sets through one owned reader, use the synchronous reader extensions or explicit async reader traversal; `GetByReaderAsync` still takes a synchronous projection callback.

Sources: `Artisan.Orm/SqlDataReaderExtensions_Read.cs`, `Artisan.Orm/SqlDataReaderExtensions_CreateObject.cs`, `Artisan.Orm/SqlCommandExtensions_Sync.cs`, `Artisan.Orm/SqlCommandExtensions_Async.cs`, `Artisan.Orm/ObjectRows.cs`, and `Artisan.Orm/RepositoryBase.cs`. Existing examples/regressions: `Tests/Tests/ReadDictionaryTest.cs`, `Tests/Tests/AsyncReadTest.cs`, and `Tests/Tests/AsyncReadCancellationRegressionTest.cs`.
