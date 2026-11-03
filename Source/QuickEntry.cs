using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace BudgetDesk
{
    public sealed partial class MainForm
    {
        private Entry newEntryPreset;
        private bool allHistory, historyDateRange, historyAmountRange;
        private DateTime historyFrom = DateTime.Today.AddYears(-1), historyThrough = DateTime.Today;
        private decimal historyMinimum, historyMaximum = 1000000;

        private ComboBox PayeeBox(string name)
        {
            ComboBox box = Theme.Combo(); box.DropDownStyle = ComboBoxStyle.DropDown;
            box.AutoCompleteMode = AutoCompleteMode.SuggestAppend; box.AutoCompleteSource = AutoCompleteSource.ListItems;
            foreach (PayeeProfile p in Data.Payees.OrderBy(p => p.Name)) box.Items.Add(p.Name);
            box.Text = name ?? ""; box.MaxLength = 80; return box;
        }
        private EntrySearch BuildEntrySearch()
        {
            EntrySearch query = new EntrySearch { Text = searchText, AccountId = accountFilter, CategoryId = categoryFilter, Kind = kindFilter };
            if (allHistory)
            {
                if (historyDateRange) { query.From = historyFrom; query.Through = historyThrough; }
                if (historyAmountRange) { query.MinimumAmount = historyMinimum; query.MaximumAmount = historyMaximum; }
            }
            else { query.From = selectedMonth; query.Through = new DateTime(selectedMonth.Year, selectedMonth.Month, DateTime.DaysInMonth(selectedMonth.Year, selectedMonth.Month)); }
            return query;
        }
        private void UpdateHistoryMetrics(decimal income, decimal expenses)
        {
            if (!allHistory || showPlans) return;
            string[] names = { "HistoryIncome", "HistoryExpenses", "HistoryNet" }; decimal[] values = { income, expenses, income - expenses };
            for (int i = 0; i < names.Length; i++) { Label label = Descendants(currentPage).OfType<Label>().FirstOrDefault(l => l.Name == names[i]); if (label != null) label.Text = Theme.MoneyText(values[i], Data.BaseCurrency); }
        }
        private Panel HistoryFilters()
        {
            FlowLayoutPanel panel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = new Padding(0) };
            CheckBox dated = new CheckBox { Text = "Date range", Checked = historyDateRange, Width = 96, Height = 28, Margin = new Padding(0, 5, 4, 0), Name = "HistoryUseDates" };
            DateTimePicker from = DatePicker(historyFrom), through = DatePicker(historyThrough); from.Name = "HistoryFrom"; through.Name = "HistoryThrough";
            foreach (DateTimePicker picker in new DateTimePicker[] { from, through }) { picker.Dock = DockStyle.None; picker.Width = 119; picker.Height = 27; picker.CustomFormat = "dd MMM yyyy"; picker.Font = Theme.Font(9, false); picker.Margin = new Padding(0, 5, 4, 0); picker.Enabled = dated.Checked; }
            CheckBox amounts = new CheckBox { Text = "Full amount", Checked = historyAmountRange, Width = 105, Height = 28, Margin = new Padding(13, 5, 4, 0), Name = "HistoryUseAmounts" };
            NumericUpDown minimum = Theme.Money(historyMinimum), maximum = Theme.Money(historyMaximum); minimum.Name = "HistoryMinimum"; maximum.Name = "HistoryMaximum";
            foreach (NumericUpDown value in new NumericUpDown[] { minimum, maximum }) { value.Minimum = 0; value.Dock = DockStyle.None; value.Width = 117; value.Font = Theme.Font(9.5f, false); value.Height = 27; value.Margin = new Padding(0, 5, 4, 0); value.Enabled = amounts.Checked; }
            Func<Label> to = delegate { Label label = Theme.Label("to", 9, false, Theme.Muted); label.Dock = DockStyle.None; label.Size = new Size(19, 28); label.Margin = new Padding(0, 5, 3, 0); return label; };
            panel.Controls.Add(dated); panel.Controls.Add(from); panel.Controls.Add(to()); panel.Controls.Add(through); panel.Controls.Add(amounts); panel.Controls.Add(minimum); panel.Controls.Add(to()); panel.Controls.Add(maximum);
            Button reset = Theme.Button("Reset filters", false); reset.Name = "ResetHistoryFilters"; reset.Width = 119; reset.Height = 30; reset.Margin = new Padding(3, 3, 0, 0); reset.Font = Theme.Font(9, true); panel.Controls.Add(reset);
            EventHandler changed = delegate {
                historyDateRange = dated.Checked; historyFrom = from.Value.Date; historyThrough = through.Value.Date;
                historyAmountRange = amounts.Checked; historyMinimum = minimum.Value; historyMaximum = maximum.Value;
                from.Enabled = through.Enabled = dated.Checked; minimum.Enabled = maximum.Enabled = amounts.Checked; FillEntries(true);
            };
            dated.CheckedChanged += changed; from.ValueChanged += changed; through.ValueChanged += changed; amounts.CheckedChanged += changed; minimum.ValueChanged += changed; maximum.ValueChanged += changed;
            reset.Click += delegate { accountFilter = categoryFilter = searchText = ""; kindFilter = "All types"; historyDateRange = historyAmountRange = false; entriesPageIndex = 0; ShowPage(1); };
            return panel;
        }
        private void UseTemplate(string id)
        {
            newEntryPreset = Ledger.TemplateDraft(Data, id, DateTime.Today);
            try { EntryEditor(null, newEntryPreset.Kind); }
            finally { newEntryPreset = null; }
        }
        private void Templates()
        {
            string chosen = null;
            using (Form f = new Form { Text = "Quick-entry templates", Width = 900, Height = 590, MinimumSize = new Size(760, 460), StartPosition = FormStartPosition.CenterParent,
                BackColor = Theme.Canvas, Font = Theme.Font(10, false), Padding = new Padding(20) })
            {
                TableLayoutPanel shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
                shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
                shell.Controls.Add(Theme.Label("Use a template to open a draft dated today. Review the details and Save to record it. A template alone never changes a balance.", 10, false, Theme.Muted), 0, 0);
                PagedGridPanel pager = new PagedGridPanel("TemplatesPager"); DataGridView grid = pager.Grid;
                Theme.Column(grid, "name", "TEMPLATE", 27); Theme.Column(grid, "type", "TYPE", 12); Theme.Column(grid, "payee", "PAYEE", 20); Theme.Column(grid, "account", "ACCOUNT", 21); Theme.Column(grid, "amount", "AMOUNT", 20);
                Action fill = delegate { pager.SetRows(Data.Templates.OrderBy(t => t.Name).Select(t => new GridItem(t.Id, t.Name, t.Kind, t.Payee ?? "", Data.Accounts.First(a => a.Id == t.AccountId).Name,
                    t.Amount.HasValue ? Theme.MoneyText(t.Amount.Value, Data.Accounts.First(a => a.Id == t.AccountId).Currency) : "Enter amount")), false); }; fill(); shell.Controls.Add(pager, 0, 1);
                FlowLayoutPanel buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 9, 0, 0) };
                Button use = Theme.Button("Use template", true), add = Theme.Button("+ New", false), edit = Theme.Button("Edit", false), delete = Theme.Button("Delete", false), close = Theme.Button("Close", false);
                use.Name = "UseQuickTemplate"; add.Name = "NewQuickTemplate"; edit.Name = "EditQuickTemplate"; delete.Name = "DeleteQuickTemplate";
                foreach (Button button in new Button[] { use, add, edit, delete, close }) buttons.Controls.Add(button);
                shell.Controls.Add(buttons, 0, 2); f.Controls.Add(shell);
                Func<QuickEntryTemplate> selected = delegate { Ledger.Require(grid.SelectedRows.Count > 0, "Select a template first."); return Data.Templates.First(t => t.Id == (string)grid.SelectedRows[0].Tag); };
                Action choose = delegate { ActionSafely(delegate { chosen = selected().Id; f.Close(); }); };
                use.Click += delegate { choose(); }; grid.CellDoubleClick += delegate(object sender, DataGridViewCellEventArgs e) { if (e.RowIndex >= 0) choose(); };
                add.Click += delegate { TemplateEditor(null, null, f); fill(); };
                edit.Click += delegate { ActionSafely(delegate { TemplateEditor(selected(), null, f); fill(); }); };
                delete.Click += delegate { ActionSafely(delegate { string id = selected().Id; if (Confirm("Delete this quick-entry template? Recorded entries will be kept.")) { Commit(d => d.Templates.RemoveAll(t => t.Id == id)); fill(); } }); };
                close.Click += delegate { f.Close(); }; f.ShowDialog(this);
            }
            if (chosen != null) ActionSafely(delegate { UseTemplate(chosen); });
        }
        private void TemplateEditor(QuickEntryTemplate existing, QuickEntryTemplate preset, IWin32Window owner)
        {
            if (Data.Accounts.Count == 0) { ActionSafely(delegate { throw new BudgetException("Add an account before creating a template."); }); return; }
            QuickEntryTemplate seed = existing ?? preset;
            using (Editor f = new Editor(existing == null ? "New quick-entry template" : "Edit quick-entry template", "Set reusable details for an expense or income. Leave Remember amount off to enter the price each time. Split templates keep a fixed total. No transaction is recorded when you save a template.", 44))
            {
                TextBox name = Theme.Text(seed == null ? "" : seed.Name); name.MaxLength = 80; name.Name = "TemplateName";
                ComboBox kind = Strings(new[] { "Expense", "Income" }, seed == null ? "Expense" : seed.Kind); kind.Name = "TemplateKind";
                ComboBox payee = PayeeBox(seed == null ? "" : seed.Payee); payee.Name = "TemplatePayee";
                TextBox description = Theme.Text(seed == null ? "" : seed.Description); description.Name = "TemplateDescription";
                ComboBox account = AccountChoices(seed == null ? accountFilter : seed.AccountId, false), category = CategoryChoices(seed == null ? categoryFilter : seed.CategoryId, false);
                account.Name = "TemplateAccount"; category.Name = "TemplateCategory";
                CheckBox remember = new CheckBox { Text = "Remember amount", Checked = seed != null && seed.Amount.HasValue, Dock = DockStyle.Fill, Name = "TemplateRememberAmount" };
                NumericUpDown amount = Theme.Money(seed == null ? 0 : seed.Amount ?? 0); amount.Minimum = 0; amount.Name = "TemplateAmount";
                List<ExpenseSplit> parts = seed != null && seed.Splits != null ? seed.Splits.Select(p => new ExpenseSplit { CategoryId = p.CategoryId, Amount = p.Amount }).ToList() : new List<ExpenseSplit>();
                FlowLayoutPanel splits = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = new Padding(0), Padding = new Padding(0, 4, 0, 0), WrapContents = false };
                Button split = Theme.Button("Split purchase…", false), single = Theme.Button("One category", false); split.Width = 154; single.Width = 130; split.Height = single.Height = 32; split.Name = "TemplateEditSplit"; single.Name = "TemplateClearSplit"; splits.Controls.Add(split); splits.Controls.Add(single);
                Action configure = delegate {
                    bool expense = Icons.Selected(kind) == "Expense", activeSplit = expense && parts.Count > 0;
                    category.Enabled = expense && !activeSplit; remember.Enabled = !activeSplit; amount.Enabled = remember.Checked;
                    split.Enabled = expense && remember.Checked; single.Visible = activeSplit; split.Text = activeSplit ? "Edit split (" + parts.Count + ")" : "Split purchase…";
                };
                kind.SelectedIndexChanged += delegate { configure(); }; remember.CheckedChanged += delegate { configure(); }; configure();
                split.Click += delegate { try { Ledger.Require(amount.Value > 0, "Enter a positive total before splitting the template."); Account a = Data.Accounts.First(x => x.Id == Icons.Selected(account));
                    List<ExpenseSplit> initial = parts.Count > 0 ? parts : new List<ExpenseSplit> { new ExpenseSplit { CategoryId = Icons.Selected(category), Amount = amount.Value } };
                    List<ExpenseSplit> edited = SplitExpenseEditor(amount.Value, a.Currency, initial, f); if (edited != null) { parts = edited; remember.Checked = true; configure(); }
                } catch (BudgetException ex) { MessageBox.Show(f, ex.Message, "Template split", MessageBoxButtons.OK, MessageBoxIcon.Information); } };
                single.Click += delegate { parts = new List<ExpenseSplit>(); configure(); };
                f.Field("Template name", name); f.Field("Type", kind); f.Field("Payee / shop", payee); f.Field("Description", description); f.Field("Account", account); f.Field("Expense category", category); f.Field("Default amount", remember); f.Field("Amount (account currency)", amount); f.Field("Split purchase", splits);
                f.Finish(delegate { Commit(d => {
                    QuickEntryTemplate t = existing == null ? new QuickEntryTemplate { Id = Ledger.NewId() } : d.Templates.First(x => x.Id == existing.Id);
                    t.Name = name.Text.Trim(); t.Kind = Icons.Selected(kind); t.Payee = payee.Text.Trim(); t.Description = description.Text.Trim(); t.AccountId = Icons.Selected(account);
                    t.Splits = t.Kind == "Expense" && parts.Count > 0 ? parts.Select(p => new ExpenseSplit { CategoryId = p.CategoryId, Amount = p.Amount }).ToList() : null;
                    t.CategoryId = t.Kind == "Expense" && (t.Splits == null || t.Splits.Count == 0) ? Icons.Selected(category) : null; t.Amount = remember.Checked ? (decimal?)amount.Value : null;
                    if (existing == null) d.Templates.Add(t); Ledger.RememberPayee(d, t.Payee, t.AccountId, t.CategoryId);
                }); }); f.ShowDialog(owner);
            }
        }
        private void Payees()
        {
            using (Form f = new Form { Text = "Payees and autofill", Width = 800, Height = 570, MinimumSize = new Size(650, 440), StartPosition = FormStartPosition.CenterParent,
                BackColor = Theme.Canvas, Font = Theme.Font(10, false), Padding = new Padding(20) })
            {
                TableLayoutPanel shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
                shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 55)); shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
                shell.Controls.Add(Theme.Label("Known payees suggest their last saved account and category on new entries. Editing or deleting a profile leaves recorded payee names unchanged.", 10, false, Theme.Muted), 0, 0);
                PagedGridPanel pager = new PagedGridPanel("PayeesPager"); DataGridView grid = pager.Grid;
                Theme.Column(grid, "name", "PAYEE", 40); Theme.Column(grid, "account", "DEFAULT ACCOUNT", 30); Theme.Column(grid, "category", "DEFAULT CATEGORY", 30);
                Action fill = delegate { pager.SetRows(Data.Payees.OrderBy(p => p.Name).Select(p => new GridItem(p.Id, p.Name,
                    Data.Accounts.Where(a => a.Id == p.DefaultAccountId).Select(a => a.Name).FirstOrDefault() ?? "No default",
                    Data.Categories.Where(c => c.Id == p.DefaultCategoryId).Select(c => c.Name).FirstOrDefault() ?? "No default")), false); }; fill(); shell.Controls.Add(pager, 0, 1);
                FlowLayoutPanel buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 9, 0, 0) };
                Button add = Theme.Button("+ New payee", true), edit = Theme.Button("Edit", false), delete = Theme.Button("Delete", false), close = Theme.Button("Close", false);
                add.Name = "NewPayee"; edit.Name = "EditPayee"; delete.Name = "DeletePayee";
                foreach (Button button in new Button[] { add, edit, delete, close }) buttons.Controls.Add(button); shell.Controls.Add(buttons, 0, 2); f.Controls.Add(shell);
                Func<PayeeProfile> selected = delegate { Ledger.Require(grid.SelectedRows.Count > 0, "Select a payee first."); return Data.Payees.First(p => p.Id == (string)grid.SelectedRows[0].Tag); };
                add.Click += delegate { PayeeEditor(null, f); fill(); }; edit.Click += delegate { ActionSafely(delegate { PayeeEditor(selected(), f); fill(); }); };
                grid.CellDoubleClick += delegate(object sender, DataGridViewCellEventArgs e) { if (e.RowIndex >= 0) ActionSafely(delegate { PayeeEditor(selected(), f); fill(); }); };
                delete.Click += delegate { ActionSafely(delegate { string id = selected().Id; if (Confirm("Delete this payee's autofill profile? Recorded entries and their payee names will be kept.")) { Commit(d => d.Payees.RemoveAll(p => p.Id == id)); fill(); } }); };
                close.Click += delegate { f.Close(); }; f.ShowDialog(this);
            }
        }
        private void PayeeEditor(PayeeProfile existing, IWin32Window owner)
        {
            using (Editor f = new Editor(existing == null ? "New payee" : "Edit payee", "Default account and category are optional suggestions for new entries. Rename changes this profile; existing entries keep the name recorded at the time."))
            {
                TextBox name = Theme.Text(existing == null ? "" : existing.Name); name.MaxLength = 80; name.Name = "PayeeName";
                ComboBox account = AccountChoices(existing == null ? "" : existing.DefaultAccountId, true), category = CategoryChoices(existing == null ? "" : existing.DefaultCategoryId, true);
                account.Items[0] = new Pick("", "No default account"); category.Items[0] = new Pick("", "No default category"); account.Name = "PayeeDefaultAccount"; category.Name = "PayeeDefaultCategory";
                f.Field("Payee name", name); f.Field("Default account", account); f.Field("Default category", category);
                f.Finish(delegate { Commit(d => { PayeeProfile p = existing == null ? new PayeeProfile { Id = Ledger.NewId() } : d.Payees.First(x => x.Id == existing.Id);
                    p.Name = name.Text.Trim(); p.DefaultAccountId = Icons.Selected(account); p.DefaultCategoryId = Icons.Selected(category); if (existing == null) d.Payees.Add(p);
                }); }); f.ShowDialog(owner);
            }
        }
    }
}
