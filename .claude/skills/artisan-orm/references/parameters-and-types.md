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

`AddParams` derives stored-procedure metadata when it is not cached. In 4.1.0 its cache distinguishes server, database, command, connection settings, and authentication identity. A known database and ordinary schema-qualified two-part procedure name are required for caching; unqualified names and unknown default databases are rediscovered.

Dictionary arguments to `AddParams` accept either `"Id"` or `"@Id"` for the SQL parameter `@Id`. Use one spelling per key; do not supply duplicate logical names. Object arguments bind public properties by their parameter name.

Ordinary SQL-login connections can share compatible metadata. Integrated, token, and other connection-specific authentication uses a cache scoped to that `SqlConnection` instance, so a new connection performs discovery again. Each command receives cloned parameters with complete metadata; cached templates do not retain parameter values. The legacy public `MappingManager.AddSqlParameters(string, ...)` cache remains separate and no longer seeds `AddParams`.

Use explicit `Add*Param` helpers when avoiding metadata discovery is important. Do not replace the scoped key with a procedure-name-only cache or assume metadata remains valid after changing the database contract. Regressions: `Tests/Tests/ParameterCacheTest.cs`.

## Check a mismatched result locally

Inspect the actual SELECT and its declared types, then compare reader ordinals, names, `GetFieldType(i)`, and `GetDataTypeName(i)` in a local diagnostic/test. Use SQL metadata inspection when it can describe the query without execution. Do not add an endpoint that executes arbitrary stored-procedure names merely to inspect mappings. Parameter/column metadata is usually sufficient; sample values are needed only when conversion or null semantics are uncertain.

Sources: `Artisan.Orm/SqlCommandExtensions_Parameters.cs`, `Artisan.Orm/DataTableExtensions.cs`, `Artisan.Orm/SqlDataReaderExtensions_Get.cs`, `Artisan.Orm/SqlDataReaderExtensions_CreateObject.cs`, and `Artisan.Orm/StringExtensions.cs`.
Wiki: [SqlCommand extensions](https://github.com/lobodava/artisan-orm/wiki/SqlCommand-Extensions), [Table parameters](https://github.com/lobodava/artisan-orm/wiki/cmd.AddTableParam), [User-defined table types](https://github.com/lobodava/artisan-orm/wiki/User-Defined-Table-Types).
