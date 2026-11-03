# Budget Desk 1.0.0

First release of Budget Desk, an offline Windows personal budget manager created by Dimitris L.

This release contains the Windows-confirmed 0.1.9 feature set. Version identifiers and release documentation are updated; financial behaviour and data schema 4 are unchanged.

## Included

- Reorderable accounts and manual multi-currency rates.
- Expenses, income, transfers, custom categories and split purchases.
- Recurring payments and separately tracked one-off future expenses.
- Monthly/yearly reporting, category budgets and savings goals.
- Quick-entry templates, payee autofill and all-history search.
- Automatic saving, JSON backups and CSV exports.
- Complete editable source and optional Windows rebuild/test commands.

## Download

Choose **BudgetDesk_v1.0.0_Windows.zip**, extract it and run **BudgetDesk/BudgetDesk.exe**. GitHub's automatically generated Source code archives contain repository source; the attached Windows ZIP is the ready-to-run download.

Designed for Windows 10/11 with .NET Framework 4.5 or later. No Python, Node, Office or developer tools are required to use the executable.

## Updating

Close the older app, extract this ZIP into a new folder and run the new executable. Existing data remains in `%LOCALAPPDATA%\BudgetDesk\Data`. The format is the same as 0.1.9; earlier supported formats migrate with automatic backups.

Future features are listed in IDEAS.md. Licensed under the [MIT License](LICENSE).
