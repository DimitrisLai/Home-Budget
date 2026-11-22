using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using BudgetDesk;

internal static class Release120Tests
{
    private static int checks;

    private static void Check(bool passed, string message)
    {
        if (!passed) throw new Exception(message);
        Console.WriteLine("PASS " + message); checks++;
    }
    private static BudgetData Sample()
    {
        BudgetData data = BudgetData.Empty();
        data.Accounts.Add(new Account { Id = "a", Name = "Home", Currency = "CZK", Icon = "Bank",
            OpeningBalance = 10000, OpeningDate = "2020-01-01" });
        return data;
    }
    private static PlannedExpense Plan(BudgetData data, string id, DateTime due, bool auto)
    {
        return new PlannedExpense { Id = id, AccountId = "a", CategoryId = data.Categories[0].Id,
            Description = "Test purchase", Amount = 20, PurchaseCurrency = "EUR", ExchangeRate = 25,
            DueDate = Ledger.Iso(due), DueTime = Ledger.Clock(due), AutoRecord = auto };
    }
    private static void Deadline()
    {
        var due = new DateTime(2026, 11, 21, 21, 15, 0);
        BudgetData data = Sample();
        data.PlannedExpenses.Add(Plan(data, "auto", due, true));
        data.PlannedExpenses.Add(Plan(data, "manual", due, false));
        Check(Ledger.NextAutomaticMoment(data) == due && Ledger.PostDue(data, due.AddMinutes(-1)) == 0 &&
            Ledger.PostDue(data, due.AddTicks(-1)) == 0 && Ledger.Balance(data, "a", due) == 10000, "21:14 and the last tick before 21:15 leave the account untouched");
        BudgetData missed = BudgetStore.Clone(data);
        Check(Ledger.PostDue(data, due) == 1 && Ledger.PostDue(data, due.AddMinutes(1)) == 0, "the 21:15 deadline posts once");
        Check(Ledger.PostDue(missed, due.AddMinutes(1)) == 1, "opening at 21:16 catches a missed 21:15 expense");
        foreach (BudgetData d in new[] { data, missed })
        {
            Entry e = d.Entries.Single();
            Check(e.PlanId == "auto" && e.Time == "21:15:00" && e.ScheduledTime == e.Time && e.Date == "2026-11-21" &&
                e.ScheduledDate == e.Date && Ledger.Balance(d, "a", due) == 9500 && Ledger.MonthTotal(d, due, "Expense") == 500,
                "automatic expense keeps both dates, original receipt and converted account charge");
            Check(d.PlannedExpenses.Single().Id == "manual" && !Ledger.NextAutomaticMoment(d).HasValue, "manual reminder remains outstanding");
        }
        data = BudgetStore.Clone(data);
        Check(Ledger.PostDue(data, due.AddDays(3)) == 0 && data.Entries.Count == 1, "reloading and checking again cannot duplicate the debit");
        Ledger.RecordPlanned(data, "manual", due.AddMinutes(3), due.AddMinutes(3));
        Check(data.Entries.Last().Time == "21:18:00" && data.Entries.Last().ScheduledTime == "21:15:00", "manual payment uses its confirmation time and preserves its due time");
        BudgetData old = Sample(); old.SchemaVersion = 5; old.Language = null; old.Appearance = null;
        old.PlannedExpenses.Add(Plan(old, "prior", due, false));
        old = BudgetStore.Parse(BudgetStore.Serialize(old));
        Check(old.Language == "en" && old.Appearance == "light" && old.SchemaVersion == Ledger.CurrentSchemaVersion &&
            Ledger.PostDue(old, due.AddDays(1)) == 0 && old.PlannedExpenses.Count == 1, "old timed plans remain manual during migration");
    }
    private static T Find<T>(Control root, string name) where T : Control
    { return (T)root.Controls.Find(name, true).Single(); }

    private static IEnumerable<Control> Tree(Control root)
    {
        foreach (Control child in root.Controls)
        { yield return child; foreach (Control nested in Tree(child)) yield return nested; }
    }
    private static void EditDialog(MainForm main, Action open, Action<Form> edit, bool save)
    {
        Exception failure = null;
        using (var timer = new System.Windows.Forms.Timer { Interval = 100 })
        {
            timer.Tick += delegate {
                timer.Stop();
                Form dialog = Application.OpenForms.Cast<Form>().Last(f => f != main);
                try { edit(dialog); if (save) dialog.AcceptButton.PerformClick(); else dialog.Close(); }
                catch (Exception ex) { failure = ex; dialog.Close(); }
            };
            timer.Start(); open();
        }
        if (failure != null) throw failure;
        Application.DoEvents();
    }
    private static void Preferences(MainForm main, string language, string appearance, bool save)
    {
        EditDialog(main, delegate { Find<Button>(main, "SettingsBackup").PerformClick(); }, delegate(Form dialog) {
            Icons.Choose(Find<ComboBox>(dialog, "InterfaceLanguage"), language);
            Icons.Choose(Find<ComboBox>(dialog, "InterfaceAppearance"), appearance);
        }, save);
    }
    private static void Windows(string directory, string images)
    {
        BudgetStore store = new BudgetStore(directory);
        BudgetData data = Sample();
        data.Categories.Add(new Category { Id = "custom", Name = "Light", Icon = "Star" });
        using (var main = new MainForm(store, data, false))
        {
            main.Size = new Size(1000, 700); main.Show(); Application.DoEvents();
            Preferences(main, "el", "dark", false);
            Check(main.Data.Language == "en" && main.Data.Appearance == "light", "cancelling preferences leaves the budget unchanged");
            Preferences(main, "el", "dark", true);
            Check(Find<Button>(main, "SettingsBackup").Text == "Ρυθμίσεις & αντίγραφα" && main.BackColor.GetBrightness() < 0.25f, "Greek and dark appearance apply in the open window");
            Check(Tree(main).Any(c => c.Name == "AccountName" && c.Text == "Home") &&
                main.Data.Categories.Last().DisplayName == "Light" && main.Data.Categories.First().DisplayName == "Τρόφιμα", "custom names stay verbatim while new built-in categories translate");
            foreach (int page in Enumerable.Range(0, 5)) { main.ShowPage(page); Application.DoEvents(); }
            main.ShowPage(1);
            var grid = Find<DataGridView>(main, "EntriesPagerGrid");
            Check(grid.Columns["description"].HeaderText == "ΠΕΡΙΓΡΑΦΗ" && grid.BackgroundColor == Theme.Surface &&
                grid.DefaultCellStyle.ForeColor == Theme.Ink, "Greek grid headings and the dark grid palette are applied");
            DateTime due = DateTime.MinValue;
            EditDialog(main, delegate { Find<Button>(main, "AddPlannedExpense").PerformClick(); }, delegate(Form dialog) {
                Check(Find<CheckBox>(dialog, "PlanAutomatic").Checked, "new one-off editor defaults to automatic");
                Find<TextBox>(dialog, "PlanDescription").Text = "Live due time";
                Find<NumericUpDown>(dialog, "PlanAmount").Value = 250;
                due = DateTime.Now.AddSeconds(3);
                Find<DateTimePicker>(dialog, "PlanDueDate").Value = due.Date;
                Find<DateTimePicker>(dialog, "PlanDueDateTime").Value = due;
            }, true);
            Stopwatch limit = Stopwatch.StartNew();
            while (main.Data.Entries.Count == 0 && limit.Elapsed.TotalSeconds < 10)
            { Application.DoEvents(); Thread.Sleep(30); }
            Check(main.Data.Entries.Count == 1 && main.Data.Entries[0].Time == Ledger.Clock(due) &&
                main.Data.PlannedExpenses.Count == 0 && store.Load().Entries.Count == 1, "real open-window timer posts at the due time and saves exactly once");
            Preferences(main, "cs", "dark", true);
            main.ShowPage(0);
            Check(Find<Button>(main, "SettingsBackup").Text == "Nastavení a zálohy" &&
                Tree(main).Any(c => c.Text == "Pravidelné platby    0 / 20"), "Czech labels rebuild on navigation");
            if (images != null)
            {
                Directory.CreateDirectory(images);
                using (var bitmap = new Bitmap(main.Width, main.Height))
                { main.DrawToBitmap(bitmap, main.ClientRectangle); bitmap.Save(Path.Combine(images, "czech-dark.png")); }
            }
            main.Close();
        }
        var offline = store.Load(); DateTime missed = DateTime.Now.AddMinutes(-5);
        offline.PlannedExpenses.Add(Plan(offline, "closed", missed, true)); store.Save(offline);
        using (var reopened = new MainForm(store, store.Load(), false))
        {
            reopened.Show(); Application.DoEvents();
            Check(reopened.Data.Language == "cs" && reopened.Data.Appearance == "dark" && reopened.Data.Entries.Count == 2 &&
                reopened.Data.Entries.Single(e => e.PlanId == "closed").Time == Ledger.Clock(missed), "a real reopen restores preferences and catches up at the scheduled time");
            Preferences(reopened, "en", "light", true);
            Check(Find<Button>(reopened, "SettingsBackup").Text == "Settings & backup" && reopened.BackColor.GetBrightness() > 0.8f &&
                store.Load().Entries.Count == 2, "switching back restores English and light appearance while preserving entries");
            reopened.Close();
        }
    }
    [STAThread]
    private static int Main(string[] args)
    {
        string folder = Path.Combine(Path.GetTempPath(), "budgetdesk-120-" + Guid.NewGuid().ToString("N"));
        try
        {
            Deadline(); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            Windows(folder, args.Length == 0 ? null : args[0]);
            Console.WriteLine(checks + " release 1.2 checks passed."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
}
