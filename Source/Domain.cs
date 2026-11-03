using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BudgetDesk
{
    public sealed class Account
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Currency { get; set; }
        public decimal OpeningBalance { get; set; }
        public string OpeningDate { get; set; }
    }

    public sealed class Category
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public decimal MonthlyBudgetCzk { get; set; }
    }

    public sealed class Entry
    {
        public string Id { get; set; }
        public string Kind { get; set; }
        public string Date { get; set; }
        public string AccountId { get; set; }
        public string ToAccountId { get; set; }
        public string CategoryId { get; set; }
        public string Description { get; set; }
        public string Payee { get; set; }
        public decimal Amount { get; set; }
        public decimal ReceivedAmount { get; set; }
        public decimal RateToCzk { get; set; }
        public string BillId { get; set; }
        public string ScheduledDate { get; set; }
        public string PlanId { get; set; }
        public List<ExpenseSplit> Splits { get; set; }
    }

    public sealed class ExpenseSplit
    {
        public string CategoryId { get; set; }
        public decimal Amount { get; set; }
    }

    public sealed class PayeeProfile
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DefaultAccountId { get; set; }
        public string DefaultCategoryId { get; set; }
    }

    public sealed class QuickEntryTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string Payee { get; set; }
        public string Description { get; set; }
        public string AccountId { get; set; }
        public string CategoryId { get; set; }
        public decimal? Amount { get; set; }
        public List<ExpenseSplit> Splits { get; set; }
    }

    public sealed class EntrySearch
    {
        public string Text { get; set; }
        public string AccountId { get; set; }
        public string CategoryId { get; set; }
        public string Kind { get; set; }
        public DateTime? From { get; set; }
        public DateTime? Through { get; set; }
        public decimal? MinimumAmount { get; set; }
        public decimal? MaximumAmount { get; set; }
    }

    public sealed class SavingsGoal
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Currency { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal SavedAmount { get; set; }
        public string TargetDate { get; set; }
    }

    public sealed class PlannedExpense
    {
        public string Id { get; set; }
        public string DueDate { get; set; }
        public string AccountId { get; set; }
        public string CategoryId { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
    }

    public sealed class RecurringBill
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string AccountId { get; set; }
        public string CategoryId { get; set; }
        public decimal Amount { get; set; }
        public string AnchorDate { get; set; }
        public int Interval { get; set; }
        public string Unit { get; set; }
        public int NextIndex { get; set; }
        public bool Enabled { get; set; }
        public bool AutoRecord { get; set; }
    }

    public sealed class BudgetData
    {
        public int SchemaVersion { get; set; }
        public string BaseCurrency { get; set; }
        // Rates use one stable reference. One unit of currency is worth this many CZK.
        public Dictionary<string, decimal> Rates { get; set; }
        public List<Account> Accounts { get; set; }
        public List<Category> Categories { get; set; }
        public List<Entry> Entries { get; set; }
        public List<RecurringBill> Bills { get; set; }
        public List<PlannedExpense> PlannedExpenses { get; set; }
        public List<SavingsGoal> Goals { get; set; }
        public List<PayeeProfile> Payees { get; set; }
        public List<QuickEntryTemplate> Templates { get; set; }
        // A tombstone survives deletion of a recorded payment, preventing duplicate replay.
        public List<string> ProcessedOccurrences { get; set; }

        public static BudgetData Empty()
        {
            BudgetData d = new BudgetData {
                SchemaVersion = Ledger.CurrentSchemaVersion, BaseCurrency = "CZK",
                Rates = new Dictionary<string, decimal> { { "CZK", 1m } },
                Accounts = new List<Account>(), Categories = new List<Category>(),
                Entries = new List<Entry>(), Bills = new List<RecurringBill>(),
                PlannedExpenses = new List<PlannedExpense>(),
                Goals = new List<SavingsGoal>(),
                Payees = new List<PayeeProfile>(), Templates = new List<QuickEntryTemplate>(),
                ProcessedOccurrences = new List<string>()
            };
            string[] names = { "Groceries", "Eating out", "Transport", "Home", "Health", "Shopping", "Bills", "Entertainment" };
            string[] icons = { "Basket", "Coffee", "Car", "Home", "Health", "Bag", "Bolt", "Music" };
            for (int i = 0; i < names.Length; i++)
                d.Categories.Add(new Category { Id = Ledger.NewId(), Name = names[i], Icon = icons[i] });
            return d;
        }
    }

    public sealed class BudgetException : Exception
    {
        public BudgetException(string message) : base(message) { }
    }

    public static class Ledger
    {
        public static readonly string[] Currencies = { "CZK", "EUR", "USD", "GBP", "CHF", "PLN" };
        public static readonly string[] Units = { "Months", "Weeks", "Years", "Days" };
        public const int ItemLimit = 20;
        public const int CurrentSchemaVersion = 4;
        public static string NewId() { return Guid.NewGuid().ToString("N"); }
        public static string Iso(DateTime date) { return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); }
        public static DateTime Day(string value)
        {
            DateTime d;
            if (!DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d)
                || d.Year < 1900 || d.Year > 2199)
                throw new BudgetException("A date must be between 1900 and 2199 (yyyy-MM-dd).");
            return d;
        }
        public static void Require(bool condition, string message)
        {
            if (!condition) throw new BudgetException(message);
        }
        static void TextValue(string s, string field, int max)
        {
            Require(!String.IsNullOrWhiteSpace(s) && s.Trim().Length <= max, field + " is required (maximum " + max + " characters).");
        }
        static void Money(decimal n, bool positive, string field)
        {
            Require((!positive || n > 0) && Math.Abs(n) <= 999999999999m && Decimal.Round(n, 2) == n,
                field + " must " + (positive ? "be greater than zero and " : "") + "have at most two decimal places.");
        }
        static void Unique(IEnumerable<string> ids, string label)
        {
            HashSet<string> seen = new HashSet<string>();
            foreach (string id in ids)
                Require(!String.IsNullOrWhiteSpace(id) && id.Length <= 100 && seen.Add(id), "Invalid or duplicate " + label + " ID.");
        }

        public static void Validate(BudgetData d)
        {
            Require(d != null && d.SchemaVersion == CurrentSchemaVersion, "This backup is not a supported Budget Desk version.");
            Require(d.Accounts != null && d.Categories != null && d.Bills != null && d.Entries != null && d.Rates != null && d.ProcessedOccurrences != null && d.PlannedExpenses != null && d.Goals != null && d.Payees != null && d.Templates != null,
                "The data file is incomplete.");
            Require(d.Accounts.Count <= 20 && d.Categories.Count <= 20 && d.Bills.Count <= 20,
                "Version 1 supports up to 20 accounts, 20 categories and 20 recurring payments.");
            Require(d.Accounts.All(a => a != null) && d.Categories.All(c => c != null) && d.Entries.All(e => e != null) && d.Bills.All(b => b != null) && d.PlannedExpenses.All(p => p != null) && d.Goals.All(g => g != null) && d.Payees.All(p => p != null) && d.Templates.All(t => t != null), "Invalid empty item in the data file.");
            Require(Currencies.Contains(d.BaseCurrency) && d.Rates.ContainsKey(d.BaseCurrency), "Add an exchange rate for your reporting currency.");
            Require(d.Rates.ContainsKey("CZK") && d.Rates["CZK"] == 1m, "The CZK reference rate must be 1.");
            foreach (var rate in d.Rates)
                Require(Currencies.Contains(rate.Key) && rate.Value > 0 && rate.Value <= 1000000m, "Exchange rates must be positive and use supported currencies.");
            Unique(d.Accounts.Select(a => a.Id), "account");
            Unique(d.Categories.Select(c => c.Id), "category");
            Unique(d.Entries.Select(e => e.Id), "entry");
            Unique(d.Bills.Select(b => b.Id), "payment");
            Unique(d.PlannedExpenses.Select(p => p.Id), "planned expense");
            Unique(d.Goals.Select(g => g.Id), "goal");
            Unique(d.Payees.Select(p => p.Id), "payee"); Unique(d.Templates.Select(t => t.Id), "template");
            Require(d.Payees.Select(p => (p.Name ?? "").Trim().ToUpperInvariant()).Distinct().Count() == d.Payees.Count, "Use a different name for each payee.");
            Require(d.Templates.Select(t => (t.Name ?? "").Trim().ToUpperInvariant()).Distinct().Count() == d.Templates.Count, "Use a different name for each template.");
            Unique(d.Entries.Where(e => !String.IsNullOrEmpty(e.PlanId)).Select(e => e.PlanId), "recorded plan");
            Require(d.Accounts.Select(a => a.Name == null ? "" : a.Name.Trim().ToUpperInvariant()).Distinct().Count() == d.Accounts.Count,
                "Use a different name for each account.");
            Require(d.Categories.Select(c => c.Name == null ? "" : c.Name.Trim().ToUpperInvariant()).Distinct().Count() == d.Categories.Count,
                "Use a different name for each category.");
            foreach (Account a in d.Accounts)
            {
                TextValue(a.Name, "Account name", 60); TextValue(a.Icon, "Account icon", 30);
                Require(Currencies.Contains(a.Currency) && d.Rates.ContainsKey(a.Currency), "Set a manual exchange rate for " + a.Currency + ".");
                Money(a.OpeningBalance, false, "Opening balance"); Day(a.OpeningDate);
            }
            foreach (Category c in d.Categories)
            {
                TextValue(c.Name, "Category name", 60); TextValue(c.Icon, "Category icon", 30);
                Money(c.MonthlyBudgetCzk, false, "Category budget");
                Require(c.MonthlyBudgetCzk >= 0, "A category budget cannot be negative.");
            }
            foreach (Entry e in d.Entries)
            {
                Require(e.Kind == "Expense" || e.Kind == "Income" || e.Kind == "Transfer", "Unknown entry type.");
                Account a = d.Accounts.FirstOrDefault(x => x.Id == e.AccountId);
                Require(a != null, "An entry refers to a missing account.");
                DateTime date = Day(e.Date);
                Require(date >= Day(a.OpeningDate), "The entry date is before the account's starting date.");
                Money(e.Amount, true, "Amount");
                Require(e.RateToCzk > 0 && e.RateToCzk <= 1000000m, "An entry has an invalid exchange rate.");
                Require(e.Description != null && e.Description.Length <= 300, "Descriptions can have up to 300 characters.");
                Require(e.Payee == null || e.Payee.Length <= 80, "Payee names can have up to 80 characters.");
                if (HasSplits(e))
                {
                    Require(e.Kind == "Expense" && String.IsNullOrEmpty(e.CategoryId), "Split categories are only for expenses; choose a single category or a split.");
                    ValidateExpenseParts(d, e.Splits, e.Amount);
                }
                else if (e.Kind == "Expense") Require(d.Categories.Any(c => c.Id == e.CategoryId), "Choose an expense category.");
                if (e.Kind == "Transfer")
                {
                    Account target = d.Accounts.FirstOrDefault(x => x.Id == e.ToAccountId);
                    Require(target != null && target.Id != a.Id, "Choose a different destination account.");
                    Require(date >= Day(target.OpeningDate), "The transfer is before the destination account's starting date.");
                    Money(e.ReceivedAmount, true, "Amount received");
                    Require(target.Currency != a.Currency || e.Amount == e.ReceivedAmount,
                        "Transfers in the same currency must send and receive the same amount.");
                }
                if (!String.IsNullOrEmpty(e.ScheduledDate)) Day(e.ScheduledDate);
                if (!String.IsNullOrEmpty(e.PlanId)) Require(e.Kind == "Expense" && !String.IsNullOrEmpty(e.ScheduledDate), "A recorded one-off plan must be an expense with its original due date.");
            }
            foreach (PlannedExpense p in d.PlannedExpenses)
            {
                TextValue(p.Description, "Planned expense description", 300);
                Account a = d.Accounts.FirstOrDefault(x => x.Id == p.AccountId);
                Require(a != null && d.Categories.Any(c => c.Id == p.CategoryId), "Choose an account and category for each planned expense.");
                Require(Day(p.DueDate) >= Day(a.OpeningDate), "The planned date is before the account's starting date.");
                Money(p.Amount, true, "Planned amount");
                Require(!d.Entries.Any(e => e.PlanId == p.Id), "This planned expense has already been recorded.");
            }
            foreach (RecurringBill b in d.Bills)
            {
                TextValue(b.Name, "Payment name", 80); TextValue(b.Icon, "Payment icon", 30);
                Account a = d.Accounts.FirstOrDefault(x => x.Id == b.AccountId);
                Require(a != null && d.Categories.Any(c => c.Id == b.CategoryId), "Choose an account and category for each recurring payment.");
                Money(b.Amount, true, "Payment amount");
                Require(Units.Contains(b.Unit) && b.Interval >= 1 && b.Interval <= 120 && b.NextIndex >= 0 && b.NextIndex <= 100000,
                    "Use a repeat interval from 1 to 120 and a valid next payment.");
                Require(DueDate(b) >= Day(a.OpeningDate), "The next payment is before the account's starting date.");
            }
            foreach (SavingsGoal g in d.Goals)
            {
                TextValue(g.Name, "Goal name", 80); TextValue(g.Icon, "Goal icon", 30);
                Require(Currencies.Contains(g.Currency) && d.Rates.ContainsKey(g.Currency), "Set a manual exchange rate for the goal currency " + g.Currency + ".");
                Money(g.TargetAmount, true, "Goal target"); Money(g.SavedAmount, false, "Saved amount");
                Require(g.SavedAmount >= 0, "A goal's saved amount cannot be negative.");
                if (!String.IsNullOrEmpty(g.TargetDate)) Day(g.TargetDate);
            }
            foreach (PayeeProfile p in d.Payees)
            {
                TextValue(p.Name, "Payee name", 80);
                Require(String.IsNullOrEmpty(p.DefaultAccountId) || d.Accounts.Any(a => a.Id == p.DefaultAccountId), "Choose an existing default account for the payee.");
                Require(String.IsNullOrEmpty(p.DefaultCategoryId) || d.Categories.Any(c => c.Id == p.DefaultCategoryId), "Choose an existing default category for the payee.");
            }
            foreach (QuickEntryTemplate t in d.Templates)
            {
                TextValue(t.Name, "Template name", 80);
                Require(t.Kind == "Expense" || t.Kind == "Income", "Templates support expenses or income.");
                Require(t.Description != null && t.Description.Length <= 300 && (t.Payee == null || t.Payee.Length <= 80), "Check the template description and payee length.");
                Require(d.Accounts.Any(a => a.Id == t.AccountId), "Choose an account for the template.");
                if (t.Amount.HasValue) Money(t.Amount.Value, true, "Template amount");
                bool split = t.Splits != null && t.Splits.Count > 0;
                if (split) { Require(t.Kind == "Expense" && String.IsNullOrEmpty(t.CategoryId) && t.Amount.HasValue, "A split template needs a fixed expense total."); ValidateExpenseParts(d, t.Splits, t.Amount.Value); }
                else if (t.Kind == "Expense") Require(d.Categories.Any(c => c.Id == t.CategoryId), "Choose a category for the template.");
            }
            Require(d.ProcessedOccurrences.All(x => !String.IsNullOrWhiteSpace(x) && x.Length <= 160), "Invalid payment history.");
            Require(d.ProcessedOccurrences.Distinct().Count() == d.ProcessedOccurrences.Count, "Duplicate recurring payment history.");
        }

        public static decimal Rate(BudgetData d, string currency)
        {
            Require(d.Rates.ContainsKey(currency), "An exchange rate is missing for " + currency + ".");
            return d.Rates[currency];
        }
        public static decimal ToBase(BudgetData d, decimal amount, string currency)
        { return amount * Rate(d, currency) / Rate(d, d.BaseCurrency); }
        public static decimal EntryInBase(BudgetData d, Entry e)
        { return e.Amount * e.RateToCzk / Rate(d, d.BaseCurrency); }
        public static bool HasSplits(Entry e) { return e.Splits != null && e.Splits.Count > 0; }
        public static void ValidateExpenseParts(BudgetData d, List<ExpenseSplit> parts, decimal total)
        {
            Require(parts != null && parts.Count >= 2 && parts.Count <= ItemLimit, "A split needs 2 to 20 categories. Use one category for an ordinary expense.");
            Require(parts.All(p => p != null && d.Categories.Any(c => c.Id == p.CategoryId)), "Choose an existing category for every split amount.");
            Require(parts.Select(p => p.CategoryId).Distinct().Count() == parts.Count, "Use each split category once.");
            foreach (ExpenseSplit p in parts) Money(p.Amount, true, "Split amount");
            Require(parts.Sum(p => p.Amount) == total, "Split amounts must add up exactly to the purchase total.");
        }
        public static bool HasCategory(Entry e, string categoryId)
        { return e.Kind == "Expense" && (HasSplits(e) ? e.Splits.Any(p => p.CategoryId == categoryId) : e.CategoryId == categoryId); }
        public static decimal CategoryExpenseAmount(Entry e, string categoryId)
        { return e.Kind != "Expense" ? 0 : HasSplits(e) ? e.Splits.Where(p => p.CategoryId == categoryId).Sum(p => p.Amount) : e.CategoryId == categoryId ? e.Amount : 0; }
        public static decimal CategoryExpenseInBase(BudgetData d, Entry e, string categoryId)
        { return CategoryExpenseAmount(e, categoryId) * e.RateToCzk / Rate(d, d.BaseCurrency); }
        public static decimal GoalRemaining(SavingsGoal goal) { return Math.Max(0, goal.TargetAmount - goal.SavedAmount); }
        public static int GoalPercent(SavingsGoal goal) { return (int)Math.Min(100, Decimal.Floor(goal.SavedAmount / goal.TargetAmount * 100)); }
        public static decimal GoalMonthlyAmount(SavingsGoal goal, DateTime today)
        {
            if (String.IsNullOrEmpty(goal.TargetDate)) return 0;
            DateTime due = Day(goal.TargetDate);
            int months = Math.Max(1, (due.Year - today.Year) * 12 + due.Month - today.Month + 1);
            return Decimal.Ceiling(GoalRemaining(goal) / months * 100) / 100;
        }
        public static void AddGoalSavings(BudgetData d, string id, decimal amount)
        {
            SavingsGoal goal = d.Goals.FirstOrDefault(g => g.Id == id);
            Require(goal != null, "Choose an existing savings goal."); Money(amount, true, "Amount to add");
            decimal saved = goal.SavedAmount + amount; Money(saved, false, "Saved amount"); goal.SavedAmount = saved;
        }
        public static PayeeProfile FindPayee(BudgetData d, string name)
        { return d.Payees.FirstOrDefault(p => String.Equals(p.Name.Trim(), (name ?? "").Trim(), StringComparison.OrdinalIgnoreCase)); }
        public static void RememberPayee(BudgetData d, string name, string accountId, string categoryId)
        {
            name = (name ?? "").Trim(); if (name.Length == 0) return;
            PayeeProfile p = FindPayee(d, name);
            if (p == null) { p = new PayeeProfile { Id = NewId(), Name = name }; d.Payees.Add(p); }
            p.DefaultAccountId = accountId; p.DefaultCategoryId = categoryId;
        }
        public static Entry TemplateDraft(BudgetData d, string templateId, DateTime date)
        {
            QuickEntryTemplate t = d.Templates.FirstOrDefault(x => x.Id == templateId); Require(t != null, "Choose an existing template.");
            return new Entry { Id = NewId(), Kind = t.Kind, Date = Iso(date), Payee = t.Payee, Description = t.Description, AccountId = t.AccountId, CategoryId = t.CategoryId,
                Amount = t.Amount ?? 0, RateToCzk = Rate(d, d.Accounts.First(a => a.Id == t.AccountId).Currency),
                Splits = t.Splits == null ? null : t.Splits.Select(p => new ExpenseSplit { CategoryId = p.CategoryId, Amount = p.Amount }).ToList() };
        }
        public static QuickEntryTemplate TemplateFromEntry(Entry e)
        {
            Require(e.Kind == "Expense" || e.Kind == "Income", "Choose an expense or income for a quick-entry template.");
            string name = !String.IsNullOrWhiteSpace(e.Description) ? e.Description : !String.IsNullOrWhiteSpace(e.Payee) ? e.Payee : "New template";
            return new QuickEntryTemplate { Id = NewId(), Name = name.Length > 80 ? name.Substring(0, 80) : name, Kind = e.Kind, Payee = e.Payee, Description = e.Description ?? "", AccountId = e.AccountId, CategoryId = e.CategoryId,
                Amount = e.Amount > 0 ? (decimal?)e.Amount : null, Splits = e.Splits == null ? null : e.Splits.Select(p => new ExpenseSplit { CategoryId = p.CategoryId, Amount = p.Amount }).ToList() };
        }
        public static IEnumerable<Entry> SearchEntries(BudgetData d, EntrySearch query)
        {
            Require(query != null, "Choose search filters.");
            Require(!query.From.HasValue || !query.Through.HasValue || query.From.Value.Date <= query.Through.Value.Date, "The start date must be on or before the end date.");
            Require(!query.MinimumAmount.HasValue || !query.MaximumAmount.HasValue || query.MinimumAmount.Value <= query.MaximumAmount.Value, "The minimum amount must not exceed the maximum.");
            if (query.MinimumAmount.HasValue) { Money(query.MinimumAmount.Value, false, "Minimum amount"); Require(query.MinimumAmount.Value >= 0, "Search amounts cannot be negative."); }
            if (query.MaximumAmount.HasValue) { Money(query.MaximumAmount.Value, false, "Maximum amount"); Require(query.MaximumAmount.Value >= 0, "Search amounts cannot be negative."); }
            IEnumerable<Entry> result = d.Entries;
            if (query.From.HasValue) { string from = Iso(Day(Iso(query.From.Value))); result = result.Where(e => String.CompareOrdinal(e.Date, from) >= 0); }
            if (query.Through.HasValue) { string through = Iso(Day(Iso(query.Through.Value))); result = result.Where(e => String.CompareOrdinal(e.Date, through) <= 0); }
            if (!String.IsNullOrEmpty(query.AccountId)) result = result.Where(e => e.AccountId == query.AccountId || e.ToAccountId == query.AccountId);
            if (!String.IsNullOrEmpty(query.CategoryId)) result = result.Where(e => HasCategory(e, query.CategoryId));
            if (!String.IsNullOrEmpty(query.Kind) && query.Kind != "All types") result = result.Where(e => e.Kind == query.Kind);
            if (query.MinimumAmount.HasValue) result = result.Where(e => e.Amount >= query.MinimumAmount.Value);
            if (query.MaximumAmount.HasValue) result = result.Where(e => e.Amount <= query.MaximumAmount.Value);
            string[] words = (query.Text ?? "").Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 0) result = result.Where(e => {
                string text = (e.Description ?? "") + " " + (e.Payee ?? "") + " " + String.Join(" ", d.Accounts.Where(a => a.Id == e.AccountId || a.Id == e.ToAccountId).Select(a => a.Name)) + " " + String.Join(" ", d.Categories.Where(c => HasCategory(e, c.Id)).Select(c => c.Name));
                return words.All(word => text.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0);
            });
            return result;
        }

        public static decimal Balance(BudgetData d, string accountId, DateTime asOf)
        {
            Account a = d.Accounts.First(x => x.Id == accountId);
            if (asOf.Date < Day(a.OpeningDate)) return 0m;
            decimal total = a.OpeningBalance;
            foreach (Entry e in d.Entries.Where(x => Day(x.Date) <= asOf.Date))
            {
                if (e.AccountId == a.Id) total += e.Kind == "Income" ? e.Amount : -e.Amount;
                if (e.Kind == "Transfer" && e.ToAccountId == a.Id) total += e.ReceivedAmount;
            }
            return total;
        }
        public static decimal TotalBalance(BudgetData d, DateTime date)
        { return d.Accounts.Sum(a => ToBase(d, Balance(d, a.Id, date), a.Currency)); }

        public static IEnumerable<Entry> MonthEntries(BudgetData d, DateTime month)
        { return d.Entries.Where(e => Day(e.Date).Year == month.Year && Day(e.Date).Month == month.Month); }
        public static decimal MonthTotal(BudgetData d, DateTime month, string kind)
        { return MonthEntries(d, month).Where(e => e.Kind == kind).Sum(e => EntryInBase(d, e)); }
        public static IEnumerable<Entry> YearEntries(BudgetData d, int year)
        { return d.Entries.Where(e => Day(e.Date).Year == year); }
        public static decimal YearTotal(BudgetData d, int year, string kind)
        { return YearEntries(d, year).Where(e => e.Kind == kind).Sum(e => EntryInBase(d, e)); }
        public static decimal PlanInBase(BudgetData d, PlannedExpense p)
        { return ToBase(d, p.Amount, d.Accounts.First(a => a.Id == p.AccountId).Currency); }
        public static decimal OneOffTotal(BudgetData d, DateTime from, DateTime through)
        { return d.PlannedExpenses.Where(p => Day(p.DueDate) >= from.Date && Day(p.DueDate) <= through.Date).Sum(p => PlanInBase(d, p)); }
        public static void RecordPlanned(BudgetData d, string id, DateTime paidOn, DateTime today)
        {
            PlannedExpense p = d.PlannedExpenses.FirstOrDefault(x => x.Id == id);
            Require(p != null && !d.Entries.Any(e => e.PlanId == id), "This planned expense has already been recorded or removed.");
            Account a = d.Accounts.First(x => x.Id == p.AccountId);
            Require(paidOn.Date <= today.Date && paidOn.Date >= Day(a.OpeningDate), "Choose a payment date between the account's starting date and today.");
            Day(Iso(paidOn));
            d.Entries.Add(new Entry { Id = NewId(), Kind = "Expense", Date = Iso(paidOn), AccountId = p.AccountId,
                CategoryId = p.CategoryId, Description = p.Description, Amount = p.Amount, RateToCzk = Rate(d, a.Currency),
                PlanId = p.Id, ScheduledDate = p.DueDate });
            d.PlannedExpenses.Remove(p);
        }

        public static DateTime Occurrence(RecurringBill b, int index)
        {
            try
            {
                DateTime anchor = Day(b.AnchorDate);
                int count = checked(index * b.Interval);
                DateTime result;
                switch (b.Unit)
                {
                    case "Months": result = anchor.AddMonths(count); break;
                    case "Years": result = anchor.AddYears(count); break;
                    case "Weeks": result = anchor.AddDays(checked(count * 7)); break;
                    case "Days": result = anchor.AddDays(count); break;
                    default: throw new BudgetException("Unknown payment interval.");
                }
                Require(result.Year <= 2199, "The payment schedule is beyond the supported date range.");
                return result;
            }
            catch (ArgumentOutOfRangeException) { throw new BudgetException("The payment schedule is beyond the supported date range."); }
            catch (OverflowException) { throw new BudgetException("The payment schedule is too large."); }
        }
        public static DateTime DueDate(RecurringBill b) { return Occurrence(b, b.NextIndex); }
        public static string OccurrenceKey(RecurringBill b) { return b.Id + ":" + Iso(DueDate(b)); }

        public static bool RecordNext(BudgetData d, RecurringBill b, DateTime today)
        {
            DateTime due = DueDate(b);
            string key = OccurrenceKey(b);
            bool fresh = !d.ProcessedOccurrences.Contains(key);
            if (fresh)
            {
                Account a = d.Accounts.First(x => x.Id == b.AccountId);
                DateTime booked = due > today.Date ? today.Date : due;
                Require(booked >= Day(a.OpeningDate), "The payment is before the account's starting date.");
                d.Entries.Add(new Entry {
                    Id = NewId(), Kind = "Expense", Date = Iso(booked), AccountId = b.AccountId,
                    CategoryId = b.CategoryId, Description = b.Name, Amount = b.Amount,
                    RateToCzk = Rate(d, a.Currency), BillId = b.Id, ScheduledDate = Iso(due)
                });
                d.ProcessedOccurrences.Add(key);
            }
            b.NextIndex++;
            return fresh;
        }
        public static int PostDue(BudgetData d, DateTime today)
        {
            int count = 0;
            foreach (RecurringBill b in d.Bills.Where(x => x.Enabled && x.AutoRecord))
            {
                int guard = 0;
                while (DueDate(b) <= today.Date)
                {
                    Require(++guard <= 10000, "This schedule has more than 10,000 overdue payments. Edit its next due date first.");
                    if (RecordNext(d, b, today)) count++;
                }
            }
            return count;
        }
        public static IEnumerable<DateTime> PlannedDates(RecurringBill b, DateTime from, DateTime through)
        {
            if (!b.Enabled || through.Date < from.Date || Day(b.AnchorDate) > through.Date) yield break;
            // Jump near the requested period instead of truncating distant years
            // after 10,000 skipped daily occurrences.
            int first = Math.Max(b.NextIndex, Math.Max(0, ScheduleIndex(b, from) - 1));
            int last = ScheduleIndex(b, through);
            for (int i = first; i <= last; i++)
            {
                DateTime due = Occurrence(b, i);
                if (due > through.Date) yield break;
                if (due >= from.Date) yield return due;
            }
        }
        private static int ScheduleIndex(RecurringBill b, DateTime date)
        {
            DateTime anchor = Day(b.AnchorDate);
            if (b.Unit == "Months") return ((date.Year - anchor.Year) * 12 + date.Month - anchor.Month) / b.Interval;
            if (b.Unit == "Years") return (date.Year - anchor.Year) / b.Interval;
            return (int)(date.Date - anchor).TotalDays / (b.Interval * (b.Unit == "Weeks" ? 7 : 1));
        }
        public static decimal PlannedTotal(BudgetData d, DateTime from, DateTime through)
        {
            return d.Bills.Sum(b => PlannedDates(b, from, through).Count() * ToBase(d, b.Amount,
                d.Accounts.First(a => a.Id == b.AccountId).Currency));
        }

        public static void AddAccount(BudgetData d, Account a)
        { Require(d.Accounts.Count < ItemLimit, "You already have 20 accounts."); d.Accounts.Add(a); }
        public static void MoveAccount(BudgetData d, string id, int position)
        {
            int current = d.Accounts.FindIndex(a => a.Id == id);
            Require(current >= 0, "Choose an existing account to move.");
            Require(position >= 0 && position < d.Accounts.Count, "Choose a display position between 1 and " + d.Accounts.Count + ".");
            if (current == position) return;
            Account account = d.Accounts[current]; d.Accounts.RemoveAt(current); d.Accounts.Insert(position, account);
        }
        public static void AddCategory(BudgetData d, Category c)
        { Require(d.Categories.Count < ItemLimit, "You already have 20 categories."); d.Categories.Add(c); }
        public static void AddBill(BudgetData d, RecurringBill b)
        { Require(d.Bills.Count < ItemLimit, "You already have 20 recurring payments."); d.Bills.Add(b); }
        public static void DeleteAccount(BudgetData d, string id)
        {
            Require(!d.Entries.Any(e => e.AccountId == id || e.ToAccountId == id) && !d.Bills.Any(b => b.AccountId == id) && !d.PlannedExpenses.Any(p => p.AccountId == id) && !d.Templates.Any(t => t.AccountId == id),
                "This account has entries, payments, plans or templates. Reassign or remove those first.");
            foreach (PayeeProfile p in d.Payees.Where(p => p.DefaultAccountId == id)) p.DefaultAccountId = null;
            d.Accounts.RemoveAll(a => a.Id == id);
        }
        public static void DeleteCategory(BudgetData d, string id)
        {
            Require(!d.Entries.Any(e => HasCategory(e, id)) && !d.Bills.Any(b => b.CategoryId == id) && !d.PlannedExpenses.Any(p => p.CategoryId == id) && !d.Templates.Any(t => t.CategoryId == id || t.Splits != null && t.Splits.Any(p => p.CategoryId == id)),
                "This category is used by entries, payments, plans or templates. Reassign those first.");
            foreach (PayeeProfile p in d.Payees.Where(p => p.DefaultCategoryId == id)) p.DefaultCategoryId = null;
            d.Categories.RemoveAll(c => c.Id == id);
        }

        public static BudgetData Demo(DateTime today)
        {
            BudgetData d = BudgetData.Empty(); d.Rates["EUR"] = 25m;
            DateTime start = new DateTime(today.Year, today.Month, 1);
            Account main = new Account { Id = NewId(), Name = "Everyday account", Icon = "Bank", Currency = "CZK", OpeningBalance = 18450m, OpeningDate = Iso(start) };
            Account savings = new Account { Id = NewId(), Name = "Rainy day savings", Icon = "Shield", Currency = "CZK", OpeningBalance = 62000m, OpeningDate = Iso(start) };
            Account euro = new Account { Id = NewId(), Name = "Euro wallet", Icon = "Wallet", Currency = "EUR", OpeningBalance = 420m, OpeningDate = Iso(start) };
            d.Accounts.AddRange(new[] { main, savings, euro });
            d.Categories[0].MonthlyBudgetCzk = 6000m;
            d.Categories[1].MonthlyBudgetCzk = 2500m;
            d.Categories[2].MonthlyBudgetCzk = 3000m;
            d.Entries.Add(new Entry { Id = NewId(), Kind = "Income", Date = Iso(start), AccountId = main.Id, Description = "Salary", Amount = 36000m, RateToCzk = 1m });
            string[] desc = { "Weekly groceries", "Lunch with friends", "Fuel", "Pharmacy", "Coffee on the way" };
            decimal[] amounts = { 1284.50m, 385m, 1450m, 268m, 79m };
            int[] cats = { 0, 1, 2, 4, 1 };
            for (int i = 0; i < desc.Length; i++)
                d.Entries.Add(new Entry { Id = NewId(), Kind = "Expense", Date = Iso(start.AddDays(Math.Min(i, today.Day - 1))), AccountId = main.Id,
                    CategoryId = d.Categories[cats[i]].Id, Description = desc[i], Amount = amounts[i], RateToCzk = 1m });
            string[] bills = { "Rent & utilities", "Mobile plan", "Streaming", "Car insurance" };
            string[] icons = { "Home", "Phone", "Music", "Car" };
            decimal[] costs = { 12500m, 549m, 219m, 2400m };
            int[] days = { 5, 10, 15, 24 };
            for (int i = 0; i < bills.Length; i++)
                d.Bills.Add(new RecurringBill { Id = NewId(), Name = bills[i], Icon = icons[i], Amount = costs[i], AccountId = main.Id,
                    CategoryId = d.Categories[6].Id, AnchorDate = Iso(new DateTime(today.Year, today.Month, Math.Min(days[i], DateTime.DaysInMonth(today.Year, today.Month)))),
                    Interval = i == 3 ? 3 : 1, Unit = "Months", Enabled = true, AutoRecord = false });
            d.PlannedExpenses.Add(new PlannedExpense { Id = NewId(), Description = "Car service", DueDate = Iso(today.AddDays(20)), AccountId = main.Id, CategoryId = d.Categories[2].Id, Amount = 2800m });
            d.PlannedExpenses.Add(new PlannedExpense { Id = NewId(), Description = "Birthday gift", DueDate = Iso(today.AddDays(45)), AccountId = main.Id, CategoryId = d.Categories[5].Id, Amount = 1200m });
            d.Goals.Add(new SavingsGoal { Id = NewId(), Name = "Emergency fund", Icon = "Shield", Currency = "CZK", TargetAmount = 100000, SavedAmount = 62000, TargetDate = Iso(today.AddMonths(12)) });
            d.Goals.Add(new SavingsGoal { Id = NewId(), Name = "Summer holiday", Icon = "Travel", Currency = "CZK", TargetAmount = 30000, SavedAmount = 4500, TargetDate = Iso(today.AddMonths(7)) });
            RememberPayee(d, "Local supermarket", main.Id, d.Categories[0].Id);
            d.Entries.First(e => e.Description == "Weekly groceries").Payee = "Local supermarket";
            d.Templates.Add(new QuickEntryTemplate { Id = NewId(), Name = "Weekly groceries", Kind = "Expense", Payee = "Local supermarket", Description = "Weekly groceries", AccountId = main.Id, CategoryId = d.Categories[0].Id });
            d.Templates.Add(new QuickEntryTemplate { Id = NewId(), Name = "Lunch", Kind = "Expense", Description = "Lunch", AccountId = main.Id, CategoryId = d.Categories[1].Id, Amount = 150 });
            Validate(d); return d;
        }
    }
}
