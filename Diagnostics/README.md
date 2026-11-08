# Budget Desk component-isolation diagnostics

This is one transparent diagnostic batch for investigating the unresolved antivirus classification of the public budgeting app. These executables are not release candidates or replacement budget apps. Scanner results are evidence for vendor review, not a way to select a supposedly safe release.

| Folder / EXE | Compiled application components | Runtime behavior |
| --- | --- | --- |
| `01_Window/BudgetDesk.exe` | Shared diagnostic launcher and assembly metadata | Minimal WinForms window |
| `02_Model/BudgetDesk.exe` | Adds unmodified `Domain.cs` | Validates three synthetic accounts in memory |
| `03_Storage/BudgetDesk.exe` | Adds unmodified `Storage.cs` | Saves and reloads synthetic JSON in a temporary folder |
| `04_Demo_UI/BudgetDesk.exe` | Adds unmodified `Layouts.cs`, `Ui.cs`, `QuickEntry.cs` | Full budgeting UI with temporary demo data, bypassing production startup |
| `05_Full_Startup/BudgetDesk.exe` | Adds production startup and diagnostics | Full production entry path with explicit temporary demo data/log arguments |

The last test uses a generated copy of Program.cs with only Main's visibility changed from private to internal so the diagnostic launcher can call it directly. The repository's production sources are untouched. All other app source bytes come from the same reviewed 1.0.1 source. Every test uses the same Microsoft compiler, Framework 4.5 references, C# 5, AnyCPU target, optimization, embedded app icon, manifest and assembly version metadata. No packer, obfuscator, signer or security-setting change is used.

The ZIP contains exactly five EXEs, one per named folder. The separate Greek HTML guide provides file hashes, scan instructions and a results form. The Windows build compiles the batch once, opens/closes each exact executable from an external test driver, verifies the normal real/demo data folders are unchanged, and runs the existing Framework/capture-boundary checks on test 05. Functional checks do not establish antivirus clearance.

Start by scanning the extracted files together with protection enabled. A file quarantined during extraction is a valid result and does not need to be restored for this experiment. Record the folder, detection label and any VirusTotal link. Different outcomes can narrow the relevant component group; compiler-generated code and classifier opacity mean they cannot prove a specific triggering method or that a file is safe.

The normal release hold remains active. This branch uploads a diagnostic artifact only and never publishes a release.
