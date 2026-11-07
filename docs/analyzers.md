# Compile-time mapper validation

The `Artisan.ORM` NuGet package includes `Artisan.Orm.Analyzers`. With a normal C# `PackageReference`, the compiler loads it automatically during `dotnet build`. No additional package, registration, source generator, or change to mapper declarations is needed. The analyzer does not run in the deployed application.

The analyzer targets .NET Standard 2.0 and uses Roslyn 4.8 APIs. The compiler host must provide Roslyn 4.8 or later (for example, .NET SDK 8.0.100 or later). This requirement concerns the compiler/IDE, independently of the application's target framework. Normal MSBuild analyzer settings still apply; explicitly disabling analyzers disables these checks.

Diagnostics run during both builds and live document analysis. Visual Studio can show them in
the editor and in the **Build + IntelliSense** Error List without enabling full-solution background
analysis. Mapper registrations from other source files and referenced assemblies are still considered.

## Diagnostics

All rules below have default severity **Error** and stop the build.

| ID | Meaning | Fix |
| --- | --- | --- |
| ART001 | A mapper required by an Artisan operation was not found in the compilation or referenced assemblies. | Add `[MapperFor(typeof(T))]`, supply an inline function, or use the corresponding `As` operation. |
| ART002 | A method required by `RequiredMethod` cannot be found by runtime lookup. | Add a public method with the expected name and compatible parameters. Delegate fields/properties do not satisfy a method requirement. |
| ART003 | A discovered method cannot bind to the runtime delegate. | Correct its static/non-generic declaration, parameters, and return type. |
| ART004 | Multiple mapper classes register the same entity type. | Keep one registration, including across referenced assemblies and inherited attributes. |
| ART005 | Reflection cannot select a mapper method unambiguously. | Remove ambiguous overloads. `CreateDataTable` is looked up by name alone. |
| ART006 | An operation requires a function that its mapper cannot provide. | Supply the named function even if it is optional in `RequiredMethod`. Referenced mappers with unusable signatures are also reported at the call site. |
| ART007 | Registration has an invalid target, open generic mapper class, null requirements array, or undefined `RequiredMethod` value. | Supply a closed usable target and valid requirements on a mapper without unbound type parameters. |

An example:

```csharp
[MapperFor(typeof(User), RequiredMethod.CreateObject)]
public static class UserMapper
{
    public static Role CreateObject(SqlDataReader reader) => new Role();
}
```

This reports `ART003`: the method's return type must be compatible with `User`.

## Mapper contract

The conventional signatures for `[MapperFor(typeof(User))]` are:

```csharp
public static User CreateObject(Microsoft.Data.SqlClient.SqlDataReader reader);
public static ObjectRow CreateObjectRow(Microsoft.Data.SqlClient.SqlDataReader reader);
public static DataTable CreateDataTable();
public static object[] CreateDataRow(User value);
```

Checks follow reflection lookup and delegate compatibility. For example, an `IDataRecord` parameter can accept the SQL reader, and a derived reference return type can satisfy a base return type. CLR delegate compatibility between enums and their underlying types is also accounted for. Boxing and user-defined conversions do not make a delegate signature compatible. The mapper class itself need not be static if its mapper methods are static. Inherited static methods are not found by the runtime's default `GetMethod` lookup.

Methods are required according to the union of the attribute's `params RequiredMethod[]` values:

| Value | Required methods |
| --- | --- |
| `All` | All four methods |
| `AllMain` | `CreateObject`, `CreateDataTable`, `CreateDataRow` |
| `BothForObject` | `CreateObject`, `CreateObjectRow` |
| `BothForDataTable` | `CreateDataTable`, `CreateDataRow` |
| `CreateObject` | `CreateObject` |
| `CreateObjectRow` | `CreateObjectRow` |

Omitting the second argument imposes no presence requirements by itself. However, any method selected by runtime reflection must have a compatible signature, and a call can independently require a function. An unrelated overload that reflection would ignore is not an error just because its name resembles a mapper method.

`AllMain` now enforces the three methods documented in the Wiki in both runtime and analyzer checks. Earlier runtime code only enforced `CreateObject` for this value.

## Which calls are checked

The analyzer identifies Artisan methods by their resolved symbols, declaring types, and overloads. It checks calls and method-group references on `RepositoryBase`, `SqlCommandExtensions`, `SqlDataReaderExtensions`, `SqlConnectionExtensions`, and `DataTableExtensions`:

- Registered-mapper overloads of `ReadTo*`, including async, streaming, tree, dictionary, and multiple-result operations.
- `ReadToObjectRow<T>` / `ReadToObjectRows<T>` require `CreateObjectRow`.
- Dictionary operations check `TValue`; multiple-result operations check every result type.
- `ToDataTable`, mapper-based `AddTableParam` / `AddTableRowParam`, and `BulkCopy` / `BulkCopyAsync` require both DataTable functions.
- Direct `MappingManager.GetCreateObjectFunc<T>` and `GetCreateObjectRowFunc<T>` calls.

`As` operations, inline-mapper overloads, and scalar read paths do not require a registered mapper. A repository's `Action<SqlCommand>` argument configures the command and is not an inline mapper. Nullable/Try-style `MappingManager` getters are allowed without a registration.

Scalar exemptions reproduce `TypeExtensions.IsSimpleType`. Nullable and non-nullable enums
both use numeric scalar conversion and do not require a registered mapper for scalar reads,
including dictionary values. Explicit generic TVP calls do not get the special non-generic
integer-list overload's exemption; a scalar read exemption does not supply TVP write functions.

## Scope and runtime-only mappers

The analyzer inspects the current assembly and referenced assemblies that reference Artisan, including nested and internal mapper types available in metadata. Partial declarations are one registration. The inherited `MapperFor` attribute is accounted for. A generated declaration visible to the compilation can also supply a mapper.

This is a compile-time view, not a guarantee about the runtime assembly load order. Runtime reflection validation is retained. The analyzer does not execute mapping code, inspect a live database, or prove that SQL result columns and values match the mapper body.

The initial implementation does not propagate requirements through arbitrary user-defined generic wrappers. An invocation whose entity still contains type parameters is deferred to runtime. `dynamic`, reflection-driven calls, mapper assemblies absent from compiler references, and custom subclasses of `MapperForAttribute` whose constructors compute registrations cannot be resolved generally.

If a project intentionally supplies mappers from runtime-only assemblies or a downstream host, opt out of **ART001 only** in `.editorconfig`:

```ini
[*.cs]
artisan_orm.allow_external_mappers = true
```

The setting can be restricted to selected files with an EditorConfig path pattern. All other checks remain active. For a single exceptional call, `#pragma warning disable ART001` / `restore` can document a local exception.

## Language

English is the neutral/fallback language. Titles, message formats, descriptions, and registration explanations are stored in `AnalyzerResources.resx` and accessed with `LocalizableResourceString`.

To add a translation, add `AnalyzerResources.<culture>.resx` with matching resource keys. The package target includes the resulting resource assemblies in culture subdirectories next to the analyzer DLL. The first release includes English only. Diagnostic IDs and code identifiers are never translated.

To request English build messages:

```shell
dotnet build -p:PreferredUILang=en
```

## Development and verification

The analyzer has no runtime reference to `Artisan.Orm`; it inspects compiler symbols. Its tests reference the real Artisan and SqlClient assemblies and exercise the Roslyn 4.8 host, including comparisons against actual reflection/delegate binding.

```shell
dotnet test Artisan.Orm.Analyzers.Tests/Artisan.Orm.Analyzers.Tests.csproj
```

`eng/Verify-AnalyzerPackage.ps1` builds an isolated prerelease package, checks its layout, and creates external consumers with only an `Artisan.ORM` package reference. It verifies successful and failed builds, English messages, the external-mapper setting, and runtime `AllMain` failures with analyzers disabled. It needs .NET SDK 10.0.403 (or a newer 10.0.4xx patch, as specified in `global.json`) and the .NET 8/10 runtimes; no database is needed. SourceLink comes from the SDK. Output and logs stay under ignored `artifacts/`.

```powershell
./eng/Verify-AnalyzerPackage.ps1
# Optionally use an already-built package or a particular installed consumer SDK:
./eng/Verify-AnalyzerPackage.ps1 -PackagePath <package.nupkg> -ConsumerSdk 9.0.318 -Frameworks net8.0
```

In this solution, project references explicitly attach the analyzer to Artisan and its existing test project. A direct project reference to the runtime library does not itself deliver the NuGet analyzer assets.
