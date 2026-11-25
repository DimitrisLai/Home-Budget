# Budget Desk 1.4.0

An offline personal budget manager for Windows, created by Dimitris L.

[Download BudgetDesk_v1.4.0_Windows.zip](https://github.com/DimitrisLai/Home-Budget/releases/download/v1.4.0/BudgetDesk_v1.4.0_Windows.zip) · [Releases and checksums](https://github.com/DimitrisLai/Home-Budget/releases)

## Start or update

On Windows 10 or 11, extract the entire ZIP and open **BudgetDesk/BudgetDesk.exe**. The application uses .NET Framework 4.5 or later. The launch folder contains the executable, its configuration, **Manuals** and **Support**.

To update, open the previous version, export a JSON backup in **Settings & backup**, then close the old window. Extract 1.4 into a new folder. Your normal budget opens from `%LOCALAPPDATA%\BudgetDesk\Data`. Earlier data migrates to schema 8. Existing budgets start with an empty term-deposit list. Saved item colors carry over. One-off plans retain their recording settings. Old transactions keep their original dates and values, with unknown times left blank. Keep the pre-update backup if you need to reopen your budget in an older version.

## User manuals

The three illustrated user guides work offline. Each has a linked contents page, thirteen chapters, a named color palette, examples and screenshots from its own interface language. Open a PDF for reading or printing, or the HTML edition for navigation. Both are included in **Manuals**.

- English · [PDF](Manuals/English.pdf) · [HTML](Manuals/English.html)
- Česky · [PDF](Manuals/Cesky.pdf) · [HTML](Manuals/Cesky.html)
- Ελληνικά · [PDF](Manuals/Ellinika.pdf) · [HTML](Manuals/Ellinika.html)

## Daily use

- Choose **Item color** while creating or editing an account, recurring payment, daily transaction, future expense, savings goal or template. Choose one of 32 named swatches or **Default color**, then save the item. Cards and individual rows show a tint and a color stripe. Paused recurring payments retain their red status until resumed. A plan or template copies its color to the new transaction.
- In **Settings & backup**, choose English, Ελληνικά or Čeština and a light or dark appearance. Select **Save** to apply both to the open budget. The JSON backup includes these preferences.
- **Future / planned** automatically books new one-off expenses at their scheduled local time. Clear **Automatic recording** for a manual reminder. With the budget closed or the computer asleep, overdue automatic payments are recorded on return using the scheduled timestamp. Manual **Record paid** uses the confirmation time.
- **Accounts & bills** shows the account cards in your chosen display order, the combined balance and up to 20 recurring schedules with manual, automatic and paused states. Paused payments have a red card with a bold Paused label. Resuming restores the active card colors.
- **Term deposits**, beside Accounts & bills, tracks principal, opening dates and maturities. Fund a new deposit from an account or enter money already held separately. Record transfers and partial or full withdrawals in either direction. Both tabs include deposits in the grand total. Maturity updates the status and leaves principal in place until you record its return.
- **Daily expenses** records expenses, income and transfers with local dates and times. Add splits, use templates, remember a payee, find entries across the full history and keep unpaid plans separately.
- **Amount / currency** starts with the selected account's currency. Select a different receipt currency and check the account charge before saving. For example, paying 20 EUR from a CZK account at 25 CZK per EUR deducts 500 CZK. A later change in rates preserves the booked charge.
- **Monthly overview** shows income, spending, net and category budgets. **Yearly overview** opens a month on double-click. CSV exports include all matching pages.
- **Savings goals** tracks money set aside, progress, remaining amounts and optional deadlines. Record any actual transfer between accounts separately.
- Names appear before icons. Account, category and icon pickers are alphabetical and support single-letter navigation. Table headings cycle through ascending, descending and original order. Sorting works across pages.

## Try a separate budget

In the Windows ZIP, **Support/Test-Sandbox.cmd** opens a separate empty test budget. **Support/Try-Demo.cmd** opens sample data. The numbered [Greek checklist](WINDOWS-CHECKLIST-EL.md) includes expected balances and acceptance checks through 1.4. **Settings & backup** offers JSON export/restore and access to the current data folder with up to twenty previous saves.

## Build and verification

The repository includes C# source, Windows launchers and the native test suites. **Rebuild-Windows.cmd** builds locally. The Windows release workflow compiles the app, exercises its accounting, compatibility, interface and startup, and checks the archive before publication. The ZIP includes the run results under **Support/Tests/Windows-CI-*.txt** and a file-by-file SHA-256 manifest in **Support**. The open feature ideas are included in **Support/IDEAS.md**.

[Release notes](RELEASE-NOTES.md) · [Version history](CHANGELOG.md) · [Release workflow](GITHUB-PUBLISHING.md) · [Ideas](IDEAS.md) · [MIT license](LICENSE)
