# Changelog

All notable changes to **Artisan.ORM** are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [4.1.0] - Pending release

### Added

- `SqlParam` factories mirror every typed `Add*Param` overload, preserving argument names,
  defaults, SQL metadata, nullable values, and string/decimal normalization. Factories work
  with existing repository parameter-array overloads and create fresh unattached parameters.
  JSON and DateOnly/TimeOnly factories follow the existing target-framework availability.
- Roslyn checks mapped `SqlParam.Table<T>` and `SqlParam.TableRow<T>` calls for the required
  table mapper functions, while explicit automatic mapping needs no registered mapper.
- Roslyn analyzer bundled in `Artisan.ORM`: errors ART001-ART007 validate mapper registrations,
  required functions, signatures, duplicate registrations, ambiguous reflection lookups, and
  mapper-dependent calls. English messages use localizable resources. Requires Roslyn 4.8+.
- Analyzer tests on .NET 8 and .NET 10 and a package-consumer verification script requiring no database.

### Fixed

- Repository parameter arrays skip null entries, allowing a null TVP factory result to mean
  omission. Null short-ID collections now follow the same omission rule as other ID collections;
  non-null empty collections still produce an empty TVP.
- Repository and command execution reuse a caller-supplied integer RETURN parameter instead
  of adding a duplicate. Return helpers reject multiple return parameters, incompatible SQL
  types, and reserved-name collisions before SQL execution. `AddReturnValueParam` is idempotent.
- Negative-ID generators stop at exhaustion with `InvalidOperationException` instead of returning
  zero, positive values, or repeated IDs. This applies to Int16, Int32, and Int64 generators.
- Decimal parameters accept fractional `decimal(p,p)` values and validate precision/scale.
  `truncateFraction: true` truncates toward zero before binding; false rejects nonzero excess scale.
- Dictionary parameter binding accepts names with or without a leading `@`.
- Automatic table mapping skips indexers and non-public getters; numeric enum mapping is consistent
  for nullable and non-nullable values. Missing mapper write functions produce descriptive errors.
- Async command materializers forward cancellation through opening, row reads, and result advancement.
- Transaction failure cleanup preserves the original exception, including after explicit completion.
- Deferred repository reads retain their command until enumeration ends or is disposed.
- MergeJoin disposes acquired detail enumerators on success, callback failure, and partial setup failure.
- DataReply parsing rejects undefined values and combined names; status-only exceptions include the status.
- Sample DataServiceBase propagates cancellation and safely handles absent or short stack traces.
- Sample SaveUsers validates login, name, and email independently, including new-only duplicate batches.
- Package verification uses an isolated restore directory and checks the exact supplied package hash.

- Folder hierarchy recalculation uses the old parent's hierarchy path and numbers each
  parent's children independently, preserving valid paths when a folder is reparented.
- `dbo.GetHidCode` preserves the complete hexadecimal representation of a `hierarchyid`
  with capacity for 1784 characters. Integration regressions cover reparenting with
  descendants and long hierarchy codes, plus null/root/short-code compatibility.
- Synchronous scalar enumeration now includes the first row, including one-row and nullable results.
- Explicit read factories are honored for scalar and nullable results across list, array, enumerable,
  and multi-result paths; SQL NULL in column zero no longer bypasses a supplied projection.
- Repositories attached to an external transaction now use its connection and leave both resources
  owned by the caller. Completed transactions and attempts to start a nested transaction are rejected.
- Automatic mapping caches distinguish runtime types, table names, ordered columns, and reader schemas.
- Procedure parameter metadata caches distinguish connection/database identities and preserve complete
  parameter metadata without sharing mutable parameter instances or values between commands.
- String parameter helpers now apply `trimToNull` and `truncate` to the parameter value.
  Trimming precedes truncation or length validation and converts blank text to SQL NULL.
- `CheckForDataReplyException` now accepts an explicit `Ok` status and advances to the data
  result set without throwing or interpreting the payload as error messages.
- Analyzer diagnostics now run during document analysis as well as builds, so mapper errors
  remain visible in Visual Studio's combined Build + IntelliSense list and in the editor.
- Patched transitive `System.Security.Cryptography.Pkcs` vulnerabilities with minimum versions
  8.0.2 (`netstandard2.1`/`net8.0`) and 9.0.21 (`net10.0`). These requirements flow to package consumers.
- Removed the explicit SourceLink package that pulled in vulnerable `Microsoft.Build.Tasks.Git`;
  builds use SourceLink from .NET SDK 10.0.403 or a newer patch in the same feature band.
- `RequiredMethod.AllMain` now requires `CreateObject`, `CreateDataTable`, and `CreateDataRow`
  as documented in the Wiki. Runtime validation previously only required `CreateObject`.

### Changed

- Updated the four design articles for the source being prepared for 4.1.0 while preserving
  their publication history. Historical examples are distinguished from current APIs,
  and the original clustered-index claim is retained with an ordering/deadlock correction.
- Updated Microsoft.Data.SqlClient to 7.1.1, test Configuration packages to 10.0.14,
  Microsoft.NET.Test.Sdk to 18.11.0, and MSTest adapter/framework to 4.5.2.
- Removed redundant direct test references to SqlClient, Configuration, and System.Text.Json.
- Enabled NuGet auditing of direct and transitive dependencies; known vulnerability warnings fail the build.

### Compatibility

- `ExecuteCommand` / `ExecuteCommandAsync` ensure a RETURN parameter after the configuration
  callback. A callback that needs that parameter should call `cmd.ReturnValueParam()` instead
  of assuming `cmd.Parameters["@ReturnValue"]` was populated before configuration.
- Null `IEnumerable<short>` TVPs are omitted instead of explicitly binding an empty table.
  Pass an empty non-null collection when an explicit empty TVP is intended.
- `truncateFraction: true` now means truncation toward zero, not implicit SQL rounding. Without
  that flag, nonzero excess fractional digits still fail. SQL precision must be 1..38, scale 0..precision.
- Negative IDs remain unique only within a generator's finite lifetime; use Int32/Int64 for larger
  workloads. An exhausted generator stays exhausted instead of silently resetting.
- Enum properties now participate in automatic mapping through their underlying numeric type.
  Explicit manual mappers still define their own SQL shape and conversions.
- `DataReply.ParseStatus` retains case-insensitive names and defined numeric values for compatibility;
  undefined numbers and comma-separated combinations now throw `InvalidCastException`.

- Explicit read factories now handle every row, including SQL NULL values; callers control null substitution.
- An external transaction passed to a repository stays caller-owned, along with its connection.
  Complete and detach an attached transaction before starting a new one through the repository helpers.
- `AddParams` no longer consumes the unscoped public `MappingManager.AddSqlParameters` cache.
  Procedure metadata is isolated by database and connection identity. Integrated/token authentication
  reuses metadata within one connection instance; a new instance performs discovery again. Use explicit
  `Add*Param` calls when avoiding metadata discovery is important. Unqualified procedure names or an
  unknown default database are not cached.
- Builds now fail for statically detected mapper contract violations. Projects that supply mapper
  assemblies only at runtime can configure `artisan_orm.allow_external_mappers = true` to skip
  missing-registration diagnostics. See [analyzer documentation](docs/analyzers.md).

## [4.0.0] — 2026

First major version since 3.5.1. Multi-targets `netstandard2.1`, `net8.0`, `net10.0`. Existing 3.x code continues to work; the changes below are additive or strictly internal unless called out under **Breaking**.

### Added

#### New APIs

- **`BulkCopy` / `BulkCopyAs` / `BulkCopyAsync` / `BulkCopyAsAsync`** — high-throughput inserts through `SqlBulkCopy`, fed by the same `MapperFor` mappers used elsewhere in the library. Sync and async variants on `RepositoryBase` and as `SqlConnection` extensions.
- **`ReadToLists<T1..T4>` / `ReadToListsAsync<T1..T4>`** — read multiple result sets into N strongly-typed lists in a single call. Available on `RepositoryBase`, `SqlCommand` and `SqlDataReader`. Removes the need for explicit `reader.NextResult()` boilerplate when stored procedures return several sets.
- **`ReadToAsyncEnumerable<T>` / `ReadAsAsyncEnumerable<T>`** — `IAsyncEnumerable<T>` streaming for very large result sets. Rows yield as they arrive; the reader and connection are disposed when enumeration ends or is cancelled.
- **`RunInTransaction` / `RunInTransactionAsync`** — `Action<SqlTransaction>` / `Func<SqlTransaction, …, Task>` wrappers with autocommit-on-return and rollback-on-exception. Overloads with and without explicit `IsolationLevel`.

#### MergeJoin

- Rewritten in-memory merge for master/detail and master/detail/sub-detail shapes; new overloads for two-detail (`TMaster + TFirstDetail + TSecondDetail`) and three-level (`TMaster + TDetail + TSubDetail`) result sets. Joins by FK lambdas, no ORM-level SQL `JOIN` required.

#### Async transaction API

- `RepositoryBase` now exposes proper `BeginTransactionAsync` / `CommitAsync` / `RollbackAsync` paths.
- `IAsyncDisposable` implemented; `await using var repo = new MyRepo()` is supported.
- `ConfigureAwait(false)` everywhere in async paths.
- `CancellationToken` parameters are honoured all the way down to ADO.NET calls.

#### Other additions

- `ArtisanMappingException` — single dedicated exception replacing internal `NullReferenceException` / `ApplicationException` from earlier versions.
- API symmetry pass: filled in missing sync/async overload pairs and `Action<SqlCommand>` / `params SqlParameter[]` variants across `Read*`, `Get*`, `Execute*`. (See commits `b8a3bfa`, `c48db1a`, `4c704b4` for the audit.)
- XML documentation comments on the entire public API; the generated `.xml` file ships inside the NuGet package.

### Changed

- **Targets** changed from single `netstandard2.1` to multi-target `netstandard2.1; net8.0; net10.0`.
- **Microsoft.Data.SqlClient** bumped from `5.2.3` (3.5.1) to `7.0.1`.
- **Nullable Reference Types** enabled across the entire library; public API now expresses nullability.
- **`AddParam` extensions** no longer use `dynamic` — replaced with `object`. Faster dispatch and AOT-friendlier; behaviour is unchanged.
- **`MappingManager`** — safe reflection and lazy initialization; no eager scan of all loaded assemblies on first use.
- **Mapper cache key** — derived from the reader schema instead of reflection on `SqlDataReader.Command` (which was brittle across `Microsoft.Data.SqlClient` versions).
- `NegativeIdentity` generators (`Int16/Int32/Int64NegativeIdentity`) are now thread-safe — `Interlocked`-based, no lock contention. Previously not safe for concurrent graph saves under load.
- `RepositoryBase` Dispose pattern reworked: proper `Dispose(bool disposing)` + finalizer-free design + `IAsyncDisposable`.

### Fixed

- `RecordTypeMapper` bug surfaced by the new test suite (commit `3957646`).
- Several spelling / formatting fixes in code samples and XML docs.

### Breaking

- `Microsoft.Data.SqlClient` major bump (5 → 7). Consumers that pin the package directly may need to update their constraint.
- Public types with reference-typed properties now declare nullability. Code that ignored nullable warnings will continue to compile; code with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` may need annotations.
- `ApplicationException` / bare `NullReferenceException` previously thrown from mapping paths are now `ArtisanMappingException`. `catch (Exception)` keeps working; specific catches need updating.
- Some internal helpers were removed during the symmetry pass; the public surface is unchanged.

### Packaging & tooling

- Deterministic builds (`ContinuousIntegrationBuild` on CI).
- [SourceLink](https://github.com/dotnet/sourcelink) embedded — F12 / step-into into library sources from any IDE.
- Symbol package (`.snupkg`) published alongside the main package.
- The four design articles originally on CodeProject ([Object Graph Saving](articles/01-object-graph-saving.md), [Reinventing the Wheel](articles/02-artisan-orm.md), [Data Reply](articles/03-data-reply.md), [Id-ParentId & HierarchyId](articles/04-hierarchyid-combination.md)) re-published in the [`articles/`](articles/) folder under [CC-BY-SA-4.0](articles/LICENSE) after CodeProject went read-only in late 2024.

---

## [3.5.1] — 2024-12

- `CancellationToken` support across asynchronous methods.
- `Microsoft.Data.SqlClient` upgraded to 5.2.3.

## [3.5.0] — 2024

- Library re-targeted to `.NET Standard 2.1`.

## [3.0.x]

- `3.0.0` — re-implemented as a `.NET Core 6.0` library.
- `Microsoft.Data.SqlClient` upgraded to 5.1.4 (vulnerability fix).
- Added `GetSqlXmlNullable`.

## [2.x.x]

- Built against `.NET Standard 2.0` for use with the .NET Framework and earlier .NET Core.
- Added `ReadDynamic` / `ReadDynamicList`.

## [1.x.x]

- Original release. Introduced the core patterns: `MapperFor`, `cmd.AddTableParam`, negative identities, the `DataReply` protocol, `ReadToTree` / `INode<T>`.
- See `articles/02-artisan-orm.md` for the origin story.

---

[4.0.0]: https://github.com/lobodava/artisan-orm/releases/tag/v4.0.0
[3.5.1]: https://www.nuget.org/packages/Artisan.ORM/3.5.1
[3.5.0]: https://www.nuget.org/packages/Artisan.ORM/3.5.0
