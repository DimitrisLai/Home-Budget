@echo off
setlocal DisableDelayedExpansion
cd /d "%~dp0"
echo Budget Desk 1.0.1 - separate manual-test budget
echo Test data: %LOCALAPPDATA%\BudgetDesk\Manual-Tests-1.0.1
echo This window uses a different data folder from your normal budget.
if not exist "BudgetDesk.exe" (
    echo Extract the Windows release ZIP first, then run this command again.
    pause
    exit /b 1
)
"BudgetDesk.exe" "--data-dir=%LOCALAPPDATA%\BudgetDesk\Manual-Tests-1.0.1"
if errorlevel 1 pause
