using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using BudgetDesk;

internal static class ScrollFreeTests
{
    private static IEnumerable<Control> Tree(Control parent)
    {
        foreach (Control child in parent.Controls) { yield return child; foreach (Control nested in Tree(child)) yield return nested; }
    }
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); }
    private static void Pump() { Application.DoEvents(); Application.DoEvents(); }
    private static void WaitState(Form form, FormWindowState state)
    {
        DateTime end = DateTime.UtcNow.AddSeconds(3);
        while (form.WindowState != state && DateTime.UtcNow < end) { Pump(); System.Threading.Thread.Sleep(10); }
        Pump(); Check(form.WindowState == state, "Window state was not applied: " + state);
        for (int settle = 0; settle < 8; settle++) { Pump(); System.Threading.Thread.Sleep(10); }
    }
    private static void Inside(Control control)
    {
        for (Control child = control; child.Parent != null; child = child.Parent)
            Check(child.Parent.ClientRectangle.Contains(child.Bounds), child.Name + " is outside its parent after layout.");
    }
    private static void Dashboard(MainForm form)
    {
        foreach (string name in new string[] { "AccountCards", "RecurringCards", "GrandTotal", "RecurringHeader", "DashboardHint" })
        {
            Control region = Tree(form).Single(x => x.Name == name); Inside(region); Check(region.Visible && region.Height > 0, name + " disappeared.");
            Panel panel = region as Panel;
            if (panel != null) Check(!panel.AutoScroll && !panel.VerticalScroll.Visible && !panel.HorizontalScroll.Visible, name + " has a scrollbar.");
        }
        Panel accounts = Tree(form).OfType<Panel>().Single(x => x.Name == "AccountCards"), bills = Tree(form).OfType<Panel>().Single(x => x.Name == "RecurringCards");
        Check(accounts.Controls.Count == form.Data.Accounts.Count, "An account is missing.");
        Check(form.Data.Bills.Count == 0 || bills.Controls.Count == form.Data.Bills.Count, "A recurring payment is missing.");
        foreach (Control card in accounts.Controls.Cast<Control>().Concat(bills.Controls.Cast<Control>())) Inside(card);
        Check(!Tree(form).OfType<DataGridView>().Any(), "Dashboard still contains a scrolling payment table.");
    }
    private static Control Field(Form form, string label)
    {
        foreach (TableLayoutPanel table in Tree(form).OfType<TableLayoutPanel>().Where(x => x.ColumnCount == 2))
            for (int row = 0; row < table.RowCount; row++)
            {
                Label name = table.GetControlFromPosition(0, row) as Label;
                if (name != null && name.Text == label) return table.GetControlFromPosition(1, row);
            }
        throw new Exception("Missing field: " + label);
    }
    private static void Dialog(MainForm main, Action open, Action<Form> edit)
    {
        Exception failure = null;
        using (Timer timer = new Timer { Interval = 65 })
        {
            timer.Tick += delegate {
                timer.Stop(); Form editor = Application.OpenForms.Cast<Form>().Last(x => x != main);
                try { edit(editor); Tree(editor).OfType<Button>().Single(x => x.Text == "Save").PerformClick(); }
                catch (Exception ex) { failure = ex; editor.Close(); }
            };
            timer.Start(); open();
        }
        if (failure != null) throw failure; Pump();
    }
    private static void Capture(Form form, string images, string name)
    {
        form.Location = new Point(20, 20); form.BringToFront(); form.Activate();
        for (int settle = 0; settle < 10; settle++) { Pump(); System.Threading.Thread.Sleep(10); }
        form.Refresh(); Pump();
        using (Bitmap bitmap = new Bitmap(form.Width, form.Height))
        {
            using (Graphics graphics = Graphics.FromImage(bitmap)) graphics.CopyFromScreen(form.Location, Point.Empty, form.Size);
            bitmap.Save(Path.Combine(images, name + ".png"), System.Drawing.Imaging.ImageFormat.Png);
        }
    }
    private static HashSet<string> WalkPages(PagedGridPanel pager)
    {
        HashSet<string> seen = new HashSet<string>(); pager.PageIndex = 0;
        for (int page = 0; page < 1000; page++)
        {
            Check(pager.Grid.ScrollBars == ScrollBars.None, "A data grid has scrollbars enabled.");
            Check(!Tree(pager.Grid).OfType<ScrollBar>().Any(x => x.Visible), "A native grid scrollbar appeared.");
            foreach (DataGridViewRow row in pager.Grid.Rows)
            {
                Check(pager.Grid.ClientRectangle.Contains(pager.Grid.GetRowDisplayRectangle(row.Index, false)), "A paged row is clipped.");
                Check(seen.Add((string)row.Tag), "A row appears on two pages.");
            }
            Button next = Tree(pager).OfType<Button>().Single(x => x.Text == "Next");
            if (!next.Enabled) return seen;
            next.PerformClick(); Pump();
        }
        throw new Exception("Pagination did not reach its end.");
    }
    [STAThread]
    private static int Main(string[] args)
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        string temp = Path.Combine(Path.GetTempPath(), "budgetdesk-scroll-free-" + Guid.NewGuid().ToString("N"));
        string images = args.Length == 0 ? Path.Combine(temp, "screens") : args[0]; Directory.CreateDirectory(images);
        try
        {
            BudgetData data = BudgetData.Empty();
            for (int i = 0; i < 20; i++)
                Ledger.AddAccount(data, new Account { Id = Ledger.NewId(), Name = "Bank " + (i + 1), Icon = i % 2 == 0 ? "Bank" : "Wallet", Currency = "CZK", OpeningBalance = 5000 + i * 100, OpeningDate = Ledger.Iso(DateTime.Today) });
            for (int i = 0; i < 20; i++)
                Ledger.AddBill(data, new RecurringBill { Id = Ledger.NewId(), Name = "Payment " + (i + 1), Icon = "Home", AccountId = data.Accounts[i].Id, CategoryId = data.Categories[0].Id, Amount = 100 + i * 5, AnchorDate = Ledger.Iso(DateTime.Today.AddDays(i + 1)), Interval = i % 3 + 1, Unit = "Months", Enabled = true, AutoRecord = false });
            while (data.Categories.Count < 20) Ledger.AddCategory(data, new Category { Id = Ledger.NewId(), Name = "Category " + data.Categories.Count, Icon = "Other" });
            for (int i = 0; i < 100; i++) data.Entries.Add(new Entry { Id = Ledger.NewId(), Kind = "Expense", Date = Ledger.Iso(DateTime.Today), AccountId = data.Accounts[0].Id, CategoryId = data.Categories[i % 20].Id, Description = "Purchase " + i, Amount = 10, RateToCzk = 1 });
            Ledger.Validate(data);
            using (MainForm main = new MainForm(new BudgetStore(Path.Combine(temp, "full")), data, false))
            {
                main.Show(); Pump();
                foreach (Size size in new Size[] { new Size(1320, 930), new Size(1000, 700) })
                {
                    main.Size = size; Pump(); Dashboard(main); Capture(main, images, "twenty-accounts-and-payments-" + size.Width);
                    for (int repeat = 0; repeat < 5; repeat++)
                    {
                        DashboardPanel dashboard = Tree(main).OfType<DashboardPanel>().Single();
                        Rectangle[] before = dashboard.AccountCards.Controls.Cast<Control>().Select(x => x.Bounds).ToArray();
                        main.WindowState = FormWindowState.Minimized; WaitState(main, FormWindowState.Minimized);
                        Check(main.WindowState == FormWindowState.Minimized, "Minimize state was not applied.");
                        dashboard.Size = new Size(160, 50); dashboard.Reflow();
                        Check(before.SequenceEqual(dashboard.AccountCards.Controls.Cast<Control>().Select(x => x.Bounds)), "Minimizing overwrote card sizes.");
                        main.Commit(d => d.Accounts[0].Name = "Bank restored " + repeat);
                        main.WindowState = FormWindowState.Normal; WaitState(main, FormWindowState.Normal); Pump(); Dashboard(main);
                    }
                    Console.WriteLine("PASS all twenty accounts and payments stay visible through five minimize/restore cycles and minimized saves at " + size.Width + "x" + size.Height);
                }
                Panel cards = Tree(main).OfType<Panel>().Single(x => x.Name == "RecurringCards"); string target = (string)cards.Controls[19].Tag;
                typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(cards.Controls[19], new object[] { EventArgs.Empty });
                Tree(main).OfType<Button>().Single(x => x.Text == "Pause / resume").PerformClick(); Pump();
                Check(!main.Data.Bills.Single(x => x.Id == target).Enabled && main.Data.Bills.Count(x => !x.Enabled) == 1, "Pause changed the wrong payment.");
                cards = Tree(main).OfType<Panel>().Single(x => x.Name == "RecurringCards"); Control chosen = cards.Controls.Cast<Control>().Single(x => (string)x.Tag == target);
                Dialog(main, delegate { typeof(Control).GetMethod("OnDoubleClick", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(chosen, new object[] { EventArgs.Empty }); }, delegate(Form editor) { Field(editor, "Payment name").Text = "Edited payment"; ((NumericUpDown)Field(editor, "Amount (account currency)")).Value = 789m; });
                Check(main.Data.Bills.Single(x => x.Id == target).Amount == 789m && main.Data.Bills.Single(x => x.Id == target).Name == "Edited payment", "Payment-card editing did not persist.");
                Dashboard(main); Console.WriteLine("PASS payment selection, pause and double-click editing affect the intended schedule");
                main.ShowPage(1); Pump(); PagedGridPanel entries = Tree(main).OfType<PagedGridPanel>().Single();
                HashSet<string> entryIds = WalkPages(entries);
                Check(entryIds.SetEquals(main.Data.Entries.Select(x => x.Id)) && entryIds.Count == 100, "History was lost behind pagination.");
                Check(Tree(main).OfType<Label>().Any(x => x.Text.StartsWith("100 matching entries")), "The filtered total counts only the visible page.");
                Capture(main, images, "daily-entries-last-page");
                entries.Grid.Rows[0].Selected = true; string selected = (string)entries.Grid.SelectedRows[0].Tag;
                Dialog(main, delegate { Tree(main).OfType<Button>().Single(x => x.Text == "Edit selected").PerformClick(); }, delegate(Form editor) { Field(editor, "Description").Text = "Edited last-page entry"; });
                Check(main.Data.Entries.Single(x => x.Id == selected).Description == "Edited last-page entry", "Editing on a later page changed the wrong entry.");
                Tree(main).OfType<TextBox>().Single(x => x.AccessibleName == "Search descriptions").Text = "Edited last-page entry"; Pump();
                entries = Tree(main).OfType<PagedGridPanel>().Single(); Check(entries.ItemCount == 1 && entries.PageIndex == 0 && (string)entries.Grid.Rows[0].Tag == selected, "Filtering does not reset pagination correctly.");
                Console.WriteLine("PASS all one hundred entries are reachable without scrollbars, with correct later-page editing, totals and filtering");
                main.ShowPage(2); Pump(); PagedGridPanel overview = Tree(main).OfType<PagedGridPanel>().Single();
                Check(WalkPages(overview).SetEquals(main.Data.Categories.Select(x => x.Id)), "A category is missing from Monthly overview.");
                Capture(main, images, "monthly-overview-last-page"); Console.WriteLine("PASS Monthly overview exposes all twenty categories through page buttons");
                main.ShowPage(1); Pump(); Exception categoryFailure = null;
                using (Timer timer = new Timer { Interval = 80 })
                {
                    timer.Tick += delegate {
                        timer.Stop(); Form dialog = Application.OpenForms.Cast<Form>().Last(x => x != main);
                        try { PagedGridPanel pager = Tree(dialog).OfType<PagedGridPanel>().Single(); Check(WalkPages(pager).SetEquals(main.Data.Categories.Select(x => x.Id)), "A category is missing from the category manager."); }
                        catch (Exception ex) { categoryFailure = ex; }
                        dialog.Close();
                    };
                    timer.Start(); Tree(main).OfType<Button>().Single(x => x.Text.StartsWith("Categories  ")).PerformClick();
                }
                if (categoryFailure != null) throw categoryFailure;
                Console.WriteLine("PASS the category manager exposes all twenty categories without scrollbars");
                main.Close();
            }
            BudgetData live = BudgetData.Empty(); Ledger.AddAccount(live, new Account { Id = Ledger.NewId(), Name = "Live account", Icon = "Bank", Currency = "CZK", OpeningBalance = 10000, OpeningDate = Ledger.Iso(DateTime.Today) });
            using (MainForm main = new MainForm(new BudgetStore(Path.Combine(temp, "live")), live, false))
            {
                main.Size = new Size(1000, 700); main.Show(); Pump();
                for (int count = 1; count <= 20; count++)
                {
                    int number = count;
                    Dialog(main, delegate { Tree(main).OfType<Button>().Single(x => x.Text == "+ Recurring payment").PerformClick(); }, delegate(Form editor) { Field(editor, "Payment name").Text = "Live payment " + number; ((NumericUpDown)Field(editor, "Amount (account currency)")).Value = number * 10; ((DateTimePicker)Field(editor, "Next unpaid date")).Value = DateTime.Today.AddDays(number); });
                    Check(main.Data.Bills.Count == count, "A newly saved payment is missing."); Dashboard(main);
                }
                Capture(main, images, "twenty-payments-after-live-add"); main.Close();
            }
            Console.WriteLine("PASS all twenty recurring payments can be added through the real editor without restarting or scrolling");
            Console.WriteLine("\n7 scroll-free and restore checks passed."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { if (Directory.Exists(temp)) Directory.Delete(temp, true); }
    }
}
