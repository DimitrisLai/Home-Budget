@echo off
setlocal DisableDelayedExpansion
cd /d "%~dp0"
set "BUDGET_REPORT_DIR=%~dp0"
>"%BUDGET_REPORT_DIR%startup-log.txt" echo Budget Desk 1.0.1 Windows launcher
if errorlevel 1 set "BUDGET_REPORT_DIR=%TEMP%\BudgetDesk-"
set "BUDGET_START_LOG=%BUDGET_REPORT_DIR%startup-log.txt"
set "BUDGET_CONSOLE_LOG=%BUDGET_REPORT_DIR%startup-console.txt"
>"%BUDGET_START_LOG%" echo Budget Desk 1.0.1 Windows launcher
>>"%BUDGET_START_LOG%" echo Windows: %OS%   Architecture: %PROCESSOR_ARCHITECTURE%
reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Release >>"%BUDGET_START_LOG%" 2>&1
if not exist "%~dp0BudgetDesk.exe" goto missing
if not exist "%~dp0BudgetDesk.exe.config" goto missing
if not exist "%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\mscorlib.dll" goto runtime
echo Opening Budget Desk...
echo This window stays open while the app runs.
echo If startup fails, the error will remain here.
echo.
start "" /wait "%~dp0BudgetDesk.exe" "--startup-log=%BUDGET_START_LOG%" %* >"%BUDGET_CONSOLE_LOG%" 2>&1
set "BUDGET_EXIT=%ERRORLEVEL%"
>>"%BUDGET_START_LOG%" echo.
>>"%BUDGET_START_LOG%" echo Windows launcher exit code: %BUDGET_EXIT%
if exist "%BUDGET_CONSOLE_LOG%" type "%BUDGET_CONSOLE_LOG%" >>"%BUDGET_START_LOG%"
if not "%BUDGET_EXIT%"=="0" goto failed
findstr /l /c:"Window shown" /c:"already open in another process" "%BUDGET_START_LOG%" >nul
if errorlevel 1 goto no_window
exit /b 0

:missing
>>"%BUDGET_START_LOG%" echo Required program files are missing. Right-click the ZIP and choose Extract All, then open the BudgetDesk folder.
goto failed

:runtime
>>"%BUDGET_START_LOG%" echo Microsoft .NET Framework 4.5 or later could not be found.
>>"%BUDGET_START_LOG%" echo Official download: https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48
goto failed

:no_window
>>"%BUDGET_START_LOG%" echo The process ended before confirming that its window was shown.
goto failed

:failed
echo.
echo Budget Desk did not open successfully.
echo.
type "%BUDGET_START_LOG%"
echo.
echo Please send this file:
echo "%BUDGET_START_LOG%"
echo.
echo You can also try Repair-and-Start.cmd in this folder.
pause
exit /b 1
