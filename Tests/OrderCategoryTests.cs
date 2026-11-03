using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using BudgetDesk;

internal static class OrderCategoryTests
{
    private static int passed;
    private static IEnumerable<Control> Tree(Control parent)
    { foreach (Control child in parent.Controls) { yield return child; foreach (Control nested in Tree(child)) yield return nested; } }
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); passed++; Console.WriteLine("PASS " + message); }
    private static Control Field(Form editor, string label)
    {
        foreach (TableLayoutPanel table in Tree(editor).OfType<TableLayoutPanel>().Where(x => x.ColumnCount == 2))
            for (int row = 0; row < table.RowCount; row++)
            {
                Label name = table.GetControlFromPosition(0, row) as Label;
                if (name != null && name.Text == label) return table.GetControlFromPosition(1, row);
            }
        throw new Exception("Missing field " + label);
    }
    private static void Dialog(MainForm main, Action open, Action<Form> edit, bool save)
    {
        Exception failure = null;
        using (Timer timer = new Timer { Interval = 80 })
        {
            timer.Tick += delegate {
                timer.Stop(); Form editor = Application.OpenForms.Cast<Form>().Last(x => x != main);
                try { edit(editor); if (save) Tree(editor).OfType<Button>().Single(x => x.Text == "Save").PerformClick(); else editor.Close(); }
                catch (Exception ex) { failure = ex; editor.Close(); }
            };
            timer.Start(); open();
        }
        if (failure != null) throw failure; Application.DoEvents();
    }
    [STAThread]
    private static int Main(string[] args)
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        string temp = Path.Combine(Path.GetTempPath(), "budgetdesk-order-" + Guid.NewGuid().ToString("N"));
        try
        {
            BudgetData data = Ledger.Demo(DateTime.Today); string[] original = data.Accounts.Select(x => x.Id).ToArray();
            Dictionary<string, decimal> balances = data.Accounts.ToDictionary(x => x.Id, x => Ledger.Balance(data, x.Id, DateTime.Today));
            string entryReferences = String.Join("|", data.Entries.Select(x => x.Id + ":" + x.AccountId + ":" + x.ToAccountId));
            string billReferences = String.Join("|", data.Bills.Select(x => x.Id + ":" + x.AccountId));
            Ledger.MoveAccount(data, original[2], 0); Ledger.Validate(data);
            Check(data.Accounts.Select(x => x.Id).SequenceEqual(new string[] { original[2], original[0], original[1] }), "moving the last bank first preserves the order of the others");
            Ledger.MoveAccount(data, original[2], 2);
            Check(data.Accounts.Select(x => x.Id).SequenceEqual(original), "moving a bank later uses its final requested position");
            Check(data.Accounts.All(x => Ledger.Balance(data, x.Id, DateTime.Today) == balances[x.Id]) && entryReferences == String.Join("|", data.Entries.Select(x => x.Id + ":" + x.AccountId + ":" + x.ToAccountId)) && billReferences == String.Join("|", data.Bills.Select(x => x.Id + ":" + x.AccountId)), "ordering keeps every balance, transaction account and payment account unchanged");
            string before = BudgetStore.Serialize(data); int rejected = 0;
            try { Ledger.MoveAccount(data, original[0], -1); } catch (BudgetException) { rejected++; }
            try { Ledger.MoveAccount(data, "unknown", 0); } catch (BudgetException) { rejected++; }
            Check(rejected == 2 && before == BudgetStore.Serialize(data), "invalid moves leave the budget unchanged");
            BudgetStore store = new BudgetStore(temp); store.Save(data);
            using (MainForm main = new MainForm(store, data, false))
            {
                main.Size = new Size(1000, 700); main.Show(); Application.DoEvents();
                Panel cards = Tree(main).OfType<Panel>().Single(x => x.Name == "AccountCards"); Control destination = cards.Controls[0];
                typeof(MainForm).GetField("accountDragId", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(main, original[2]);
                DataObject payload = new DataObject("BudgetDesk.AccountOrder", original[2]);
                DragEventArgs enter = new DragEventArgs(payload, 1, 0, 0, DragDropEffects.Move, DragDropEffects.None);
                typeof(Control).GetMethod("OnDragEnter", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(destination, new object[] { enter });
                DragEventArgs drop = new DragEventArgs(payload, 1, 0, 0, DragDropEffects.Move, DragDropEffects.None);
                typeof(Control).GetMethod("OnDragDrop", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(destination, new object[] { drop });
                typeof(MainForm).GetMethod("FinishAccountDrag", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(main, null); Application.DoEvents();
                Check(enter.Effect == DragDropEffects.Move && main.Data.Accounts[0].Id == original[2] && store.Load().Accounts[0].Id == original[2], "card drop events reorder immediately and save the bank order");
                DragEventArgs foreign = new DragEventArgs(new DataObject("unrelated text"), 1, 0, 0, DragDropEffects.Move, DragDropEffects.None);
                cards = Tree(main).OfType<Panel>().Single(x => x.Name == "AccountCards");
                typeof(Control).GetMethod("OnDragEnter", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(cards.Controls[0], new object[] { foreign });
                Check(foreign.Effect == DragDropEffects.None, "unrelated external drops are ignored");
                Dialog(main, delegate { typeof(MainForm).GetMethod("AccountEditor", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(main, new object[] { main.Data.Accounts[0] }); }, delegate(Form editor) { ((NumericUpDown)Field(editor, "Display position")).Value = 2; }, true);
                Check(main.Data.Accounts[1].Id == original[2] && store.Load().Accounts[1].Id == original[2], "the account editor can set and persist a numbered display position");
                string[] savedOrder = main.Data.Accounts.Select(x => x.Id).ToArray();
                main.ShowPage(1); Application.DoEvents();
                Dialog(main, delegate { Tree(main).OfType<Button>().Single(x => x.AccessibleName == "Add expense category").PerformClick(); }, delegate(Form editor) {
                    Field(editor, "Category name").Text = "My custom category";
                    Icons.Choose((ComboBox)Field(editor, "Icon"), "Education");
                    ((NumericUpDown)Field(editor, "Monthly budget (CZK)")).Value = 750;
                }, true);
                Category custom = main.Data.Categories.Single(x => x.Name == "My custom category");
                Check(custom.Icon == "Education" && custom.MonthlyBudgetCzk == 750 && store.Load().Categories.Any(x => x.Id == custom.Id), "the visible Add category button saves a custom name, icon and optional budget");
                Dialog(main, delegate { Tree(main).OfType<Button>().Single(x => x.Text == "+ Expense").PerformClick(); }, delegate(Form editor) {
                    ComboBox accounts = (ComboBox)Field(editor, "Account / send from");
                    Check(accounts.Items.Cast<Pick>().Select(x => x.Id).SequenceEqual(savedOrder), "account selectors follow the saved bank order");
                    Field(editor, "Description").Text = "Custom-category purchase"; ((NumericUpDown)Field(editor, "Amount (account currency)")).Value = 12;
                    Icons.Choose((ComboBox)Field(editor, "Expense category"), custom.Id);
                }, true);
                Check(main.Data.Entries.Any(x => x.Description == "Custom-category purchase" && x.CategoryId == custom.Id), "custom categories are available immediately when recording an expense");
                main.Commit(d => { while (d.Categories.Count < 20) Ledger.AddCategory(d, new Category { Id = Ledger.NewId(), Name = "Extra " + d.Categories.Count, Icon = "Other" }); });
                Check(!Tree(main).OfType<Button>().Single(x => x.AccessibleName == "Add expense category").Enabled, "the direct category button respects the twenty-category limit");
                BudgetData reloaded = store.Load(); Check(reloaded.Accounts.Select(x => x.Id).SequenceEqual(savedOrder) && reloaded.Categories.Any(x => x.Id == custom.Id), "bank order and custom categories survive save and reload together");
                main.Close();
            }
            Console.WriteLine("\n" + passed + " bank-order and category checks passed."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { if (Directory.Exists(temp)) Directory.Delete(temp, true); }
    }
}
