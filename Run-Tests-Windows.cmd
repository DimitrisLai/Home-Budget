@echo off
setlocal DisableDelayedExpansion
cd /d "%~dp0"
set "BUDGET_APP_DIR=%~dp0"
if exist "%~dp0..\BudgetDesk.exe.config" set "BUDGET_APP_DIR=%~dp0..\"
set "BUDGET_COMPILER=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%BUDGET_COMPILER%" set "BUDGET_COMPILER=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%BUDGET_COMPILER%" goto missing
"%BUDGET_COMPILER%" /nologo /target:exe /platform:anycpu /out:Tests\LedgerTests.exe /reference:System.dll /reference:System.Core.dll /reference:System.Web.Extensions.dll Source\Domain.cs Source\Storage.cs Tests\LedgerTests.cs
if errorlevel 1 goto failed
Tests\LedgerTests.exe
if errorlevel 1 goto failed
"%BUDGET_COMPILER%" /nologo /target:exe /platform:anycpu /out:Tests\FrameworkCompatibilityTests.exe /reference:System.dll Tests\FrameworkCompatibilityTests.cs
if errorlevel 1 goto failed
Tests\FrameworkCompatibilityTests.exe "%BUDGET_APP_DIR%BudgetDesk.exe"
if errorlevel 1 goto failed
pause
exit /b 0

:missing
echo The .NET Framework C# compiler was not found.
goto failed

:failed
pause
exit /b 1
