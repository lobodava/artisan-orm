# Reading and mapping

Examples use `Microsoft.Data.SqlClient` and `Artisan.Orm`. Apply the [version and source compatibility](../SKILL.md#version-and-source-compatibility) guidance before using newer APIs or assuming the analyzer is packaged. Source paths are relative to the ArtisanOrm repository root and apply when that checkout is available.

## Choose the operation and mapper

- `ReadTo<T>` / `ReadToList<T>` use a registered `CreateObject` for complex entities. Scalar paths such as `ReadTo<int>()` read column 0 without a mapper. An overload taking `Func<SqlDataReader, T>` can supply the mapping inline.
- `ReadAs<T>` / `ReadAsList<T>` map public writable simple properties by exact, case-sensitive column name and cache the generated mapping by result shape. They are valid choices when that contract fits; they are not restricted to prototypes. Alias SQL columns deliberately.
- `ReadToObjectRow<T>` / `ReadToObjectRows<T>` require `CreateObjectRow`, not `CreateObject`. `ReadAsObjectRow()` / `ReadAsObjectRows()` take no entity type argument and copy result columns into compact rows.
- `GetByCommand` provides a configured command to a delegate. `RunCommand` does the same without a return value: merely configuring SQL inside it does not execute anything. The delegate must invoke a read/execution operation. `ExecuteCommand` executes the command and returns the T-SQL `RETURN` value, not the affected-row count.

In 4.1.0, synchronous scalar enumeration includes its first row, including one-row and nullable results. A supplied factory takes precedence over scalar conversion in list, array, enumerable, and multi-result paths. It receives each row even when column 0 is SQL NULL; the factory decides how to read other columns or substitute nulls. Do not work around an earlier first-row bug by pre-reading the reader, or assume null in column 0 bypasses a custom projection. Regressions: `Tests/Tests/ReadProjectionRegressionTest.cs`.

Keep at most one mapper registration per entity type. Conventional mapper methods are public, static, non-generic methods; use a static mapper class for clarity. Do not assume C# property declaration order controls an explicit mapper: reader ordinals must match the SELECT list.

Automatic reader caches use actual runtime `Type` identity plus ordered column names and field types, rather than SQL text or `Type.FullName` alone. Automatic DataTable caches distinguish runtime type, table name, and ordered selected columns. Reusing a cache does not verify the live SQL schema or impose automatic-mapping rules on a hand-written mapper. Regressions: `Tests/Tests/MappingCacheTest.cs`.

Automatic mapping skips indexers. Reader mapping requires public setters; automatic DataTable mapping requires public getters. Numeric enum properties, including nullable enums, participate consistently using their underlying numeric types. SQL NULL is preserved for nullable value types and strings, independently of nullable-reference annotations. These rules do not impose property-to-column matching on manual mappers.

| Requirement | Methods required |
| --- | --- |
| `CreateObject` | `CreateObject` |
| `CreateObjectRow` | `CreateObjectRow` |
| `BothForObject` | Both object-reading methods |
| `BothForDataTable` | `CreateDataTable`, `CreateDataRow` |
| `AllMain` | `CreateObject`, `CreateDataTable`, `CreateDataRow` |
| `All` | All four methods |

`RequiredMethod` declares the mapper's intended capabilities. The analyzer checks this contract even without callers. Combine values as comma-separated attribute arguments: this enum is not a flags enum, so do not use bitwise `|`.

Requirements do not disable optional methods. Omitting the second attribute argument removes declared method-presence requirements; it does not disable signature validation or supply functions needed by actual operations. In 4.1.0, `AllMain` enforces all three documented methods; choose `CreateObject` when the mapper is intended only for entity reads.

A read-only registration cannot satisfy a TVP operation by itself. Runtime-only calls without the required `CreateDataTable` / `CreateDataRow` fail with `ArtisanMappingException`; do not generate placeholder write methods or silently fall back to automatic mapping.

The 4.1.0 analyzer reports missing registrations (`ART001`), missing required methods (`ART002`), incompatible signatures (`ART003`), duplicate registrations (`ART004`), ambiguous overloads (`ART005`), unavailable operation-specific functions (`ART006`), and invalid registrations (`ART007`). Resolve the contract, rather than suppressing errors to make a build pass.

Analyzer success does not validate SQL column order, types, or nullability. See `docs/analyzers.md` in a source checkout or [compile-time mapper validation](https://github.com/lobodava/artisan-orm/wiki/Compile-time-Mapper-Validation) for contract examples and intentional runtime-only mapper scenarios.

## Minimal shared model and mapper

The examples in this file and the `User` TVP examples in [parameters and types](parameters-and-types.md#tvp-overloads-are-not-interchangeable) share these complete declarations. Their illustrative SELECT shape is `Id int, Name nvarchar(50), Email nvarchar(100) NULL`; their TVP is `dbo.UserTableType` with the same columns and order. Use the actual schema in an application.

```csharp
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Artisan.Orm;
using Microsoft.Data.SqlClient;

public sealed class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
}

[MapperFor(typeof(User), RequiredMethod.AllMain)]
public static class UserMapper
{
    public static User CreateObject(SqlDataReader reader) => new User
    {
        Id = reader.GetInt32(0),
        Name = reader.GetString(1),
        Email = reader.GetStringNullable(2)
    };

    public static DataTable CreateDataTable() => new DataTable("dbo.UserTableType")
        .AddColumn<int>("Id", isNullable: false)
        .AddColumn<string>("Name", isNullable: false)
        .AddColumn<string>("Email", isNullable: true);

    public static object[] CreateDataRow(User user) =>
        new object[] { user.Id, user.Name, (object?)user.Email ?? DBNull.Value };
}
```

Negative IDs are a graph-saving convention, not an obligation for every mapper; see `writes-and-transactions.md` when new related entities need temporary keys.

## Multiple result sets

Materializing reader operations such as `reader.ReadTo<T>()`, `ReadToList<T>()`, and `ReadToArray<T>()` advance to the next result set by default, including after an empty set. Do not add an extra `NextResult()` after them. Pass `getNextResult: false` when deliberately retaining the current set.

This example assumes `dbo.GetUserWithRoles` returns a user row with the shape above, followed by a result set whose first column is `RoleId tinyint`. Read the declared result sets even if the first is empty; avoid dereferencing an absent user.

```csharp
public static class UserReadExamples
{
    public static (User? User, IList<byte> RoleIds) LoadWithRoles(RepositoryBase repo, int id)
    {
        return repo.GetByCommand(cmd =>
        {
            cmd.UseProcedure("dbo.GetUserWithRoles");
            cmd.AddIntParam("@Id", id);
            return cmd.GetByReader(reader =>
            {
                var user = reader.ReadTo<User>();
                var roleIds = reader.ReadToList<byte>();
                return (user, roleIds);
            });
        });
    }

    public static Task<User?> LoadAsync(RepositoryBase repo, int id, CancellationToken cancellationToken)
    {
        return repo.GetByCommandAsync((cmd, token) =>
        {
            cmd.UseSql("SELECT Id, Name, Email FROM dbo.Users WHERE Id = @Id;");
            cmd.AddIntParam("@Id", id);
            return cmd.ReadToAsync<User>(token);
        }, cancellationToken);
    }

    public static async Task<int> CountStreamAsync(RepositoryBase repo, CancellationToken cancellationToken)
    {
        var count = 0;
        await foreach (var user in repo.ReadToAsyncEnumerable<User>(
            "SELECT Id, Name, Email FROM dbo.Users;", cancellationToken))
        {
            count++;
        }
        return count;
    }
}
```

For several full lists, 4.1.0 provides `ReadToLists<T1, T2>` through four result types, with async and inline-mapper overloads.

## Cancellation and streaming lifetimes

Pass the token both to `GetByCommandAsync` / `RunCommandAsync` and to the command operation inside the delegate. These wrappers forward the token; they do not automatically cancel arbitrary callback work. `ExecuteCommandAsync` takes an `Action<SqlCommand>` for configuration, so do not pass an `async` lambda that becomes `async void`.

In 4.1.0, async command materializers and streams forward the token to connection opening, command execution, and asynchronous row reads; multi-result materializers also use cancellable result advancement. Cancellation is cooperative: synchronous mapper callbacks and application code must perform their own checks when needed. Earlier packages may need explicit connection/reader ownership to provide that coverage.

The streaming APIs are `ReadToAsyncEnumerable` / `ReadAsAsyncEnumerable`. Keep the repository alive through `await foreach`. Command-level streams also need the command alive; reader-level streams leave ownership with the caller. Reader-level async streams do **not** call `NextResult`: advance explicitly after enumeration when another result set is needed. Materializing async methods and streaming methods are different contracts.

`GetByReaderAsync` currently accepts a synchronous `Func<SqlDataReader, T>` projection. Do not pass an async callback that returns a task: the reader is disposed when that callback returns, before its asynchronous work finishes. Use an appropriate materializing async method or explicitly own the reader when asynchronous processing is needed.

The 4.1.0 repository `ReadToEnumerable` / `ReadAsEnumerable` shortcuts keep their command alive until enumeration finishes or the enumerator is disposed. Keep the repository alive too, and dispose a started enumerator promptly; `foreach` does so even after `break`. For caller-created command/reader sequences, retain those resources for the entire enumeration. Do not return a lazy reader-dependent result from `GetByCommand` or `GetByReader` after their callback has ended.

Do not treat arbitrary exceptions as proof of cancellation. An in-flight SQL cancellation may surface as `SqlException` rather than `OperationCanceledException`; the existing cancellation tests in `Tests/Tests/CommandReadTest.cs` distinguish the provider's cancellation signature from unrelated errors and fail on normal completion.

## MergeJoin and trees

`MergeJoin` walks forward and advances a detail cursor only on a match. It is not a general-purpose join that skips orphan details. Both streams must follow the same parent traversal order, details for one parent must be contiguous, and every detail must belong to a supplied master. Filter details together with masters; otherwise one unmatched detail can prevent later matches.

In 4.1.0, `MergeJoin` disposes acquired detail enumerators on completion and exceptional exit, including when acquiring a later enumerator fails. This resource guarantee does not remove its ordering and matching preconditions.

For three levels, sub-details must follow the order in which the detail stream is visited. Sorting sub-details globally by `DetailId` is insufficient if details are grouped by master and their IDs interleave across masters. Order SQL by the full parent/detail path, or use a lookup/grouping approach when the preconditions are inconvenient. The nested overload links detail to sub-detail; the sibling overload links both independent child lists to the master. Named arguments or explicitly typed delegates can disambiguate similar calls.

`ReadToTree<T>` / `ReadToTreeList<T>` use `INode<T>` (`int Id`, `int? ParentId`, `IList<T>? Children`). Custom `ToTreeList<TNode,TId>` selectors support a struct key type and nullable parent key. `hierarchicallySorted: true` means depth-first parent-before-descendants order, not merely sorting by ID; retain the default unless the query guarantees that order. `ReadToTree` returns only the first root; use `ReadToTreeList` for a forest.

### Current folder sample SQL

The sample combines both save algorithms in `dbo.SaveFolder` through `@WithHidReculc`; C# calls `SaveFolder(folder, withHidReorder: true)` for contiguous sibling paths. The SQL object is `dbo.RecalcSubFolderHids`; its source file retains the spelling `ReculcSubFolderHids.sql`. There is no separately installed `SaveFolderWithHidReculc` procedure.

The 4.1.0 corrections use `@OldParentHid` as the old parent's path prefix and `ROW_NUMBER() OVER (PARTITION BY ParentId ORDER BY Name, Id)` in the recalculation anchor. Both old and new sibling groups, and their descendants, need coverage when testing a move. `dbo.GetHidCode` returns up to 1784 hexadecimal characters for a hierarchyid's maximum 892 bytes. Keep that width in consumers; it is separate from the historical `HidPath` string buffers and recursive-depth limits.

Use explicit `ORDER BY Hid` within a user, or `ORDER BY UserId, Hid` across users, for hierarchy order. A clustered `(UserId, Hid)` key supports access but does not define a result-set order. Validate parent/user/cycle preconditions in the application; the sample procedure does not implement every possible tree validation. An external transaction must supply appropriate isolation. Do not infer those guarantees from the three SQL fixes.

Sources: `Database/5. Procedures/Folders/SaveFolder.sql`, `Database/5. Procedures/Folders/ReculcSubFolderHids.sql`, `Database/3. Functions/GetHidCode.sql`, `Tests/Tests/FolderSqlRegressionTest.cs`, and `articles/04-hierarchyid-combination.md`.

Sources: `Artisan.Orm/MapperForAttribute.cs`, `Artisan.Orm/MappingManager.cs`, `Artisan.Orm/SqlDataReaderExtensions_Read.cs`, `Artisan.Orm/SqlCommandExtensions_Async.cs`, `Artisan.Orm/SqlCommandExtensions_ReadToLists.cs`, `Artisan.Orm/MergeJoinExtensions.cs`, `Artisan.Orm/TreeExtensions.cs`, and `Artisan.Orm/RepositoryBase.cs`.
Wiki: [Mappers](https://github.com/lobodava/artisan-orm/wiki/Mappers), [Read methods](https://github.com/lobodava/artisan-orm/wiki/Read-Methods-Understanding), [MergeJoin](https://github.com/lobodava/artisan-orm/wiki/MergeJoin), [Streaming](https://github.com/lobodava/artisan-orm/wiki/Streaming-Async-Enumerable).
