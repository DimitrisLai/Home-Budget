# Budget Desk 1.0.1

Complete offline Windows budget manager, created by Dimitris L.

Version 1.0.1 retains every budgeting feature from 1.0.0: accounts and ordering, expenses/income/transfers, recurring payments, one-off future expenses, split purchases, monthly/yearly reports, category budgets, savings goals, templates, payees, all-history search, CSV exports and JSON backups.

The developer-only screenshot command has been removed from the distributed app. Screenshot helpers stay in separate test programs. Startup and executable-compatibility checks were improved. Financial calculations, data paths and schema 4 are unchanged.

The release retains the exact Windows-tested 1.0.1 executable from the review candidate, SHA-256 `2b312bd79f3a835296d8f3f7b0386911a7c449e46f1b3106850de93c968fa7cd`. The release process verifies its source/configuration, reruns native Windows checks, packages it once and verifies uploaded downloads. The owner requested release after clean antivirus retests; the dated investigation record is in ANTIVIRUS-STATUS.md.

## Download and update

Download **BudgetDesk_v1.0.1_Windows.zip**, extract it and run **BudgetDesk/BudgetDesk.exe**. Windows 10/11 with .NET Framework 4.5 or later is supported. Developer tools are not required.

Close the previous app and export a JSON backup before updating. Extract into a new folder. Existing data stays in `%LOCALAPPDATA%\BudgetDesk\Data` and loads automatically.

## Manual tests

Run **Test-Sandbox.cmd** for a separate, initially empty test budget, then follow **WINDOWS-CHECKLIST-EL.md**. **Try-Demo.cmd** opens separate sample data. Native test results are included under **Tests/Windows-CI-*.txt**.

Licensed under the MIT License. Source is included; unfinished ideas remain in IDEAS.md.
