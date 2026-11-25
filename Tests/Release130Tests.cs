using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using BudgetDesk;

internal static class Release130Tests
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        Console.WriteLine("PASS " + message); checks++;
    }
    private static IEnumerable<Control> Tree(Control root)
    {
        foreach (Control c in root.Controls) { yield return c; foreach (Control nested in Tree(c)) yield return nested; }
    }
    private static T Named<T>(Control root, string name) where T : Control
    { return (T)root.Controls.Find(name, true).Single(); }

    // Drive real nested modal windows as a user would, unwinding both loops on failure.
    private static void Modal(Action open, Action<Form> action)
    {
        Exception error = null;
        using (var timer = new Timer { Interval = 90 })
        {
            timer.Tick += delegate {
                timer.Stop(); Form f = Application.OpenForms.Cast<Form>().Last();
                try { action(f); } catch (Exception ex) { error = ex; f.Close(); }
            };
            timer.Start(); open();
        }
        if (error != null) throw error;
        Application.DoEvents();
    }
    private static void ColorEditor(MainForm main, string method, object[] args, int expected,
        int choice, bool save, Action<Form> fill)
    {
        Modal(delegate { typeof(MainForm).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(main, args); }, delegate(Form f) {
            var field = Named<ItemColorButton>(f, "ItemColor");
            if (field.Index != expected) throw new Exception(method + " reopened with the wrong color.");
            if (fill != null) fill(f);
            Modal(field.PerformClick, delegate(Form dialog) {
                if (choice < 0) dialog.CancelButton.PerformClick();
                else Named<Button>(dialog, choice == 0 ? "DefaultItemColor" : "Swatch" + choice).PerformClick();
            });
            if (field.Index != (choice < 0 ? expected : choice)) throw new Exception("Draft palette selection was lost.");
            if (save) f.AcceptButton.PerformClick(); else f.CancelButton.PerformClick();
        });
    }
    private static Control Field(Form form, string label)
    {
        foreach (var table in Tree(form).OfType<TableLayoutPanel>())
            for (int row = 0; row < table.RowCount; row++)
            {
                var title = table.GetControlFromPosition(0, row) as Label;
                if (title != null && title.Text == label) return table.GetControlFromPosition(1, row);
            }
        throw new Exception("Missing field: " + label);
    }
    private static double Luminance(Color color)
    {
        Func<byte,double> linear = value => value <= 10 ? value / 3294.6 : Math.Pow((value / 255.0 + .055) / 1.055, 2.4);
        return .2126 * linear(color.R) + .7152 * linear(color.G) + .0722 * linear(color.B);
    }
    private static bool Legible(Color foreground, Color background)
    {
        double a = Luminance(foreground), b = Luminance(background);
        return (Math.Max(a,b) + .05) / (Math.Min(a,b) + .05) >= 4.5;
    }
    private static void CheckPaletteContrast(bool dark)
    {
        bool before = Theme.Dark;
        try
        {
            Theme.Dark = dark;
            var indexes = Enumerable.Range(1, Ledger.CardColorCount);
            var semantic = new[] { Theme.Ink, Theme.Muted, Theme.Teal, Theme.Red };
            Check(indexes.All(i => Legible(ItemPalette.SwatchText(ItemPalette.Colors[i].Color), ItemPalette.Colors[i].Color)
                && new[] { true, false }.All(selected => semantic.All(t => Legible(ItemPalette.DetailColor(i,t), ItemPalette.Surface(i,selected))))),
                "all 32 swatches and selected/unselected item details meet 4.5:1 text contrast in the " + (dark ? "dark" : "light") + " theme");
        }
        finally { Theme.Dark = before; }
    }
    private static Account Account(string id, string name)
    {
        return new Account { Id = id, Name = name, Icon = "Bank", Currency = "CZK", OpeningBalance = 10000, OpeningDate = "2020-01-01" };
    }
    private static RecurringBill Bill(BudgetData d)
    {
        return new RecurringBill { Id = "rent", Name = "Rent", Icon = "Home", AccountId = "bank", CategoryId = d.Categories[0].Id,
            Amount = 50, AnchorDate = Ledger.Iso(DateTime.Today), DueTime = "08:00:00", Interval = 1, Unit = "Months", Enabled = true, AutoRecord = false };
    }
    private static void LedgerColors()
    {
        var d = BudgetData.Empty(); d.Accounts.Add(Account("bank", "Bank"));
        var legacy = BudgetStore.Clone(d); legacy.SchemaVersion = 6;
        var old = BudgetStore.Parse(BudgetStore.Serialize(legacy).Replace("\"ColorIndex\":0,", ""));
        Check(old.SchemaVersion == Ledger.CurrentSchemaVersion && old.Accounts[0].ColorIndex == 0, "1.2 backup with missing colors reopens with the theme default");
        var bill = Bill(d); bill.AutoRecord = true; bill.ColorIndex = 3; d.Bills.Add(bill);
        var now = DateTime.Today.AddHours(12);
        d.PlannedExpenses.Add(new PlannedExpense { Id = "plan", AccountId = "bank", CategoryId = d.Categories[0].Id, Amount = 100, Description = "Planned",
            DueDate = Ledger.Iso(now), DueTime = "09:00:00", ColorIndex = 17, AutoRecord = true });
        Check(Ledger.PostDue(d, now) == 2 && d.Entries.Single(e => e.PlanId == "plan").ColorIndex == 17 && d.Entries.Single(e => e.BillId == "rent").ColorIndex == 3,
            "automatic one-off and recurring payments copy the original item colors into two recorded rows");
        bill.ColorIndex = 14; Ledger.RecordNext(d, bill, now);
        Check(d.Entries.First(e => e.BillId == "rent").ColorIndex == 3 && d.Entries.Last().ColorIndex == 14, "changing a schedule colors future occurrences independently of paid history");
        var template = Ledger.TemplateFromEntry(d.Entries[0]); d.Templates.Add(template);
        var draft = Ledger.TemplateDraft(d, template.Id, now); draft.ColorIndex = 31;
        Check(template.ColorIndex == 3 && d.Entries[0].ColorIndex == 3 && d.Entries.Count == 3, "a template stores the entry color and fresh drafts can override it independently");
        d.Accounts[0].ColorIndex = 33; bool rejected = false; try { BudgetStore.Clone(d); } catch (BudgetException) { rejected = true; }
        Check(rejected, "an out-of-range color is rejected before a backup can replace a budget");
    }
    private static void Windows(string folder)
    {
        var data = BudgetData.Empty(); data.Accounts.Add(Account("bank", "Bank")); data.Accounts.Add(Account("cash", "Cash"));
        var store = new BudgetStore(folder); var today = DateTime.Today;
        using (var main = new MainForm(store, data, false))
        {
            main.Size = new Size(1000, 700); main.Show(); Application.DoEvents();
            var raw = ItemPalette.Colors.Skip(1).Select(x => x.Color.ToArgb()).ToArray();
            Check(raw.Length == 32 && raw.Distinct().Count() == 32, "palette has 32 distinct colors plus an independent theme default");
            using (var dialog = new ItemColorDialog(24)) {
                Check(Tree(dialog).OfType<Button>().Count(b => b.Name.StartsWith("Swatch")) == 32 &&
                    Named<Button>(dialog, "Swatch24").AccessibleName == "Blue" && Named<Button>(dialog,"Swatch24").Text.Length != 0,
                    "the palette exposes all 32 named swatches and marks the current choice");
            }
            ColorEditor(main,"AccountEditor",new object[] {null},0,7,true,delegate(Form f) { Field(f,"Account name").Text = "New account"; });
            Check(main.Data.Accounts.Single(a => a.Name == "New account").ColorIndex == 7, "an account saves a selected color directly on creation");
            ColorEditor(main,"BillEditor",new object[] {null},0,5,true,delegate(Form f) {
                Field(f,"Payment name").Text = "New bill"; Named<NumericUpDown>(f,"EntryAmount").Value = 25;
            });
            Check(main.Data.Bills.Single().ColorIndex == 5, "a new recurring payment saves a color from its creation form");
            main.Commit(d => { d.Bills.Clear(); d.Accounts.RemoveAll(a => a.Name == "New account"); });
            ColorEditor(main,"AccountEditor",new object[] {main.Data.Accounts[0]},0,24,true,null);
            Check(Tree(main).Single(c => c.Name == "AccountCard" && (string)c.Tag == "bank").BackColor == ItemPalette.Surface(24,false) &&
                main.Data.Accounts[1].ColorIndex == 0 && Ledger.Balance(main.Data,"bank",today) == 10000, "editing one account changes only its own card and leaves the balance intact");
            ColorEditor(main,"AccountEditor",new object[] {main.Data.Accounts[0]},24,11,false,null);
            ColorEditor(main,"AccountEditor",new object[] {main.Data.Accounts[0]},24,-1,true,null);
            Check(main.Data.Accounts[0].ColorIndex == 24, "cancelling either the editor or palette keeps the saved color");
            main.Commit(d => d.Bills.Add(Bill(d)));
            ColorEditor(main,"BillEditor",new object[] {main.Data.Bills.Single()},0,19,true,null);
            main.Commit(d => d.Bills[0].Enabled = false);
            var paused = Named<Panel>(main,"RecurringCard");
            Check((paused.BackColor == Theme.Paused || paused.BackColor == Theme.DarkRed) && Tree(paused).OfType<Label>().Any(l => l.Text.StartsWith("Paused")),
                "a custom-colored recurring payment still turns red with a Paused label");
            main.Commit(d => d.Bills[0].Enabled = true);
            Check(Named<Panel>(main,"RecurringCard").BackColor == ItemPalette.Surface(19,true), "resuming restores the chosen recurring color");
            main.ShowPage(1);
            ColorEditor(main,"EntryEditor",new object[] {null,"Expense"},0,4,true,delegate(Form f) {
                Tree(f).OfType<TextBox>().First(t => t.GetType() == typeof(TextBox)).Text = "Receipt";
                Named<NumericUpDown>(f,"EntryAmount").Value = 100;
            });
            var grid=Named<DataGridView>(main,"EntriesPagerGrid");
            Check(main.Data.Entries.Single().ColorIndex == 4 && grid.Rows[0].DefaultCellStyle.BackColor == ItemPalette.Surface(4,false) &&
                grid.Rows[0].Cells[4].Style.ForeColor == ItemPalette.DetailColor(4,Theme.Red), "a new expense gets its own row tint and keeps the spending indicator");
            ColorEditor(main,"PlannedExpenseEditor",new object[] {null},0,9,true,delegate(Form f) {
                Named<TextBox>(f,"PlanDescription").Text = "Next purchase"; Named<NumericUpDown>(f,"PlanAmount").Value=200;
                Named<CheckBox>(f,"PlanAutomatic").Checked = false;
            });
            Check(main.Data.PlannedExpenses.Single().ColorIndex == 9 && Ledger.Balance(main.Data,"bank",today) + Ledger.Balance(main.Data,"cash",today) == 19900,
                "a colored future plan leaves the combined account balance untouched");
            main.Commit(d => Ledger.RecordPlanned(d,d.PlannedExpenses[0].Id,DateTime.Now,DateTime.Now));
            Check(main.Data.Entries.Last().ColorIndex == 9, "manual Record paid retains the plan color");
            main.ShowPage(4);
            ColorEditor(main,"GoalEditor",new object[] {null},0,16,true,delegate(Form f) {
                Named<TextBox>(f,"GoalName").Text="Holiday"; Named<NumericUpDown>(f,"GoalTarget").Value=10000;
                Named<NumericUpDown>(f,"GoalSaved").Value=1000;
            });
            Check(Named<DataGridView>(main,"GoalsPagerGrid").Rows[0].DefaultCellStyle.BackColor == ItemPalette.Surface(16,false) &&
                Ledger.GoalPercent(main.Data.Goals[0]) == 10, "new goal color appears on its own row with progress preserved");
            main.Commit(d => { d.Appearance="dark"; d.Language="el"; });
            Check(Named<DataGridView>(main,"GoalsPagerGrid").Rows[0].DefaultCellStyle.BackColor == ItemPalette.Surface(16,false) &&
                Named<DataGridView>(main,"GoalsPagerGrid").DefaultCellStyle.ForeColor == Theme.Ink, "dark appearance retints the row and keeps the theme text colors");
            string backup=Path.Combine(folder,"export.json"); BudgetStore.ExportBackup(main.Data,backup);
            main.Close();
            var recovered=BudgetStore.ReadBackup(backup);
            Check(recovered.Accounts[0].ColorIndex == 24 && recovered.Bills[0].ColorIndex == 19 && recovered.Goals[0].ColorIndex == 16 &&
                recovered.Entries.Select(e => e.ColorIndex).SequenceEqual(new[] {4,9}), "JSON export and restore retain each independent color");
        }
        using (var reopened = new MainForm(store,store.Load(),false))
        {
            reopened.Show(); Application.DoEvents();
            Check(reopened.Data.Accounts[0].ColorIndex == 24 && UiText.Get("Blue") == "Μπλε", "real reopening preserves per-item choices and localizes the palette");
            ColorEditor(reopened,"AccountEditor",new object[] {reopened.Data.Accounts[0]},24,0,true,null);
            Check(reopened.Data.Accounts[0].ColorIndex == 0 && Tree(reopened).Single(c => c.Name == "AccountCard" && (string)c.Tag == "bank").BackColor == Theme.Surface,
                "Default color restores the account's theme appearance");
        }
    }
    [STAThread]
    private static int Main(string[] args)
    {
        var folder=Path.Combine(Path.GetTempPath(),"budgetdesk-color-"+Guid.NewGuid().ToString("N"));
        try { Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); CheckPaletteContrast(false); CheckPaletteContrast(true); LedgerColors(); Windows(folder);
            Console.WriteLine(checks+" release 1.3 checks passed."); return 0; }
        catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { if(Directory.Exists(folder)) Directory.Delete(folder,true); }
    }
}
