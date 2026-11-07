@echo off
setlocal DisableDelayedExpansion
cd /d "%~dp0"
set "BUDGET_COMPILER=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%BUDGET_COMPILER%" set "BUDGET_COMPILER=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%BUDGET_COMPILER%" goto compiler_missing
if not exist "Source\StartupDiagnostics.cs" goto source_missing
echo Building Budget Desk with the Microsoft Windows C# compiler...
"%BUDGET_COMPILER%" /nologo /target:winexe /platform:anycpu /optimize+ /langversion:5 /codepage:65001 /win32manifest:Source\app.manifest /win32icon:Source\BudgetDesk.ico /out:BudgetDesk.rebuilt.exe /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll Source\Domain.cs Source\Storage.cs Source\Ui.cs Source\QuickEntry.cs Source\Layouts.cs Source\StartupDiagnostics.cs Source\Program.cs
if errorlevel 1 goto build_failed
move /y "BudgetDesk.rebuilt.exe" "BudgetDesk.exe" >nul
if errorlevel 1 goto replace_failed
echo Budget Desk 1.0.1 built successfully.
if /i not "%~1"=="/quiet" pause
exit /b 0

:compiler_missing
echo The Microsoft .NET Framework C# compiler could not be found.
echo The included BudgetDesk.exe can still be tried with Start-BudgetDesk.cmd.
echo Official .NET Framework download: https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48
goto failed

:source_missing
echo Source files are missing. Extract the entire ZIP before running this command.
goto failed

:build_failed
echo The Windows build failed. The original executable was left in place.
goto failed

:replace_failed
echo The rebuilt file could not replace BudgetDesk.exe.
echo Close any running Budget Desk window, then try again.
goto failed

:failed
if /i not "%~1"=="/quiet" pause
exit /b 1
