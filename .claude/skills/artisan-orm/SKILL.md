---
name: artisan-orm
description: Write, review, and debug C# repositories, mappers, stored procedure calls, TVPs/UDTTs, DataReply, and transactions in projects using Artisan.Orm with SQL Server. Apply when project dependencies or existing code establish Artisan.Orm usage, even if the request does not name it, or when the user explicitly asks to adopt Artisan.Orm. Do not apply to unrelated EF Core, Dapper, plain ADO.NET, or SQL-only work.
---

# Artisan.Orm

Use this skill for application code that uses Artisan.Orm, adoption explicitly requested by the user, and changes to the library's mapping and data-access contracts. Artisan.Orm extends ADO.NET; commands contain explicit SQL or stored procedure calls. Use `Microsoft.Data.SqlClient`, not the similarly named types from `System.Data.SqlClient`.

## Version and source compatibility

This skill targets **Artisan.Orm 4.1.0**, the release being prepared. It describes that release's source contract; it does not assert that a 4.1.0 package is already published.

The 4.1.0 changes include the bundled Roslyn analyzer and complete `AllMain` checks, corrected reads and cancellation, transaction ownership and error preservation, isolated caches, string/decimal normalization, numeric enum mapping, finite negative IDs, and resource cleanup. DataReply remains optional with stricter status validation. Sample SQL fixes cover folder hierarchy handling and user batch validation; installing a library package does not deploy SQL. Earlier packages may behave differently. Check the consuming project's package/source and release notes; a development version field is not proof of published package contents.

## Establish the actual contract

Start with the version and target framework used by the project. When the library's source is available, inspect its implementation and relevant tests. In a consumer repository, inspect the referenced package and existing repositories/mappers instead of assuming this skill's source checkout is present.

Use the [project wiki](https://github.com/lobodava/artisan-orm/wiki) for patterns and background. Match documentation to the version in use. Resolve disagreements against that version's implementation and executable tests.

Before writing a mapper or procedure call, establish:

- The actual SELECT columns, their ordinal positions, SQL types, and nullability.
- The number and order of result sets, including any DataReply header.
- The UDTT schema and column order when writing a table-valued parameter.
- Which layer owns the connection and transaction.

Do not infer these from C# property declaration order. Manual mappers may deliberately produce a SQL shape different from the model, including computed or collection-derived columns. Validate that explicit mapping against its actual SQL contract, not against a one-property-per-column rule. If the relevant schema or procedure is unavailable, state the missing contract or request the specific definition; neither the skill nor the analyzer automatically sees a database.

## Read the relevant reference

| Task | Reference |
| --- | --- |
| Entity mappers, registered vs automatic mapping, single/multiple results, streaming, trees, MergeJoin | [Reading and mapping](references/reading-and-mapping.md) |
| Scalar parameters, TVP overloads, column types, nullable values, GUID/rowversion conversions | [Parameters and types](references/parameters-and-types.md); its `User` TVP examples also need the [shared model and mapper](references/reading-and-mapping.md#minimal-shared-model-and-mapper) |
| Transactions, graph saving, insertion order/deadlocks, DataReply and JSON/HTTP contracts | [Writes and transactions](references/writes-and-transactions.md) |

Read the task's reference and any example prerequisites it explicitly links. For the `User` TVP examples, also read the shared declarations; the rest of the reading reference is unnecessary unless the task involves reads. Examples illustrate contracts; adapt their entity, schema, size, and nullability to the user's application.

## Choose the execution path

| Entry point | Callback responsibility | Result |
| --- | --- | --- |
| `GetByCommand` / `GetByCommandAsync` | Configure and execute the command, usually through a read extension | The callback's value |
| `RunCommand` / `RunCommandAsync` | Configure and execute the command | No returned value |
| `ExecuteCommand` / `ExecuteCommandAsync` | Configure the command; the repository executes it | T-SQL return value, not affected-row count |

The async `ExecuteCommandAsync` configuration callback is synchronous. For the other async callbacks, pass the cancellation token to the wrapper and to the actual async operation inside it.

`ReadTo*` uses a registered mapper for entity types, an inline mapping delegate when supplied, or the scalar path for supported simple types. `ReadAs*` uses automatic mapping. Follow the existing application's choice; automatic mapping is not limited to prototypes. Both raw SQL and stored procedures are supported.

Use `RequiredMethod` to declare the mapper's intended capabilities, including capabilities with no callers yet. A read-only mapper normally needs `CreateObject`; a TVP needs `CreateDataTable` and `CreateDataRow`. `AllMain` requires all three. Actual operations independently require their functions. Do not add empty placeholder methods or suppress diagnostics merely to satisfy an incorrectly chosen contract.

For transactions, make the commit policy explicit: `BeginTransaction*` requires a caller commit, while `RunInTransaction*` commits on normal completion. Reuse the same repository, or attach repositories to the same caller-owned transaction through the external-transaction constructor; see the ownership rules in the write reference.

## Source map

Paths below are relative to the ArtisanOrm repository root, not to this skill directory. Use them when that checkout is available; otherwise consult the corresponding package API and wiki pages.

| Contract | Implementation / examples |
| --- | --- |
| Command execution, connections, transactions, DataReply status handling | `Artisan.Orm/RepositoryBase.cs` |
| Mapper discovery and requirements | `Artisan.Orm/MappingManager.cs`, `Artisan.Orm/MapperForAttribute.cs`, `docs/analyzers.md` |
| Reader getters and result-set advancement | `Artisan.Orm/SqlDataReaderExtensions_Get.cs`, `Artisan.Orm/SqlDataReaderExtensions_Read.cs` |
| Command reads and streaming | `Artisan.Orm/SqlCommandExtensions_Sync.cs`, `Artisan.Orm/SqlCommandExtensions_Async.cs` |
| Parameters and TVPs | `Artisan.Orm/SqlCommandExtensions_Parameters.cs`, `Artisan.Orm/DataTableExtensions.cs` |
| Graph joining | `Artisan.Orm/MergeJoinExtensions.cs`, `Tests/DAL/GrandRecords/Repository.cs` |
| Working graph persistence | `Tests/DAL/GrandRecords/`, `Database/5. Procedures/GrandRecords/SaveGrandRecords.sql` |
| Folder hierarchy and SQL regressions | `Database/5. Procedures/Folders/`, `Database/3. Functions/GetHidCode.sql`, `Tests/Tests/FolderSqlRegressionTest.cs` |
| DataReply wire format | `Artisan.Orm/DataReply.cs`, `Artisan.Orm/DataReplyState.cs`, `Artisan.Orm/DataReplyMessage.cs`, `Tests/Tests/DataReplyTest.cs` |

The mapper/UDTT generator is `Database/Utility Scripts/Artisan Code Generation.sql`; inspect its settings and generated column types before using the output. See [Code Generation](https://github.com/lobodava/artisan-orm/wiki/Code-Generation).

## Verify the change

Build the affected project against its actual target frameworks. Compile mapper examples; then exercise the relevant read/write contract with representative SQL data when a test database is available. Compilation alone does not establish that reader ordinals, SQL types, result-set ordering, or UDTT schemas agree.

For mapper failures, inspect result metadata (`GetName`, `GetDataTypeName`, `GetFieldType`) in a local test or debugger, or use `sp_describe_first_result_set` when it can describe the query. Do not introduce an HTTP endpoint that executes arbitrary procedure names for this purpose.

For library maintenance in this repository:

- Check mapper diagnostics using `Artisan.Orm.Analyzers.Tests` and the documented contract in `docs/analyzers.md`. The analyzer checks mapping availability/signatures, not a live SQL schema.
- The `Tests` project uses a seeded SQL Server database and modifies shared fixtures. Run its .NET 8 and .NET 10 targets sequentially against the same database.
- A complete `Artisan.sln` build includes an SSDT SQL project and needs Visual Studio MSBuild with SQL build targets; a library-only `dotnet build` does not validate the entire solution.
- SQL fixes need the matching objects in the test database. A build does not deploy them. Review deployment scope: the sample post-deployment folder generator truncates folder data, so do not use a full publish as a routine substitute for updating individual test objects.

Report what was actually built or exercised and any missing database/schema evidence. Request examples from consumer projects only when application-specific conventions cannot be inferred from the code already available.

## Evaluate skill changes

Use [validation scenarios](evals/evals.json) when maintaining this skill; they are not prerequisites for application tasks. Each case supplies project context, a user prompt, an expected applicability decision, and observable assertions.

For selection cases, use a fresh Claude Code session with the skill discoverable but not explicitly invoked. Provide the case's context and prompt without its expected decision or assertions, and inspect whether the skill is selected. For behavior cases, provide the context and prompt and make the skill available; check the resulting answer/code against the assertions. Keep fixture work isolated from application code and databases.

Record the model, skill revision, cases run, observed behavior, and assertion results outside the distributed skill. Re-run representative cases without the skill when comparing its benefit. Parsing the JSON or reviewing prompts alone does not establish Claude's actual selection behavior.
