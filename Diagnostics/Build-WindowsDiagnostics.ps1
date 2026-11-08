[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Set-Location (Split-Path $PSScriptRoot -Parent)
$root = (Get-Location).Path
$work = Join-Path $env:RUNNER_TEMP ('budgetdesk-diagnostics-' + [guid]::NewGuid().ToString('N'))
$package = Join-Path $work 'executables'
$checks = Join-Path $work 'checks'
New-Item -ItemType Directory -Path $package, $checks | Out-Null

$referenceZip = Join-Path $work 'net45-references.zip'
Invoke-WebRequest -Uri 'https://api.nuget.org/v3-flatcontainer/microsoft.netframework.referenceassemblies.net45/1.0.3/microsoft.netframework.referenceassemblies.net45.1.0.3.nupkg' -OutFile $referenceZip
if ((Get-FileHash -LiteralPath $referenceZip -Algorithm SHA256).Hash.ToLowerInvariant() -ne '23a9f94ea3e2cb88cd8341af75b811c6fb5cb82516fc696e95ed4620279128e3') { throw 'Reference package checksum mismatch.' }
Expand-Archive -LiteralPath $referenceZip -DestinationPath (Join-Path $work 'references')
$referenceFolder = Join-Path $work 'references/build/.NETFramework/v4.5'
$csc = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (-not (Test-Path -LiteralPath $csc)) { throw 'Microsoft Windows C# compiler is missing.' }
$common = @('/nologo', '/noconfig', '/nostdlib+', '/warn:4', '/warnaserror+', '/langversion:5', '/codepage:65001', '/platform:anycpu', '/optimize+')
foreach ($name in @('mscorlib.dll', 'System.dll', 'System.Core.dll', 'System.Drawing.dll', 'System.Windows.Forms.dll', 'System.Web.Extensions.dll', 'System.Xml.dll')) {
    $common += '/reference:' + (Join-Path $referenceFolder $name)
}
$program = Get-Content -LiteralPath 'Source/Program.cs' -Raw
$entry = 'private static int Main(string[] args)'
if ([regex]::Matches($program, [regex]::Escape($entry)).Count -ne 1) { throw 'Production Main could not be isolated unambiguously.' }
$generatedProgram = Join-Path $work 'Program.TestEntry.cs'
[IO.File]::WriteAllText($generatedProgram, $program.Replace($entry, 'internal static int Main(string[] args)'), [Text.UTF8Encoding]::new($false))
$metadata = "using System.Reflection;`n" + ((@($program -split '\r?\n' | Where-Object { $_.StartsWith('[assembly: ') })) -join "`n") + "`n"
$metadataPath = Join-Path $work 'SharedAssemblyInfo.cs'
[IO.File]::WriteAllText($metadataPath, $metadata, [Text.UTF8Encoding]::new($false))
$variants = @(
    @{ Id = '01_Window'; Symbol = 'DIAG_WINDOW'; Sources = @(); Title = 'Απλό παράθυρο'; Detail = 'Μόνο το κοινό διαγνωστικό παράθυρο, χωρίς κώδικα προϋπολογισμού.' },
    @{ Id = '02_Model'; Symbol = 'DIAG_MODEL'; Sources = @('Domain.cs'); Title = 'Λογική υπολογισμών'; Detail = 'Προσθέτει το πραγματικό μοντέλο λογαριασμών και υπολογισμών, με δείγματα στη μνήμη.' },
    @{ Id = '03_Storage'; Symbol = 'DIAG_STORAGE'; Sources = @('Domain.cs', 'Storage.cs'); Title = 'Αποθήκευση JSON'; Detail = 'Προσθέτει αποθήκευση και επαναφόρτωση δειγμάτων σε προσωρινό φάκελο.' },
    @{ Id = '04_Demo_UI'; Symbol = 'DIAG_UI'; Sources = @('Domain.cs', 'Storage.cs', 'Layouts.cs', 'Ui.cs', 'QuickEntry.cs'); Title = 'Πλήρες περιβάλλον με δείγματα'; Detail = 'Προσθέτει όλες τις καρτέλες και φόρμες. Ανοίγει απευθείας με προσωρινά δεδομένα.' },
    @{ Id = '05_Full_Startup'; Symbol = 'DIAG_STARTUP'; Sources = @('Domain.cs', 'Storage.cs', 'Layouts.cs', 'Ui.cs', 'QuickEntry.cs', 'StartupDiagnostics.cs'); Title = 'Πλήρης εκκίνηση με δείγματα'; Detail = 'Προσθέτει την κανονική εκκίνηση, το κλείδωμα και τη διάγνωση σφαλμάτων, με προσωρινά δεδομένα και logs.' }
)
$records = @()
foreach ($variant in $variants) {
    $directory = Join-Path $package $variant.Id
    New-Item -ItemType Directory -Path $directory | Out-Null
    $output = Join-Path $directory 'BudgetDesk.exe'
    $sources = @((Get-Item -LiteralPath 'Diagnostics/DiagnosticProgram.cs').FullName)
    foreach ($name in $variant.Sources) { $sources += (Get-Item -LiteralPath (Join-Path 'Source' $name)).FullName }
    if ($variant.Symbol -eq 'DIAG_STARTUP') { $sources += $generatedProgram } else { $sources += $metadataPath }
    & $csc @common '/target:winexe' '/main:DiagnosticProgram' ("/define:" + $variant.Symbol) '/win32manifest:Source/app.manifest' '/win32icon:Source/BudgetDesk.ico' ("/out:" + $output) @sources
    if ($LASTEXITCODE -ne 0) { throw "Compilation failed: $($variant.Id)" }
    if ([Diagnostics.FileVersionInfo]::GetVersionInfo($output).FileVersion -ne '1.0.1.0') { throw 'Diagnostic version metadata changed.' }
    $records += [ordered]@{ id = $variant.Id; path = $variant.Id + '/BudgetDesk.exe'; title = $variant.Title; detail = $variant.Detail; bytes = (Get-Item -LiteralPath $output).Length; sha256 = (Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash.ToLowerInvariant(); application_sources = @($variant.Sources) }
}

function Invoke-Check {
    param([string]$Name, [string]$Source, [string]$Argument)
    $test = Join-Path $checks ($Name + '.exe')
    $absolute = (Get-Item -LiteralPath $Source).FullName
    & $csc @common '/target:exe' ("/main:" + $Name) ("/out:" + $test) $absolute
    if ($LASTEXITCODE -ne 0) { throw "$Name compilation failed." }
    $stdout = Join-Path $checks ($Name + '.stdout.txt')
    $stderr = Join-Path $checks ($Name + '.stderr.txt')
    $process = Start-Process -FilePath $test -ArgumentList @('"' + $Argument + '"') -PassThru -NoNewWindow -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    if (-not $process.WaitForExit(180000)) { Stop-Process -Id $process.Id -Force; throw "$Name timed out." }
    $process.WaitForExit()
    $text = (Get-Content -LiteralPath $stdout -Raw) + (Get-Content -LiteralPath $stderr -Raw)
    Write-Host $text
    if ($process.ExitCode -ne 0) { throw "$Name failed." }
    return $text
}
$verification = Invoke-Check -Name 'VerifyWindows' -Source 'Diagnostics/VerifyWindows.cs' -Argument $package
$boundary = Invoke-Check -Name 'FrameworkCompatibilityTests' -Source 'Tests/FrameworkCompatibilityTests.cs' -Argument (Join-Path $package '05_Full_Startup/BudgetDesk.exe')

$dist = Join-Path $root 'diagnostic-dist'
New-Item -ItemType Directory -Path $dist -Force | Out-Null
$zipPath = Join-Path $dist 'BudgetDesk_Diagnostic_EXEs.zip'
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($package, $zipPath, [IO.Compression.CompressionLevel]::Optimal, $false)
$archive = [IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $entries = @($archive.Entries | Where-Object { -not $_.FullName.EndsWith('/') })
    if ($entries.Count -ne 5 -or @($entries | Where-Object { $_.FullName -notmatch '^(01_Window|02_Model|03_Storage|04_Demo_UI|05_Full_Startup)/BudgetDesk\.exe$' }).Count -ne 0) { throw 'EXE-only ZIP contents differ from the five documented variants.' }
} finally { $archive.Dispose() }
$buildUrl = "https://github.com/$env:GITHUB_REPOSITORY/actions/runs/$env:GITHUB_RUN_ID"
$metadata = [ordered]@{ built_utc = [DateTime]::UtcNow.ToString('o'); commit = $env:GITHUB_SHA; build_url = $buildUrl; source_base = 'ecad7563edf73adfa5a07d7908185c738b65b230'; zip_sha256 = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant(); variants = $records }
$json = ConvertTo-Json -InputObject $metadata -Depth 8
$guide = (Get-Content -LiteralPath 'Diagnostics/Guide.template.html' -Raw).Replace('__BUILD_METADATA__', $json)
[IO.File]::WriteAllText((Join-Path $dist 'BudgetDesk_Diagnostic_Guide.html'), $guide, [Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText((Join-Path $dist 'Diagnostic-Manifest.json'), $json + "`n", [Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText((Join-Path $dist 'Windows-Verification.txt'), "Build: $buildUrl`r`nCommit: $env:GITHUB_SHA`r`n" + $verification + $boundary, [Text.UTF8Encoding]::new($false))
$checksums = @(Get-ChildItem -LiteralPath $dist -File | Sort-Object Name | ForEach-Object { (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + $_.Name })
[IO.File]::WriteAllLines((Join-Path $dist 'SHA256SUMS.txt'), $checksums, [Text.UTF8Encoding]::new($false))
Write-Host "Diagnostic batch ready: $zipPath"
