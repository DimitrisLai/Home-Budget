# Budget Desk version history

## 1.0.0 — First release

- Promoted the Windows-confirmed 0.1.9 feature set to 1.0.0.
- Updated assembly, manifest, window, launcher and diagnostic version identifiers.
- Kept all financial behaviour, data paths and schema 4 unchanged.
- Reworked the README for release use and included release notes, source-upload guidance and a project ignore file.
- Included the MIT license selected by the repository owner in the source and Windows package.
- Retained the categorized backlog for future versions.

## 0.1.9 — Templates, payees and all-history search

- Added paged management for quick-entry expense/income templates, optional amounts, split allocations and Save as template from an entry editor. Use template creates a fresh draft dated today, with current exchange rates and no financial effect until saved.
- Added payee names, remembered defaults, suggestions on blank new entries and a paged payee manager. Explicit choices, template defaults and existing financial fields are preserved. Renaming/deleting a profile keeps historical payee text.
- Added Search all history within Daily expenses: text across payee/description/account/category, account/category/type/date/amount filters, full-result metrics, paged editing and result export. Amount ranges use full native entry amounts; category totals use matching split shares.
- Added migration from schema 1–3 to schema 4, previous-file backups, template reference protection and safe clearing of unused payee defaults.
- Reorganized IDEAS.md into 29 unfinished ideas grouped by purpose, removing completed features and keeping rejected import/attachment work outside the roadmap.
- Included TEST-BEFORE-1.0.md for the final Windows check. This package remains version 0.1.9.

## 0.1.8 — Savings goals and split purchases

- Added a Savings goals tab with targets, currencies, saved allocations, progress bars, optional deadlines, remaining amounts and monthly suggestions. Add savings and allocation adjustments do not create financial transactions.
- Added a compact two-column split-purchase editor supporting all 20 categories without scrollbars. One expense is deducted once; its parts must sum exactly to its total.
- Updated category filters, monthly charts/budgets, category deletion protection and existing exports for split shares. Yearly totals continue to count each purchase once.
- Added schema-1/2 to schema-3 migration and exact previous-file backups on the first save. Older executables reject the newer format.
- Expanded IDEAS.md, retaining the original ideas and marking CSV import and receipt attachments as not wanted.
- Retained bank ordering, custom categories, one-off plans, compact cards, pagination and minimize/restore handling.

## 0.1.7 — Yearly overview and one-off plans

- Added Yearly overview alongside Monthly overview, with all 12 months, actual income/expense/net totals, separate pending-plan and bill columns, monthly drill-down and year CSV.
- Added + Future expense and a Future / planned list inside Daily expenses, with all-date visibility, filters, overdue status, editing, deletion and separate CSV.
- Plans stay undeducted until Record paid is saved with the actual payment date. Recording removes the plan and creates a single expense. Due dates do not automatically post one-off plans.
- Added schema-1 to schema-2 migration; the first save keeps the exact previous budget in automatic Backups. Schema 2 makes older executables reject the file instead of silently losing plans.
- Protected account/category references and account currency when used by plans. Current rates estimate plans; recorded expenses retain saved rates.
- Improved recurring forecasts for distant years and the supported calendar boundary.
- Retained account ordering, category creation, compact cards, pagination and minimize/restore behavior.

## 0.1.6 — Bank order and custom categories

- Added drag-and-drop ordering of account cards, including movement across wrapped rows and dropping into empty space to move last.
- Added a Display position field in the account editor as a numbered alternative.
- Saved account order is retained across reloads, backups and account selectors, using the existing ordered account list and schema.
- Added a direct + Category button on Daily expenses, with custom names, icons and optional budgets. The category manager remains available and the 20-category limit is retained.
- The drag loop completes before rebuilding its source controls; unrelated external drops are ignored.
- Retains compact account/payment grids, pagination and minimize/restore handling.

## 0.1.5 — Compact payments and restore layout

- Replaced the recurring-payment table with compact cards showing amount, next date, paying account, repeat interval and mode. Full details are available on hover.
- Kept selection, recording, editing, pause/resume and schedule deletion available.
- Fits all 20 accounts and all 20 recurring schedules on the first tab without scrollbars at the supported minimum size.
- Replaced large dashboard metrics with a slim grand-total strip; monthly spending and unrecorded bills are on Monthly overview. Other summary panels are smaller.
- Added Previous/Next buttons to transaction and category tables. Totals, filtering, edits and exports retain access to the full data.
- Replaced mutable dashboard table heights with viewport-based card layout, froze geometry while minimized, and added restore/activation layout refresh.
- Uses the same data location and schema as earlier versions.

## 0.1.4 — Refresh after saving an account

- Fixed the page rebuild after adding or editing accounts, so totals and recurring payments stay visible without restarting.
- Account layout now uses the actual viewport size and runs after the full page is constructed and docking has completed.
- Added a follow-up layout pass after the save dialog closes; callbacks for replaced pages are ignored.
- Added live form-save checks for all 20 accounts at normal and minimum sizes, including visibility of lower sections through every parent container.
- Keeps the compact cards, account order and existing budget data format.

## 0.1.3 — Compact accounts

- Reduced account-card width and height while retaining names, icons, balances and click-to-edit behavior.
- Replaced the scrolling account container with a grid that wraps into compact rows.
- The account section expands and contracts with the number of rows, keeping totals below the accounts.
- All 20 accounts stay visible at normal and minimum supported window sizes; resizing rearranges the existing grid.
- Added hover text for full account names and balances.
- Uses the same data location and schema as 0.1.2.

## 0.1.2 — Windows startup compatibility

- Corrected the missing String.TrimEnd(Char) method call to use the Windows-compatible character-array overload.
- Compiled against Microsoft's .NET Framework 4.5 reference assemblies and added an executable compatibility check.
- Windows launch confirmed by the user.

## 0.1.1 — Startup diagnostics

- Added early startup logging, visible errors and optional Windows rebuild commands.

## 0.1.0 — First preview

- Accounts, recurring payments, daily expenses, monthly reporting, income, transfers and backups.
