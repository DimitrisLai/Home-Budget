# Budget Desk — Windows smoke-test checklist

Open the new BudgetDesk.exe to check your existing budget. Try-Demo.cmd provides separate sample data for testing new entries. The user confirmed 0.1.9 working before the version-only promotion to 1.0.0. This checklist remains useful for later releases.

- [ ] Existing accounts, bank order, balances, entries, plans and savings goals load correctly.
- [ ] Daily expenses → Templates → + New: save a template with an optional amount. Use it, enter an amount and Save. Exactly one new transaction is recorded.
- [ ] Use a template and cancel its draft. The balance and transaction count stay unchanged.
- [ ] Create/use a fixed-amount split template. The purchase is deducted once and category shares remain correct.
- [ ] In a new entry, type a payee and save. On the next new entry, its name suggests the saved account/category.
- [ ] Choose an account/category yourself, then choose a known payee. Your choices stay in place. Editing an old entry also preserves its account.
- [ ] Save as template from an entry, then cancel the entry. The shortcut exists without an extra transaction.
- [ ] Search all history for an older purchase by payee or description. Check account/category/type filters, inclusive date ranges and Full amount. Reset filters shows all records again.
- [ ] Edit an older search result. The intended entry changes without a duplicate. Split-category results show only their share.
- [ ] Close and reopen the app. Templates, payees, entries, goals and plans remain saved.
- [ ] Minimize/restore and resize the dashboard and history view. Lower sections remain visible and page buttons expose all rows.

If something fails, note the step, what you entered and what happened. For launch errors, Start-BudgetDesk.cmd creates a startup report.
