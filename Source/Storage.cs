using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace BudgetDesk
{
    public sealed class BudgetStore
    {
        public string Folder { get; private set; }
        public string FilePath { get { return Path.Combine(Folder, "budget.json"); } }
        public BudgetStore(string folder) { Folder = folder; }
        static JavaScriptSerializer Serializer()
        { return new JavaScriptSerializer { MaxJsonLength = 32 * 1024 * 1024, RecursionLimit = 80 }; }
        public static string Serialize(BudgetData d) { return Serializer().Serialize(d); }
        public static BudgetData Parse(string json)
        {
            BudgetData d;
            try { d = Serializer().Deserialize<BudgetData>(json); }
            catch (Exception ex) { throw new BudgetException("The file could not be read as Budget Desk data. " + ex.Message); }
            // Schema 4 prevents older apps from silently dropping payees or templates.
            // Loading migrates in memory; the first save keeps the old file in Backups.
            if (d != null && d.SchemaVersion == 1)
            {
                if (d.PlannedExpenses == null) d.PlannedExpenses = new List<PlannedExpense>();
                d.SchemaVersion = 2;
            }
            if (d != null && d.SchemaVersion == 2)
            {
                if (d.Goals == null) d.Goals = new List<SavingsGoal>();
                d.SchemaVersion = 3;
            }
            if (d != null && d.SchemaVersion == 3)
            {
                if (d.Payees == null) d.Payees = new List<PayeeProfile>();
                if (d.Templates == null) d.Templates = new List<QuickEntryTemplate>();
                d.SchemaVersion = Ledger.CurrentSchemaVersion;
            }
            Ledger.Validate(d); return d;
        }
        public static BudgetData Clone(BudgetData d) { return Parse(Serialize(d)); }
        public BudgetData Load()
        {
            if (!File.Exists(FilePath)) return BudgetData.Empty();
            return ReadBackup(FilePath);
        }
        public static BudgetData ReadBackup(string path)
        {
            FileInfo info = new FileInfo(path);
            Ledger.Require(info.Exists && info.Length <= 32 * 1024 * 1024, "Select a Budget Desk JSON backup under 32 MB.");
            return Parse(File.ReadAllText(path, Encoding.UTF8));
        }
        public void Save(BudgetData d)
        {
            Ledger.Validate(d);
            Directory.CreateDirectory(Folder);
            string temp = Path.Combine(Folder, "writing-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                byte[] bytes = new UTF8Encoding(false).GetBytes(Serialize(d));
                Ledger.Require(bytes.Length <= 32 * 1024 * 1024, "This budget has reached the 32 MB file limit. Export a backup before archiving old history.");
                using (FileStream fs = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { fs.Write(bytes, 0, bytes.Length); fs.Flush(true); }
                if (File.Exists(FilePath))
                {
                    string backups = Path.Combine(Folder, "Backups"); Directory.CreateDirectory(backups);
                    string backup = Path.Combine(backups, "budget-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6) + ".json");
                    File.Replace(temp, FilePath, backup);
                    // Cleanup failure must not report a successfully committed save as a failed write.
                    try
                    {
                        foreach (FileInfo old in new DirectoryInfo(backups).GetFiles("budget-*.json").OrderByDescending(f => f.Name).Skip(20)) old.Delete();
                    }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
                else File.Move(temp, FilePath);
            }
            finally { if (File.Exists(temp)) { try { File.Delete(temp); } catch (IOException) { } } }
        }
        public static void ExportBackup(BudgetData d, string path)
        {
            Ledger.Validate(d);
            File.WriteAllText(path, Serialize(d), new UTF8Encoding(false));
        }
        static string Cell(string value)
        {
            value = value ?? "";
            string trimmed = value.TrimStart();
            if (trimmed.Length > 0 && "=+-@\t\r".IndexOf(trimmed[0]) >= 0) value = "'" + value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        public static string Csv(BudgetData d, DateTime month)
        { return EntriesCsv(d, Ledger.MonthEntries(d, month)); }
        public static string YearCsv(BudgetData d, int year)
        { return EntriesCsv(d, Ledger.YearEntries(d, year)); }
        public static string SearchCsv(BudgetData d, EntrySearch query)
        { return EntriesCsv(d, Ledger.SearchEntries(d, query)); }
        static string EntriesCsv(BudgetData d, IEnumerable<Entry> entries)
        {
            StringBuilder s = new StringBuilder("Date,Type,Description,Category,From account,Amount,Currency,To account,Received amount,Destination currency,Reporting amount,Reporting currency,Payee\r\n");
            foreach (Entry e in entries.OrderBy(x => x.Date).ThenBy(x => x.Id))
            {
                Account a = d.Accounts.First(x => x.Id == e.AccountId);
                Account target = d.Accounts.FirstOrDefault(x => x.Id == e.ToAccountId);
                Category c = d.Categories.FirstOrDefault(x => x.Id == e.CategoryId);
                string categoryText = Ledger.HasSplits(e) ? "Split: " + String.Join("; ", e.Splits.Select(p => d.Categories.First(x => x.Id == p.CategoryId).Name + "=" + p.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " " + a.Currency)) : c == null ? "" : c.Name;
                string[] cells = { e.Date, e.Kind, e.Description, categoryText, a.Name,
                    e.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), a.Currency,
                    target == null ? "" : target.Name, e.Kind == "Transfer" ? e.ReceivedAmount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) : "",
                    target == null ? "" : target.Currency,
                    e.Kind == "Transfer" ? "" : Ledger.EntryInBase(d, e).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), d.BaseCurrency, e.Payee ?? "" };
                s.AppendLine(String.Join(",", cells.Select(Cell)));
            }
            return s.ToString();
        }
        public static string PlansCsv(BudgetData d)
        {
            StringBuilder s = new StringBuilder("Due date,Description,Category,Account,Amount,Currency,Estimated reporting amount,Reporting currency,Status\r\n");
            foreach (PlannedExpense p in d.PlannedExpenses.OrderBy(x => x.DueDate).ThenBy(x => x.Id))
            {
                Account a = d.Accounts.First(x => x.Id == p.AccountId);
                Category c = d.Categories.First(x => x.Id == p.CategoryId);
                string[] cells = { p.DueDate, p.Description, c.Name, a.Name, p.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), a.Currency,
                    Ledger.PlanInBase(d, p).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), d.BaseCurrency, "Planned - not deducted" };
                s.AppendLine(String.Join(",", cells.Select(Cell)));
            }
            return s.ToString();
        }
    }
}
