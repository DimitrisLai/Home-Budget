# Validation — Budget Desk 1.0.1 review candidate

Prepared 7 November 2026 from public v1.0.0 commit aad8fde148c37866155041c50c418d0fc9d135ed.

## Change scope

Removed the optional production screenshot command and its capture-only exception behavior. Normal startup diagnostics and UI error reporting remain. Financial logic and schema 4 are unchanged; application version identifiers are 1.0.1.

UI screenshots remain only in separate test programs. FrameworkCompatibilityTests checks the actual executable for screen-capture calls and native imports in every method body, including compiler-generated helpers. StartupSmokeTests opens/closes the actual executable with isolated empty/demo budgets, checks the retired screenshot flag has no capture effect, and verifies an unusable log destination does not prevent startup or alter the occupied file.

## Candidate checks

Local checks compiled against the Microsoft .NET Framework 4.5 references with warnings as errors and passed 35 accounting/persistence, 17 general UI, 42 quick-entry/payee/history and 3 executable compatibility/capability checks. As a negative control, the new capability check rejected the original v1.0.0 executable at its actual screen-capture call. These local executions used Mono with an isolated virtual display; native startup checks are performed separately on Windows.

The build-only candidate workflow compiles on Windows using the pinned Microsoft .NET Framework 4.5 reference package, with C# 5 and warnings as errors. It runs accounting/persistence, actual-executable compatibility/capability checks, general UI, quick-entry/search and actual-executable startup checks. Results are generated into Tests/Windows-CI-*.txt in the candidate ZIP; a failed check prevents artifact creation.

The candidate is retained as a workflow artifact, without publishing a stable release. The tested executable is staged once and its SHA-256 is included in the generated evidence. Package checks reject additional executables, runtime data and reference DLLs.

Functional and capability checks do not establish antivirus clearance. See SECURITY-REVIEW.md for vendor status. The historical 1.0.0 results below describe that released version, not a rerun of all historical suites for 1.0.1.

---

# Validation — Budget Desk 1.0.0

Build date: 3 November 2026.

## Release basis

The user confirmed version 0.1.9 working on Windows and requested promotion to version 1. The 1.0.0 application keeps that confirmed feature set. Comparing every C# source file against the delivered 0.1.9 archive shows identical code after normalizing the version strings. Assembly/file versions, window title, diagnostics, launch/build labels and the manifest are updated. Financial logic, data paths and schema 4 are unchanged.

Release packaging adds a clean README, release notes, GitHub publishing guidance and a project ignore file. The MIT license selected by the repository owner is included unchanged in the source and Windows package. The backlog retains its 29 unfinished ideas. Current mock screenshots show the 1.0.0 build. The automatic publishing setup changes build and packaging files; application source remains unchanged.

## Checks rerun for 1.0.0

| Check | Result |
| --- | --- |
| Source comparison with confirmed 0.1.9 | Passed: version strings only |
| Microsoft .NET Framework 4.5 reference build, C# 5, warnings as errors | Passed |
| Accounting and persistence | 35 passed |
| General UI and all five pages | 17 passed |
| Templates, payees and all-history search | 42 passed |
| Actual-executable startup/integration | 10 passed, including fresh empty/demo page captures and startup-error/fallback checks |
| Executable API/target compatibility | 2 passed |

The release was also visually reviewed using current screenshots of the dashboard and all-history results. The executable in the packaged ZIP is byte-identical to the tested executable. ZIP integrity and all packaged SHA-256 checksums were verified.

## Confirmed 0.1.9 regression baseline

The unchanged source also retains the earlier completed regression coverage. Included result files identify their original suites:

- 5 compact-account layout checks, covering 42 count/window combinations.
- 8 live-refresh checks, including 40 additions through the real account editor.
- 7 scroll-free/restore checks, including ten actual minimize/restore cycles.
- 12 bank-order and category checks.
- 28 yearly-overview and one-off-plan checks.
- 35 savings-goal and split-purchase checks.
- A separate physical mouse drag test across account-card rows with persistence verification.

These baseline suites were completed for 0.1.9; they are not presented as additional reruns of the version-only 1.0.0 change. Complete model/UI test source remains in Tests.

## Automatic Windows release

The GitHub release workflow builds the same source on Windows with the Microsoft C# compiler and the checksum-verified .NET Framework 4.5 references below. It runs the accounting/persistence suite, executable compatibility checks, general UI suite, quick-entry/search suite, and actual-executable empty/demo startup across all five tabs. All generated sample data and screenshots stay outside the package. A failing check prevents publication.

Each Windows ZIP includes its native results in Tests/Windows-CI-*.txt, including the workflow run, source commit and actual executable hash. The build stages the tested executable once, creates and verifies every internal file checksum, then the publishing job checks the ZIP, source and license again. It uploads a draft release, downloads its assets and verifies their SHA-256 values before publishing. Existing releases are not overwritten.

## Build and platform

The executable is compiled against Microsoft's .NET Framework 4.5 reference assemblies with implicit Mono references disabled (`-noconfig -nostdlib+`). It retains the original Windows startup fix for String.TrimEnd(Char[]): a compatibility test checks that signature and the Framework 4.5 target. The reference package is [Microsoft.NETFramework.ReferenceAssemblies.net45 1.0.3](https://www.nuget.org/packages/Microsoft.NETFramework.ReferenceAssemblies.net45/1.0.3), SHA-256 `23a9f94ea3e2cb88cd8341af75b811c6fb5cb82516fc696e95ed4620279128e3`.

The original release execution checks used Mono 6.8 on Linux with Openbox and a virtual display. The user's 0.1.9 Windows test remains the manual confirmation; automated native Windows results accompany the GitHub-built package. Version 1.0.0 makes no application-logic changes. Source/QuickEntry.cs is included in the application and optional Windows rebuild command. Linux dependencies and reference DLLs are not bundled as runtime requirements.

WINDOWS-CHECKLIST.md remains available for later smoke checks. Start-BudgetDesk.cmd creates a startup report if needed.
