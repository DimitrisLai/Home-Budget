using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using BudgetDesk;

internal static class AccountLayoutTests
{
    private static IEnumerable<Control> Tree(Control root)
    {
        yield return root;
        foreach (Control child in root.Controls)
            foreach (Control nested in Tree(child)) yield return nested;
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
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
        string temporary = Path.Combine(Path.GetTempPath(), "budgetdesk-cards-" + Guid.NewGuid().ToString("N"));
        string images = args.Length == 0 ? Path.Combine(temporary, "screens") : args[0];
        Directory.CreateDirectory(images);
        try
        {
            using (MainForm form = new MainForm(new BudgetStore(Path.Combine(temporary, "data")), BudgetData.Empty(), false))
            {
                form.Show(); Application.DoEvents();
                foreach (Size window in new Size[] { new Size(1320, 930), new Size(1000, 700) })
                {
                    form.Size = window; form.Data.Accounts.Clear();
                    for (int count = 0; count <= 20; count++)
                    {
                        if (count > 0)
                            Ledger.AddAccount(form.Data, new Account {
                                Id = Ledger.NewId(), Name = count == 4 ? "A long account name that must stay inside its card" : "Account " + count.ToString("00"),
                                Currency = "CZK", Icon = count % 2 == 0 ? "Wallet" : "Bank", OpeningDate = Ledger.Iso(DateTime.Today),
                                OpeningBalance = count == 5 ? 999999999999m : count == 6 ? -100.25m : count * 1500.50m
                            });
                        form.ShowPage(0); Application.DoEvents();
                        Panel cards = Tree(form).OfType<Panel>().Single(x => x.Name == "AccountCards");
                        string context = count + " accounts at " + window.Width + "x" + window.Height;
                        Require(!cards.AutoScroll && !cards.VerticalScroll.Visible && !cards.HorizontalScroll.Visible, "Account scrollbar: " + context);
                        Control[] items = cards.Controls.Cast<Control>().ToArray();
                        Require(count == 0 || items.Length == count, "Missing account: " + context);
                        Require(items.All(x => x.Visible && cards.ClientRectangle.Contains(x.Bounds)), "Clipped account: " + context);
                        if (count > 0)
                        {
                            Require(items.All(x => x.Width < 275 && x.Height < 94), "Cards are not smaller: " + context);
                            Require(items.Select(x => (string)x.Tag).SequenceEqual(form.Data.Accounts.Select(x => x.Id)), "Account order changed: " + context);
                            for (int i = 0; i < items.Length; i++)
                                for (int j = i + 1; j < items.Length; j++)
                                    Require(!items[i].Bounds.IntersectsWith(items[j].Bounds), "Overlapping accounts: " + context);
                        }
                        if (count == 0 || count == 3 || count == 20) Capture(form, images, count + "-accounts-" + window.Width + "x" + window.Height);
                    }
                    Console.WriteLine("PASS all account counts from 0 to 20 fit without scrolling, clipping or overlap at " + window.Width + "x" + window.Height);
                }
                // Resizing an existing grid must work without rebuilding the page.
                foreach (Size window in new Size[] { new Size(1320, 930), new Size(1000, 700), new Size(1120, 780) })
                {
                    form.Size = window; Application.DoEvents();
                    Panel cards = Tree(form).OfType<Panel>().Single(x => x.Name == "AccountCards");
                    Require(cards.Controls.Count == 20 && cards.Controls.Cast<Control>().All(x => cards.ClientRectangle.Contains(x.Bounds)) && !cards.VerticalScroll.Visible, "Resize clipped an account.");
                }
                Console.WriteLine("PASS an existing twenty-account grid reflows when the window is resized");
                form.Data.Accounts.RemoveRange(3, 17); form.ShowPage(0); Application.DoEvents();
                Panel reduced = Tree(form).OfType<Panel>().Single(x => x.Name == "AccountCards");
                Require(reduced.Height < 100 && reduced.Controls.Count == 3, "Removing accounts leaves an oversized empty section.");
                Console.WriteLine("PASS removing accounts shrinks the section back to a single compact row");
                Exception clickFailure = null; bool editorOpened = false;
                using (Timer timer = new Timer { Interval = 120 })
                {
                    timer.Tick += delegate {
                        timer.Stop(); Form editor = Application.OpenForms.Cast<Form>().LastOrDefault(x => x != form);
                        if (editor == null) { clickFailure = new Exception("Clicking the compact account did not open its editor."); return; }
                        editorOpened = Tree(editor).OfType<TextBox>().Any(x => x.Text == "Account 01"); editor.Close();
                    };
                    Label name = Tree(reduced.Controls[0]).OfType<Label>().Single(x => x.Text == "Account 01");
                    timer.Start(); typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(name, new object[] { EventArgs.Empty });
                }
                if (clickFailure != null) throw clickFailure;
                Require(editorOpened, "The compact card opened the wrong account.");
                Console.WriteLine("PASS clicking the compact account name still opens the correct editor");
                form.Close();
            }
            Console.WriteLine("\n5 account layout checks passed (42 account-count/window combinations plus resize, removal and editing).");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { if (Directory.Exists(temporary)) Directory.Delete(temporary, true); }
    }
}
