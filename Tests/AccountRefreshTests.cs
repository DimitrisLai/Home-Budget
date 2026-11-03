using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using BudgetDesk;

internal static class AccountRefreshTests
{
    private static IEnumerable<Control> Tree(Control parent)
    {
        foreach (Control child in parent.Controls) { yield return child; foreach (Control nested in Tree(child)) yield return nested; }
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
    private static Control Field(Form dialog, string label)
    {
        foreach (TableLayoutPanel table in Tree(dialog).OfType<TableLayoutPanel>().Where(x => x.ColumnCount == 2))
            for (int row = 0; row < table.RowCount; row++)
            {
                Label name = table.GetControlFromPosition(0, row) as Label;
                if (name != null && name.Text == label) return table.GetControlFromPosition(1, row);
            }
        throw new Exception("Missing editor field: " + label);
    }
    private static void InsideAncestors(Control control, string context)
    {
        for (Control child = control; child.Parent != null; child = child.Parent)
            Require(child.Parent.ClientRectangle.Contains(child.Bounds), context + ": " + child.GetType().Name + " extends beyond " + child.Parent.GetType().Name + ".");
    }
    private static int[] CheckPage(MainForm form, int count, string context)
    {
        Panel cards = Tree(form).OfType<Panel>().Single(x => x.Name == "AccountCards");
        Panel page = (Panel)cards.Parent;
        Require(form.Data.Accounts.Count == count && cards.Controls.Count == count, context + ": account missing.");
        Require(!cards.AutoScroll && !cards.VerticalScroll.Visible && !cards.HorizontalScroll.Visible, context + ": account scrollbar appeared.");
        InsideAncestors(cards, context);
        foreach (Control card in cards.Controls) InsideAncestors(card, context);
        foreach (string name in new string[] { "GrandTotal", "RecurringHeader", "RecurringCards", "DashboardHint" })
        {
            Control section = Tree(page).Single(x => x.Name == name);
            Require(section.Visible && section.Height > 0, context + ": a lower section disappeared.");
            InsideAncestors(section, context + ": " + name);
        }
        Panel bills = Tree(page).OfType<Panel>().Single(x => x.Name == "RecurringCards");
        Require(bills.Height >= 40, context + ": payments are hidden.");
        return new int[] { cards.Height, bills.Top, bills.Height, Tree(page).Single(x => x.Name == "GrandTotal").Top };
    }
    private static void SaveAccount(MainForm form, int number, bool edit)
    {
        Exception failure = null;
        using (Timer timer = new Timer { Interval = 70 })
        {
            timer.Tick += delegate {
                timer.Stop(); Form dialog = Application.OpenForms.Cast<Form>().LastOrDefault(x => x != form);
                if (dialog == null) { failure = new Exception("The account editor did not open."); return; }
                try
                {
                    Field(dialog, "Account name").Text = edit ? "Renamed account" : "Saved account " + number;
                    ((NumericUpDown)Field(dialog, "Opening balance")).Value = number * 1250m;
                    Tree(dialog).OfType<Button>().Single(x => x.Text == "Save").PerformClick();
                    CheckPage(form, edit ? form.Data.Accounts.Count : number, "Immediately after saving " + number);
                }
                catch (Exception ex) { failure = ex; dialog.Close(); }
            };
            timer.Start();
            if (edit) typeof(MainForm).GetMethod("AccountEditor", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, new object[] { form.Data.Accounts[0] });
            else Tree(form).OfType<Button>().Single(x => x.Text == "+ Add account").PerformClick();
        }
        if (failure != null) throw failure;
        Application.DoEvents();
        CheckPage(form, edit ? form.Data.Accounts.Count : number, "After closing the Save dialog " + number);
    }
    private static void Capture(Form form, string folder, string name)
    {
        form.Refresh(); Application.DoEvents();
        using (Bitmap bitmap = new Bitmap(form.Width, form.Height))
        {
            using (Graphics graphics = Graphics.FromImage(bitmap)) graphics.CopyFromScreen(form.Location, Point.Empty, form.Size);
            bitmap.Save(Path.Combine(folder, name + ".png"), System.Drawing.Imaging.ImageFormat.Png);
        }
    }
    [STAThread]
    private static int Main(string[] args)
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        string temporary = Path.Combine(Path.GetTempPath(), "budgetdesk-refresh-" + Guid.NewGuid().ToString("N"));
        string images = args.Length == 0 ? Path.Combine(temporary, "screens") : args[0]; Directory.CreateDirectory(images);
        try
        {
            foreach (Size size in new Size[] { new Size(1320, 930), new Size(1000, 700) })
            {
                BudgetStore store = new BudgetStore(Path.Combine(temporary, size.Width.ToString())); int[] liveRows;
                using (MainForm form = new MainForm(store, BudgetData.Empty(), false))
                {
                    form.Size = size; form.Show(); Application.DoEvents();
                    for (int count = 1; count <= 20; count++)
                    {
                        SaveAccount(form, count, false);
                        if (count == 1)
                            form.Commit(d => Ledger.AddBill(d, new RecurringBill { Id = Ledger.NewId(), Name = "Visible recurring bill", Icon = "Home", AccountId = d.Accounts[0].Id, CategoryId = d.Categories[0].Id, Amount = 100m, AnchorDate = Ledger.Iso(DateTime.Today.AddDays(7)), Interval = 1, Unit = "Months", Enabled = true, AutoRecord = false }));
                        CheckPage(form, count, "Live account addition " + count);
                        if (count == 3 || count == 6 || count == 20) Capture(form, images, "saved-" + count + "-" + size.Width + "x" + size.Height);
                    }
                    Console.WriteLine("PASS totals, recurring controls and bill rows remain inside the visible page after each of 20 account-form saves at " + size.Width + "x" + size.Height);
                    SaveAccount(form, 20, true);
                    Console.WriteLine("PASS editing an account refreshes all lower sections at " + size.Width + "x" + size.Height);
                    for (int update = 0; update < 8; update++)
                    {
                        int amount = update;
                        form.Commit(d => d.Accounts[1].OpeningBalance = amount);
                        form.ShowPage(update % 3);
                    }
                    form.ShowPage(0); Application.DoEvents(); CheckPage(form, 20, "Queued refreshes after tab switches");
                    Console.WriteLine("PASS queued refreshes ignore replaced pages after repeated commits and tab switches at " + size.Width + "x" + size.Height);
                    liveRows = CheckPage(form, 20, "Before closing");
                    Require(store.Load().Accounts.Count == 20 && store.Load().Bills.Count == 1, "Saved data was lost.");
                    form.Close();
                }
                using (MainForm reopened = new MainForm(store, store.Load(), false))
                {
                    reopened.Size = size; reopened.Show(); Application.DoEvents();
                    Require(liveRows.SequenceEqual(CheckPage(reopened, 20, "Reopened reference layout")), "Live and reopened layouts differ.");
                    reopened.Close();
                }
                Console.WriteLine("PASS the live saved page has the same row sizes as a reopened page at " + size.Width + "x" + size.Height);
            }
            Console.WriteLine("\n8 live-refresh checks passed, including 40 additions through the actual account form.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { if (Directory.Exists(temporary)) Directory.Delete(temporary, true); }
    }
}
