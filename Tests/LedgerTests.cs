using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using BudgetDesk;

internal static class LedgerTests
{
    static int passed, failed;
    static readonly DateTime Today = new DateTime(2026, 11, 3);
    static void Test(string name, Action run)
    {
        try { run(); passed++; Console.WriteLine("PASS  " + name); }
        catch (Exception ex) { failed++; Console.WriteLine("FAIL  " + name + ": " + ex); }
    }
    static void Equal<T>(T expected, T actual)
    { if (!Object.Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual); }
    static void Reject(Action action)
    { try { action(); } catch (BudgetException) { return; } throw new Exception("Expected the operation to be rejected."); }
    static BudgetData Data()
    {
        BudgetData d = BudgetData.Empty();
        d.Accounts.Add(new Account { Id = "a", Name = "Bank", Icon = "Bank", Currency = "CZK", OpeningBalance = 1000m, OpeningDate = "2026-01-01" });
        d.Accounts.Add(new Account { Id = "b", Name = "Savings", Icon = "Wallet", Currency = "CZK", OpeningBalance = 500m, OpeningDate = "2026-01-01" });
        return d;
    }
    static Entry Entry(BudgetData d, string kind, decimal amount, string date)
    {
        return new Entry { Id = Ledger.NewId(), Kind = kind, Amount = amount, Date = date, AccountId = "a", CategoryId = kind == "Expense" ? d.Categories[0].Id : null, Description = "Test", RateToCzk = 1m };
    }
    static RecurringBill Bill(BudgetData d, string date, string unit, int interval)
    {
        return new RecurringBill { Id = "bill", Name = "Rent", Icon = "Home", AccountId = "a", CategoryId = d.Categories[0].Id, Amount = 100m,
            AnchorDate = date, Unit = unit, Interval = interval, Enabled = true, AutoRecord = true };
    }
    static void Main()
    {
        Test("empty data is valid and starts without invented balances", delegate { BudgetData d = BudgetData.Empty(); Ledger.Validate(d); Equal(0m, Ledger.TotalBalance(d, Today)); Equal(8, d.Categories.Count); });
        Test("expenses and income affect only their selected account", delegate {
            BudgetData d = Data(); d.Entries.Add(Entry(d, "Expense", 125.25m, "2026-11-02")); d.Entries.Add(Entry(d, "Income", 400m, "2026-11-03")); Ledger.Validate(d);
            Equal(1274.75m, Ledger.Balance(d, "a", Today)); Equal(500m, Ledger.Balance(d, "b", Today)); Equal(1774.75m, Ledger.TotalBalance(d, Today));
        });
        Test("decimal arithmetic does not accumulate floating point drift", delegate {
            BudgetData d = Data(); for (int i = 0; i < 1000; i++) d.Entries.Add(Entry(d, "Expense", 0.01m, "2026-11-01")); Equal(990m, Ledger.Balance(d, "a", Today));
        });
        Test("transfer preserves the combined balance and is not spending", delegate {
            BudgetData d = Data(); Entry e = Entry(d, "Transfer", 300m, "2026-11-02"); e.ToAccountId = "b"; e.ReceivedAmount = 300; d.Entries.Add(e); Ledger.Validate(d);
            Equal(700m, Ledger.Balance(d, "a", Today)); Equal(800m, Ledger.Balance(d, "b", Today)); Equal(1500m, Ledger.TotalBalance(d, Today));
            Equal(0m, Ledger.MonthTotal(d, Today, "Expense")); Equal(0m, Ledger.MonthTotal(d, Today, "Income"));
        });
        Test("cross-currency transfer uses the actual amount received", delegate {
            BudgetData d = Data(); d.Rates["EUR"] = 25m; d.Accounts[1].Currency = "EUR"; d.Accounts[1].OpeningBalance = 0;
            Entry e = Entry(d, "Transfer", 250m, "2026-11-02"); e.ToAccountId = "b"; e.ReceivedAmount = 9.8m; d.Entries.Add(e); Ledger.Validate(d);
            Equal(750m, Ledger.Balance(d, "a", Today)); Equal(9.8m, Ledger.Balance(d, "b", Today)); Equal(995m, Ledger.TotalBalance(d, Today));
        });
        Test("same-currency transfers reject an unequal received amount", delegate { BudgetData d = Data(); Entry e = Entry(d, "Transfer", 200, "2026-11-02"); e.ToAccountId = "b"; e.ReceivedAmount = 100; d.Entries.Add(e); Reject(() => Ledger.Validate(d)); });
        Test("transfers cannot target their source account", delegate { BudgetData d = Data(); Entry e = Entry(d, "Transfer", 200, "2026-11-02"); e.ToAccountId = "a"; e.ReceivedAmount = 200; d.Entries.Add(e); Reject(() => Ledger.Validate(d)); });
        Test("monthly totals isolate the year and month", delegate {
            BudgetData d = Data(); d.Entries.Add(Entry(d, "Expense", 10, "2026-10-31")); d.Entries.Add(Entry(d, "Expense", 20, "2026-11-01"));
            d.Entries.Add(Entry(d, "Expense", 30, "2026-12-01")); d.Entries.Add(Entry(d, "Expense", 40, "2027-11-01")); Equal(20m, Ledger.MonthTotal(d, Today, "Expense"));
        });
        Test("future dated entries do not affect the balance yet", delegate { BudgetData d = Data(); d.Entries.Add(Entry(d, "Expense", 100, "2026-11-04")); Equal(1000m, Ledger.Balance(d, "a", Today)); });
        Test("entries before the opening date are rejected", delegate { BudgetData d = Data(); d.Entries.Add(Entry(d, "Expense", 100, "2025-12-31")); Reject(() => Ledger.Validate(d)); });
        Test("opening balance applies at start of day", delegate { BudgetData d = Data(); d.Entries.Add(Entry(d, "Expense", 10, "2026-01-01")); Equal(990m, Ledger.Balance(d, "a", new DateTime(2026, 1, 1))); Equal(0m, Ledger.Balance(d, "a", new DateTime(2025, 12, 31))); });
        Test("edits and deletion recalculate rather than double deducting", delegate {
            BudgetData d = Data(); Entry e = Entry(d, "Expense", 100, "2026-11-02"); d.Entries.Add(e); Equal(900m, Ledger.Balance(d, "a", Today));
            e.Amount = 150; Equal(850m, Ledger.Balance(d, "a", Today)); e.AccountId = "b"; Equal(1000m, Ledger.Balance(d, "a", Today)); Equal(350m, Ledger.Balance(d, "b", Today));
            d.Entries.Remove(e); Equal(500m, Ledger.Balance(d, "b", Today));
        });
        Test("monthly dates retain the original day after February", delegate { BudgetData d = Data(); var b = Bill(d, "2026-01-31", "Months", 1); Equal(new DateTime(2026, 2, 28), Ledger.Occurrence(b, 1)); Equal(new DateTime(2026, 3, 31), Ledger.Occurrence(b, 2)); });
        Test("a schedule can move to a newer account while keeping its original monthly anchor", delegate {
            BudgetData d = Data(); var b = Bill(d, "2026-01-31", "Months", 1); b.NextIndex = 10; b.AccountId = "b"; d.Accounts[1].OpeningDate = "2026-11-01"; d.Bills.Add(b);
            Ledger.Validate(d); Equal(new DateTime(2026, 11, 30), Ledger.DueDate(b)); Equal(new DateTime(2026, 12, 31), Ledger.Occurrence(b, 11));
            b.NextIndex = 9; Reject(() => Ledger.Validate(d));
        });
        Test("quarterly dates cross years correctly", delegate { BudgetData d = Data(); var b = Bill(d, "2026-11-30", "Months", 3); Equal(new DateTime(2027, 2, 28), Ledger.Occurrence(b, 1)); Equal(new DateTime(2027, 5, 30), Ledger.Occurrence(b, 2)); });
        Test("leap year annual payments retain February 29 when possible", delegate { BudgetData d = Data(); var b = Bill(d, "2024-02-29", "Years", 1); Equal(new DateTime(2025, 2, 28), Ledger.Occurrence(b, 1)); Equal(new DateTime(2028, 2, 29), Ledger.Occurrence(b, 4)); });
        Test("weekly and daily schedules", delegate { BudgetData d = Data(); var b = Bill(d, "2026-11-03", "Weeks", 2); Equal(new DateTime(2026, 11, 17), Ledger.Occurrence(b, 1)); b.Unit = "Days"; b.Interval = 3; Equal(new DateTime(2026, 11, 9), Ledger.Occurrence(b, 2)); });
        Test("overdue bills catch up once and affect their actual months", delegate {
            BudgetData d = Data(); var b = Bill(d, "2026-09-01", "Months", 1); d.Bills.Add(b);
            Equal(3, Ledger.PostDue(d, Today)); Equal(3, d.Entries.Count); Equal(0, Ledger.PostDue(d, Today)); Equal(3, d.Entries.Count);
            Equal(700m, Ledger.Balance(d, "a", Today)); Equal(100m, Ledger.MonthTotal(d, new DateTime(2026, 10, 1), "Expense")); Equal(new DateTime(2026, 12, 1), Ledger.DueDate(b)); Ledger.Validate(d);
        });
        Test("no duplicates after saving and reopening", delegate { BudgetData d = Data(); d.Bills.Add(Bill(d, "2026-11-01", "Months", 1)); Ledger.PostDue(d, Today); BudgetData reopened = BudgetStore.Parse(BudgetStore.Serialize(d)); Equal(0, Ledger.PostDue(reopened, Today)); Equal(1, reopened.Entries.Count); });
        Test("deleting a scheduled expense does not replay it", delegate {
            BudgetData d = Data(); var b = Bill(d, "2026-11-01", "Months", 1); d.Bills.Add(b); Ledger.PostDue(d, Today); d.Entries.Clear(); b.NextIndex = 0;
            Equal(0, Ledger.PostDue(d, Today)); Equal(0, d.Entries.Count); Equal(1, b.NextIndex);
        });
        Test("paused and manual schedules never auto-deduct", delegate { BudgetData d = Data(); var b = Bill(d, "2026-10-01", "Months", 1); b.Enabled = false; d.Bills.Add(b); Equal(0, Ledger.PostDue(d, Today)); b.Enabled = true; b.AutoRecord = false; Equal(0, Ledger.PostDue(d, Today)); Ledger.RecordNext(d, b, Today); Equal(1, d.Entries.Count); });
        Test("recording an early payment books today and advances schedule", delegate { BudgetData d = Data(); var b = Bill(d, "2026-11-20", "Months", 1); d.Bills.Add(b); Ledger.RecordNext(d, b, Today); Equal("2026-11-03", d.Entries[0].Date); Equal("2026-11-20", d.Entries[0].ScheduledDate); Equal(new DateTime(2026, 12, 20), Ledger.DueDate(b)); });
        Test("forecast does not deduct or double count recorded occurrences", delegate { BudgetData d = Data(); var b = Bill(d, "2026-11-01", "Months", 1); d.Bills.Add(b); Equal(100m, Ledger.PlannedTotal(d, new DateTime(2026, 11, 1), new DateTime(2026, 11, 30))); Equal(1000m, Ledger.Balance(d, "a", Today)); Ledger.PostDue(d, Today); Equal(0m, Ledger.PlannedTotal(d, new DateTime(2026, 11, 1), new DateTime(2026, 11, 30))); });
        Test("foreign expense totals retain the recorded rate", delegate { BudgetData d = Data(); d.Rates["EUR"] = 25m; d.Accounts[0].Currency = "EUR"; Entry e = Entry(d, "Expense", 10, "2026-11-02"); e.RateToCzk = 25; d.Entries.Add(e); d.Rates["EUR"] = 26m; Equal(250m, Ledger.MonthTotal(d, Today, "Expense")); Equal(26240m, Ledger.TotalBalance(d, Today)); d.BaseCurrency = "EUR"; Equal(250m / 26m, Ledger.MonthTotal(d, Today, "Expense")); });
        Test("unknown currency and missing rates are rejected", delegate { BudgetData d = Data(); d.Accounts[0].Currency = "EUR"; Reject(() => Ledger.Validate(d)); d.Accounts[0].Currency = "INVALID"; Reject(() => Ledger.Validate(d)); });
        Test("zero amounts and excessive decimal precision are rejected", delegate { BudgetData d = Data(); var e = Entry(d, "Expense", 0, "2026-11-02"); d.Entries.Add(e); Reject(() => Ledger.Validate(d)); e.Amount = 1.001m; Reject(() => Ledger.Validate(d)); });
        Test("negative balances are allowed and computed accurately", delegate { BudgetData d = Data(); d.Entries.Add(Entry(d, "Expense", 1200, "2026-11-02")); Ledger.Validate(d); Equal(-200m, Ledger.Balance(d, "a", Today)); });
        Test("referenced accounts and categories cannot be deleted", delegate { BudgetData d = Data(); d.Entries.Add(Entry(d, "Expense", 10, "2026-11-02")); Reject(() => Ledger.DeleteAccount(d, "a")); Reject(() => Ledger.DeleteCategory(d, d.Categories[0].Id)); Ledger.DeleteAccount(d, "b"); Equal(1, d.Accounts.Count); });
        Test("20-account, 20-category and 20-bill limits are enforced", delegate {
            BudgetData d = Data(); while (d.Accounts.Count < 20) Ledger.AddAccount(d, new Account { Id = Ledger.NewId(), Name = "Account " + d.Accounts.Count, Icon = "Bank", Currency = "CZK", OpeningDate = "2026-01-01" });
            Reject(() => Ledger.AddAccount(d, new Account()));
            while (d.Categories.Count < 20) Ledger.AddCategory(d, new Category { Id = Ledger.NewId(), Name = "Category " + d.Categories.Count, Icon = "Other" }); Reject(() => Ledger.AddCategory(d, new Category()));
            while (d.Bills.Count < 20) { var b = Bill(d, "2026-12-01", "Months", 1); b.Id = Ledger.NewId(); Ledger.AddBill(d, b); } Reject(() => Ledger.AddBill(d, new RecurringBill())); Ledger.Validate(d);
        });
        Test("malformed backups and future schema versions are rejected", delegate { Reject(() => BudgetStore.Parse("not-json")); var d = Data(); d.SchemaVersion = Ledger.CurrentSchemaVersion + 1; Reject(() => BudgetStore.Parse(BudgetStore.Serialize(d))); Reject(() => BudgetStore.Parse("{}")); });
        Test("duplicate identifiers and orphan references are rejected", delegate { BudgetData d = Data(); d.Accounts[1].Id = "a"; Reject(() => Ledger.Validate(d)); d = Data(); var e = Entry(d, "Expense", 10, "2026-11-02"); e.AccountId = "missing"; d.Entries.Add(e); Reject(() => Ledger.Validate(d)); });
        Test("saving is persistent, atomic, and retains previous versions", delegate {
            string folder = Path.Combine(Path.GetTempPath(), "budgetdesk-test-" + Guid.NewGuid().ToString("N"));
            try { BudgetStore store = new BudgetStore(folder); BudgetData d = Data(); store.Save(d); d.Entries.Add(Entry(d, "Expense", 25.50m, "2026-11-02")); store.Save(d);
                Equal(974.50m, Ledger.Balance(store.Load(), "a", Today)); string backup = Directory.GetFiles(Path.Combine(folder, "Backups"), "*.json").Single(); Equal(1000m, Ledger.Balance(BudgetStore.ReadBackup(backup), "a", Today));
                Equal(0, Directory.GetFiles(folder, "*.tmp").Length);
            } finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        });
        Test("invalid saves leave the previous financial data untouched", delegate {
            string folder = Path.Combine(Path.GetTempPath(), "budgetdesk-test-" + Guid.NewGuid().ToString("N"));
            try { var store = new BudgetStore(folder); var d = Data(); store.Save(d); string original = File.ReadAllText(store.FilePath); d.Entries.Add(Entry(d, "Expense", -1, "2026-11-02")); Reject(() => store.Save(d)); Equal(original, File.ReadAllText(store.FilePath)); } finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        });
        Test("CSV exports preserve text, decimals and neutralize formulas", delegate { BudgetData d = Data(); var e = Entry(d, "Expense", 12.34m, "2026-11-02"); e.Description = "=HYPERLINK(\"x\")"; d.Entries.Add(e); string csv = BudgetStore.Csv(d, Today); Equal(true, csv.Contains("\"'=HYPERLINK(")); Equal(true, csv.Contains("12.34")); });
        Test("sample data is valid and separate from an empty budget", delegate { var d = Ledger.Demo(Today); Ledger.Validate(d); Equal(3, d.Accounts.Count); Equal(0, BudgetData.Empty().Accounts.Count); });
        Console.WriteLine("\n" + passed + " passed; " + failed + " failed."); Environment.ExitCode = failed == 0 ? 0 : 1;
    }
}
