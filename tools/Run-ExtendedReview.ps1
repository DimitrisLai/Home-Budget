[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$ApplicationPackage)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Set-Location (Split-Path $PSScriptRoot -Parent)
$root = (Get-Location).Path
$version = (Get-Content -LiteralPath VERSION -Raw).Trim()
$work = Join-Path $env:RUNNER_TEMP ('budgetdesk-full-review-' + [guid]::NewGuid().ToString('N'))
$results = Join-Path $root 'review-results'
New-Item -ItemType Directory -Path $work, $results -Force | Out-Null
Expand-Archive -LiteralPath (Get-Item -LiteralPath $ApplicationPackage).FullName -DestinationPath (Join-Path $work 'package')
$package = Join-Path $work 'package/BudgetDesk'
foreach ($line in Get-Content -LiteralPath (Join-Path $package 'SHA256SUMS.txt')) {
    $hash, $relative = $line -split '  ', 2
    if ((Get-FileHash -LiteralPath (Join-Path $package $relative) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $hash) { throw "Package checksum differs: $relative" }
}
$names = @(Get-ChildItem Source -File | Sort-Object Name | ForEach-Object Name)
$packagedNames = @(Get-ChildItem (Join-Path $package Source) -File | Sort-Object Name | ForEach-Object Name)
if (Compare-Object $names $packagedNames) { throw 'Packaged source file set differs.' }
foreach ($relative in @($names | ForEach-Object { 'Source/' + $_ }) + @('VERSION', 'BudgetDesk.exe.config')) {
    if ((Get-FileHash -LiteralPath $relative -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath (Join-Path $package $relative) -Algorithm SHA256).Hash) { throw "Current and packaged input differ: $relative" }
}
$app = Join-Path $package 'BudgetDesk.exe'
if ([Diagnostics.FileVersionInfo]::GetVersionInfo($app).FileVersion -ne "$version.0") { throw 'Executable version differs.' }
if ($version -eq '1.0.1' -and (Get-FileHash $app -Algorithm SHA256).Hash.ToLowerInvariant() -ne '2b312bd79f3a835296d8f3f7b0386911a7c449e46f1b3106850de93c968fa7cd') { throw 'Reviewed 1.0.1 executable hash differs.' }

$referenceZip = Join-Path $work 'net45-reference-assemblies.zip'
Invoke-WebRequest -Uri 'https://api.nuget.org/v3-flatcontainer/microsoft.netframework.referenceassemblies.net45/1.0.3/microsoft.netframework.referenceassemblies.net45.1.0.3.nupkg' -OutFile $referenceZip
if ((Get-FileHash $referenceZip -Algorithm SHA256).Hash.ToLowerInvariant() -ne '23a9f94ea3e2cb88cd8341af75b811c6fb5cb82516fc696e95ed4620279128e3') { throw 'Reference package checksum differs.' }
Expand-Archive -LiteralPath $referenceZip -DestinationPath (Join-Path $work references)
$references = Join-Path $work 'references/build/.NETFramework/v4.5'
$csc = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$common = @('/nologo', '/noconfig', '/nostdlib+', '/warn:4', '/warnaserror+', '/langversion:5', '/codepage:65001', '/platform:anycpu', '/optimize+')
foreach ($name in @('mscorlib','System','System.Core','System.Drawing','System.Windows.Forms','System.Web.Extensions','System.Xml')) { $common += '/reference:' + (Join-Path $references ($name + '.dll')) }
$sources = @(Get-ChildItem Source -Filter '*.cs' | Sort-Object Name | ForEach-Object FullName)
$suites = @('LedgerTests','FrameworkCompatibilityTests','UiSmokeTests','QuickEntryTests','StartupSmokeTests','AccountLayoutTests','AccountRefreshTests','ScrollFreeTests','OrderCategoryTests','YearPlanTests','GoalSplitTests')
foreach ($suite in $suites) {
    $test = Join-Path $work ($suite + '.exe')
    $testSources = @((Get-Item -LiteralPath ("Tests/$suite.cs")).FullName)
    if ($suite -notin @('FrameworkCompatibilityTests','StartupSmokeTests')) { $testSources = @($sources + $testSources) }
    & $csc @common '/target:exe' "/main:$suite" "/out:$test" @testSources
    if ($LASTEXITCODE -ne 0) { throw "$suite compilation failed." }
    $arguments = @()
    if ($suite -in @('FrameworkCompatibilityTests','StartupSmokeTests')) { $arguments = @('"' + $app + '"') }
    elseif ($suite -ne 'LedgerTests') { $arguments = @('"' + (Join-Path $results ($suite + '-images')) + '"') }
    $stdout = Join-Path $results ($suite + '.stdout.txt')
    $stderr = Join-Path $results ($suite + '.stderr.txt')
    $start = @{ FilePath=$test; PassThru=$true; NoNewWindow=$true; RedirectStandardOutput=$stdout; RedirectStandardError=$stderr }
    if ($arguments.Count -gt 0) { $start.ArgumentList = $arguments }
    $process = Start-Process @start
    if (-not $process.WaitForExit(180000)) { Stop-Process -Id $process.Id -Force; throw "$suite timed out." }
    $process.WaitForExit()
    Get-Content $stdout, $stderr | ForEach-Object { Write-Host $_ }
    if ($process.ExitCode -ne 0) { throw "$suite failed with exit code $($process.ExitCode)." }
}
$summary = @("Budget Desk $version full Windows review", "Commit: $env:GITHUB_SHA", "Run: https://github.com/$env:GITHUB_REPOSITORY/actions/runs/$env:GITHUB_RUN_ID", ('Executable SHA-256: ' + (Get-FileHash $app -Algorithm SHA256).Hash.ToLowerInvariant()), 'All 11 existing test suites passed. Accounting, persistence, compatibility, startup, layouts, restore, ordering, categories, yearly/planned, goals/splits, templates/payees/history were checked using isolated sample data.')
$summary | Set-Content (Join-Path $results 'summary.txt') -Encoding utf8
$summary | ForEach-Object { Write-Host $_ }
