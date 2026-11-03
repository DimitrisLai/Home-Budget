[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Set-Location (Split-Path $PSScriptRoot -Parent)
$root = (Get-Location).Path
$version = (Get-Content -LiteralPath 'VERSION' -Raw).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'VERSION must contain a stable three-part version.' }
$assemblyVersion = "$version.0"
$program = Get-Content -LiteralPath 'Source/Program.cs' -Raw
foreach ($attribute in @('AssemblyVersion', 'AssemblyFileVersion')) {
    if ($program -notmatch ($attribute + '\("' + [regex]::Escape($assemblyVersion) + '"\)')) {
        throw "$attribute does not match VERSION."
    }
}
$manifest = [xml](Get-Content -LiteralPath 'Source/app.manifest' -Raw)
if ($manifest.assembly.assemblyIdentity.version -ne $assemblyVersion) { throw 'Manifest version does not match VERSION.' }

$temporary = if ($env:RUNNER_TEMP) { $env:RUNNER_TEMP } else { [IO.Path]::GetTempPath() }
$work = Join-Path $temporary ('budgetdesk-release-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
$referenceZip = Join-Path $work 'net45-reference-assemblies.zip'
Invoke-WebRequest -Uri 'https://api.nuget.org/v3-flatcontainer/microsoft.netframework.referenceassemblies.net45/1.0.3/microsoft.netframework.referenceassemblies.net45.1.0.3.nupkg' -OutFile $referenceZip
$referenceHash = '23a9f94ea3e2cb88cd8341af75b811c6fb5cb82516fc696e95ed4620279128e3'
if ((Get-FileHash -LiteralPath $referenceZip -Algorithm SHA256).Hash.ToLowerInvariant() -ne $referenceHash) {
    throw 'Microsoft .NET Framework reference package checksum mismatch.'
}
Expand-Archive -LiteralPath $referenceZip -DestinationPath (Join-Path $work 'references')
$referenceFolder = Join-Path $work 'references/build/.NETFramework/v4.5'
$csc = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (-not (Test-Path -LiteralPath $csc)) { $csc = Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319/csc.exe' }
if (-not (Test-Path -LiteralPath $csc)) { throw 'Windows .NET Framework C# compiler is missing.' }
$references = @('mscorlib.dll', 'System.dll', 'System.Core.dll', 'System.Drawing.dll', 'System.Windows.Forms.dll', 'System.Web.Extensions.dll', 'System.Xml.dll')
$common = @('/nologo', '/noconfig', '/nostdlib+', '/warn:4', '/warnaserror+', '/langversion:5', '/codepage:65001', '/platform:anycpu', '/optimize+')
foreach ($reference in $references) {
    $path = Join-Path $referenceFolder $reference
    if (-not (Test-Path -LiteralPath $path)) { throw "Reference assembly missing: $reference" }
    $common += "/reference:$path"
}
$sources = @(Get-ChildItem -LiteralPath 'Source' -Filter '*.cs' | Sort-Object Name | ForEach-Object FullName)

$staging = Join-Path $work 'package'
$package = Join-Path $staging 'BudgetDesk'
New-Item -ItemType Directory -Path $package | Out-Null
$tracked = @(& git ls-files)
if ($LASTEXITCODE -ne 0 -or $tracked.Count -eq 0) { throw 'Cannot enumerate tracked source files.' }
foreach ($relative in $tracked) {
    if ($relative -match '(?i)(\.exe|\.dll|\.zip|\.json)$') { throw "Unexpected generated or data file tracked in source: $relative" }
    $destination = Join-Path $package $relative
    New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $root $relative) -Destination $destination
}
$executable = Join-Path $package 'BudgetDesk.exe'
& $csc @common '/target:winexe' '/main:BudgetDesk.Program' '/win32manifest:Source/app.manifest' '/win32icon:Source/BudgetDesk.ico' "/out:$executable" @sources
if ($LASTEXITCODE -ne 0) { throw 'Windows application compilation failed.' }
if ([Diagnostics.FileVersionInfo]::GetVersionInfo($executable).FileVersion -ne $assemblyVersion) { throw 'Compiled executable version is incorrect.' }

$checks = Join-Path $work 'checks'
New-Item -ItemType Directory -Path $checks | Out-Null
function Run-CheckedProcess {
    param([string]$Executable, [string[]]$Arguments, [string]$Name, [int]$TimeoutSeconds = 180)
    $stdout = Join-Path $checks "$Name.stdout.txt"
    $stderr = Join-Path $checks "$Name.stderr.txt"
    $start = @{ FilePath = $Executable; PassThru = $true; NoNewWindow = $true; RedirectStandardOutput = $stdout; RedirectStandardError = $stderr }
    if ($Arguments.Count -gt 0) { $start.ArgumentList = $Arguments }
    $process = Start-Process @start
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        throw "$Name exceeded its timeout."
    }
    $process.WaitForExit()
    $exitCode = $process.ExitCode
    $lines = @()
    foreach ($log in @($stdout, $stderr)) {
        if (Test-Path -LiteralPath $log) { $lines += @(Get-Content -LiteralPath $log) }
    }
    $lines | ForEach-Object { Write-Host $_ }
    $lines | Set-Content -LiteralPath (Join-Path $package "Tests/Windows-CI-$Name.txt") -Encoding utf8
    if ($exitCode -ne 0) { throw "$Name failed with exit code $exitCode." }
}
function Build-Test {
    param([string]$Name, [string[]]$TestSources)
    $output = Join-Path $checks "$Name.exe"
    $diagnostics = @(& $csc @common '/target:exe' "/main:$Name" "/out:$output" @TestSources 2>&1)
    $compilerExit = $LASTEXITCODE
    $diagnostics | ForEach-Object { Write-Host $_ }
    if ($compilerExit -ne 0) { throw "$Name compilation failed." }
    return $output
}

$ledger = Build-Test -Name 'LedgerTests' -TestSources @($sources + 'Tests/LedgerTests.cs')
Run-CheckedProcess -Executable $ledger -Arguments @() -Name 'accounting'
$compatibility = Build-Test -Name 'FrameworkCompatibilityTests' -TestSources @('Tests/FrameworkCompatibilityTests.cs')
Run-CheckedProcess -Executable $compatibility -Arguments @('"' + $executable + '"') -Name 'compatibility'
foreach ($name in @('UiSmokeTests', 'QuickEntryTests')) {
    $test = Build-Test -Name $name -TestSources @($sources + "Tests/$name.cs")
    $images = Join-Path $checks "$name-images"
    Run-CheckedProcess -Executable $test -Arguments @('"' + $images + '"') -Name $name
}
foreach ($mode in @('empty', 'demo')) {
    $images = Join-Path $checks "$mode-pages"
    $data = Join-Path $checks "$mode-data"
    $arguments = @('"--capture=' + $images + '"', '"--data-dir=' + $data + '"')
    if ($mode -eq 'demo') { $arguments += '--demo' }
    Run-CheckedProcess -Executable $executable -Arguments $arguments -Name "startup-$mode" -TimeoutSeconds 60
    if (@(Get-ChildItem -LiteralPath $images -Filter '*.png').Count -ne 5) { throw "$mode startup did not render all five pages." }
}
$evidence = @(
    "Budget Desk $version — native Windows release checks",
    "Commit: $env:GITHUB_SHA",
    "Run: https://github.com/$env:GITHUB_REPOSITORY/actions/runs/$env:GITHUB_RUN_ID",
    'Compiled with the Microsoft Windows C# compiler and checksum-verified .NET Framework 4.5 references.',
    'Accounting, executable compatibility, general UI, quick-entry/search and actual empty/demo startup checks passed.',
    'All checks used isolated temporary sample budgets. No budget data or test executables are packaged.',
    ('Executable SHA-256: ' + (Get-FileHash -LiteralPath $executable -Algorithm SHA256).Hash.ToLowerInvariant())
)
$evidence | Set-Content -LiteralPath (Join-Path $package 'Tests/Windows-CI-summary.txt') -Encoding utf8
$evidence | Add-Content -LiteralPath (Join-Path $package 'VALIDATION.md') -Encoding utf8

$manifestLines = @(Get-ChildItem -LiteralPath $package -Recurse -File -Force | Sort-Object FullName | ForEach-Object {
    $relative = [IO.Path]::GetRelativePath($package, $_.FullName).Replace('\', '/')
    ((Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + $relative)
})
[IO.File]::WriteAllLines((Join-Path $package 'SHA256SUMS.txt'), $manifestLines, [Text.UTF8Encoding]::new($false))
foreach ($line in $manifestLines) {
    $digest, $relative = $line -split '  ', 2
    if ((Get-FileHash -LiteralPath (Join-Path $package $relative) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $digest) { throw "Package checksum mismatch: $relative" }
}

$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Path $dist -Force | Out-Null
$zip = Join-Path $dist "BudgetDesk_v${version}_Windows.zip"
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($staging, $zip, [IO.Compression.CompressionLevel]::Optimal, $false)
$archive = [IO.Compression.ZipFile]::OpenRead($zip)
try {
    $entries = @($archive.Entries | Where-Object { -not $_.FullName.EndsWith('/') })
    if ($entries.Count -ne $manifestLines.Count + 1) { throw 'ZIP entry count differs from the checked package.' }
    if (@($entries | Where-Object { $_.FullName -match '(?i)\.(json|dll)$' }).Count -ne 0) { throw 'Runtime data or reference DLLs leaked into the ZIP.' }
    if (@($entries | Where-Object { $_.FullName -match '(?i)\.exe$' }).FullName -ne 'BudgetDesk/BudgetDesk.exe') { throw 'Unexpected executable in release ZIP.' }
} finally { $archive.Dispose() }
$zipHash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText((Join-Path $dist 'SHA256SUMS.txt'), "$zipHash  $([IO.Path]::GetFileName($zip))`n", [Text.UTF8Encoding]::new($false))
Write-Host "Ready: $zip"
