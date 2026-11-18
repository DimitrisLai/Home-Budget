@echo off
setlocal DisableDelayedExpansion
cd /d "%~dp0"
set "BUDGET_APP_DIR=%~dp0"
if exist "%~dp0..\BudgetDesk.exe.config" set "BUDGET_APP_DIR=%~dp0..\"
echo Budget Desk 1.1.1 - separate manual-test budget
echo Test data: %LOCALAPPDATA%\BudgetDesk\Manual-Tests-1.1.0
echo This window uses a different data folder from your normal budget.
if not exist "%BUDGET_APP_DIR%BudgetDesk.exe" (
    echo Extract the Windows release ZIP first, then run this command again.
    pause
    exit /b 1
)
"%BUDGET_APP_DIR%BudgetDesk.exe" "--data-dir=%LOCALAPPDATA%\BudgetDesk\Manual-Tests-1.1.0"
if errorlevel 1 pause
