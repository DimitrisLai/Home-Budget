using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using BudgetDesk;

internal static class Release110Tests
{
    static int passed;
    static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
        passed++;
        Console.WriteLine("PASS " + message);
    }
    static void Reject(Action action, string message)
    {
        try { action(); } catch (BudgetException) { Check(true, message); return; }
        throw new Exception(message);
    }
    static BudgetData Sample()
    {
        var d = BudgetData.Empty();
        d.Accounts.Add(new Account { Id = "cz", Name = "Zeta bank", Currency = "CZK", Icon = "Bank", OpeningBalance = 10000, OpeningDate = "2024-01-01" });
        d.Accounts.Add(new Account { Id = "eur", Name = "Alpha cash", Currency = "EUR", Icon = "Wallet", OpeningDate = "2024-01-01" });
        d.Rates["EUR"] = 25;
        return d;
    }
    static void MoneyAndTime()
    {
        var d = Sample();
        var due = new DateTime(2026, 11, 18, 15, 30, 0); var paid = due.AddHours(2).AddSeconds(14);
        var plan = new PlannedExpense { Id = "plan", DueDate = Ledger.Iso(due), DueTime = Ledger.Clock(due), PurchaseCurrency = "EUR", ExchangeRate = 25,
            AccountId = "cz", CategoryId = d.Categories[0].Id, Amount = 20, Description = "Vienna lunch" };
        d.PlannedExpenses.Add(plan); Ledger.Validate(d);
        Check(Ledger.Balance(d, "cz", due) == 10000 && Ledger.PlanInBase(d, plan) == 500, "a planned 20 EUR purchase estimates 500 CZK without posting");
        Check(!Ledger.PlanOverdue(plan, due.AddSeconds(-1)) && Ledger.PlanOverdue(plan, due.AddSeconds(1)), "timed plans cross overdue at their scheduled local time");
        Ledger.RecordPlanned(d, "plan", paid, paid);
        var e = d.Entries.Single();
        Check(e.Time == "17:30:14" && e.ScheduledTime == "15:30:00" && e.Date == e.ScheduledDate, "paid and scheduled timestamps are retained separately");
        Check(e.Amount == 20 && e.PurchaseCurrency == "EUR" && Ledger.Balance(d, "cz", due) == 9500, "foreign expense preserves the receipt and deducts the account once");
        Reject(delegate { Ledger.RecordPlanned(d, "plan", paid, paid); }, "a second Record paid cannot create a duplicate");
        d.Rates["EUR"] = 26;
        Check(Ledger.MonthTotal(d, paid, "Expense") == 500 && Ledger.Balance(d, "cz", paid) == 9500, "new settings do not reprice a recorded expense");
        e.Amount = 1; e.ExchangeRate = 1.01m; e.Splits = new List<ExpenseSplit> {
            new ExpenseSplit { CategoryId = d.Categories[0].Id, Amount = 0.50m }, new ExpenseSplit { CategoryId = d.Categories[1].Id, Amount = 0.50m } }; e.CategoryId = null;
        Ledger.Validate(d);
        Check(e.Splits.Sum(part => Ledger.CategoryExpenseInBase(d, e, part.CategoryId)) == 1.01m, "converted split cents reconcile to the exact bank debit");
        var template = Ledger.TemplateFromEntry(e); d.Templates.Add(template);
        var draft = Ledger.TemplateDraft(d, template.Id, paid);
        Check(draft.PurchaseCurrency == "EUR" && draft.Amount == 1 && draft.Splits.Count == 2, "foreign split templates retain the receipt denomination");
        var imported = BudgetStore.Parse(BudgetStore.Serialize(d));
        Check(imported.Entries.Single().Time == e.Time && imported.Entries.Single().ExchangeRate == 1.01m, "JSON round trip preserves historical rate and timestamps");
        var csv = BudgetStore.Csv(d, paid);
        Check(csv.Contains("17:30:14") && csv.Contains("15:30:00") && csv.Contains("EUR") && csv.Contains("1.01"), "CSV contains original purchase, converted amount, rate and both timestamps");
        var old = Sample(); old.SchemaVersion = 4;
        old.Entries.Add(new Entry { Id = "old", Date = "2026-11-17", Kind = "Income", AccountId = "cz", Description = "Legacy", Amount = 100, RateToCzk = 1 });
        var legacy = BudgetStore.Parse(BudgetStore.Serialize(old));
        Check(legacy.SchemaVersion == 5 && legacy.Entries[0].Time == null && Ledger.Balance(legacy, "cz", due) == 10100, "schema 4 keeps old balances and a date-only history");
        var bill = new RecurringBill { Id = "bill", Name = "Gym", Icon = "Sport", AnchorDate = Ledger.Iso(due), DueTime = Ledger.Clock(due), AccountId = "cz",
            CategoryId = d.Categories[0].Id, Amount = 10, PurchaseCurrency = "EUR", ExchangeRate = 25, Unit = "Months", Interval = 1, Enabled = true, AutoRecord = true };
        d.Bills.Add(bill);
        Check(Ledger.PostDue(d, due.AddSeconds(-1)) == 0 && Ledger.PostDue(d, due) == 1 && Ledger.PostDue(d, paid) == 0, "automatic recurring charges wait until due and post exactly once");
        Check(d.Entries.Last().Time == "15:30:00" && Ledger.AccountAmount(d.Entries.Last().Amount, d.Entries.Last().ExchangeRate) == 250, "automatic charges preserve their scheduled time and foreign rate");
        var manualTime = due.AddMonths(1).AddDays(2).AddHours(1);
        Ledger.RecordNext(d, bill, manualTime);
        Check(d.Entries.Last().Date == Ledger.Iso(manualTime) && d.Entries.Last().Time == Ledger.Clock(manualTime) && d.Entries.Last().ScheduledDate != d.Entries.Last().Date, "manual recurring payment books at click time and keeps the scheduled date");
        e.ExchangeRate = 0; Reject(delegate { Ledger.Validate(d); }, "foreign expense cannot silently post at a missing rate");
    }
    static void Header(DataGridView grid, int column)
    {
        var args = new DataGridViewCellMouseEventArgs(column, -1, 4, 4, new MouseEventArgs(MouseButtons.Left, 1, 4, 4, 0));
        typeof(DataGridView).GetMethod("OnColumnHeaderMouseClick", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(grid, new object[] { args });
    }
    static void Controls(string folder)
    {
        Application.EnableVisualStyles();
        var d = Sample();
        using (var form = new MainForm(new BudgetStore(folder), d, false))
        {
            form.Size = new Size(1000, 700); form.Show(); Application.DoEvents();
            var card = form.Controls.Find("AccountCard", true).First();
            var name = card.Controls.Find("AccountName", true).Single(); var icon = card.Controls.Find("AccountIcon", true).Single();
            var balance = card.Controls.Find("AccountBalance", true).Single();
            Check(name.Right <= icon.Left && icon.Bounds.IntersectsWith(balance.Bounds) == false, "minimum-size account card reserves separate space for name, trailing icon and balance");
            using (var picker = Icons.Picker("Bank"))
            {
                var choices = picker.Items.Cast<Pick>().ToList();
                Check(choices.All(i => i.Text.StartsWith(i.Id)) && choices.Select(i => i.Id).SequenceEqual(choices.Select(i => i.Id).OrderBy(s => s, StringComparer.CurrentCultureIgnoreCase)), "icon picker is alphabetical and every option starts with its name");
                typeof(NamePicker).GetMethod("OnKeyPress", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(picker, new object[] { new KeyPressEventArgs('s') });
                Check(Icons.Selected(picker) == choices.First(i => i.Id.StartsWith("S")).Id, "one letter jumps to the first matching option");
                var next = new KeyPressEventArgs('s');
                typeof(NamePicker).GetMethod("OnKeyPress", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(picker, new object[] { next });
                Check(Icons.Selected(picker) == choices.First(i => i.Id.StartsWith("S")).Id && next.Handled, "repeated initials still select the first name");
                picker.DropDownStyle = ComboBoxStyle.DropDown;
                var typed = new KeyPressEventArgs('z');
                typeof(NamePicker).GetMethod("OnKeyPress", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(picker, new object[] { typed });
                Check(!typed.Handled, "editable payee fields accept freely typed names");
            }
            using (var host = new Form()) using (var pager = new PagedGridPanel("sort"))
            {
                host.Size = new Size(340, 240); host.Controls.Add(pager); Theme.Column(pager.Grid, "amount", "Amount", 100);
                var rows = Enumerable.Range(0, 40).Select(i => new BudgetDesk.GridItem(i.ToString(), (i == 27 ? -120 : i) + " CZK") { SortKeys = new object[] { i == 27 ? -120 : i } }).ToList();
                host.Show(); Application.DoEvents(); pager.SetRows(rows, true);
                Check(pager.PageCount > 1, "sort test spans several pages");
                Header(pager.Grid, 0); Check((string)pager.Grid.Rows[0].Tag == "27", "first click sorts numeric values globally across pages");
                Header(pager.Grid, 0); Check((string)pager.Grid.Rows[0].Tag == "39", "second click reverses numeric order");
                Header(pager.Grid, 0); Check((string)pager.Grid.Rows[0].Tag == "0" && pager.Grid.Columns[0].HeaderCell.SortGlyphDirection == SortOrder.None, "third click restores the original order and clears the arrow");
            }
            form.ShowPage(3);
            var months = (DataGridView)form.Controls.Find("YearlyMonthsGrid", true).Single();
            Header(months, 0); Header(months, 0);
            Check((int)months.Rows[0].Tag == 12, "yearly month sort preserves the month used for drill-down");
            Header(months, 0); Check((int)months.Rows[0].Tag == 1, "yearly third click restores January first");
            Exception modalFailure = null;
            using (var timer = new Timer { Interval = 150 })
            {
                timer.Tick += delegate {
                    timer.Stop(); var editor = (Editor)Application.OpenForms.Cast<Form>().Last(f => f is Editor);
                    try {
                        var currency = (ComboBox)editor.Controls.Find("PurchaseCurrency", true).Single();
                        Check(Icons.Selected(currency) == "CZK", "new expense defaults to its bank currency");
                        ((NumericUpDown)editor.Controls.Find("EntryAmount", true).Single()).Value = 12.50m;
                        Icons.Choose(currency, "EUR"); var quote = (NumericUpDown)editor.Controls.Find("PurchaseExchangeRate", true).Single();
                        Check(quote.Value == 25, "editor suggests EUR to CZK from manual settings");
                        quote.Value = 24.8m;
                        ((DateTimePicker)editor.Controls.Find("EntryDateTime", true).Single()).Value = DateTime.Today.AddHours(8).AddMinutes(17).AddSeconds(9);
                        editor.Save.PerformClick();
                    } catch (Exception ex) { modalFailure = ex; editor.Close(); }
                };
                timer.Start();
                typeof(MainForm).GetMethod("EntryEditor", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, new object[] { null, "Expense" });
            }
            if (modalFailure != null) throw modalFailure;
            var entry = form.Store.Load().Entries.Single();
            Check(entry.PurchaseCurrency == "EUR" && entry.Amount == 12.5m && entry.ExchangeRate == 24.8m && Ledger.Balance(form.Data, "cz", DateTime.Today) == 9690m && entry.Time == "08:17:09", "the real editor saves 12.50 EUR at 24.8 as exactly 310 CZK with the chosen time");
            var plan = new PlannedExpense { Id = "tomorrow", DueDate = Ledger.Iso(DateTime.Today.AddDays(1)), DueTime = "15:30:00",
                CategoryId = d.Categories[0].Id, AccountId = "cz", Amount = 90, Description = "Booked train" };
            form.Commit(data => data.PlannedExpenses.Add(plan));
            DateTime confirmed = DateTime.MinValue;
            using (var timer = new Timer { Interval = 1350 })
            {
                timer.Tick += delegate {
                    timer.Stop();
                    var editor = Application.OpenForms.OfType<Editor>().Single();
                    confirmed = DateTime.Now;
                    editor.Save.PerformClick();
                };
                timer.Start();
                typeof(MainForm).GetMethod("RecordPlannedEditor", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, new object[] { plan });
            }
            var receipt = form.Store.Load().Entries.Single(e => e.PlanId == plan.Id);
            DateTime posted = Ledger.Moment(receipt.Date, receipt.Time);
            Check(posted >= confirmed.AddTicks(-(confirmed.Ticks % TimeSpan.TicksPerSecond)) && posted <= DateTime.Now
                && receipt.ScheduledDate == plan.DueDate && receipt.ScheduledTime == plan.DueTime && form.Data.PlannedExpenses.Count == 0,
                "Record paid uses the confirmation click time and retains the original due date and time");
        }
    }
    [STAThread]
    static int Main(string[] args)
    {
        var folder = Path.Combine(Path.GetTempPath(), "budgetdesk-110-" + Guid.NewGuid().ToString("N"));
        try { MoneyAndTime(); if (!args.Contains("--ledger-only")) Controls(folder); Console.WriteLine(passed + " release 1.1 checks passed."); return 0; }
        catch (Exception ex) { Console.Error.WriteLine("FAIL " + ex); return 1; }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
}
