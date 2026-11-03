# Budget Desk — ideas for future versions

Updated for 1.0.0 on 3 November 2026. This file contains 29 unfinished ideas, grouped by purpose. Completed features have been removed. These are options for later versions, rather than promises for a particular future release.

## Planning payments and future balances

### 1. Balance forecast for the next 30, 60 and 90 days
Start with today's real account balance and calculate a dated estimate after upcoming one-off plans, recurring payments and explicitly planned income. Show the lowest expected balance as well as the balance at the end of the period, so a shortfall before payday is visible. The actual balance would remain clearly separate from the forecast.

### 2. Expected one-off income
Plan a salary, sale, reimbursement or other incoming payment without immediately increasing the real balance. Mark it as received on the actual date to create an income entry once. This would make the balance forecast more useful than subtracting future expenses alone.

### 3. Payment calendar and upcoming-payments view
Combine recurring schedules and one-off plans in a compact calendar or list ordered by date. Show overdue, due-today and upcoming items, with their paying account and amount. Month/week navigation or page buttons would keep the screen usable without scrollbars.

### 4. Due-soon alerts inside the app
Choose how many days before a payment or renewal the app should highlight it. A small action list could group overdue items and payments due soon, with direct links to their editor or Record paid action. An alert would never book a one-off expense by itself.

### 5. Finite instalment plans
Enter an instalment amount, interval and number of payments, then track how many payments and how much money remain. Stop the schedule after the final instalment and retain its history. For example, show that four of ten payments for a purchase are still outstanding.

### 6. Subscription renewals and price history
Keep earlier prices and renewal dates when a subscription changes. Flag an upcoming renewal for review and show the monthly/yearly difference from its previous price. Editing a future price would preserve already recorded expenses.

## Category budgets and spending control

### 7. Different budgets for different months
Store a separate budget for each category and month. This would allow a larger holiday or gift budget in a particular month while preserving the budgets actually used in earlier reports. Copying a previous month's plan could provide a starting point.

### 8. Budget rollover
Offer an explicit choice about carrying unused category money into the following month. Overspending could either reduce the next month's available amount or start fresh, according to the chosen rule. Show the carried amount separately so the available budget is easy to explain.

### 9. Budget progress warnings
Highlight categories approaching or exceeding their limit, using configurable thresholds such as 80% and 100%. Show the amount remaining and the transactions behind it. A warning would inform the user without blocking an expense they still need to record.

### 10. Refunds linked to the original purchase
Connect a full or partial refund to its original expense and reduce the appropriate category's net spending. For a split purchase, let the user identify which category shares were refunded. Keep the original purchase and refund dates visible, including when they fall in different months.

### 11. Tags for activities and projects
Add optional labels such as Holiday, Work or a household project independently of expense categories. A purchase could retain its Groceries category while also belonging to a trip. Filter reports by one or more tags without duplicating the transaction.

## Faster daily entry and checking

### 12. Duplicate an expense in one action
Copy an existing purchase directly into a new draft, including its payee, account and split allocations. Ask for the new date and allow changes before saving. This would provide a one-off copy without first creating a named reusable template.

### 13. Faster keyboard entry
Add dedicated shortcuts, a focused quick-add window and an optional Save and add another action. Keep the commonly repeated fields while resetting the amount or description as appropriate. Keyboard behaviour should remain predictable and require review before recording money.

### 14. Warnings for possible manual duplicates
Before saving, flag another entry with the same account, date, amount and similar payee/description. Offer to review the earlier entry or keep both when they are genuinely separate purchases. Never silently delete or merge financial history.

### 15. Match records against a bank statement
Let the user enter a statement balance, mark transactions they have checked and see the remaining difference. Preserve a last-checked date and distinguish verified entries from unchecked ones. This can work entirely manually, without importing bank files.

### 16. Month-end review checklist
Collect outstanding plans, unchecked balances, uncategorized work and goal progress in a short review screen. Mark the month as reviewed while still allowing a deliberate later correction. A later edit could indicate that the review needs another look.

## Savings goals

### 17. Contribution and allocation history
Keep dated additions and reductions for each goal, optionally with a note. Show how the saved allocation changed over time and allow a mistaken adjustment to be corrected with an understandable record. These tracking changes would remain separate from actual bank transactions.

### 18. Goal priorities and reallocation
Order goals by priority and move a tracked allocation from one goal to another. For example, move part of a holiday allocation toward an urgent car repair without creating fictitious income or expenses. Show both sides of each reallocation in the goal history.

## Reports and comparisons

### 19. Compare periods
Compare one month with the previous month or with the same month last year. Show the difference in money and percentage for overall spending and individual categories, with a clear treatment of a zero comparison amount. Let the user open the purchases behind a change.

### 20. Reports for custom date ranges
Build an income, spending and category summary for any chosen start and end dates, such as payday to payday. Unlike transaction search, this would provide a full period report with category budgets or comparisons where those are meaningful. Clearly identify which dates and accounts the report covers.

### 21. Printable summaries
Produce a clean monthly, yearly or custom-period summary for printing or saving as PDF. Include the chosen filters, currency, totals and category breakdown, with a readable layout independent of the app window size. Allow personal details to be omitted when sharing a report.

### 22. Net-worth trend
Show how the combined value of accounts changes over time, using historical balance calculations or saved snapshots. Explain the effect of exchange-rate changes separately from money earned or spent. Any later asset/debt support would need explicit valuation rules rather than assuming every change is cash flow.

## Accounts, categories and currencies

### 23. Archive inactive accounts
Hide an old account from new-entry choices while keeping its balance, transactions and reports available. Allow archived accounts to be shown and restored. Templates and scheduled payments using that account would need to be reassigned or reviewed first.

### 24. Archive unused categories
Hide a category from new-entry choices without deleting earlier purchases or split allocations. Keep its historical spending in reports and allow it to be restored later. Offer a clear replacement for templates or active schedules that still use it.

### 25. Separate local budgets
Keep personal, household or work budgets in separate files and switch between them inside the app. Give each budget a recognizable name and its own backup history. Avoid copying or mixing transactions between budgets accidentally.

### 26. Manual exchange-rate history
Keep dated changes to manually entered exchange rates. Use that history for historical balance comparisons while leaving each recorded transaction's saved rate intact. Show the date and source of a rate, and explain what happens when a historical rate is missing.

## Corrections, backups and privacy

### 27. Undo the latest change
Offer a reviewable reversal for a recent addition, edit or deletion. Show exactly which records would change and preserve financial links, including split parts and recorded one-off payments. An undo operation would be saved and backed up like any other change.

### 28. Browse automatic backups inside the app
List available backup dates with their account, transaction, plan, goal and template counts before restoring. Allow a preview or separate demo opening so the user can identify the right version. Keep the current budget backed up before a restore replaces it.

### 29. Optional local encryption
Protect the budget file and its backups with a password. Include a clear recovery policy, explain the consequences of forgetting the password and keep temporary writes protected too. This would require careful design and testing before becoming a release feature.

## Design preferences

Keep screens compact and use page buttons for longer lists. Preserve offline use, clear financial calculations and automatic backups. CSV import and receipt attachments are outside the requested roadmap.
