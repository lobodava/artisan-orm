---
name: artisan-orm
description: Use only for tasks involving Artisan.Orm established by C# project dependencies or existing C# data-access code, or when the user explicitly requests its adoption. Unless adoption is explicitly requested, do not load for standalone SQL-only workspaces or unrelated EF Core, Dapper, or plain ADO.NET tasks. SQL Server terminology, TVPs, or this skill folder being present are not evidence of Artisan.Orm usage. In qualifying projects, write, review, and debug repositories, mappers, stored procedure calls, TVPs/UDTTs, DataReply, caching, and transactions, even when the request does not name the library.
---

# Artisan.Orm

Establish the applicability condition in the description before loading this skill; the presence of its files alone is not a trigger. Within an established Artisan.Orm project, relevant SQL/schema changes and library maintenance qualify alongside C# application work. For unrelated tasks, follow the project's existing tools without introducing Artisan.Orm. Artisan.Orm extends ADO.NET; commands contain explicit SQL or stored procedure calls. Use `Microsoft.Data.SqlClient`, not the similarly named types from `System.Data.SqlClient`.

## Version and source compatibility

This skill targets **Artisan.Orm 4.1.0**. It describes that version's source contract; publication status must be checked separately when relevant.

The 4.1.0 changes include mapper diagnostics, corrected reads/cancellation, transaction ownership, caches, parameter normalization, enum mapping, finite negative IDs, and cleanup. DataReply remains optional. Installing a package does not deploy sample SQL. Check the consuming project's actual package/source; a development version field is not proof of package contents. For older explicit-`Ok` handling, verify `RepositoryBase.CheckForDataReplyException`; the `DataReplyStatus.Ok` enum member alone is insufficient. The NuGet package includes the analyzer; local-source consumers need an [explicit analyzer reference](references/code-generation.md#use-the-analyzer-with-local-source).

## Establish the actual contract

Start with the version and target framework used by the project. When the library's source is available, inspect its implementation and relevant tests. In a consumer repository, inspect the referenced package and existing repositories/mappers instead of assuming this skill's source checkout is present.

Before adding or adapting application code, locate and read its existing project/dependency file, model, repository, mapper, and relevant SQL definition. Identify the declarations and SQL names the implementation will reuse; import their namespaces instead of redeclaring existing models or inventing replacement types, tables, or parameters. Edit requested application files. For explicitly requested standalone examples, distinguish illustrative declarations from existing application types. Distinguish a supplied SQL contract from inspected source or database metadata.

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
| Choose repository shortcuts, command extensions, or reader extensions | [Execution levels](references/execution-levels.md) |
| Entity mappers, registered vs automatic mapping, single/multiple results, streaming, trees, MergeJoin | [Reading and mapping](references/reading-and-mapping.md) |
| Dynamic results, dictionaries, compact `ObjectRow` / `ObjectRows` results | [Result shapes](references/result-shapes.md) |
| `SqlParam` factories, scalar/OUTPUT/JSON parameters, TVPs, DateOnly/TimeOnly, nullable values, GUID/rowversion conversions | [Parameters and types](references/parameters-and-types.md); its `User` TVP examples also need the [shared model and mapper](references/reading-and-mapping.md#minimal-shared-model-and-mapper) |
| Bulk inserts through a connection or repository, mapped vs automatic columns | [Bulk copy](references/bulk-copy.md) |
| Reader mapping, DataTable shape, and procedure-parameter caches | Read both [mapping caches](references/reading-and-mapping.md#automatic-mapping-and-caches) and [parameter metadata caching](references/parameters-and-types.md#procedure-parameter-discovery-and-caching) |
| Transactions, graph saving, insertion order/deadlocks, DataReply and JSON/HTTP contracts | [Writes and transactions](references/writes-and-transactions.md) |
| Generate and adapt models, mappers, and SQL templates from an existing table/view | [Code generation](references/code-generation.md) |

Read the task's reference and any example prerequisites it explicitly links. For the `User` TVP examples, also read the shared declarations; the rest of the reading reference is unnecessary unless the task involves reads. Examples illustrate contracts; adapt their entity, schema, size, and nullability to the user's application. Deliver application examples in the application or the answer, with every requested helper body included. Treat skill references as guidance; edit them only when the user explicitly requests skill maintenance.

## Choose the execution path

Start with `RepositoryBase.ReadTo*`, `ReadAs*`, or `Execute*` shortcuts for simple operations. Use typed factories such as `SqlParam.Int` and `SqlParam.Table` with their parameter-array overloads; null TVP results are omitted. Repository shortcuts also accept parameter-configuration callbacks. Use command or reader callbacks when the task needs their additional control; see [execution levels](references/execution-levels.md) for the same lookup at all three levels.

| Entry point | Callback responsibility | Result |
| --- | --- | --- |
| Repository `ReadTo*` / `ReadAs*` shortcuts | Optionally configure parameters; the repository executes and reads | The requested result shape |
| `GetByCommand` / `GetByCommandAsync` | Configure and execute the command, usually through a read extension | The callback's value |
| `RunCommand` / `RunCommandAsync` | Configure and execute the command | No returned value |
| `ExecuteCommand` / `ExecuteCommandAsync` | Configure the command; the repository executes it | T-SQL return value, not affected-row count |

Configuration actions remain synchronous, including those on async repository shortcuts and `ExecuteCommandAsync`. In `GetByCommandAsync` / `RunCommandAsync` callbacks, pass the cancellation token to the wrapper and to the actual async operation inside it.

`ReadTo*` uses a registered mapper for entity types, an inline mapping delegate when supplied, or the scalar path for supported simple types. `ReadAs*` uses automatic mapping. Follow the existing application's choice; automatic mapping is not limited to prototypes. Both raw SQL and stored procedures are supported.

Use `RequiredMethod` to declare the mapper's intended capabilities, including capabilities with no callers yet. A read-only mapper normally needs `CreateObject`; a TVP needs `CreateDataTable` and `CreateDataRow`. `AllMain` requires all three in both analyzer checks and the runtime mapper scan, which throws `ArtisanMappingException` for a broken declaration. Actual operations independently require their functions.

With an otherwise valid registration, a mapper-based `ToDataTable()` or `AddTableParam` call missing `CreateDataTable` or `CreateDataRow` reports `ART006` at the call site, even with `RequiredMethod.CreateObject`. Implementing both methods satisfies that call; broadening `RequiredMethod` is an optional additional declaration check, not a prerequisite for using existing methods.

Do not add empty placeholder methods or suppress diagnostics merely to satisfy an incorrectly chosen contract.

For transactions, make the commit policy explicit: `BeginTransaction*` requires a caller commit, while `RunInTransaction*` commits on normal completion. Reuse the same repository, or attach repositories to the same caller-owned transaction through the external-transaction constructor; see the ownership rules in the write reference.

## Source map

Paths below are relative to the ArtisanOrm repository root, not to this skill directory. Use them when that checkout is available; otherwise consult the corresponding package API and wiki pages. File names are not necessarily CLR type names: the `SqlCommandExtensions_*.cs` files implement the partial `SqlCommandExtensions` class.

| Contract | Implementation / examples |
| --- | --- |
| Command execution, connections, transactions, DataReply status handling | `Artisan.Orm/RepositoryBase.cs` |
| Mapper discovery and requirements | `Artisan.Orm/MappingManager.cs`, `Artisan.Orm/MapperForAttribute.cs`, `docs/analyzers.md` |
| Reader getters and result-set advancement | `Artisan.Orm/SqlDataReaderExtensions_Get.cs`, `Artisan.Orm/SqlDataReaderExtensions_Read.cs` |
| Command reads and streaming | `Artisan.Orm/SqlCommandExtensions_Sync.cs`, `Artisan.Orm/SqlCommandExtensions_Async.cs` |
| Parameters and TVPs | `Artisan.Orm/SqlParam.cs`, `Artisan.Orm/SqlCommandExtensions_Parameters.cs`, `Artisan.Orm/DataTableExtensions.cs` |
| Graph joining | `Artisan.Orm/MergeJoinExtensions.cs`, `Tests/DAL/GrandRecords/Repository.cs` |
| Working graph persistence | `Tests/DAL/GrandRecords/`, `Database/5. Procedures/GrandRecords/SaveGrandRecords.sql` |
| Folder hierarchy and SQL regressions | 4.1.0 fixes only `dbo.RecalcSubFolderHids` and `dbo.GetHidCode`; `dbo.SaveFolder` and the existing procedure parameters are unchanged. See [folder scope](references/reading-and-mapping.md#current-folder-sample-sql). |
| DataReply wire format | `Artisan.Orm/DataReply.cs`, `Artisan.Orm/DataReplyState.cs`, `Artisan.Orm/DataReplyMessage.cs`, `Tests/Tests/DataReplyTest.cs` |

Additional source pointers and complete examples are in the task references above. Use their API families to find an operation; this entrypoint does not enumerate every overload.

## Verify the change

Build the affected project against its actual target frameworks. Compile mapper examples; then exercise the relevant read/write contract with representative SQL data when a test database is available. Compilation alone does not establish that reader ordinals, SQL types, result-set ordering, or UDTT schemas agree.

For mapper failures, inspect result metadata (`GetName`, `GetDataTypeName`, `GetFieldType`) in a local test or debugger, or use `sp_describe_first_result_set` when it can describe the query. Do not introduce an HTTP endpoint that executes arbitrary procedure names for this purpose.

For library maintenance in this repository:

- Check mapper diagnostics using `Artisan.Orm.Analyzers.Tests` and the documented contract in `docs/analyzers.md`. The analyzer checks mapping availability/signatures, not a live SQL schema.
- The `Tests` project uses a seeded SQL Server database and modifies shared fixtures. Run its .NET 8 and .NET 10 targets sequentially against the same database.
- A complete `Artisan.sln` build includes an SSDT SQL project and needs Visual Studio MSBuild with SQL build targets; a library-only `dotnet build` does not validate the entire solution.
- SQL fixes need the matching objects in the test database. A build does not deploy them. Review deployment scope: the sample post-deployment folder generator truncates folder data, so do not use a full publish as a routine substitute for updating individual test objects.

Report what was actually built or exercised and any missing database/schema evidence. Claim compilation only when an actual build result supports it; otherwise say the example was not compiled. Request consumer examples only when application-specific conventions cannot be inferred from available code.

## Evaluate skill changes

Use [validation scenarios](evals/evals.json) when maintaining this skill; they are not prerequisites for application tasks. Each case supplies project context, a user prompt, an expected applicability decision, and observable assertions.

Follow the [evaluation protocol](evals/running-evals.md) for isolated sessions, negative selection cases, evidence, and repeat reporting. Derive case counts from the current JSON; a previous run of 14 scenarios does not cover later additions. Reviewing prompts or compiling examples alone does not establish Claude's actual selection behavior.
