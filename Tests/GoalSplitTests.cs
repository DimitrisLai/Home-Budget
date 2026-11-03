using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using BudgetDesk;

internal static class GoalSplitTests
{
    static int passed;
    static string images;
    static readonly DateTime Today = DateTime.Today;
    static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); passed++; Console.WriteLine("PASS " + message); }
    static void Reject(Action action)
    { try { action(); } catch (BudgetException) { return; } throw new Exception("Expected rejection."); }
    static BudgetData Data()
    {
        BudgetData d = BudgetData.Empty();
        d.Accounts.Add(new Account { Id = "bank", Name = "Main bank", Icon = "Bank", Currency = "CZK", OpeningBalance = 10000, OpeningDate = "2024-01-01" });
        d.Accounts.Add(new Account { Id = "cash", Name = "Cash", Icon = "Wallet", Currency = "CZK", OpeningBalance = 100, OpeningDate = "2024-01-01" });
        d.Categories[0].MonthlyBudgetCzk = 800; return d;
    }
    static SavingsGoal Goal(BudgetData d)
    {
        SavingsGoal goal = new SavingsGoal { Id = Ledger.NewId(), Name = "Holiday", Icon = "Travel", Currency = "CZK", TargetAmount = 10000, SavedAmount = 2500, TargetDate = Ledger.Iso(Today.AddMonths(2)) };
        d.Goals.Add(goal); return goal;
    }
    static Entry Purchase(BudgetData d)
    {
        Entry entry = new Entry { Id = Ledger.NewId(), Kind = "Expense", Date = Ledger.Iso(Today), AccountId = "bank", Description = "Mixed supermarket purchase", Amount = 1000, RateToCzk = 1,
            Splits = new List<ExpenseSplit> { new ExpenseSplit { CategoryId = d.Categories[0].Id, Amount = 600 }, new ExpenseSplit { CategoryId = d.Categories[1].Id, Amount = 400 } } };
        d.Entries.Add(entry); return entry;
    }
    static IEnumerable<Control> Tree(Control parent)
    { foreach (Control c in parent.Controls) { yield return c; foreach (Control nested in Tree(c)) yield return nested; } }
    static void Pump() { Application.DoEvents(); Application.DoEvents(); }
    static Control Field(Form f, string label)
    {
        foreach (TableLayoutPanel t in Tree(f).OfType<TableLayoutPanel>().Where(t => t.ColumnCount == 2))
            for (int row = 0; row < t.RowCount; row++) { Label l = t.GetControlFromPosition(0, row) as Label; if (l != null && l.Text == label) return t.GetControlFromPosition(1, row); }
        throw new Exception("Missing field " + label);
    }
    static Control Named(Control form, string name) { return form.Controls.Find(name, true).Single(); }
    static void Call(MainForm main, string method, params object[] args)
    { typeof(MainForm).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(main, args); }
    static void Capture(Form form, string name)
    {
        form.Location = new Point(20, 20); form.BringToFront(); form.Activate();
        for (int settle = 0; settle < 12; settle++) { Pump(); System.Threading.Thread.Sleep(10); }
        form.Refresh(); Pump();
        using (Bitmap b = new Bitmap(form.Width, form.Height)) { using (Graphics g = Graphics.FromImage(b)) g.CopyFromScreen(form.Location, Point.Empty, form.Size); b.Save(Path.Combine(images, name + ".png")); }
    }
    static void Visible(Control control)
    {
        for (Control c = control; c.Parent != null; c = c.Parent)
            if (!c.Parent.ClientRectangle.Contains(c.Bounds)) throw new Exception(c.Name + " is clipped by its parent.");
    }
    static void Dialog(MainForm main, Action open, Action<Form> edit, string capture, bool save)
    {
        Exception failure = null;
        using (Timer timer = new Timer { Interval = 100 })
        {
            timer.Tick += delegate {
                timer.Stop(); Form editor = Application.OpenForms.Cast<Form>().Last(f => f != main);
                try { edit(editor); if (capture != null) Capture(editor, capture); if (save) ((Editor)editor).Save.PerformClick(); else editor.Close(); }
                catch (Exception ex) { failure = ex; editor.Close(); }
            };
            timer.Start(); open();
        }
        if (failure != null) throw failure; Pump();
    }
    static void SplitDialog(MainForm main, Form expense, decimal first, decimal second, string capture, bool save, bool allCategories)
    {
        Exception failure = null;
        using (Timer timer = new Timer { Interval = 100 })
        {
            timer.Tick += delegate {
                timer.Stop(); Form split = Application.OpenForms.Cast<Form>().Last(f => f != main && f != expense);
                try {
                    foreach (NumericUpDown n in Tree(split).OfType<NumericUpDown>()) n.Value = allCategories ? 50 : 0;
                    if (!allCategories) { ((NumericUpDown)Named(split, "SplitAmount-" + main.Data.Categories[0].Id)).Value = first; ((NumericUpDown)Named(split, "SplitAmount-" + main.Data.Categories[1].Id)).Value = second; }
                    foreach (NumericUpDown n in Tree(split).OfType<NumericUpDown>()) Visible(n);
                    if (capture != null) Capture(split, capture);
                    if (save) ((Button)Named(split, "SavePurchaseSplit")).PerformClick(); else split.Close();
                }
                catch (Exception ex) { failure = ex; split.Close(); }
            };
            timer.Start(); ((Button)Named(expense, "EditPurchaseSplit")).PerformClick();
        }
        if (failure != null) throw failure; Pump();
    }
    static HashSet<string> Walk(PagedGridPanel pager)
    {
        HashSet<string> ids = new HashSet<string>(); pager.PageIndex = 0;
        for (int p = 0; p < 1000; p++)
        {
            if (pager.Grid.ScrollBars != ScrollBars.None) throw new Exception("Unexpected scrollbar.");
            foreach (DataGridViewRow row in pager.Grid.Rows)
                if (!pager.Grid.ClientRectangle.Contains(pager.Grid.GetRowDisplayRectangle(row.Index, false)) || !ids.Add((string)row.Tag)) throw new Exception("Clipped or repeated row.");
            Button next = Tree(pager).OfType<Button>().Single(b => b.Text == "Next"); if (!next.Enabled) return ids;
            next.PerformClick(); Pump();
        }
        throw new Exception("Pagination did not end.");
    }
    [STAThread]
    static int Main(string[] args)
    {
        string temp = Path.Combine(Path.GetTempPath(), "budgetdesk-goals-splits-" + Ledger.NewId()); Directory.CreateDirectory(temp);
        images = args.Length == 0 ? Path.Combine(temp, "images") : args[0]; Directory.CreateDirectory(images);
        try
        {
            BudgetData d = Data(); SavingsGoal goal = Goal(d); string actual = BudgetStore.Csv(d, Today); decimal balance = Ledger.TotalBalance(d, Today);
            Ledger.AddGoalSavings(d, goal.Id, 1000); Ledger.Validate(d);
            Check(goal.SavedAmount == 3500 && Ledger.GoalRemaining(goal) == 6500 && Ledger.GoalPercent(goal) == 35 && balance == Ledger.TotalBalance(d, Today) && actual == BudgetStore.Csv(d, Today) && d.Entries.Count == 0, "goal contributions update progress without moving money, adding entries or changing account totals");
            Check(Ledger.GoalMonthlyAmount(goal, Today) == 2166.67m, "monthly goal suggestions round up and include this month and the target month");
            goal.TargetDate = Ledger.Iso(Today.AddDays(-1)); Check(Ledger.GoalMonthlyAmount(goal, Today) == 6500, "overdue goals show the full remaining amount without a negative monthly suggestion");
            goal.TargetDate = null; Check(Ledger.GoalMonthlyAmount(goal, Today) == 0 && Ledger.GoalRemaining(goal) == 6500, "goals can have no deadline");
            goal.SavedAmount = 11000; Check(Ledger.GoalRemaining(goal) == 0 && Ledger.GoalPercent(goal) == 100 && Ledger.GoalMonthlyAmount(goal, Today) == 0, "completed and overfunded goals cap progress at one hundred percent and have no negative remaining amount");
            string before = BudgetStore.Serialize(d); Reject(() => Ledger.AddGoalSavings(d, goal.Id, -1)); Reject(() => Ledger.AddGoalSavings(d, goal.Id, 0)); Check(before == BudgetStore.Serialize(d), "invalid goal contributions leave saved amounts unchanged");
            goal.TargetAmount = 0; Reject(() => Ledger.Validate(d)); goal.TargetAmount = 10000; goal.SavedAmount = -1; Reject(() => Ledger.Validate(d)); goal.SavedAmount = 0; goal.Currency = "EUR"; Reject(() => Ledger.Validate(d));
            d.Rates["EUR"] = 25; Ledger.Validate(d); Check(d.Entries.Count == 0, "goal validation protects targets, saved amounts and currency rates without requiring a bank account link");
            d = Data(); Entry purchase = Purchase(d); Ledger.Validate(d);
            Check(d.Entries.Count == 1 && Ledger.Balance(d, "bank", Today) == 9000 && Ledger.MonthTotal(d, Today, "Expense") == 1000 && Ledger.YearTotal(d, Today.Year, "Expense") == 1000, "a split purchase reduces its account and monthly/yearly spending exactly once");
            Check(Ledger.CategoryExpenseInBase(d, purchase, d.Categories[0].Id) == 600 && Ledger.CategoryExpenseInBase(d, purchase, d.Categories[1].Id) == 400 && d.Categories.Sum(c => Ledger.CategoryExpenseInBase(d, purchase, c.Id)) == 1000, "category allocations add up to the purchase total without duplicating spending");
            Reject(() => Ledger.DeleteCategory(d, d.Categories[1].Id)); Check(d.Categories.Count == 8, "categories used only inside split purchases cannot be deleted");
            purchase.Splits[0].Amount = 700; Reject(() => Ledger.Validate(d)); purchase.Splits[0].Amount = 600.001m; Reject(() => Ledger.Validate(d)); purchase.Splits[0].Amount = 600;
            string category = purchase.Splits[1].CategoryId; purchase.Splits[1].CategoryId = purchase.Splits[0].CategoryId; Reject(() => Ledger.Validate(d)); purchase.Splits[1].CategoryId = "missing"; Reject(() => Ledger.Validate(d)); purchase.Splits[1].CategoryId = category;
            purchase.Kind = "Income"; Reject(() => Ledger.Validate(d)); purchase.Kind = "Expense"; purchase.CategoryId = d.Categories[0].Id; Reject(() => Ledger.Validate(d)); purchase.CategoryId = null;
            Check(d.Entries.Count == 1, "invalid split sums, precision, categories, types and ambiguous parent categories are rejected");
            purchase.Amount = 1200; purchase.Splits[0].Amount = 700; purchase.Splits[1].Amount = 500; Ledger.Validate(d);
            Check(Ledger.Balance(d, "bank", Today) == 8800, "editing a split replaces its previous balance effect");
            d.Entries.Remove(purchase); Check(Ledger.Balance(d, "bank", Today) == 10000 && Ledger.MonthTotal(d, Today, "Expense") == 0, "deleting a split reverses the full expense once");
            d = Data(); purchase = Purchase(d); d.Accounts[0].Currency = "EUR"; d.Rates["EUR"] = 26; purchase.RateToCzk = 25;
            Check(Ledger.CategoryExpenseInBase(d, purchase, d.Categories[0].Id) == 15000 && Ledger.MonthTotal(d, Today, "Expense") == 25000, "split category reports retain the parent expense's saved foreign exchange rate");
            Goal(d); BudgetData cloned = BudgetStore.Parse(BudgetStore.Serialize(d));
            Check(cloned.Goals.Count == 1 && cloned.Entries.Single().Splits.Count == 2 && cloned.Entries.Single().Splits[1].Amount == 400, "goals and split allocations survive clone, backup and reload together");
            string csv = BudgetStore.Csv(d, Today); Check(csv.Contains("Groceries=600.00 EUR") && csv.Contains("Eating out=400.00 EUR") && csv.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Length == 2, "existing exports preserve split details while keeping one purchase row");
            d = Data(); d.PlannedExpenses.Add(new PlannedExpense { Id = "plan", Description = "Keep existing plan", AccountId = "bank", CategoryId = d.Categories[0].Id, DueDate = Ledger.Iso(Today.AddDays(20)), Amount = 150 });
            JavaScriptSerializer serializer = new JavaScriptSerializer(); var old = serializer.Deserialize<Dictionary<string, object>>(BudgetStore.Serialize(d)); old["SchemaVersion"] = 2; old.Remove("Goals"); string oldJson = serializer.Serialize(old);
            string oldFolder = Path.Combine(temp, "upgrade"); Directory.CreateDirectory(oldFolder); File.WriteAllText(Path.Combine(oldFolder, "budget.json"), oldJson); BudgetStore oldStore = new BudgetStore(oldFolder); BudgetData upgraded = oldStore.Load();
            Check(upgraded.SchemaVersion == Ledger.CurrentSchemaVersion && upgraded.Goals.Count == 0 && upgraded.PlannedExpenses.Single().Id == "plan" && File.ReadAllText(oldStore.FilePath) == oldJson, "schema-two upgrades retain one-off plans without overwriting the original budget during load");
            Goal(upgraded); oldStore.Save(upgraded); Check(Directory.GetFiles(Path.Combine(oldFolder, "Backups")).Any(p => File.ReadAllText(p) == oldJson) && oldStore.Load().Goals.Count == 1, "the first goals/splits save preserves the exact previous-schema backup");
            BudgetData incomplete = Data(); incomplete.Goals = null; Reject(() => BudgetStore.Parse(BudgetStore.Serialize(incomplete)));
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            d = Data(); BudgetStore store = new BudgetStore(Path.Combine(temp, "ui")); store.Save(d);
            using (MainForm main = new MainForm(store, d, false))
            {
                main.Size = new Size(1000, 700); main.Show(); Pump(); main.ShowPage(4); Pump();
                Check(Tree(main).OfType<Button>().Where(b => new string[] { "Accounts & bills", "Daily expenses", "Monthly overview", "Yearly overview", "Savings goals" }.Contains(b.Text)).All(b => { Visible(b); return b.Visible; }), "all five tabs fit at the minimum window width");
                Dialog(main, delegate { ((Button)Named(main, "AddSavingsGoal")).PerformClick(); }, delegate(Form f) {
                    Named(f, "GoalName").Text = "New car"; ((NumericUpDown)Named(f, "GoalTarget")).Value = 2000; ((NumericUpDown)Named(f, "GoalSaved")).Value = 250;
                    ((CheckBox)Named(f, "GoalUseDate")).Checked = true; ((DateTimePicker)Named(f, "GoalTargetDate")).Value = Today.AddMonths(2);
                }, "new-savings-goal", true);
                string goalId = main.Data.Goals.Single().Id;
                Check(store.Load().Goals.Single().Name == "New car" && Ledger.Balance(main.Data, "bank", Today) == 10000, "the real goal editor saves its target, allocation and deadline while preserving the account balance");
                Dialog(main, delegate { ((Button)Named(main, "AddGoalSavings")).PerformClick(); }, delegate(Form f) { ((NumericUpDown)Named(f, "GoalContribution")).Value = 500; }, "add-savings", true);
                Check(main.Data.Goals.Single().SavedAmount == 750 && Ledger.GoalPercent(main.Data.Goals.Single()) == 37 && main.Data.Entries.Count == 0 && store.Load().Goals.Single().SavedAmount == 750, "Add savings through the real form persists progress without making a transaction");
                Capture(main, "savings-goals-minimum");
                Dialog(main, delegate { ((Button)Named(main, "EditSavingsGoal")).PerformClick(); }, delegate(Form f) { ((NumericUpDown)Named(f, "GoalSaved")).Value = 600; }, null, true);
                Check(main.Data.Goals.Single().SavedAmount == 600 && Ledger.Balance(main.Data, "bank", Today) == 10000, "goal allocations can be adjusted down without changing bank balances");
                Dialog(main, delegate { ((Button)Named(main, "AddSavingsGoal")).PerformClick(); }, delegate(Form f) { Named(f, "GoalName").Text = "Cancelled"; }, null, false);
                Check(main.Data.Goals.Count == 1, "cancelling a new goal does not create an incomplete tracker");
                main.ShowPage(1); Pump();
                Dialog(main, delegate { Call(main, "EntryEditor", null, "Expense"); }, delegate(Form f) {
                    Field(f, "Description").Text = "Supermarket split"; ((NumericUpDown)Field(f, "Amount (account currency)")).Value = 1000;
                    SplitDialog(main, f, 600, 400, "split-purchase-editor", true, false);
                    if (Field(f, "Expense category").Enabled) throw new Exception("A split still allows an ambiguous parent category.");
                    foreach (Control c in Tree(f).Where(c => c is NumericUpDown || c is ComboBox)) Visible(c);
                }, "split-purchase-entry", true);
                string expenseId = main.Data.Entries.Single().Id;
                Check(main.Data.Entries.Single().Splits.Count == 2 && Ledger.Balance(main.Data, "bank", Today) == 9000 && store.Load().Entries.Single().Splits[1].Amount == 400, "the nested split editor saves one expense with two category allocations and one deduction");
                ComboBox categoryFilter = Tree(main).OfType<ComboBox>().Single(c => c.Items.Cast<Pick>().Any(p => p.Text == "All categories")); Icons.Choose(categoryFilter, main.Data.Categories[1].Id); Pump();
                DataGridView entries = Tree(main).OfType<DataGridView>().Single();
                Check(entries.Rows.Count == 1 && entries.Rows[0].Cells[4].Value.ToString().Contains("400.00") && Tree(main).OfType<Label>().Any(l => l.Text.Contains("Expenses across these results: 400.00 CZK")), "category filtering displays and totals only the selected split share");
                main.ShowPage(2); Pump(); DataGridView report = Tree(main).OfType<DataGridView>().Single(); DataGridViewRow groceries = report.Rows.Cast<DataGridViewRow>().Single(r => (string)r.Tag == main.Data.Categories[0].Id);
                Check(groceries.Cells[1].Value.ToString().Contains("600.00") && groceries.Cells[3].Value.ToString().Contains("200.00"), "Monthly overview budgets and category bars use split shares rather than full purchase amounts");
                main.ShowPage(3); Pump(); DataGridView yearly = Tree(main).OfType<DataGridView>().Single(); Check(yearly.Rows[Today.Month - 1].Cells[2].Value.ToString().Contains("1,000.00"), "Yearly overview counts the full split purchase only once");
                main.ShowPage(1); Pump();
                Dialog(main, delegate { Call(main, "EntryEditor", main.Data.Entries.Single(), "Expense"); }, delegate(Form f) { SplitDialog(main, f, 300, 700, null, true, false); }, null, false);
                Check(main.Data.Entries.Single().Splits[0].Amount == 600 && Ledger.Balance(main.Data, "bank", Today) == 9000, "cancelling the parent expense discards accepted split-dialog edits");
                Dialog(main, delegate { Call(main, "EntryEditor", main.Data.Entries.Single(), "Expense"); }, delegate(Form f) { SplitDialog(main, f, 300, 700, null, true, false); }, null, true);
                Check(main.Data.Entries.Single().Splits[0].Amount == 300 && main.Data.Entries.Count == 1 && Ledger.Balance(main.Data, "bank", Today) == 9000, "editing saved split allocations replaces them without deducting again");
                main.Commit(data => { while (data.Categories.Count < 20) data.Categories.Add(new Category { Id = Ledger.NewId(), Name = "Category " + data.Categories.Count, Icon = "Other" }); });
                Dialog(main, delegate { Call(main, "EntryEditor", main.Data.Entries.Single(), "Expense"); }, delegate(Form f) { SplitDialog(main, f, 0, 0, "twenty-split-categories", true, true); }, null, true);
                Check(main.Data.Entries.Single().Splits.Count == 20 && Ledger.Balance(main.Data, "bank", Today) == 9000, "all twenty category inputs fit without scrolling and can form one correctly balanced purchase");
                Dialog(main, delegate { Call(main, "EntryEditor", main.Data.Entries.Single(), "Expense"); }, delegate(Form f) { ((Button)Named(f, "ClearPurchaseSplit")).PerformClick(); Icons.Choose((ComboBox)Field(f, "Expense category"), main.Data.Categories[0].Id); }, null, true);
                Check(!Ledger.HasSplits(main.Data.Entries.Single()) && main.Data.Entries.Single().CategoryId == main.Data.Categories[0].Id && Ledger.Balance(main.Data, "bank", Today) == 9000, "a split can be converted back to one category while preserving the purchase and balance");
                main.Commit(data => { for (int i = 0; i < 99; i++) { SavingsGoal g = Goal(data); g.Name = "Goal " + i; } });
                foreach (Size size in new Size[] { new Size(1000, 700), new Size(1320, 930) })
                {
                    main.Size = size; main.ShowPage(4); Pump(); PagedGridPanel pager = Tree(main).OfType<PagedGridPanel>().Single();
                    Check(Walk(pager).SetEquals(main.Data.Goals.Select(g => g.Id)), "all one hundred goals are reachable without clipped rows or scrollbars at " + size.Width + "x" + size.Height);
                    pager.PageIndex = 0; Capture(main, "savings-goals-" + size.Width);
                    main.WindowState = FormWindowState.Minimized; for (int i = 0; i < 15; i++) { Pump(); System.Threading.Thread.Sleep(10); }
                    if (main.WindowState != FormWindowState.Minimized) throw new Exception("Minimize not applied.");
                    main.Commit(data => Ledger.AddGoalSavings(data, goalId, 1)); main.WindowState = FormWindowState.Normal;
                    for (int i = 0; i < 15; i++) { Pump(); System.Threading.Thread.Sleep(10); }
                    if (main.WindowState != FormWindowState.Normal || Tree(main).OfType<PagedGridPanel>().Single().ItemCount != 100) throw new Exception("Goal rows lost after restore.");
                }
                Check(store.Load().Goals.Count == 100 && store.Load().Entries.Single().Id == expenseId, "goals, purchases and progress persist through minimized saves and reload"); main.Close();
            }
            string blocked = Path.Combine(temp, "blocked"); File.WriteAllText(blocked, "occupied"); d = Data(); goal = Goal(d); Purchase(d); before = BudgetStore.Serialize(d);
            using (MainForm main = new MainForm(new BudgetStore(blocked), d, false))
            {
                try { main.Commit(data => { Ledger.AddGoalSavings(data, goal.Id, 100); data.Entries[0].Splits[0].Amount = 500; data.Entries[0].Splits[1].Amount = 500; }); throw new Exception("Expected failed save."); } catch (IOException) { }
                Check(BudgetStore.Serialize(main.Data) == before, "failed saves preserve the live goal allocation, split amounts, entries and balances");
            }
            Console.WriteLine("\n" + passed + " savings-goal and split-purchase checks passed."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { if (Directory.Exists(temp)) Directory.Delete(temp, true); }
    }
}
