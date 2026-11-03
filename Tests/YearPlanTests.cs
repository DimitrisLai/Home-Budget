using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using BudgetDesk;

internal static class YearPlanTests
{
    static int passed;
    static string images;
    static readonly DateTime Today = DateTime.Today;
    static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); passed++; Console.WriteLine("PASS " + message); }
    static void Reject(Action action)
    { try { action(); } catch (BudgetException) { return; } throw new Exception("Expected validation rejection."); }
    static BudgetData Data()
    {
        BudgetData d = BudgetData.Empty();
        d.Accounts.Add(new Account { Id = "bank", Name = "Main bank", Icon = "Bank", Currency = "CZK", OpeningBalance = 10000, OpeningDate = "2024-01-01" });
        d.Accounts.Add(new Account { Id = "cash", Name = "Cash", Icon = "Wallet", Currency = "CZK", OpeningBalance = 500, OpeningDate = "2024-01-01" });
        return d;
    }
    static PlannedExpense Plan(BudgetData d, string name, DateTime due, decimal amount)
    {
        PlannedExpense p = new PlannedExpense { Id = Ledger.NewId(), Description = name, DueDate = Ledger.Iso(due), AccountId = "bank", CategoryId = d.Categories[0].Id, Amount = amount };
        d.PlannedExpenses.Add(p); return p;
    }
    static Entry Entry(BudgetData d, string kind, string date, decimal amount)
    { return new Entry { Id = Ledger.NewId(), Kind = kind, Date = date, AccountId = "bank", CategoryId = kind == "Expense" ? d.Categories[0].Id : null, Description = kind + " on " + date, Amount = amount, RateToCzk = 1 }; }
    static IEnumerable<Control> Tree(Control parent)
    { foreach (Control c in parent.Controls) { yield return c; foreach (Control nested in Tree(c)) yield return nested; } }
    static void Pump() { Application.DoEvents(); Application.DoEvents(); }
    static Button Button(MainForm main, string name)
    { return Tree(main).OfType<Button>().Single(x => x.Name == name); }
    static void Call(MainForm main, string method, params object[] args)
    { typeof(MainForm).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(main, args); }
    static void Dialog(MainForm main, Action open, Action<Form> edit, string capture, bool save)
    {
        Exception failure = null;
        using (Timer timer = new Timer { Interval = 100 })
        {
            timer.Tick += delegate {
                timer.Stop(); Form editor = Application.OpenForms.Cast<Form>().Last(x => x != main);
                try { edit(editor); if (capture != null) Capture(editor, capture); if (save) ((Editor)editor).Save.PerformClick(); else editor.Close(); }
                catch (Exception ex) { failure = ex; editor.Close(); }
            };
            timer.Start(); open();
        }
        if (failure != null) throw failure; Pump();
    }
    static void Capture(Form form, string name)
    {
        form.Location = new Point(20, 20); form.BringToFront(); form.Activate();
        for (int settle = 0; settle < 15; settle++) { Pump(); System.Threading.Thread.Sleep(10); }
        form.Refresh(); Pump();
        using (Bitmap b = new Bitmap(form.Width, form.Height))
        { using (Graphics g = Graphics.FromImage(b)) g.CopyFromScreen(form.Location, Point.Empty, form.Size); b.Save(Path.Combine(images, name + ".png")); }
    }
    static void Visible(Control control)
    {
        for (Control c = control; c.Parent != null; c = c.Parent)
            if (!c.Parent.ClientRectangle.Contains(c.Bounds)) throw new Exception(c.Name + " is clipped by its parent.");
    }
    static HashSet<string> Walk(PagedGridPanel pager)
    {
        HashSet<string> seen = new HashSet<string>(); pager.PageIndex = 0;
        do
        {
            if (pager.Grid.ScrollBars != ScrollBars.None) throw new Exception("Scrollbars enabled.");
            foreach (DataGridViewRow row in pager.Grid.Rows)
                if (!pager.Grid.ClientRectangle.Contains(pager.Grid.GetRowDisplayRectangle(row.Index, false)) || !seen.Add((string)row.Tag)) throw new Exception("Missing or repeated page row.");
            Button next = Tree(pager).OfType<Button>().Single(x => x.Text == "Next");
            if (!next.Enabled) return seen; next.PerformClick(); Pump();
        } while (seen.Count < 1000);
        throw new Exception("Page loop did not finish.");
    }
    [STAThread]
    static int Main(string[] args)
    {
        string temp = Path.Combine(Path.GetTempPath(), "budgetdesk-year-plans-" + Ledger.NewId()); Directory.CreateDirectory(temp);
        images = args.Length == 0 ? Path.Combine(temp, "images") : args[0]; Directory.CreateDirectory(images);
        try
        {
            BudgetData original = Data(); Ledger.MoveAccount(original, "cash", 0);
            original.Rates["EUR"] = 25;
            original.Entries.Add(Entry(original, "Expense", Ledger.Iso(Today), 123.45m));
            original.Bills.Add(new RecurringBill { Id = "legacy-bill", Name = "Legacy bill", Icon = "Bolt", AccountId = "bank", CategoryId = original.Categories[0].Id, Amount = 120, AnchorDate = Ledger.Iso(Today.AddDays(1)), Interval = 1, Unit = "Months", Enabled = true, AutoRecord = false });
            original.ProcessedOccurrences.Add("older-bill:2024-01-01");
            string serialized = BudgetStore.Serialize(original);
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            var legacy = serializer.Deserialize<Dictionary<string, object>>(serialized); legacy["SchemaVersion"] = 1; legacy.Remove("PlannedExpenses"); legacy.Remove("Goals"); legacy.Remove("Payees"); legacy.Remove("Templates");
            string legacyJson = serializer.Serialize(legacy); string oldFolder = Path.Combine(temp, "old"); Directory.CreateDirectory(oldFolder); File.WriteAllText(Path.Combine(oldFolder, "budget.json"), legacyJson);
            BudgetStore oldStore = new BudgetStore(oldFolder); BudgetData migrated = oldStore.Load();
            Check(migrated.SchemaVersion == Ledger.CurrentSchemaVersion && BudgetStore.Serialize(migrated) == serialized && File.ReadAllText(oldStore.FilePath) == legacyJson, "version-one data migrates without rewriting the file or altering bank order, entries, rates and payment history");
            Plan(migrated, "New plan", Today.AddDays(20), 150); oldStore.Save(migrated);
            Check(serializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(oldStore.FilePath))["SchemaVersion"].ToString() == Ledger.CurrentSchemaVersion.ToString() && Directory.GetFiles(Path.Combine(oldFolder, "Backups")).Any(p => File.ReadAllText(p) == legacyJson), "the first current-schema save retains the exact previous budget as an automatic backup");
            BudgetData missing = Data(); missing.PlannedExpenses = null; Reject(() => BudgetStore.Parse(BudgetStore.Serialize(missing)));
            Check(oldStore.Load().PlannedExpenses.Count == 1, "plans survive reload and incomplete current-schema files are rejected");
            BudgetData d = Data(); PlannedExpense plan = Plan(d, "Repair in twenty days", Today.AddDays(20), 800);
            Ledger.Validate(d); Ledger.PostDue(d, Today.AddDays(40));
            Check(Ledger.Balance(d, "bank", Today.AddDays(40)) == 10000 && Ledger.MonthTotal(d, Today.AddDays(20), "Expense") == 0 && d.Entries.Count == 0 && d.PlannedExpenses.Count == 1, "future and overdue one-off plans never deduct or auto-post when their due date passes");
            Check(Ledger.OneOffTotal(d, Today, Today.AddDays(20)) == 800 && Ledger.OneOffTotal(d, Today, Today.AddDays(19)) == 0, "forecast date ranges include their endpoints without changing actual spending");
            string before = BudgetStore.Serialize(d); Reject(() => Ledger.RecordPlanned(d, plan.Id, Today.AddDays(1), Today));
            Check(BudgetStore.Serialize(d) == before, "recording a payment in the future is rejected without changing the plan");
            Ledger.RecordPlanned(d, plan.Id, Today, Today); Ledger.Validate(d); Reject(() => Ledger.RecordPlanned(d, plan.Id, Today, Today));
            Check(d.PlannedExpenses.Count == 0 && d.Entries.Count == 1 && d.Entries[0].PlanId == plan.Id && d.Entries[0].Date == Ledger.Iso(Today) && d.Entries[0].ScheduledDate == plan.DueDate && Ledger.Balance(d, "bank", Today) == 9200, "recording early uses the actual payment date, removes the plan and deducts exactly once");
            d = BudgetStore.Parse(BudgetStore.Serialize(d)); Ledger.PostDue(d, Today.AddDays(60)); d.Entries.Clear(); Ledger.PostDue(d, Today.AddDays(60));
            Check(d.PlannedExpenses.Count == 0 && d.Entries.Count == 0 && Ledger.Balance(d, "bank", Today) == 10000, "reload and deleting a recorded expense do not recreate its original one-off plan");
            d = Data(); plan = Plan(d, "Overdue", Today.AddDays(-2), 99); Ledger.RecordPlanned(d, plan.Id, Today, Today);
            Check(d.Entries.Single().Date == Ledger.Iso(Today), "paying an overdue plan uses the chosen payment date instead of its old due date");
            d = Data(); plan = Plan(d, "Referenced plan", Today.AddDays(1), 10); Reject(() => Ledger.DeleteAccount(d, "bank")); Reject(() => Ledger.DeleteCategory(d, plan.CategoryId));
            plan.AccountId = "missing"; Reject(() => Ledger.Validate(d)); plan.AccountId = "bank"; plan.Amount = -1; Reject(() => Ledger.Validate(d)); plan.Amount = 10; plan.DueDate = "2023-12-31"; Reject(() => Ledger.Validate(d));
            Check(d.Accounts.Count == 2 && d.Categories.Count == 8, "planned references protect accounts and categories, with amount and opening-date validation");
            d = Data(); d.Accounts[0].Currency = "EUR"; d.Rates["EUR"] = 25; plan = Plan(d, "Foreign plan", Today.AddDays(1), 10);
            decimal estimate = Ledger.PlanInBase(d, plan); d.Rates["EUR"] = 26; decimal changed = Ledger.PlanInBase(d, plan); Ledger.RecordPlanned(d, plan.Id, Today, Today); d.Rates["EUR"] = 27;
            Check(estimate == 250 && changed == 260 && Ledger.MonthTotal(d, Today, "Expense") == 260, "plans estimate with current exchange rates while paid entries retain their recorded rate");
            d = Data(); d.Entries.Add(Entry(d, "Income", "2024-01-01", 1000)); d.Entries.Add(Entry(d, "Income", "2024-12-31", 2000)); d.Entries.Add(Entry(d, "Expense", "2024-02-29", 125.25m)); d.Entries.Add(Entry(d, "Expense", "2025-01-01", 900));
            Entry transfer = Entry(d, "Transfer", "2024-12-01", 100); transfer.ToAccountId = "cash"; transfer.ReceivedAmount = 100; d.Entries.Add(transfer); Plan(d, "Unpaid", new DateTime(2024, 12, 31), 700);
            Ledger.Validate(d);
            Check(Ledger.YearTotal(d, 2024, "Income") == 3000 && Ledger.YearTotal(d, 2024, "Expense") == 125.25m && Ledger.YearTotal(d, 2025, "Expense") == 900 && Enumerable.Range(1, 12).Sum(m => Ledger.MonthTotal(d, new DateTime(2024, m, 1), "Expense")) == 125.25m, "year totals handle leap days and year boundaries, excluding transfers, opening balances and plans");
            string yearCsv = BudgetStore.YearCsv(d, 2024), planCsv = BudgetStore.PlansCsv(d);
            Check(yearCsv.Contains("2024-02-29") && yearCsv.Contains("2024-12-31") && !yearCsv.Contains("2025-01-01") && !yearCsv.Contains("Unpaid") && planCsv.Contains("Unpaid") && planCsv.Contains("not deducted"), "yearly CSV includes every recorded entry in that year and plans export separately");
            d.PlannedExpenses[0].Description = "=FORMULA"; Check(BudgetStore.PlansCsv(d).Contains("\"'=FORMULA\""), "planned-expense exports preserve spreadsheet formula protection");
            RecurringBill bill = new RecurringBill { Id = Ledger.NewId(), AnchorDate = "1900-01-01", Interval = 1, Unit = "Days", Enabled = true, NextIndex = 0 };
            Check(Ledger.PlannedDates(bill, new DateTime(2199, 1, 1), new DateTime(2199, 12, 31)).Count() == 365, "recurring forecasts cover distant years and stop safely at the supported date boundary");
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            d = Data(); d.Entries.Add(Entry(d, "Income", Ledger.Iso(Today), 2500));
            BudgetStore store = new BudgetStore(Path.Combine(temp, "ui")); store.Save(d);
            using (MainForm main = new MainForm(store, d, false))
            {
                main.Size = new Size(1000, 700); main.Show(); Pump(); main.ShowPage(1); Pump();
                DateTimePicker monthPicker = Tree(main).OfType<DateTimePicker>().Single(); Button add = Button(main, "AddPlannedExpense"); Visible(add);
                Check(!monthPicker.RectangleToScreen(monthPicker.ClientRectangle).IntersectsWith(add.RectangleToScreen(add.ClientRectangle)), "the Future expense button and month picker fit together at minimum width");
                Dialog(main, delegate { add.PerformClick(); }, delegate(Form f) {
                    f.Controls.Find("PlanDescription", true).Single().Text = "Car repair in twenty days";
                    ((NumericUpDown)f.Controls.Find("PlanAmount", true).Single()).Value = 850;
                    ((DateTimePicker)f.Controls.Find("PlanDueDate", true).Single()).Value = Today.AddDays(20);
                }, "new-future-expense", true);
                Check(main.Data.PlannedExpenses.Count == 1 && Ledger.Balance(main.Data, "bank", Today) == 12500 && store.Load().PlannedExpenses.Count == 1 && Tree(main).Any(x => x.Name == "PlansPager"), "the real Future expense editor saves and immediately displays the plan without deduction");
                string planId = main.Data.PlannedExpenses.Single().Id;
                Dialog(main, delegate { Call(main, "PlannedExpenseEditor", main.Data.PlannedExpenses.Single()); }, delegate(Form f) { ((DateTimePicker)f.Controls.Find("PlanDueDate", true).Single()).Value = new DateTime(Today.Year + 1, 1, 10); }, null, true);
                Check(Tree(main).OfType<PagedGridPanel>().Single().ItemCount == 1 && Ledger.Day(main.Data.PlannedExpenses.Single().DueDate).Year == Today.Year + 1, "editing a plan into another year keeps it visible in the all-dates list");
                Capture(main, "planned-expenses-minimum");
                Dialog(main, delegate { Button(main, "RecordPlannedPaid").PerformClick(); }, delegate(Form f) { ((DateTimePicker)f.Controls.Find("ActualPaymentDate", true).Single()).Value = Today; }, "record-paid", true);
                Check(main.Data.PlannedExpenses.Count == 0 && main.Data.Entries.Count(e => e.PlanId == planId) == 1 && Ledger.Balance(main.Data, "bank", Today) == 11650 && store.Load().PlannedExpenses.Count == 0, "Record paid through the real form persists exactly one expense and removes the plan");
                Button(main, "RecordedEntriesView").PerformClick(); Pump(); Check(Tree(main).OfType<PagedGridPanel>().Single().ItemCount == 2, "the paid one-off expense appears immediately in Recorded entries");
                Dialog(main, delegate { Button(main, "AddPlannedExpense").PerformClick(); }, delegate(Form f) { f.Controls.Find("PlanDescription", true).Single().Text = "Cancelled"; }, null, false);
                Check(main.Data.PlannedExpenses.Count == 0, "cancelling the future-expense editor creates no plan or balance change");
                main.Commit(data => { for (int i = 0; i < 100; i++) Plan(data, "Planned purchase " + i, Today.AddDays(i - 20), i + 1); });
                Button(main, "PlannedExpensesView").PerformClick(); Pump(); PagedGridPanel pager = Tree(main).OfType<PagedGridPanel>().Single();
                Check(Walk(pager).SetEquals(main.Data.PlannedExpenses.Select(p => p.Id)), "all one hundred plans remain reachable through page buttons without clipped rows or scrollbars");
                ComboBox dueFilter = Tree(main).OfType<ComboBox>().Single(x => x.Name == "PlanDateFilter"); Icons.Choose(dueFilter, "Overdue"); Pump();
                Check(Tree(main).OfType<PagedGridPanel>().Single().ItemCount == 20 && Tree(main).OfType<PagedGridPanel>().Single().PageIndex == 0, "overdue filtering uses all dates and resets the plan page");
                Icons.Choose(dueFilter, "All dates"); Pump(); Capture(main, "planned-expenses-many-minimum");
                Dialog(main, delegate { Call(main, "AccountEditor", main.Data.Accounts[0]); }, delegate(Form f) { if (Tree(f).OfType<ComboBox>().Single(c => c.Items.Count == Ledger.Currencies.Length).Enabled) throw new Exception("Planned account currency was editable."); }, null, false);
                foreach (Size size in new Size[] { new Size(1000, 700), new Size(1320, 930) })
                {
                    main.Size = size; Pump(); main.ShowPage(3); Pump();
                    DataGridView grid = Tree(main).OfType<DataGridView>().Single();
                    bool inside = grid.Rows.Count == 12 && grid.ScrollBars == ScrollBars.None && grid.Rows.Cast<DataGridViewRow>().All(r => grid.ClientRectangle.Contains(grid.GetRowDisplayRectangle(r.Index, false)));
                    Check(inside && Tree(main).OfType<Button>().Any(x => x.Text == "Yearly overview" && x.Visible), "all twelve yearly rows and the new tab fit without scrolling at " + size.Width + "x" + size.Height);
                    Capture(main, "yearly-overview-" + size.Width);
                    main.WindowState = FormWindowState.Minimized; for (int i = 0; i < 15; i++) { Pump(); System.Threading.Thread.Sleep(10); }
                    if (main.WindowState != FormWindowState.Minimized) throw new Exception("Real minimize was not applied.");
                    main.WindowState = FormWindowState.Normal; for (int i = 0; i < 15; i++) { Pump(); System.Threading.Thread.Sleep(10); }
                    if (main.WindowState != FormWindowState.Normal || !grid.Rows.Cast<DataGridViewRow>().All(r => grid.ClientRectangle.Contains(grid.GetRowDisplayRectangle(r.Index, false)))) throw new Exception("Year rows disappeared after restoring.");
                }
                DataGridView months = Tree(main).OfType<DataGridView>().Single();
                typeof(DataGridView).GetMethod("OnCellDoubleClick", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(months, new object[] { new DataGridViewCellEventArgs(0, 2) }); Pump();
                Check((DateTime)typeof(MainForm).GetField("selectedMonth", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(main) == new DateTime(Today.Year, 3, 1), "double-clicking a yearly month opens its Monthly overview");
                BudgetData reload = store.Load(); Check(reload.PlannedExpenses.Count == 100 && reload.Entries.Count(e => e.PlanId == planId) == 1, "plans, recorded payments and bank order coexist after reload");
                main.Close();
            }
            string blocked = Path.Combine(temp, "blocked"); File.WriteAllText(blocked, "occupied"); BudgetData unsaved = Data(); PlannedExpense kept = Plan(unsaved, "Save failure", Today.AddDays(20), 500);
            using (MainForm main = new MainForm(new BudgetStore(blocked), unsaved, false))
            {
                try { main.Commit(data => Ledger.RecordPlanned(data, kept.Id, Today, Today)); throw new Exception("Expected save failure."); } catch (IOException) { }
                Check(main.Data.PlannedExpenses.Count == 1 && main.Data.Entries.Count == 0 && Ledger.Balance(main.Data, "bank", Today) == 10000, "failed payment saves leave the live plan, entries and balance unchanged");
            }
            Console.WriteLine("\n" + passed + " yearly-overview and planned-expense checks passed."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { if (Directory.Exists(temp)) Directory.Delete(temp, true); }
    }
}
