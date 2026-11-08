# Parameters and SQL types

Apply the [version and source compatibility](../SKILL.md#version-and-source-compatibility) guidance before using version-sensitive behavior. Source paths are relative to the ArtisanOrm repository root and apply when that checkout is available.

C# examples use `System`, `System.Collections.Generic`, `Artisan.Orm`, and `Microsoft.Data.SqlClient`. Before using the `User` TVP examples, read the [minimal shared model and mapper](reading-and-mapping.md#minimal-shared-model-and-mapper) for the complete `User` and `UserMapper` declarations. The conversion and string-parameter examples do not depend on those declarations.

## Match the SQL contract

Use `Microsoft.Data.SqlClient` types with Artisan extension methods. Choose the declared SQL type, size, precision, scale, and null semantics deliberately. `AddIntParam`, `AddTinyIntParam`, `AddNVarcharParam`, and related helpers express that contract without inferred parameter types.

| SQL column | CLR value | Typed reader method |
| --- | --- | --- |
| `bit` | `bool` | `GetBoolean` |
| `tinyint` / `smallint` / `int` / `bigint` | `byte` / `short` / `int` / `long` | `GetByte` / `GetInt16` / `GetInt32` / `GetInt64` |
| `real` / `float` | `float` / `double` | `GetFloat` / `GetDouble` |
| `decimal`, `numeric`, money types | `decimal` within CLR range | `GetDecimal` |
| `date`, `datetime`, `datetime2` | `DateTime` | `GetDateTime` |
| `datetimeoffset` / `time` | `DateTimeOffset` / `TimeSpan` | `GetDateTimeOffset` / `GetTimeSpan` |
| `uniqueidentifier` | `Guid` | `GetGuid` |
| character types | `string` | `GetString` |
| `rowversion` | eight bytes | Artisan `GetBytesFromRowVersion` |

Typed getters do not promote values: `GetInt32` on `tinyint`, `GetBoolean` on `int`, or `GetString` on `uniqueidentifier` is a mismatch. Artisan scalar conversion helpers and auto-mapping have separate conversion behavior; do not generalize their conversions to typed getters.

For nullable columns, use `IsDBNull` or the corresponding Artisan nullable getter, such as `GetInt32Nullable`, `GetStringNullable`, or `GetTimeSpanNullable`. Preserve null unless the business contract explicitly defines a replacement. A blanket `?? 0` or `?? false` can conceal bad data or a wrong SELECT shape. High-precision SQL numerics can exceed CLR `decimal`; check values/range even when using `GetBigDecimal`.

`GetGuidFromStringNullable` parses a textual GUID into `Guid?`. To represent a native `uniqueidentifier` as text, use `reader.GetGuidNullable(ordinal)?.ToString()`. The direction is not interchangeable. `GetUtcDateTime` marks a value as UTC; use it only when the stored value already represents UTC, not as a timezone conversion.

These complete helper methods make the conversions explicit:

```csharp
public static class ReaderTypeExamples
{
    public static string? ReadNativeGuidAsText(SqlDataReader reader, int ordinal) =>
        reader.GetGuidNullable(ordinal)?.ToString();

    public static Guid? ReadTextAsGuid(SqlDataReader reader, int ordinal) =>
        reader.GetGuidFromStringNullable(ordinal);

    public static bool ReadIntegerFlag(SqlDataReader reader, int ordinal) =>
        reader.GetInt32(ordinal) != 0;
}
```

`ReadIntegerFlag` is appropriate only when the column is actually `int` and the application defines nonzero as true. If a shared mapper sees incompatible projections, prefer consistent SQL casts/aliases or separate mapping functions over accidental conversions.

## Typed SqlParam factories

The 4.1.0 `SqlParam` factories create unattached SqlClient parameters for repository shortcuts or command collections. Remove `Add` and the trailing `Param` from a typed command helper's name: `AddIntParam` becomes `SqlParam.Int`, `AddNVarcharParam` becomes `SqlParam.NVarchar`, and `AddIntOutputParam` becomes `SqlParam.IntOutput`. Types, sizes, nullable overloads, conversions, validation, `trimToNull`, `truncate`, and `truncateFraction` follow the corresponding helper.

Start a simple query with the repository shortcut. This independent example assumes `dbo.Products(Id int NOT NULL)` and needs no mapper because it reads scalar IDs:

```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Artisan.Orm;

public static class ProductIdQueries
{
    public static Task<IList<int>> ReadAsync(
        RepositoryBase repository, int minimumId, CancellationToken cancellationToken) =>
        repository.ReadToListAsync<int>(
            "SELECT Id FROM dbo.Products WHERE Id >= @MinimumId ORDER BY Id;",
            cancellationToken, SqlParam.Int("@MinimumId", minimumId));
}
```

Use fresh factory results for each command; a mutable `SqlParameter` cannot be shared between command collections or concurrent calls. Factories do not execute SQL or inspect database metadata. There is no factory counterpart for the metadata-discovering `cmd.AddParams(dictionary)` or `RepositoryBase.AddParams(cmd, object)` batch operation.

`SqlParam.Table` mirrors every `AddTableParam` overload in the table below; `TableRow` mirrors `AddTableRowParam`. A table-name override alone still uses the registered mapper. Automatic mapping requires the explicit column list. Mapped factories and method groups produce `ART001` for a missing registration or `ART006` for missing table functions; `DataTable CreateDataTable()` and `object[] CreateDataRow(T value)` satisfy that capability. Non-generic integer-ID overloads and explicit-column automatic overloads need no registration; an explicit generic integer call still does.

A null collection or DataTable passed to `Table` returns null and is omitted by repository parameter arrays. An empty collection supplies a real empty table. This rule now includes the `short` ID overload on both APIs. A null reference passed to mapper-based `TableRow<T>` instead creates an empty mapped table, because row conversion skips null entities; it still needs a valid mapper. Table conversion happens at factory invocation, not during command execution. Scalar nulls produce a parameter with `Value = DBNull.Value` rather than an omitted parameter.

Raw `cmd.Parameters.Add` / `AddRange` does not skip null factory results. Guard them explicitly. This helper depends on the shared `User` and `UserMapper` declarations linked above:

```csharp
public static class OptionalUserParameters
{
    public static void AddUsers(SqlCommand cmd, IEnumerable<User>? users)
    {
        var parameter = SqlParam.Table("@Users", users);
        if (parameter != null)
            cmd.Parameters.Add(parameter);
    }
}
```

Output factories preserve `Direction.Output`; retain the object and read its value after execution and any reader disposal, handling `DBNull.Value`. They do not create input/output parameters. `SqlParam.ReturnValue()` creates a fresh integer return parameter; see the return-helper rules below before combining it with `Execute*`.

`SqlParam.Json<T>` and the `DateOnly` / `TimeOnly` overloads of `Date` / `Time` exist in the `net8.0` and `net10.0` assets, not `netstandard2.1`. Core factories are present in all three assets. JSON keeps the `AddJsonParam` default-serializer, `nvarchar(max)`, and SQL-null contract; use `NVarcharMax` with explicitly serialized text for custom settings. Typed date/time helpers do not change automatic-mapping support.

## TVP overloads are not interchangeable

| Call shape | Mapping behavior |
| --- | --- |
| `AddTableParam(name, DataTable)` | Uses `DataTable.TableName` as the SQL type name |
| `AddTableParam(name, IEnumerable<User>)` | Registered `CreateDataTable` + `CreateDataRow` |
| `AddTableParam(name, users, tableName)` | The same registered mapper; overrides its table name |
| `AddTableParam(name, users, tableName, columnNames)` | Auto-mapping of named readable properties, in the requested order |
| `AddTableRowParam(name, user)` | One-row TVP using the mapper |
| `AddTableParam(name, IEnumerable<int>)` | Built-in single-column `IntIdTableType`; analogous byte/short/long overloads exist |

The three-argument list overload is **not** an automatic all-properties mapping. For automatic mapping, supply the explicit column list or pass `users.AsDataTable(...)` to the DataTable overload. Explicit columns are preferable for a fixed UDTT contract; unknown property names are skipped by current auto-mapping, so verify the resulting schema.

SQL TVP parameters are `READONLY`; their UDTT must already exist. Match the schema-qualified type name, column order, CLR/SQL types, and allowed nulls. `DataTable.AddColumn<T>` defaults to `isNullable: true`, so specify `false` when appropriate. The built-in integer-ID helpers use their canonical type names; use a DataTable with an explicit name for a different schema/type.

This is a SQL contract, not a requirement that a manually mapped DataTable mirror the C# model. A mapper can flatten, transform, omit, or add values deliberately. For the current sample's `dbo.SaveUser`, pass both the User TVP and the separate `@RoleIds` TVP; that procedure replaces role assignments, and omitting a TVP supplies an empty table. The three-column User example here is illustrative, not that sample's six-column User type.

The illustrative `User` TVP is:

```sql
CREATE TYPE dbo.UserTableType AS TABLE
(
    Id int NOT NULL,
    Name nvarchar(50) NOT NULL,
    Email nvarchar(100) NULL
);
```

The following methods only configure the supplied command. Its caller selects the real procedure and executes it. The string-ID example assumes a matching `dbo.StringIdTableType` containing one string column `Id`; it does not require a mapper for `string`.

```csharp
public static class TableParameterExamples
{
    public static void AddMappedUsers(SqlCommand cmd, IEnumerable<User> users) =>
        cmd.AddTableParam("@Users", users);

    public static void AddProjectedUsers(SqlCommand cmd, IEnumerable<User> users) =>
        cmd.AddTableParam("@Users", users, "dbo.UserTableType", "Id,Name,Email");

    public static void AddCustomIntegerIds(SqlCommand cmd, IEnumerable<int> ids) =>
        cmd.AddTableParam("@Ids", ids.ToIntIdDataTable("dbo.IntIdTableType", "Id"));

    public static void AddStringIds(SqlCommand cmd, IEnumerable<string> ids) =>
        cmd.AddTableParam("@Ids", ids, "dbo.StringIdTableType", "Id");
}
```

## String normalization flags

`AddVarcharParam` / `AddNVarcharParam` support `trimToNull` and `truncate`, both defaulting to `false`. In the 4.1.0 contract:

- `trimToNull: true` trims leading/trailing whitespace and sends an empty or whitespace-only result as `DBNull.Value`.
- `truncate: true` limits the resulting string to `size`.
- Trimming happens before truncation or length validation.
- With `truncate: false`, an overlong non-null value throws `ArgumentException` before the parameter is added.
- Null input is sent as `DBNull.Value` with any flag combination.

`AddVarcharMaxParam` / `AddNVarcharMaxParam` support the same `trimToNull` behavior and use `Size = -1`; they do not have a truncation flag. With the 4.1.0 fix, use these flags directly without preliminary `TrimToNull()` / `TruncateTo()` calls. For an earlier package, verify its behavior before applying this example.

This example intentionally trims blank text to null and intentionally truncates it. For identifiers or other fields where truncation would corrupt meaning, reject excess length instead.

```csharp
public static class StringParameterExamples
{
    public static void AddOptionalComment(SqlCommand cmd, string? comment)
    {
        cmd.AddNVarcharParam("@Comment", 200, comment, trimToNull: true, truncate: true);
    }
}
```

## Decimal precision and truncation

`AddDecimalParam(name, precision, scale, value, truncateFraction: false)` requires precision 1..38 and scale 0..precision, including for null values. A fractional `decimal(p,p)` such as 0.25 in decimal(2,2) is valid. Values outside the integer-digit range are rejected before binding.

By default, nonzero fractional digits beyond scale cause an exception; insignificant trailing zeroes do not. With `truncateFraction: true`, discard excess fractional digits toward zero before sending the parameter: 1.239 becomes 1.23 and -1.239 becomes -1.23 at scale 2. This is not rounding. Use explicit application rounding first if that is the desired policy. Earlier versions left truncation to SQL conversion and could round instead.

## Procedure parameter discovery and caching

`AddParams` derives stored-procedure metadata when it is not cached. For cache questions, also read [automatic reader and DataTable caches](reading-and-mapping.md#automatic-mapping-and-caches). In 4.1.0:

- Parameter-cache keys distinguish server, database, procedure, connection settings, and authentication identity.
- Caching requires an ordinary schema-qualified two-part procedure name and a known database at cache lookup. An unqualified name or unknown database bypasses caching and requires discovery again.
- Omitting `Initial Catalog` alone does not establish that the database is unknown: an open connection may already know its database. Inspect the connection state used by the cache.

Dictionary arguments to `AddParams` accept either `"Id"` or `"@Id"` for the SQL parameter `@Id`. Use one spelling per key; do not supply duplicate logical names. Object arguments bind public properties by their parameter name.

Ordinary SQL-login connections can share compatible metadata. Integrated, token, and other connection-specific authentication uses a cache scoped to that `SqlConnection` instance: repeated eligible calls on the same instance reuse metadata, while a new instance performs discovery again. Each command receives cloned parameters with complete metadata; cached templates do not retain parameter values. The legacy public `MappingManager.AddSqlParameters(string, ...)` cache remains separate and no longer seeds `AddParams`.

Use explicit `Add*Param` helpers when avoiding metadata discovery is important. Do not replace the scoped key with a procedure-name-only cache or assume metadata remains valid after changing the database contract. Regressions: `Tests/Tests/ParameterCacheTest.cs`.

## Output parameters and procedure return values

Use `AddBitOutputParam`, `AddTinyIntOutputParam`, `AddSmallIntOutputParam`, `AddIntOutputParam`, `AddBigIntOutputParam`, `AddDecimalOutputParam`, `AddGuidOutputParam`, `AddDateTime2OutputParam`, `AddDateTimeOffsetOutputParam`, `AddVarcharOutputParam`, or `AddNVarcharOutputParam` to declare output values. Specify the procedure's string size and decimal precision/scale. These helpers return `void`; read `cmd.Parameters[name].Value` after execution, handling `DBNull.Value` for nullable outputs. For an input/output parameter, construct a `SqlParameter` with `Direction = ParameterDirection.InputOutput` and an initial value; these output-only helpers do not express that contract.

Three results are independent: a procedure's `RETURN` code, its named `OUTPUT` parameters, and the affected-row count returned by `SqlCommand.ExecuteNonQuery*`. `RepositoryBase.ExecuteCommand*` returns the first. Affected-row counts can be `-1`, including when counts are suppressed by `SET NOCOUNT ON`. `GetReturnValueParam()` validates and returns the sole integer return parameter regardless of its name, or null if absent. `ReturnValueParam()` reuses that parameter or adds `@ReturnValue`; `AddReturnValueParam()` ensures the same parameter exists and is idempotent. `SqlParam.ReturnValue()` creates a fresh unattached parameter for explicit parameter arrays or callbacks. Multiple return parameters, a non-Int return SQL type, or a non-return parameter using the reserved `ReturnValue` name (with or without `@`, case-insensitive) are rejected before repository execution.

Repository `Execute` / `ExecuteAsync` and `ExecuteCommand` / `ExecuteCommandAsync` reuse a caller-supplied integer return parameter. Configuration callbacks now run before that parameter is ensured; call `cmd.ReturnValueParam()` inside a callback that needs it instead of assuming `@ReturnValue` is already in the collection.

This complete helper assumes a fresh command attached to an already open, caller-owned connection, with any external transaction already assigned. The procedure is `dbo.SaveLabel(@Name nvarchar(100), @SavedId int OUTPUT, @Message nvarchar(200) OUTPUT)`, allows null outputs, and returns an integer code. It returns no row sets. The helper neither closes the connection nor commits the caller's transaction.

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Artisan.Orm;
using Microsoft.Data.SqlClient;

public static class OutputParameterExamples
{
    public static async Task<(int ReturnCode, int? SavedId, string? Message, int AffectedRows)>
        SaveAsync(SqlCommand cmd, string name, CancellationToken cancellationToken)
    {
        cmd.UseProcedure("dbo.SaveLabel");
        cmd.AddNVarcharParam("@Name", 100, name);
        cmd.AddIntOutputParam("@SavedId");
        cmd.AddNVarcharOutputParam("@Message", 200);
        var returnParameter = cmd.ReturnValueParam();

        var affectedRows = await cmd.ExecuteNonQueryAsync(cancellationToken);
        var id = cmd.Parameters["@SavedId"].Value;
        var message = cmd.Parameters["@Message"].Value;
        return ((int)returnParameter.Value,
            id is null or DBNull ? null : (int)id,
            message is null or DBNull ? null : (string)message,
            affectedRows);
    }
}
```

With `ExecuteReader*`, read outputs only after closing/disposing the reader. Normal close processes remaining results to populate outputs; an explicit loop over every `NextResult()` is not required just to obtain them. A projection inside `GetByReader*` still runs while the reader is open; return its materialized payload first, then inspect outputs while the command is still alive. In a repository callback, finish this work before returning, since `GetByCommand*` disposes its command afterward.

## JSON parameters

The complete call is `cmd.AddJsonParam("@Preferences", payload)`. Its own binding contract is:

| Aspect | `AddJsonParam<T>` behavior |
| --- | --- |
| SQL metadata | `SqlDbType.NVarChar`, `Size = -1`: `nvarchar(max)` |
| Null payload | `DBNull.Value` (SQL NULL), not JSON text `"null"` |
| Non-null payload | `System.Text.Json.JsonSerializer.Serialize(value)` with default options and applicable model attributes |
| Custom settings | No options overload and no inheritance of ASP.NET Core JSON settings |

For custom naming, converters, or field handling, serialize explicitly and pass that text to `AddNVarcharMaxParam`. Passing pre-serialized JSON to `AddJsonParam` serializes it again as a JSON string. When comparing the two approaches, keep the JSON helper's SQL metadata/null contract separate from the serializer-options decision.

This helper is compiled into the `net8.0` and `net10.0` assets, not `netstandard2.1`. A netstandard consumer can serialize with its own supported serializer and use `AddNVarcharMaxParam`. JSON is an explicit SQL text contract; it does not create a UDTT, validate the server-side shape, or deploy JSON parsing code. Use a TVP when the procedure expects its typed table contract.

These independent helpers only configure a supplied command. The custom example assumes the procedure reads a lowercase `theme` property; the default example uses `Theme`.

```csharp
using System.Text.Json;
using Artisan.Orm;
using Microsoft.Data.SqlClient;

public sealed class PreferencePayload
{
    public string Theme { get; set; } = "dark";
}

public static class JsonParameterExamples
{
    public static void AddDefault(SqlCommand cmd, PreferencePayload? payload) =>
        cmd.AddJsonParam("@Preferences", payload);

    public static void AddCamelCase(SqlCommand cmd, PreferencePayload? payload)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var json = payload is null ? null : JsonSerializer.Serialize(payload, options);
        cmd.AddNVarcharMaxParam("@Preferences", json);
    }
}
```

## DateOnly and TimeOnly

The `net8.0` and `net10.0` assets provide `AddDateParam` overloads for `DateOnly` / `DateOnly?` and `AddTimeParam` overloads for `TimeOnly` / `TimeOnly?`. They bind SQL `date` through `DateTime` at midnight and SQL `time` through `TimeSpan`; null becomes `DBNull.Value`. Read with `GetDateOnly`, `GetDateOnlyNullable`, `GetTimeOnly`, or `GetTimeOnlyNullable`. Default-value getter overloads exist, but only substitute a value when the application contract calls for it.

These overloads are absent from the `netstandard2.1` asset. Use `DateTime` / `TimeSpan` and their existing helpers there. Availability of typed helpers does not add these types to automatic mapping: 4.1.0's simple-type list excludes `DateOnly` and `TimeOnly`. Use a registered or inline reader mapper and explicit conversion to `DateTime` / `TimeSpan` in manually built DataTables when needed.

The automatic-mapping simple-type list does include `string`, `char`, `bool`, `Guid`, `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal`, `DateTime`, `DateTimeOffset`, `TimeSpan`, enums, and nullable forms of those value types. Public writable properties still need matching column names and compatible SQL values. In particular, a `DateTime Day` / `TimeSpan? StartsAt` model can use `ReadAs*` for matching `date` / nullable `time` columns on all three assets. Do not transfer the DateOnly/TimeOnly limitation to that legacy representation.

This self-contained example configures `@Day date` and `@StartsAt time`, and maps `Day date NOT NULL, StartsAt time NULL`, in that order. `procedureName` identifies an existing application procedure with this contract. The `Read` factory expects a positioned reader; `cmd.ReadToAsync` positions it automatically. The factory itself does not call `Read()` or `NextResult()`.

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Artisan.Orm;
using Microsoft.Data.SqlClient;

public sealed class ScheduleValue
{
    public DateOnly Day { get; set; }
    public TimeOnly? StartsAt { get; set; }
}

public static class ScheduleParameterExamples
{
    public static void Configure(SqlCommand cmd, ScheduleValue value)
    {
        cmd.AddDateParam("@Day", value.Day);
        cmd.AddTimeParam("@StartsAt", value.StartsAt);
    }

    public static ScheduleValue Read(SqlDataReader reader) => new ScheduleValue
    {
        Day = reader.GetDateOnly(0),
        StartsAt = reader.GetTimeOnlyNullable(1)
    };

    public static ScheduleValue? ReadConfigured(SqlCommand cmd) =>
        cmd.ReadTo(Read);

    public static Task<ScheduleValue?> ReadAsync(
        RepositoryBase repo, string procedureName, ScheduleValue input,
        CancellationToken cancellationToken) =>
        repo.GetByCommandAsync((cmd, token) =>
        {
            cmd.UseProcedure(procedureName);
            Configure(cmd, input);
            return cmd.ReadToAsync(Read, token);
        }, cancellationToken);
}
```

`ReadConfigured` expects an already configured, live command. The single-object repository `ReadTo` / `ReadToAsync` shortcuts do not accept an inline factory; use the command-level calls above or register a mapper. See [execution levels](execution-levels.md) for the actual shortcut argument order.

The equivalent legacy model can use automatic reading without registering a mapper. This complete example also compiles against the `netstandard2.1` asset:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Artisan.Orm;

public sealed class LegacyScheduleValue
{
    public DateTime Day { get; set; }
    public TimeSpan? StartsAt { get; set; }
}

public static class LegacyScheduleExamples
{
    public static Task<LegacyScheduleValue?> ReadAsync(
        RepositoryBase repo, string procedureName, DateTime day, TimeSpan? startsAt,
        CancellationToken cancellationToken) =>
        repo.ReadAsAsync<LegacyScheduleValue>(procedureName, cmd =>
        {
            cmd.AddDateParam("@Day", day);
            cmd.AddTimeParam("@StartsAt", startsAt);
        }, cancellationToken);
}
```

## Compact numeric lists and rowversion tokens

`GetByteArrayFromString`, `GetInt16ArrayFromString`, and `GetInt32ArrayFromString` parse a textual comma-separated numeric column. SQL NULL and blank text produce an empty array. Invalid, empty interior, or out-of-range elements throw; this is not a general CSV parser and does not skip malformed values. Use these helpers only when that text shape is the procedure's declared result, not instead of a TVP for typed input rows.

For rowversion, `GetBytesFromRowVersion` returns bytes and `GetBase64StringFromRowVersion` returns base64; both preserve SQL NULL. Send the corresponding non-null token back with `AddRowVersionParam` or `AddRowVersionFromBase64StringParam`. Validate externally supplied tokens as eight bytes. Treat rowversion as an opaque concurrency token, not a timestamp or application ordering key.

`GetInt64FromRowVersion` / `GetInt64FromRowVersionNullable` and `AddRowVersionFromInt64Param` use `BitConverter` in both directions. Their integer representation follows the platform's byte order; it is not a promise of SQL numeric conversion or portable numeric sorting. Prefer bytes/base64 at application boundaries.

```csharp
using System;
using Artisan.Orm;
using Microsoft.Data.SqlClient;

public static class CompactValueExamples
{
    // Positioned reader: RoleIds nvarchar(max) NULL, Version binary(8) NULL.
    public static (int[] RoleIds, string? Version) Read(SqlDataReader reader) =>
        (reader.GetInt32ArrayFromString(0), reader.GetBase64StringFromRowVersion(1));

    // Configured command; this example defines an empty result as no IDs/token.
    public static (int[] RoleIds, string? Version) ReadSingle(SqlCommand cmd) =>
        cmd.GetByReader(reader => reader.Read()
            ? Read(reader)
            : (Array.Empty<int>(), (string?)null));

    public static void AddVersion(SqlCommand cmd, string token)
    {
        if (Convert.FromBase64String(token).Length != 8)
            throw new ArgumentException("A rowversion token must contain eight bytes.", nameof(token));
        cmd.AddRowVersionFromBase64StringParam("@Version", token);
    }
}
```

`GetByReader` supplies a reader before its first row. A positioned-row helper such as `Read` cannot be passed to it directly; `ReadSingle` advances once and handles an empty result explicitly. A reader materializer such as `reader.ReadTo(factory)` already advances, so do not add another `Read()` before that materializer.

Sources for these additional contracts: `Tests/Tests/OutputParamsTest.cs`, `Tests/Tests/JsonParamTest.cs`, `Tests/Tests/DateOnlyTimeOnlyTest.cs`, `Tests/Tests/SqlParameterTest.cs`, and `Artisan.Orm/TypeExtensions.cs`.

## Check a mismatched result locally

Inspect the actual SELECT and its declared types, then compare reader ordinals, names, `GetFieldType(i)`, and `GetDataTypeName(i)` in a local diagnostic/test. Use SQL metadata inspection when it can describe the query without execution. Do not add an endpoint that executes arbitrary stored-procedure names merely to inspect mappings. Parameter/column metadata is usually sufficient; sample values are needed only when conversion or null semantics are uncertain.

Sources: `Artisan.Orm/SqlCommandExtensions_Parameters.cs`, `Artisan.Orm/DataTableExtensions.cs`, `Artisan.Orm/SqlDataReaderExtensions_Get.cs`, `Artisan.Orm/SqlDataReaderExtensions_CreateObject.cs`, and `Artisan.Orm/StringExtensions.cs`.
Wiki: [SqlCommand extensions](https://github.com/lobodava/artisan-orm/wiki/SqlCommand-Extensions), [Table parameters](https://github.com/lobodava/artisan-orm/wiki/cmd.AddTableParam), [User-defined table types](https://github.com/lobodava/artisan-orm/wiki/User-Defined-Table-Types).
