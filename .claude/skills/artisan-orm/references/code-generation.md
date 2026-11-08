# Generating a mapper and SQL templates

Use `Database/Utility Scripts/Artisan Code Generation.sql` when a source checkout is available and an existing table or view is a useful starting point for a mapper. It is a SQL metadata script, not a Roslyn source generator. The packaged analyzer validates C# mapper contracts; it does not generate models or inspect the database.

## Configure and collect the output

Open the script in a query window connected to the intended database. Replace its initial declarations with the application's names, for example:

```sql
DECLARE @NameSpacePrefix sysname = N'ExampleApp.Data';
DECLARE @Schema sysname = N'dbo';
DECLARE @TableName sysname = N'Products';
DECLARE @ObjectName sysname = N'Product';
DECLARE @PluralName sysname = @TableName;
```

These declarations configure the rest of the existing script; running them alone generates nothing. `@TableName` names an existing table or view. The script reads `INFORMATION_SCHEMA` metadata and composes text in table variables. It emits its output through severity-zero `RAISERROR` into the query tool's Messages pane. It does not create the emitted schema objects or modify application rows.

Copy the C# output into an application file and SQL templates into separate reviewable files. The output includes a model, a mapper with four methods and `RequiredMethod.All`, a table type, and view/table-valued-function templates. Generated SQL is not deployed merely by running the generator or installing Artisan.ORM.

With the settings above, the literal names are `ExampleApp.Data.Products.Models.Product`, `ProductMapper`, `new DataTable("ProductTableType")`, and SQL `dbo.ProductTableType`. The `###type###` markers identify unsupported type mappings; they are not part of the generated model or table-type names. Read a collection through `ReadToList<Product>()`; the generic argument is the element type.

If the database or script is unavailable, work from supplied DDL and SELECT contracts instead. Do not invent generated output or claim metadata inspection occurred. The source path identifies a repository utility; it is not a file automatically installed in a consumer project by NuGet.

## Adapt the generated contract

Treat output as a scaffold that needs review against the 4.1.0 API:

- Compare every emitted property with the table/view metadata. The script has a limited SQL-type mapping and emits `###type###` for unsupported entries, including common types such as `uniqueidentifier`. Replace these with the actual CLR type and getter.
- Correct the historical `timestamp` mapping: it emits `GetRowVersion` and `Binary`, which are not the current byte-array API. Choose `byte[]` with `GetBytesFromRowVersion`, or an explicitly agreed Base64/Int64 representation. Do not mix a textual model value with a binary TVP column without conversion.
- Review SQL type declarations, especially `float`: the current formatter applies decimal precision/scale formatting and can produce invalid or missing type text. Preserve the actual database type rather than trusting that text as executable DDL.
- Set a schema-qualified `DataTable.TableName` for the intended UDTT. The generated name is unqualified. Set each `AddColumn` nullability explicitly where needed; its default is nullable even when the generated SQL says `NOT NULL`.
- Check constraints and identifiers. The key-discovery query is not fully schema-qualified; composite keys, same-named tables in different schemas, reserved identifiers, and generated view/function syntax need explicit review.
- Match reader ordinals to the actual SELECT, which can differ from table column order. A hand-written mapper can intentionally transform or omit fields and produce a different TVP shape.
- Keep `RequiredMethod.All` only if all four generated methods are part of the intended contract. Use `AllMain` without compact-row reading, or `CreateObject` for a read-only mapper. Remove unused methods deliberately; do not keep invalid stubs to satisfy the analyzer.

For example, a read-only projection `Id int NOT NULL, Token uniqueidentifier NOT NULL, Version rowversion NOT NULL` can use this complete corrected mapper. The binary concurrency token is kept as bytes; write-side token comparison would be a separate procedure contract.

```csharp
using System;
using Artisan.Orm;
using Microsoft.Data.SqlClient;

public sealed class GeneratedProductView
{
    public int Id { get; set; }
    public Guid Token { get; set; }
    public byte[] Version { get; set; } = Array.Empty<byte>();
}

[MapperFor(typeof(GeneratedProductView), RequiredMethod.CreateObject)]
public static class GeneratedProductViewMapper
{
    public static GeneratedProductView CreateObject(SqlDataReader reader) =>
        new GeneratedProductView
        {
            Id = reader.GetInt32(0),
            Token = reader.GetGuid(1),
            Version = reader.GetBytesFromRowVersion(2)
                ?? throw new InvalidOperationException("Version must not be NULL.")
        };
}
```

Compile the adapted C# with the target package and analyzer. Review and validate the SQL templates separately against the intended schema before deployment. Compilation cannot validate ordinals, UDTT compatibility, or SQL syntax stored inside comments.

## Use the analyzer with local source

An `Artisan.ORM` PackageReference loads the analyzer included in the package, subject to the compiler's analyzer settings. A plain ProjectReference to `Artisan.Orm.csproj` supplies the library but does not propagate its private analyzer reference to the consumer compilation.

For a source consumer, add both references explicitly. This example assumes the consumer project is a sibling of the two library projects; adjust the paths to the actual checkout:

```xml
<ItemGroup>
  <ProjectReference Include="../Artisan.Orm/Artisan.Orm.csproj" />
  <ProjectReference Include="../Artisan.Orm.Analyzers/Artisan.Orm.Analyzers.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false"
                    PrivateAssets="all" />
</ItemGroup>
```

The library reference already supplies its transitive SqlClient dependency. Do not add an arbitrary older direct package pin: this 4.1.0 source requires `Microsoft.Data.SqlClient` 7.1.1, so a direct `5.*` reference causes a restore downgrade (`NU1605`). Inspect the actual dependency graph if the application needs an explicit pin or central package version. Validate the advertised project file as well as C# snippets; replacing its references during validation can hide a broken example.

The analyzer reference is for build/IDE diagnostics, not a runtime dependency. Verify it in an isolated consumer with a declaration such as `[MapperFor(typeof(Probe), RequiredMethod.AllMain)]` whose mapper contains only `CreateObject`: it must report `ART002` for the two missing table methods. A bare `[MapperFor(typeof(Probe))]` without callers or required methods is not that probe. A valid mapper building successfully alone does not prove the analyzer ran. See `docs/analyzers.md` for supported compiler hosts and diagnostics.

For shared read/write mapper declarations see [reading and mapping](reading-and-mapping.md#minimal-shared-model-and-mapper). For rowversion formats and SQL parameter types see [parameters and types](parameters-and-types.md).

Sources: `Database/Utility Scripts/Artisan Code Generation.sql`, `Artisan.Orm/SqlDataReaderExtensions_Get.cs`, `Artisan.Orm/DataTableExtensions.cs`, `Artisan.Orm/MapperForAttribute.cs`, and `docs/analyzers.md`.
