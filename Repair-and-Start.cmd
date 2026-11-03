@echo off
setlocal DisableDelayedExpansion
cd /d "%~dp0"
set "BUDGET_REPAIR_LOG=%~dp0repair-log.txt"
>"%BUDGET_REPAIR_LOG%" echo Budget Desk 1.0.0 Windows repair
if errorlevel 1 goto read_only
echo Rebuilding Budget Desk for this Windows PC...
echo Your saved budget is stored separately.
call "%~dp0Rebuild-Windows.cmd" /quiet >>"%BUDGET_REPAIR_LOG%" 2>&1
if errorlevel 1 goto build_failed
type "%BUDGET_REPAIR_LOG%"
echo.
call "%~dp0Start-BudgetDesk.cmd" %*
exit /b %ERRORLEVEL%

:read_only
echo This folder cannot be written to.
echo Extract the ZIP into a folder you can write to, such as Downloads, and try again.
pause
exit /b 1

:build_failed
echo.
type "%BUDGET_REPAIR_LOG%"
echo.
echo Please send repair-log.txt from this folder.
pause
exit /b 1
