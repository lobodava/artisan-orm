param(
    [string] $PackagePath,
    [string[]] $Frameworks = @('net8.0', 'net10.0'),
    [string] $ConsumerSdk
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$runId = [Guid]::NewGuid().ToString('N')
$work = Join-Path $repo "artifacts/analyzer-package-tests/$runId"
New-Item -ItemType Directory -Path $work -Force | Out-Null

if (-not $PackagePath) {
    [xml] $project = Get-Content -LiteralPath (Join-Path $repo 'Artisan.Orm/Artisan.Orm.csproj') -Raw
    $version = "$($project.Project.PropertyGroup.Version | Where-Object { $_ })-analyzers-test.$runId"
    & dotnet pack (Join-Path $repo 'Artisan.Orm/Artisan.Orm.csproj') -c Release --nologo -v minimal `
        "-p:PackageVersion=$version" -p:GeneratePackageOnBuild=false -o $work
    if ($LASTEXITCODE -ne 0) { throw 'Package build failed.' }
    $PackagePath = Join-Path $work "Artisan.ORM.$version.nupkg"
}
$PackagePath = (Resolve-Path -LiteralPath $PackagePath).Path

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead($PackagePath)
try {
    $paths = @($zip.Entries | ForEach-Object { $_.FullName })
    if (@($paths | Where-Object { $_ -eq 'analyzers/dotnet/cs/Artisan.Orm.Analyzers.dll' }).Count -ne 1) {
        throw 'Package must contain exactly one analyzer DLL at the NuGet analyzer path.'
    }
    if ($paths -match '^lib/.*/Artisan.Orm.Analyzers.dll$|Microsoft.CodeAnalysis.*\.dll$') {
        throw 'Analyzer or Roslyn dependencies leaked into runtime assets.'
    }
    foreach ($target in @('netstandard2.1', 'net8.0', 'net10.0')) {
        foreach ($extension in @('dll', 'xml')) {
            if ($paths -notcontains "lib/$target/Artisan.Orm.$extension") {
                throw "Package is missing $target library/documentation assets."
            }
        }
    }
    foreach ($asset in @('README.md', 'LICENSE.txt', 'Logo.png')) {
        if ($paths -notcontains $asset) { throw "Package is missing $asset." }
    }
    $nuspec = $zip.Entries | Where-Object { $_.FullName -like '*.nuspec' } | Select-Object -First 1
    $reader = [IO.StreamReader]::new($nuspec.Open())
    try { [xml] $manifest = $reader.ReadToEnd() } finally { $reader.Dispose() }
    $version = $manifest.package.metadata.version
    if ($manifest.OuterXml -match 'dependency id="(Artisan.Orm.Analyzers|Microsoft.CodeAnalysis)') {
        throw 'Analyzer dependencies must not be exposed as package dependencies.'
    }
} finally { $zip.Dispose() }

$localFeed = Join-Path $work 'feed'
$packages = Join-Path $work 'packages'
New-Item -ItemType Directory -Path $localFeed, $packages | Out-Null
Copy-Item -LiteralPath $PackagePath -Destination $localFeed
$feed = [Security.SecurityElement]::Escape($localFeed)
$packageHash = (Get-FileHash -LiteralPath $PackagePath -Algorithm SHA256).Hash
$results = [Collections.Generic.List[object]]::new()
@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources><clear /><add key="local" value="$feed" /><add key="nuget" value="https://api.nuget.org/v3/index.json" /></packageSources>
  <fallbackPackageFolders><clear /></fallbackPackageFolders>
  <packageSourceMapping>
    <clear />
    <packageSource key="local"><package pattern="Artisan.ORM" /></packageSource>
    <packageSource key="nuget"><package pattern="*" /></packageSource>
  </packageSourceMapping>
</configuration>
"@ | Set-Content -LiteralPath (Join-Path $work 'NuGet.Config') -Encoding UTF8
if ($ConsumerSdk) {
    @{ sdk = @{ version = $ConsumerSdk; rollForward = 'disable' } } | ConvertTo-Json |
        Set-Content -LiteralPath (Join-Path $work 'global.json') -Encoding UTF8
}

$prefix = @'
using System;
using System.Data;
using System.Collections.Generic;
using Artisan.Orm;
using Microsoft.Data.SqlClient;
public class User { public int Id { get; set; } }
'@
$cases = @(
    @{ Name = 'missing'; Error = 'ART001'; Source = 'public class C { void Run(SqlCommand c) { c.ReadTo<User>(); } }' },
    @{ Name = 'required'; Error = 'ART002'; Source = '[MapperFor(typeof(User), RequiredMethod.AllMain)] public static class M { public static User CreateObject(SqlDataReader r) => new User(); }' },
    @{ Name = 'signature'; Error = 'ART003'; Source = '[MapperFor(typeof(User))] public static class M { public static string CreateObject(SqlDataReader r) => ""; }' },
    @{ Name = 'optional'; Error = 'ART006'; Source = '[MapperFor(typeof(User))] public static class M {} public class C { void Run(SqlCommand c) { c.ReadToObjectRow<User>(); } }' },
    @{ Name = 'valid'; Error = ''; Source = '[MapperFor(typeof(User), RequiredMethod.CreateObject)] public static class M { public static User CreateObject(SqlDataReader r) => new User(); } public class C { void Run(SqlCommand c) { c.ReadTo<User>(); c.ReadAs<User>(); c.ReadTo<int>(); c.ReadTo(r => new { Id = 1 }); } }' },
    @{ Name = 'external'; Error = ''; External = $true; Source = 'public class C { void Run(SqlCommand c) { c.ReadTo<User>(); } }' }
)

foreach ($framework in $Frameworks) {
    foreach ($case in $cases) {
        $directory = Join-Path $work "$framework-$($case.Name)"
        New-Item -ItemType Directory -Path $directory | Out-Null
        @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>$framework</TargetFramework></PropertyGroup>
  <ItemGroup><PackageReference Include="Artisan.ORM" Version="$version" /></ItemGroup>
</Project>
"@ | Set-Content -LiteralPath (Join-Path $directory 'Consumer.csproj') -Encoding UTF8
        ($prefix + "`n" + $case.Source) | Set-Content -LiteralPath (Join-Path $directory 'Consumer.cs') -Encoding UTF8
        if ($case.External) {
            "root = true`n[*.cs]`nartisan_orm.allow_external_mappers = true" |
                Set-Content -LiteralPath (Join-Path $directory '.editorconfig') -Encoding UTF8
        }
        Push-Location $directory
        try {
            $output = & dotnet build Consumer.csproj --nologo -v minimal -p:PreferredUILang=en "-p:RestorePackagesPath=$packages" -p:RestoreFallbackFolders= 2>&1
            $code = $LASTEXITCODE
        } finally { Pop-Location }
        $text = $output -join "`n"
        $output | Set-Content -LiteralPath (Join-Path $directory 'build.log') -Encoding UTF8
        if (($case.Error -and ($code -eq 0 -or $text -notmatch "error $($case.Error):")) -or
            (-not $case.Error -and $code -ne 0) -or $text -match 'AD0001|CS8032|CS9057') {
            throw "Package case $framework/$($case.Name) failed:`n$text"
        }
        $results.Add(@{ Framework = $framework; Case = $case.Name; ExpectedDiagnostic = $case.Error; Passed = $true })
        Write-Host "PASS $framework/$($case.Name)"
    }

    # A fresh process isolates MappingManager's one-shot registry. No database is needed.
    foreach ($missing in @('CreateDataTable', 'CreateDataRow')) {
        $directory = Join-Path $work "$framework-runtime-$missing"
        New-Item -ItemType Directory -Path $directory | Out-Null
        @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>$framework</TargetFramework><OutputType>Exe</OutputType></PropertyGroup>
  <ItemGroup><PackageReference Include="Artisan.ORM" Version="$version" /></ItemGroup>
</Project>
"@ | Set-Content -LiteralPath (Join-Path $directory 'Consumer.csproj') -Encoding UTF8
        $otherMethod = if ($missing -eq 'CreateDataTable') {
            'public static object[] CreateDataRow(User u) => new object[0];'
        } else { 'public static DataTable CreateDataTable() => new DataTable();' }
        ($prefix + "`n" + @"
[MapperFor(typeof(User), RequiredMethod.AllMain)]
public static class M {
  public static User CreateObject(SqlDataReader r) => new User();
  $otherMethod
}
public class Program {
  public static int Main() {
    try { MappingManager.GetCreateObjectFunc<User>(); return 1; }
    catch (ArtisanMappingException e) { return e.Message.Contains("$missing") ? 0 : 2; }
  }
}
"@) | Set-Content -LiteralPath (Join-Path $directory 'Consumer.cs') -Encoding UTF8
        Push-Location $directory
        try {
            $output = & dotnet build Consumer.csproj --nologo -v quiet -p:RunAnalyzers=false "-p:RestorePackagesPath=$packages" -p:RestoreFallbackFolders= 2>&1
            $output | Set-Content -LiteralPath (Join-Path $directory 'build.log') -Encoding UTF8
            if ($LASTEXITCODE -ne 0) { throw "Runtime fixture build failed: $output" }
            & dotnet "bin/Debug/$framework/Consumer.dll"
            if ($LASTEXITCODE -ne 0) { throw "Runtime AllMain validation failed for $missing." }
        } finally { Pop-Location }
        $results.Add(@{ Framework = $framework; Case = "runtime-$missing"; Passed = $true })
        Write-Host "PASS $framework/runtime-$missing"
    }
}
$cachedPackage = Join-Path $packages "artisan.orm/$($version.ToLowerInvariant())/artisan.orm.$($version.ToLowerInvariant()).nupkg"
if ((Get-FileHash -LiteralPath $cachedPackage -Algorithm SHA256).Hash -ne $packageHash) {
    throw 'Consumer restore did not use the exact supplied package.'
}
@{ Package = $PackagePath; Version = $version; Sha256 = $packageHash; ExactPackageVerified = $true; Cases = $results } |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $work 'verification.json') -Encoding UTF8
Write-Host "Analyzer package checks passed. Logs: $work"
