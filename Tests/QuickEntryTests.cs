using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using BudgetDesk;

internal static class QuickEntryTests
{
    static int passed;
    static string images;
    static readonly DateTime Today = DateTime.Today;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); passed++; Console.WriteLine("PASS " + message); }
    static void Reject(Action action) { try { action(); } catch (BudgetException) { return; } throw new Exception("Expected rejection."); }
    static BudgetData Data()
    {
        BudgetData d = BudgetData.Empty();
        d.Accounts.Add(new Account { Id = "bank", Name = "Main bank", Icon = "Bank", Currency = "CZK", OpeningDate = "1900-01-01", OpeningBalance = 10000 });
        d.Accounts.Add(new Account { Id = "cash", Name = "Cash", Icon = "Wallet", Currency = "CZK", OpeningDate = "1900-01-01", OpeningBalance = 1000 });
        return d;
    }
    static Entry Expense(BudgetData d, string description, DateTime date, decimal amount)
    { return new Entry { Id = Ledger.NewId(), Kind = "Expense", Date = Ledger.Iso(date), AccountId = "bank", CategoryId = d.Categories[0].Id, Description = description, Amount = amount, RateToCzk = 1 }; }
    static QuickEntryTemplate Template(BudgetData d)
    {
        QuickEntryTemplate t = new QuickEntryTemplate { Id = Ledger.NewId(), Name = "Lunch", Kind = "Expense", Payee = "Market", Description = "Lunch", AccountId = "bank", CategoryId = d.Categories[0].Id, Amount = 150 };
        d.Templates.Add(t); return t;
    }
    static IEnumerable<Control> Tree(Control parent) { foreach (Control c in parent.Controls) { yield return c; foreach (Control nested in Tree(c)) yield return nested; } }
    static void Pump() { Application.DoEvents(); Application.DoEvents(); }
    static Control Named(Control parent, string name) { return parent.Controls.Find(name, true).Single(); }
    static Control Field(Form f, string label)
    {
        foreach (TableLayoutPanel table in Tree(f).OfType<TableLayoutPanel>().Where(t => t.ColumnCount == 2))
            for (int row = 0; row < table.RowCount; row++) { Label l = table.GetControlFromPosition(0, row) as Label; if (l != null && l.Text == label) return table.GetControlFromPosition(1, row); }
        throw new Exception("Missing field " + label);
    }
    static void Call(MainForm main, string method, params object[] args) { typeof(MainForm).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(main, args); }
    static void Capture(Form form, string name)
    {
        form.Location = new Point(20, 20); form.BringToFront(); form.Activate();
        for (int i = 0; i < 10; i++) { Pump(); System.Threading.Thread.Sleep(10); }
        form.Refresh(); Pump(); using (Bitmap b = new Bitmap(form.Width, form.Height)) { using (Graphics g = Graphics.FromImage(b)) g.CopyFromScreen(form.Location, Point.Empty, form.Size); b.Save(Path.Combine(images, name + ".png")); }
    }
    static void Visible(Control control) { for (Control c = control; c.Parent != null; c = c.Parent) if (!c.Parent.ClientRectangle.Contains(c.Bounds)) throw new Exception(c.Name + " is outside its parent."); }
    static void Modal(MainForm main, Action open, Action<Form> work)
    {
        Exception failure = null;
        using (Timer timer = new Timer { Interval = 110 })
        {
            timer.Tick += delegate { timer.Stop(); Form f = Application.OpenForms.Cast<Form>().Last(x => x != main); try { work(f); } catch (Exception ex) { failure = ex; f.Close(); } };
            timer.Start(); open();
        }
        if (failure != null) throw failure; Pump();
    }
    static void Editor(MainForm main, Action open, Action<Form> work, bool save)
    { Modal(main, open, delegate(Form f) { work(f); if (save) ((Editor)f).Save.PerformClick(); else f.Close(); }); }
    static void Nested(MainForm main, Form parent, Action open, Action<Form> work, bool save)
    {
        Exception failure = null;
        using (Timer timer = new Timer { Interval = 100 })
        {
            timer.Tick += delegate { timer.Stop(); Form f = Application.OpenForms.Cast<Form>().Last(x => x != main && x != parent); try { work(f); if (save) ((Editor)f).Save.PerformClick(); else f.Close(); } catch (Exception ex) { failure = ex; f.Close(); } };
            timer.Start(); open();
        }
        if (failure != null) throw failure; Pump();
    }
    static void UseThroughManager(MainForm main, Action<Form> edit, bool save)
    {
        Exception failure = null; int stage = 0;
        using (Timer timer = new Timer { Interval = 110 })
        {
            timer.Tick += delegate {
                try {
                    Form f = Application.OpenForms.Cast<Form>().Last(x => x != main);
                    if (stage == 0) { stage = 1; ((Button)Named(f, "UseQuickTemplate")).PerformClick(); }
                    else { timer.Stop(); edit(f); if (save) ((Editor)f).Save.PerformClick(); else f.Close(); }
                }
                catch (Exception ex) { failure = ex; timer.Stop(); foreach (Form f in Application.OpenForms.Cast<Form>().Where(x => x != main).ToArray()) f.Close(); }
            };
            timer.Start(); ((Button)Named(main, "QuickTemplates")).PerformClick();
        }
        if (failure != null) throw failure; Pump();
    }
    static HashSet<string> Walk(PagedGridPanel pager)
    {
        HashSet<string> ids = new HashSet<string>(); pager.PageIndex = 0;
        for (int page = 0; page < 2000; page++)
        {
            if (pager.Grid.ScrollBars != ScrollBars.None) throw new Exception("Unexpected scrollbar.");
            foreach (DataGridViewRow row in pager.Grid.Rows) if (!pager.Grid.ClientRectangle.Contains(pager.Grid.GetRowDisplayRectangle(row.Index, false)) || !ids.Add((string)row.Tag)) throw new Exception("Clipped or repeated row.");
            Button next = Tree(pager).OfType<Button>().Single(b => b.Text == "Next"); if (!next.Enabled) return ids; next.PerformClick(); Pump();
        }
        throw new Exception("Pagination did not finish.");
    }
    [STAThread]
    static int Main(string[] args)
    {
        string temp = Path.Combine(Path.GetTempPath(), "budgetdesk-quick-entry-" + Ledger.NewId()); Directory.CreateDirectory(temp);
        images = args.Length > 0 ? args[0] : Path.Combine(temp, "images"); Directory.CreateDirectory(images);
        try
        {
            BudgetData d = Data(); QuickEntryTemplate t = Template(d); Ledger.Validate(d); decimal balance = Ledger.TotalBalance(d, Today);
            Entry first = Ledger.TemplateDraft(d, t.Id, Today), second = Ledger.TemplateDraft(d, t.Id, Today);
            Check(d.Entries.Count == 0 && Ledger.TotalBalance(d, Today) == balance && first.Id != second.Id && first.Date == Ledger.Iso(Today), "templates create fresh drafts dated today without recording or deducting money");
            t.Amount = null; first = Ledger.TemplateDraft(d, t.Id, Today); Check(first.Amount == 0, "templates can leave the amount for each new entry");
            d.Entries.Add(first); Reject(() => Ledger.Validate(d)); d.Entries.Clear(); first.Amount = 120; d.Entries.Add(first); Ledger.Validate(d); Check(Ledger.Balance(d, "bank", Today) == 9880, "a template draft becomes one expense only when a valid amount is recorded");
            d = Data(); t = Template(d); t.Amount = 1000; t.CategoryId = null; t.Splits = new List<ExpenseSplit> { new ExpenseSplit { CategoryId = d.Categories[0].Id, Amount = 600 }, new ExpenseSplit { CategoryId = d.Categories[1].Id, Amount = 400 } }; Ledger.Validate(d);
            first = Ledger.TemplateDraft(d, t.Id, Today); first.Splits[0].Amount = 500; Check(t.Splits[0].Amount == 600 && d.Entries.Count == 0, "split template drafts own independent allocations and do not mutate the reusable template");
            first.BillId = "original-bill"; first.PlanId = "original-plan"; first.ScheduledDate = Ledger.Iso(Today); first.Splits[0].Amount = 600;
            QuickEntryTemplate copied = Ledger.TemplateFromEntry(first); copied.Name = "Copied split"; d.Templates.Add(copied); Entry copyDraft = Ledger.TemplateDraft(d, copied.Id, Today);
            Check(copyDraft.BillId == null && copyDraft.PlanId == null && copyDraft.ScheduledDate == null && copyDraft.Splits.Count == 2, "saved templates retain split amounts without copying recurring or one-off payment identities");
            Reject(() => Ledger.DeleteAccount(d, "bank")); Reject(() => Ledger.DeleteCategory(d, d.Categories[1].Id)); t.Amount = null; Reject(() => Ledger.Validate(d)); t.Amount = 1000;
            Check(d.Accounts.Count == 2 && d.Categories.Count == 8, "template references protect accounts/categories and split templates require a fixed total");
            d.Accounts[0].Currency = "EUR"; d.Rates["EUR"] = 26; copyDraft = Ledger.TemplateDraft(d, copied.Id, Today); Check(copyDraft.RateToCzk == 26, "new template uses take the current account exchange rate");
            copied.Kind = "Income"; copied.Splits = null; copied.CategoryId = null; copied.Amount = 500; Ledger.Validate(d); Check(Ledger.TemplateDraft(d, copied.Id, Today).Kind == "Income", "income templates are supported without an expense category");
            d = Data(); Ledger.RememberPayee(d, " Market ", "bank", d.Categories[0].Id); Ledger.RememberPayee(d, "market", "cash", d.Categories[3].Id); Ledger.Validate(d);
            Check(d.Payees.Count == 1 && Ledger.FindPayee(d, "MARKET").DefaultAccountId == "cash", "payees are trimmed and matched without case duplicates and remember the latest defaults");
            first = Expense(d, "Lunch", new DateTime(2024, 2, 29), 50); first.Payee = "Market"; d.Entries.Add(first); d.Payees[0].Name = "Renamed shop"; d.Payees.Clear();
            Check(Ledger.SearchEntries(d, new EntrySearch { Text = "market" }).Single().Id == first.Id && first.Payee == "Market", "renaming or deleting payee profiles preserves historical payee names and searchability");
            d = Data(); Ledger.RememberPayee(d, "Unused defaults", "cash", d.Categories[1].Id); Ledger.DeleteAccount(d, "cash"); Ledger.DeleteCategory(d, d.Categories[1].Id); Ledger.Validate(d);
            Check(String.IsNullOrEmpty(d.Payees.Single().DefaultAccountId) && String.IsNullOrEmpty(d.Payees.Single().DefaultCategoryId), "deleting unused accounts or categories clears optional payee defaults safely");
            d = Data(); Entry oldEntry = Expense(d, "Work lunch", new DateTime(2024, 2, 29), 50); oldEntry.Payee = "Café Praha"; d.Entries.Add(oldEntry);
            Entry current = Expense(d, "Groceries", Today, 120); current.Payee = "Market"; d.Entries.Add(current);
            Entry split = Expense(d, "Split history", new DateTime(2025, 1, 1), 1000); split.CategoryId = null; split.Splits = new List<ExpenseSplit> { new ExpenseSplit { CategoryId = d.Categories[0].Id, Amount = 600 }, new ExpenseSplit { CategoryId = d.Categories[1].Id, Amount = 400 } }; d.Entries.Add(split);
            Entry transfer = new Entry { Id = Ledger.NewId(), Kind = "Transfer", Date = Ledger.Iso(Today), AccountId = "bank", ToAccountId = "cash", Amount = 10, ReceivedAmount = 10, RateToCzk = 1, Description = "ATM" }; d.Entries.Add(transfer); Ledger.Validate(d);
            Check(Ledger.SearchEntries(d, new EntrySearch()).Count() == 4 && Ledger.SearchEntries(d, new EntrySearch { Text = "cAFÉ bank" }).Single().Id == oldEntry.Id, "history searches every year and combines payee, description and account words without case sensitivity");
            Check(Ledger.SearchEntries(d, new EntrySearch { From = new DateTime(2024, 2, 29), Through = new DateTime(2025, 1, 1) }).Count() == 2, "history date ranges include both endpoints and leap days");
            Check(Ledger.SearchEntries(d, new EntrySearch { AccountId = "cash" }).Single().Id == transfer.Id && Ledger.SearchEntries(d, new EntrySearch { CategoryId = d.Categories[1].Id }).Single().Id == split.Id, "account search includes transfer destinations and category search includes split allocations");
            Check(Ledger.SearchEntries(d, new EntrySearch { MinimumAmount = 50, MaximumAmount = 50 }).Single().Id == oldEntry.Id && !Ledger.SearchEntries(d, new EntrySearch { Text = "split", MinimumAmount = 600, MaximumAmount = 600 }).Any(), "amount ranges use the full entry amount in its account currency, including exact matches");
            string before = BudgetStore.Serialize(d); Reject(() => Ledger.SearchEntries(d, new EntrySearch { From = Today, Through = Today.AddDays(-1) }).ToList()); Reject(() => Ledger.SearchEntries(d, new EntrySearch { MinimumAmount = 10, MaximumAmount = 1 }).ToList());
            Check(before == BudgetStore.Serialize(d), "invalid search ranges report a problem without changing financial data");
            Ledger.RememberPayee(d, "Market", "bank", d.Categories[0].Id); Template(d); BudgetData clone = BudgetStore.Parse(BudgetStore.Serialize(d));
            Check(clone.Templates.Count == 1 && clone.Payees.Count == 1 && clone.Entries[0].Payee == "Café Praha", "templates, payee preferences and historical payees persist together");
            string csv = BudgetStore.SearchCsv(d, new EntrySearch { Text = "café" }); Check(csv.Contains("Café Praha") && !csv.Contains("Split history"), "existing result exports include payee names and every matching page only");
            BudgetData prior = Data(); prior.Entries.Add(Expense(prior, "Old purchase", Today.AddMonths(-1), 20)); prior.Goals.Add(new SavingsGoal { Id = "goal", Name = "Keep goal", Icon = "Star", Currency = "CZK", TargetAmount = 5000, SavedAmount = 1000 }); prior.PlannedExpenses.Add(new PlannedExpense { Id = "plan", Description = "Keep plan", AccountId = "bank", CategoryId = prior.Categories[0].Id, DueDate = Ledger.Iso(Today.AddDays(20)), Amount = 100 });
            JavaScriptSerializer serializer = new JavaScriptSerializer(); var old = serializer.Deserialize<Dictionary<string, object>>(BudgetStore.Serialize(prior)); old["SchemaVersion"] = 3; old.Remove("Payees"); old.Remove("Templates"); string oldJson = serializer.Serialize(old).Replace(",\"Payee\":null", "");
            string upgradeFolder = Path.Combine(temp, "upgrade"); Directory.CreateDirectory(upgradeFolder); File.WriteAllText(Path.Combine(upgradeFolder, "budget.json"), oldJson); BudgetStore upgradedStore = new BudgetStore(upgradeFolder); BudgetData upgraded = upgradedStore.Load();
            Check(upgraded.SchemaVersion == 4 && upgraded.Goals.Single().SavedAmount == 1000 && upgraded.PlannedExpenses.Single().Id == "plan" && upgraded.Entries.Single().Description == "Old purchase" && File.ReadAllText(upgradedStore.FilePath) == oldJson, "schema-three migration preserves goals, plans and historical entries without rewriting during load");
            Template(upgraded); upgradedStore.Save(upgraded); Check(Directory.GetFiles(Path.Combine(upgradeFolder, "Backups")).Any(p => File.ReadAllText(p) == oldJson), "the first template/payee save keeps the exact previous-format backup");
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            d = Data(); BudgetStore store = new BudgetStore(Path.Combine(temp, "ui")); store.Save(d);
            using (MainForm main = new MainForm(store, d, false))
            {
                main.Size = new Size(1000, 700); main.Show(); Pump(); main.ShowPage(1); Pump();
                foreach (string name in new string[] { "RecordedEntriesView", "PlannedExpensesView", "AllHistoryView", "QuickTemplates", "ManagePayees" }) Visible(Named(main, name));
                Modal(main, delegate { ((Button)Named(main, "ManagePayees")).PerformClick(); }, delegate(Form manager) {
                    Nested(main, manager, delegate { ((Button)Named(manager, "NewPayee")).PerformClick(); }, delegate(Form f) { Named(f, "PayeeName").Text = "Market"; Icons.Choose((ComboBox)Named(f, "PayeeDefaultAccount"), "cash"); Icons.Choose((ComboBox)Named(f, "PayeeDefaultCategory"), main.Data.Categories[3].Id); Capture(f, "new-payee"); }, true);
                    manager.Close();
                });
                Check(store.Load().Payees.Single().DefaultAccountId == "cash" && main.Data.Entries.Count == 0, "the Payees manager saves optional defaults without recording an entry");
                Editor(main, delegate { Call(main, "EntryEditor", null, "Expense"); }, delegate(Form f) {
                    ((ComboBox)Named(f, "EntryPayee")).Text = "market";
                    Check(Icons.Selected((ComboBox)Field(f, "Account / send from")) == "cash" && Icons.Selected((ComboBox)Field(f, "Expense category")) == main.Data.Categories[3].Id, "a known payee autofills account and category in the real new-entry form");
                    Capture(f, "payee-autofill"); Icons.Choose((ComboBox)Field(f, "Account / send from"), "bank"); Icons.Choose((ComboBox)Field(f, "Expense category"), main.Data.Categories[0].Id);
                    ((ComboBox)Named(f, "EntryPayee")).Text = ""; ((ComboBox)Named(f, "EntryPayee")).Text = "Market";
                    Check(Icons.Selected((ComboBox)Field(f, "Account / send from")) == "bank" && Icons.Selected((ComboBox)Field(f, "Expense category")) == main.Data.Categories[0].Id, "payee autofill preserves account and category choices already made by the user");
                    Field(f, "Description").Text = "Manual purchase"; ((NumericUpDown)Field(f, "Amount (account currency)")).Value = 123.45m;
                    foreach (Control c in Tree(f).Where(c => c is ComboBox || c is NumericUpDown || c is Button)) Visible(c);
                }, true);
                Check(main.Data.Payees.Single().DefaultAccountId == "bank" && main.Data.Entries.Single().Payee == "Market" && Ledger.Balance(main.Data, "bank", Today) == 9876.55m, "saving a payee entry records one expense and remembers the final explicit choices");
                main.Commit(data => Ledger.RememberPayee(data, "Other shop", "cash", data.Categories[3].Id));
                Editor(main, delegate { Call(main, "EntryEditor", null, "Expense"); }, delegate(Form f) {
                    ComboBox account = (ComboBox)Field(f, "Account / send from"), category = (ComboBox)Field(f, "Expense category");
                    typeof(ComboBox).GetMethod("OnSelectionChangeCommitted", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(account, new object[] { EventArgs.Empty });
                    typeof(ComboBox).GetMethod("OnSelectionChangeCommitted", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(category, new object[] { EventArgs.Empty });
                    ((ComboBox)Named(f, "EntryPayee")).Text = "Other shop";
                    Check(Icons.Selected(account) == "bank" && Icons.Selected(category) == main.Data.Categories[0].Id, "explicitly choosing an already-selected account or category also protects it from payee autofill");
                }, false);
                Editor(main, delegate { Call(main, "EntryEditor", main.Data.Entries.Single(), "Expense"); }, delegate(Form f) { ((ComboBox)Named(f, "EntryPayee")).Text = "Other shop"; if (Icons.Selected((ComboBox)Field(f, "Account / send from")) != "bank") throw new Exception("Historical account changed by autofill."); }, false);
                Check(main.Data.Entries.Single().Payee == "Market", "editing or cancelling a historical payee does not silently change its account or saved text");
                Modal(main, delegate { ((Button)Named(main, "QuickTemplates")).PerformClick(); }, delegate(Form manager) {
                    Nested(main, manager, delegate { ((Button)Named(manager, "NewQuickTemplate")).PerformClick(); }, delegate(Form f) {
                        Named(f, "TemplateName").Text = "Optional lunch"; Named(f, "TemplateDescription").Text = "Lunch template"; ((ComboBox)Named(f, "TemplatePayee")).Text = "Market";
                        Icons.Choose((ComboBox)Named(f, "TemplateAccount"), "bank"); Icons.Choose((ComboBox)Named(f, "TemplateCategory"), main.Data.Categories[0].Id); Capture(f, "new-template");
                    }, true); manager.Close();
                });
                Check(main.Data.Templates.Single().Amount == null && main.Data.Entries.Count == 1, "the Templates manager saves a reusable optional-amount template without a transaction");
                main.Commit(data => Ledger.RememberPayee(data, "Market", "cash", data.Categories[3].Id));
                before = BudgetStore.Serialize(main.Data);
                UseThroughManager(main, delegate(Form f) { Check(((NumericUpDown)Field(f, "Amount (account currency)")).Value == 0 && Icons.Selected((ComboBox)Field(f, "Account / send from")) == "bank", "Use template opens a draft and preserves its explicit defaults instead of applying different payee preferences"); Capture(f, "template-draft"); }, false);
                Check(before == BudgetStore.Serialize(main.Data), "cancelling a used template changes neither data nor balances");
                UseThroughManager(main, delegate(Form f) { ((NumericUpDown)Field(f, "Amount (account currency)")).Value = 50; }, true);
                Check(main.Data.Entries.Count == 2 && main.Data.Entries.Last().Date == Ledger.Iso(Today) && Ledger.Balance(main.Data, "bank", Today) == 9826.55m, "saving a template draft records today's expense exactly once");
                Editor(main, delegate { Call(main, "EntryEditor", null, "Expense"); }, delegate(Form f) {
                    Field(f, "Description").Text = "Fuel shortcut"; ((NumericUpDown)Field(f, "Amount (account currency)")).Value = 750;
                    Nested(main, f, delegate { ((Button)Named(f, "SaveAsTemplate")).PerformClick(); }, delegate(Form template) { Named(template, "TemplateName").Text = "Fuel quick entry"; }, true);
                }, false);
                Check(main.Data.Templates.Count == 2 && main.Data.Entries.Count == 2 && main.Data.Templates.Any(x => x.Name == "Fuel quick entry"), "Save as template can persist a shortcut while cancelling the parent expense records no purchase");
                main.Commit(data => {
                    for (int i = 0; i < 100; i++) { Entry entry = Expense(data, "Historic item" + i, Today.AddMonths(-i), 10 + i); entry.Payee = "Shop " + i; data.Entries.Add(entry); }
                    Entry entrySplit = Expense(data, "Split history", new DateTime(2020, 3, 1), 1000); entrySplit.CategoryId = null; entrySplit.Splits = new List<ExpenseSplit> { new ExpenseSplit { CategoryId = data.Categories[0].Id, Amount = 600 }, new ExpenseSplit { CategoryId = data.Categories[1].Id, Amount = 400 } }; data.Entries.Add(entrySplit);
                });
                ((Button)Named(main, "AllHistoryView")).PerformClick(); Pump(); ((Button)Named(main, "ResetHistoryFilters")).PerformClick(); Pump();
                PagedGridPanel history = Tree(main).OfType<PagedGridPanel>().Single();
                Check(Walk(history).SetEquals(main.Data.Entries.Select(e => e.Id)), "all-history pages expose every entry across more than eight years without clipping, duplicates or scrollbars");
                Capture(main, "all-history-minimum");
                ((TextBox)Named(main, "EntrySearchText")).Text = "Historic item79"; Pump(); history = Tree(main).OfType<PagedGridPanel>().Single();
                Check(history.ItemCount == 1 && history.PageIndex == 0 && history.Grid.Rows[0].Cells[1].Value.ToString().Contains("item79"), "typing a history search resets pagination and finds an older-year purchase");
                string editedId = (string)history.Grid.Rows[0].Tag;
                Editor(main, delegate { Tree(main).OfType<Button>().Single(b => b.Text == "Edit selected").PerformClick(); }, delegate(Form f) { Field(f, "Description").Text = "Edited historic purchase"; }, true);
                Check(main.Data.Entries.Single(e => e.Id == editedId).Description == "Edited historic purchase" && main.Data.Entries.Count == 103, "editing an older search result updates the intended entry without adding another");
                ((Button)Named(main, "ResetHistoryFilters")).PerformClick(); Pump();
                ((CheckBox)Named(main, "HistoryUseDates")).Checked = true; ((DateTimePicker)Named(main, "HistoryFrom")).Value = Today; ((DateTimePicker)Named(main, "HistoryThrough")).Value = Today.AddDays(-1); Pump();
                Check(Tree(main).OfType<PagedGridPanel>().Single().ItemCount == 0 && Tree(main).OfType<Label>().Any(l => l.Text.Contains("start date must")), "invalid date ranges display a clear message without crashing or losing data");
                ((Button)Named(main, "ResetHistoryFilters")).PerformClick(); Pump(); ((TextBox)Named(main, "EntrySearchText")).Text = "Historic";
                ((CheckBox)Named(main, "HistoryUseAmounts")).Checked = true; ((NumericUpDown)Named(main, "HistoryMinimum")).Value = 50; ((NumericUpDown)Named(main, "HistoryMaximum")).Value = 50; Pump();
                Check(Tree(main).OfType<PagedGridPanel>().Single().ItemCount == 1 && Named(main, "HistoryExpenses").Text.Contains("50.00"), "amount filtering and result metrics use all matching history entries");
                ((Button)Named(main, "ResetHistoryFilters")).PerformClick(); Pump(); ((TextBox)Named(main, "EntrySearchText")).Text = "Split history";
                ComboBox categories = Tree(main).OfType<ComboBox>().Single(c => c.Items.Cast<Pick>().Any(p => p.Text == "All categories")); Icons.Choose(categories, main.Data.Categories[1].Id); Pump();
                Check(Tree(main).OfType<PagedGridPanel>().Single().ItemCount == 1 && Named(main, "HistoryExpenses").Text.Contains("400.00"), "history category filters and totals correctly isolate the matching split share");
                Capture(main, "history-split-filter");
                ((Button)Named(main, "ResetHistoryFilters")).PerformClick(); Pump();
                main.WindowState = FormWindowState.Minimized; for (int i = 0; i < 15; i++) { Pump(); System.Threading.Thread.Sleep(10); }
                if (main.WindowState != FormWindowState.Minimized) throw new Exception("Minimize not applied.");
                main.Commit(data => data.Entries.Single(e => e.Id == editedId).Description = "Edited during minimize"); main.WindowState = FormWindowState.Normal; for (int i = 0; i < 15; i++) { Pump(); System.Threading.Thread.Sleep(10); }
                Check(main.WindowState == FormWindowState.Normal && Tree(main).OfType<PagedGridPanel>().Single().ItemCount == 103, "history results restore after minimization and a saved edit");
                main.Size = new Size(1320, 930); Pump(); Capture(main, "all-history-normal");
                main.Commit(data => {
                    for (int i = 0; i < 100; i++) { data.Templates.Add(new QuickEntryTemplate { Id = Ledger.NewId(), Name = "Template " + i, Kind = "Expense", Description = "Shortcut " + i, AccountId = "bank", CategoryId = data.Categories[0].Id }); Ledger.RememberPayee(data, "Payee " + i, "bank", data.Categories[0].Id); }
                });
                Modal(main, delegate { ((Button)Named(main, "QuickTemplates")).PerformClick(); }, delegate(Form manager) { Check(Walk(Tree(manager).OfType<PagedGridPanel>().Single()).SetEquals(main.Data.Templates.Select(tpl => tpl.Id)), "all templates remain reachable through paged management without a twenty-item limit"); manager.Close(); });
                Modal(main, delegate { ((Button)Named(main, "ManagePayees")).PerformClick(); }, delegate(Form manager) { Check(Walk(Tree(manager).OfType<PagedGridPanel>().Single()).SetEquals(main.Data.Payees.Select(profile => profile.Id)), "all remembered payees remain reachable through paged management without a twenty-item limit"); manager.Close(); });
                BudgetData saved = store.Load(); Check(saved.Templates.Count == 102 && saved.Payees.Count >= 102 && saved.Entries.Count == 103, "history, templates and payee preferences survive the same persisted budget reload"); main.Close();
            }
            string blocked = Path.Combine(temp, "blocked"); File.WriteAllText(blocked, "occupied"); d = Data(); Template(d); before = BudgetStore.Serialize(d);
            using (MainForm main = new MainForm(new BudgetStore(blocked), d, false))
            {
                try { main.Commit(data => { data.Entries.Add(Ledger.TemplateDraft(data, data.Templates[0].Id, Today)); Ledger.RememberPayee(data, "Failed save", "bank", data.Categories[0].Id); }); throw new Exception("Expected failed save."); } catch (IOException) { }
                Check(BudgetStore.Serialize(main.Data) == before, "failed template-entry saves preserve transactions, payee preferences, templates and balances");
            }
            Console.WriteLine("\n" + passed + " quick-entry, payee and all-history checks passed."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { if (Directory.Exists(temp)) Directory.Delete(temp, true); }
    }
}
