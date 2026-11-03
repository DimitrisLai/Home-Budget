using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using BudgetDesk;

internal static class UiSmokeTests
{
    static MainForm main;
    static string images;
    static int passed;
    static IEnumerable<Control> Tree(Control parent)
    {
        foreach (Control c in parent.Controls) { yield return c; foreach (Control child in Tree(c)) yield return child; }
    }
    static Control Field(Form f, string label)
    {
        foreach (TableLayoutPanel t in Tree(f).OfType<TableLayoutPanel>().Where(x => x.ColumnCount == 2))
            for (int row = 0; row < t.RowCount; row++)
            {
                Label l = t.GetControlFromPosition(0, row) as Label;
                if (l != null && l.Text == label) return t.GetControlFromPosition(1, row);
            }
        throw new Exception("Field not found: " + label);
    }
    static void Check(bool condition, string text)
    { if (!condition) throw new Exception(text); passed++; Console.WriteLine("PASS  " + text); }
    static void Capture(Form f, string name)
    {
        f.Refresh(); Application.DoEvents();
        using (Bitmap b = new Bitmap(f.Width, f.Height))
        {
            using (Graphics g = Graphics.FromImage(b)) g.CopyFromScreen(f.Location, Point.Empty, f.Size);
            b.Save(Path.Combine(images, name + ".png"), System.Drawing.Imaging.ImageFormat.Png);
        }
    }
    static void Dialog(string method, object[] arguments, Action<Form> edit, string capture, bool save)
    {
        Exception failure = null;
        using (Timer timer = new Timer { Interval = 130 })
        {
            timer.Tick += delegate {
                timer.Stop(); Form dialog = Application.OpenForms.Cast<Form>().Last(x => x != main);
                try
                {
                    edit(dialog); if (capture != null) Capture(dialog, capture);
                    if (save) Tree(dialog).OfType<Button>().Single(x => x.Text == "Save").PerformClick();
                    else dialog.Close();
                }
                catch (Exception ex) { failure = ex; dialog.Close(); }
            };
            timer.Start();
            typeof(MainForm).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(main, arguments);
        }
        if (failure != null) throw failure;
        Application.DoEvents();
    }
    [STAThread]
    static void Main(string[] args)
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        string folder = Path.Combine(Path.GetTempPath(), "budgetdesk-ui-" + Guid.NewGuid().ToString("N"));
        images = args.Length > 0 ? args[0] : Path.Combine(folder, "images"); Directory.CreateDirectory(images);
        try
        {
            using (main = new MainForm(new BudgetStore(folder), BudgetData.Empty(), false))
            {
                main.Show(); Application.DoEvents(); Capture(main, "empty-state");
                Check(main.Data.Accounts.Count == 0, "first launch has no financial sample data");
                Dialog("AccountEditor", new object[] { null }, delegate(Form f) {
                    Field(f, "Account name").Text = "Test bank"; ((NumericUpDown)Field(f, "Opening balance")).Value = 10000m;
                }, "new-account", true);
                Check(main.Data.Accounts.Count == 1 && Ledger.Balance(main.Data, main.Data.Accounts[0].Id, DateTime.Today) == 10000m, "account form saves a usable account");
                Dialog("AccountEditor", new object[] { null }, delegate(Form f) {
                    Field(f, "Account name").Text = "Cash"; ((NumericUpDown)Field(f, "Opening balance")).Value = 500m;
                }, null, true);
                string bank = main.Data.Accounts[0].Id, cash = main.Data.Accounts[1].Id;
                main.ShowPage(1);
                Dialog("EntryEditor", new object[] { null, "Expense" }, delegate(Form f) {
                    Field(f, "Description").Text = "Groceries"; ((NumericUpDown)Field(f, "Amount (account currency)")).Value = 250.25m;
                }, "new-expense", true);
                Check(Ledger.Balance(main.Data, bank, DateTime.Today) == 9749.75m && main.Data.Entries.Count == 1, "expense form deducts its amount");
                Dialog("EntryEditor", new object[] { main.Data.Entries[0], "Expense" }, delegate(Form f) {
                    ((NumericUpDown)Field(f, "Amount (account currency)")).Value = 300m;
                }, null, true);
                Check(Ledger.Balance(main.Data, bank, DateTime.Today) == 9700m && main.Data.Entries.Count == 1, "editing an expense replaces its balance effect");
                Dialog("EntryEditor", new object[] { null, "Income" }, delegate(Form f) {
                    Field(f, "Description").Text = "Pay"; ((NumericUpDown)Field(f, "Amount (account currency)")).Value = 1000m;
                    Check(!Field(f, "Expense category").Enabled, "income form disables expense category");
                }, null, true);
                Check(Ledger.Balance(main.Data, bank, DateTime.Today) == 10700m, "income form credits its selected account");
                Dialog("EntryEditor", new object[] { null, "Transfer" }, delegate(Form f) {
                    Field(f, "Description").Text = "ATM withdrawal"; ((NumericUpDown)Field(f, "Amount (account currency)")).Value = 100m;
                    Check(!Field(f, "Received (target currency)").Enabled && ((NumericUpDown)Field(f, "Received (target currency)")).Value == 100m, "same-currency transfer fills the received amount");
                }, "new-transfer", true);
                Check(Ledger.Balance(main.Data, bank, DateTime.Today) == 10600m && Ledger.Balance(main.Data, cash, DateTime.Today) == 600m, "transfer form updates both accounts");
                Check(Ledger.MonthTotal(main.Data, DateTime.Today, "Expense") == 300m, "transfer is excluded from spending in the live UI");
                main.ShowPage(0);
                Dialog("BillEditor", new object[] { null }, delegate(Form f) {
                    Field(f, "Payment name").Text = "Internet"; ((NumericUpDown)Field(f, "Amount (account currency)")).Value = 99m;
                    ((DateTimePicker)Field(f, "Next unpaid date")).Value = DateTime.Today;
                }, "new-recurring-payment", true);
                Check(main.Data.Bills.Count == 1 && main.Data.Entries.Count == 4 && Ledger.Balance(main.Data, bank, DateTime.Today) == 10501m, "due recurring form records once and deducts its account");
                BudgetData reloaded = main.Store.Load();
                Check(Ledger.PostDue(reloaded, DateTime.Today) == 0 && reloaded.Entries.Count == 4, "reopening a saved UI budget does not duplicate bills");
                Dialog("AccountEditor", new object[] { main.Data.Accounts[0] }, delegate(Form f) {
                    Check(!Field(f, "Currency").Enabled, "currency is locked for an account with history");
                }, null, false);
                Dialog("CategoryEditor", new object[] { null, main }, delegate(Form f) {
                    Field(f, "Category name").Text = "Hobbies"; ((NumericUpDown)Field(f, "Monthly budget (CZK)")).Value = 1500m;
                }, "new-category", true);
                Check(main.Data.Categories.Count == 9 && main.Data.Categories.Last().MonthlyBudgetCzk == 1500m, "category form saves its optional monthly budget");
                Dialog("Settings", new object[0], delegate(Form f) {
                    ((NumericUpDown)Field(f, "1 EUR = CZK")).Value = 25m;
                }, "settings", true);
                Check(main.Data.Rates["EUR"] == 25m, "settings save a manual exchange rate");
                string before = BudgetStore.Serialize(main.Data), disk = File.ReadAllText(main.Store.FilePath);
                try { main.Commit(d => d.Accounts[0].Name = ""); throw new Exception("Expected invalid commit rejection"); } catch (BudgetException) { }
                Check(before == BudgetStore.Serialize(main.Data) && disk == File.ReadAllText(main.Store.FilePath), "invalid UI commit changes neither memory nor disk");
                for (int i = 0; i < 5; i++) { main.ShowPage(i); Capture(main, "working-page-" + i); }
                main.Size = new Size(1000, 700); Application.DoEvents();
                for (int i = 0; i < 5; i++) { main.ShowPage(i); Capture(main, "minimum-page-" + i); }
                Check(main.ClientSize.Width > 900 && main.Visible, "all five pages render at the minimum window size");
                main.Commit(d => {
                    while (d.Accounts.Count < 20) Ledger.AddAccount(d, new Account { Id = Ledger.NewId(), Name = "Extra account " + d.Accounts.Count, Icon = "Bank", Currency = "CZK", OpeningBalance = 100m, OpeningDate = Ledger.Iso(DateTime.Today) });
                    while (d.Bills.Count < 20) Ledger.AddBill(d, new RecurringBill { Id = Ledger.NewId(), Name = "Payment " + d.Bills.Count, Icon = "Phone", AccountId = bank, CategoryId = d.Categories[0].Id, Amount = 10, AnchorDate = Ledger.Iso(DateTime.Today.AddDays(1)), Interval = 3, Unit = "Months", Enabled = true, AutoRecord = false });
                });
                main.ShowPage(0); Capture(main, "twenty-accounts-and-bills");
                Panel cards = Tree(main).OfType<Panel>().Single(x => x.Name == "AccountCards");
                Panel bills = Tree(main).OfType<Panel>().Single(x => x.Name == "RecurringCards");
                Check(bills.Controls.Count == 20 && cards.Controls.Count == 20 && !cards.AutoScroll && !bills.AutoScroll && cards.Controls.Cast<Control>().All(x => cards.ClientRectangle.Contains(x.Bounds)) && bills.Controls.Cast<Control>().All(x => bills.ClientRectangle.Contains(x.Bounds)), "all twenty accounts and twenty recurring payments fit without scrolling");
                main.Close();
            }
            Console.WriteLine("\n" + passed + " UI checks passed.");
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); Environment.ExitCode = 1; }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
}
