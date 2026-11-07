# Preparing and publishing a release

The library targets `netstandard2.1`, `net8.0`, and `net10.0`. Runtime and analyzer tests run
on .NET 8 and .NET 10. Package creation does not publish a package or deploy sample SQL.

## Prepare the source

1. Set `Version` and `PackageReleaseNotes` in `Artisan.Orm/Artisan.Orm.csproj`.
2. Finalize the matching CHANGELOG section, including compatibility changes. Replace
   `Pending release` with the actual release date when publishing, not during preparation.
3. Record analyzer rules in `Artisan.Orm.Analyzers/AnalyzerReleases.Shipped.md` under the
   release version. Keep subsequent rules in `AnalyzerReleases.Unshipped.md`.
4. Review the README, articles, wiki, and `.claude/skills/artisan-orm` against the same source.
   Keep `.claude/settings.local.json` and generated evaluation artifacts out of Git.
5. Commit the reviewed source before the final release build so SourceLink and repository
   metadata identify the exact published code. A package built from a dirty checkout is
   useful for verification but should not be the publication artifact.

## Validate the solution

Use the SDK pinned in `global.json` and Visual Studio MSBuild with the SSDT targets installed.
The SQL project is part of the solution; a library-only `dotnet build` is not a full build.

```powershell
# Run from the repository root; use the installed Visual Studio MSBuild path.
& $msbuild Artisan.sln /restore /t:Rebuild /p:Configuration=Release /m /verbosity:minimal
if ($LASTEXITCODE -ne 0) { throw 'Solution rebuild failed.' }

foreach ($framework in @('net8.0', 'net10.0')) {
    foreach ($project in @('Tests/Tests.csproj', 'Artisan.Orm.Analyzers.Tests/Artisan.Orm.Analyzers.Tests.csproj')) {
        dotnet test $project -c Release -f $framework --no-build --no-restore
        if ($LASTEXITCODE -ne 0) { throw "Tests failed: $project / $framework" }
    }
}
```

The runtime tests need the configured, seeded SQL Server test database. They modify shared
fixtures, so run framework targets sequentially. Deploy reviewed changes to individual SQL
objects before their regression tests. A full database publish executes a post-deployment
folder generator that resets sample data; it is not a routine test preparation step.

NuGet auditing covers direct and transitive dependencies, with known-vulnerability warnings
treated as errors through `Directory.Build.props`. Review the audit output for all four C#
projects and investigate any failure rather than suppressing it for the release.

## Build and inspect the actual package

```powershell
dotnet pack Artisan.Orm/Artisan.Orm.csproj -c Release --no-restore `
    -p:GeneratePackageOnBuild=false -o artifacts/release-package
if ($LASTEXITCODE -ne 0) { throw 'Package creation failed.' }

./eng/Verify-AnalyzerPackage.ps1 -PackagePath artifacts/release-package/Artisan.ORM.4.1.0.nupkg
```

The verification script creates a separate package directory and local feed for every run,
maps Artisan.ORM to that feed, and compares the restored archive's SHA-256 hash with the
supplied archive. It exercises expected analyzer failures, valid consumers, external mapper
configuration, and runtime `AllMain` validation on both application target frameworks.

Inspect the final `.nupkg` and matching `.snupkg`: version, target-specific dependency minimums,
all three library/XML asset pairs, README/license/icon, the analyzer under `analyzers/dotnet/cs`,
portable symbols, and correct source metadata. The package must not contain tests, settings,
credentials, or Roslyn runtime dependencies. Keep logs and package hashes with the release evidence.

## Publish NuGet and wiki separately

Publishing requires an authorized NuGet API key. Supply it through the local environment or
secret store; do not save it in this repository. Keep matching `.nupkg` and `.snupkg` files together.
With the release package verified, publish once:

```powershell
dotnet nuget push artifacts/release-package/Artisan.ORM.4.1.0.nupkg `
    --source https://api.nuget.org/v3/index.json --api-key $env:NUGET_API_KEY
```

The matching symbol package is pushed with the package. Verify the public package's metadata
and symbols after NuGet processing. See [dotnet nuget push](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-nuget-push)
and [symbol packages](https://learn.microsoft.com/en-us/nuget/create-packages/symbol-packages-snupkg).

The wiki is a separate Git repository, `https://github.com/lobodava/artisan-orm.wiki.git`.
Review and commit its intended pages, then push its default branch (`master`). Pushing the
library repository does not update the wiki. See [GitHub wiki editing](https://docs.github.com/en/communities/documenting-your-project-with-wikis/adding-or-editing-wiki-pages).

The current Windows wiki checkout uses indexed pages with `skip-worktree` because some historic
filenames cannot be represented as ordinary Windows paths. Review the Git index and the current
safe-name mirror under `_tools`; do not run old sprint import scripts over newer staged content.
Publish the reviewed index, without discarding earlier staged changes or adding `_tools` artifacts.

After publication, update pending-release wording and link the actual release. The Claude skill
evaluation report is separate evidence about assistant behavior; it does not replace library,
SQL, analyzer, or package tests.
