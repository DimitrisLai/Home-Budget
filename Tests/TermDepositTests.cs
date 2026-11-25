using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using System.Collections.Generic;
using BudgetDesk;

internal static class TermDepositTests
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++; Console.WriteLine("PASS " + message);
    }

    private static BudgetData Sample()
    {
        var d = BudgetData.Empty();
        d.Rates["EUR"] = 25m;
        d.Accounts.Add(new Account { Id = "bank", Name = "Test Bank", Icon = "Bank",
            Currency = "CZK", OpeningBalance = 10000, OpeningDate = "2020-01-01" });
        return d;
    }

    private static void Reject(BudgetData data, Action<BudgetData> change, string message)
    {
        var d = BudgetStore.Clone(data);
        try { change(d); Ledger.Validate(d); }
        catch (BudgetException) { Check(true, message); return; }
        throw new Exception(message);
    }

    private static void Accounting(string folder)
    {
        var d = Sample();
        var bank = d.Accounts[0];
        DateTime start = new DateTime(2024, 1, 2), maturity = start.AddMonths(3);
        var deposit = new TermDeposit { Id = "term", Name = "Spring deposit", Icon = "Shield",
            OpeningDate = Ledger.Iso(start), MaturityDate = Ledger.Iso(maturity), Currency = "CZK", ColorIndex = 7 };
        var serializer = new JavaScriptSerializer();
        var legacy = serializer.Deserialize<Dictionary<string, object>>(BudgetStore.Serialize(d));
        legacy["SchemaVersion"] = 7; legacy.Remove("TermDeposits");
        string oldJson = serializer.Serialize(legacy);
        var store = new BudgetStore(folder); Directory.CreateDirectory(folder); File.WriteAllText(store.FilePath, oldJson);
        var migrated = store.Load();
        Check(migrated.SchemaVersion == 8 && migrated.TermDeposits.Count == 0 && Ledger.TotalBalance(migrated,start) == 10000 &&
            File.ReadAllText(store.FilePath) == oldJson,"schema 7 opens with unchanged balances and the original file intact");
        store.Save(migrated);
        Check(Directory.GetFiles(Path.Combine(folder,"Backups")).Any(path => File.ReadAllText(path) == oldJson),"first new save preserves the old JSON");
        d.TermDeposits.Add(deposit);
        Ledger.SaveTransfer(d,null,bank,deposit,start,"11:30:00",4000,4000,deposit.Name,7); Ledger.Validate(d);
        Check(Ledger.Balance(d,"bank",start) == 6000 && Ledger.TermDepositTotal(d,start) == 4000 &&
            Ledger.TotalBalance(d,start) == 10000 && d.Entries.Count == 1,"funding creates one transfer and counts the principal once");
        Check(Ledger.Balance(d,"term",start.AddDays(-1)) == 0 && Ledger.Balance(d,"bank",start.AddDays(-1)) == 10000,"balances respect the opening and posting dates");
        Check(Ledger.MonthTotal(d,start,"Income") == 0 && Ledger.MonthTotal(d,start,"Expense") == 0 &&
            Ledger.YearTotal(d,start.Year,"Income") == 0 && Ledger.YearTotal(d,start.Year,"Expense") == 0,"principal is excluded from both reports");
        Check(Ledger.PostDue(d,maturity.AddDays(5)) == 0 && Ledger.Balance(d,"term",maturity) == 4000 &&
            d.Entries.Count == 1,"maturity leaves funds in place until an actual return is recorded");
        Ledger.SaveTransfer(d,null,deposit,bank,maturity,null,1500,1500,"Part return",0); Ledger.Validate(d);
        Check(Ledger.Balance(d,"bank",maturity) == 7500 && Ledger.Balance(d,"term",maturity) == 2500,"partial return credits the selected regular account");
        string id = d.Entries.Last().Id;
        Ledger.SaveTransfer(d,id,deposit,bank,maturity,null,1000,1000,"Corrected",7); Ledger.Validate(d);
        Check(d.Entries.Count == 2 && Ledger.Balance(d,"term",maturity) == 3000 && Ledger.TotalBalance(d,maturity)==10000,"editing a return changes its original posting");
        Reject(d,x => { x.Entries.Last().Date = Ledger.Iso(start); x.Entries.Last().Time = "10:00:00"; },
            "a backdated withdrawal cannot use principal credited later on the same day");
        Reject(d,x => {
            x.TermDeposits.Add(new TermDeposit { Id = "other", Name = "Second term", Icon = "Shield", Currency = "CZK",
                OpeningDate = Ledger.Iso(start), MaturityDate = Ledger.Iso(maturity) });
            x.Entries.Last().ToAccountId = "other";
        },"term-to-term transfers require a regular account as the counterparty");
        Reject(d,x => x.Entries.RemoveAt(0),"deleting the source funding cannot invalidate a subsequent withdrawal");
        Reject(d,x => x.Entries.Last().Amount = x.Entries.Last().ReceivedAmount = 4100,"withdrawals cannot overdraw the deposit");
        Reject(d,x => x.TermDeposits[0].MaturityDate = "2024-01-01","maturity cannot precede opening");
        Reject(d,x => x.TermDeposits[0].Id = "bank","bank and deposit identifiers cannot collide");
        Reject(d,x => x.Entries.Last().Kind = "Income","a deposit cannot be used as a spending or income account");
        Reject(d,x => Ledger.DeleteAccount(x,"bank"),"a bank used in a deposit transfer keeps its history");
        Reject(d,x => Ledger.SaveTransfer(x,null,x.Accounts[0],x.TermDeposits[0],start,null,7000,7000,"",0),"funding rejects more than the bank balance as of the posting date");
        var search = new EntrySearch { AccountId = "term", Text = "Spring", Kind = "Transfer" };
        Check(Ledger.SearchEntries(d,search).Count() == 2 && BudgetStore.SearchCsv(d,search).Contains("Spring deposit") &&
            BudgetStore.Csv(d,start).Contains("Test Bank") && BudgetStore.YearCsv(d,start.Year).Contains("Corrected"),"search and all CSV paths resolve both transfer endpoints");
        deposit.Currency = "EUR"; d.Entries.Clear();
        Ledger.SaveTransfer(d,null,bank,deposit,start,null,2500,100,"FX funding",7);
        Ledger.SaveTransfer(d,null,deposit,bank,maturity,null,10,249,"FX return",7); Ledger.Validate(d);
        d.Rates["EUR"] = 26;
        Check(Ledger.Balance(d,"term",maturity)==90 && Ledger.Balance(d,"bank",maturity)==7749 &&
            Ledger.TermDepositTotal(d,maturity)==2340 && Ledger.TotalBalance(d,maturity)==10089,"FX revalues remaining principal and preserves both booked amounts");
        Check(BudgetStore.YearCsv(d,start.Year).Contains("\"100.00\",\"EUR\""),"CSV retains the received currency and amount");
        string backup = Path.Combine(folder,"export.json"); BudgetStore.ExportBackup(d,backup); store.Save(d);
        Check(BudgetStore.Serialize(BudgetStore.ReadBackup(backup))==BudgetStore.Serialize(store.Load()) &&
            store.Load().TermDeposits.Single().MaturityDate == Ledger.Iso(maturity),"backup, restore and restart round-trip maturity, colors and both sides of transfers");
    }

    private static T Named<T>(Control root, string name) where T : Control
    { return (T)root.Controls.Find(name,true).Single(); }

    private static IEnumerable<Control> Children(Control root)
    {
        foreach (Control child in root.Controls) {
            yield return child;
            foreach (Control nested in Children(child)) yield return nested;
        }
    }

    private static void Modal(Action open, Action<Form> action)
    {
        Exception error = null;
        using (var timer = new Timer { Interval = 100 })
        {
            timer.Tick += delegate {
                timer.Stop(); var form = Application.OpenForms.Cast<Form>().Last();
                try { action(form); } catch (Exception ex) { error = ex; form.Close(); }
            };
            timer.Start(); open();
        }
        if (error != null) throw error;
        Application.DoEvents();
    }

    private static void Windows(string folder)
    {
        var store = new BudgetStore(folder); var today = DateTime.Today;
        using (var main = new MainForm(store,Sample(),false))
        {
            main.Size = new Size(1000,700); main.Show(); Application.DoEvents();
            var nav = main.Controls.OfType<TableLayoutPanel>().Single().GetControlFromPosition(0,1) as TableLayoutPanel;
            Button termTab = (Button)nav.GetControlFromPosition(1,0);
            Check(termTab.Text=="Term deposits" && nav.GetControlFromPosition(0,0).Text=="Accounts & bills" &&
                nav.Controls.Cast<Control>().All(c=>c.Visible && c.Right<=nav.ClientSize.Width),"all six tabs fit at 1000 × 700 with deposits second");
            termTab.PerformClick();
            Modal(Named<Button>(main,"NewTermDeposit").PerformClick, f=>{
                Named<TextBox>(f,"DepositName").Text="Test term"; Named<NumericUpDown>(f,"DepositOpeningAmount").Value=4000;
                f.AcceptButton.PerformClick();
            });
            string id = main.Data.TermDeposits.Single().Id;
            Check(main.Data.Entries.Count==1 && Ledger.Balance(main.Data,"bank",today)==6000 &&
                Ledger.Balance(main.Data,id,today)==4000 && main.Data.TermDeposits[0].OpeningBalance==0,"one Save creates a bank-funded deposit atomically");
            Check(Named<DataGridView>(main,"TermDepositsPagerGrid").Rows[0].Cells["status"].Value.ToString()=="Locked" &&
                Named<Label>(main,"DepositsGrandTotal").Text==Theme.MoneyText(10000,"CZK"),"deposit page shows the locked state and combined total");
            Modal(Named<Button>(main,"WithdrawTermDeposit").PerformClick, f=>{
                Named<NumericUpDown>(f,"TransferSent").Value=500;
                var confirm=Named<CheckBox>(f,"ConfirmEarlyWithdrawal");
                Check(confirm.Enabled && !confirm.Checked,"a withdrawal before maturity presents explicit confirmation");
                confirm.Checked=true; f.AcceptButton.PerformClick();
            });
            Check(Ledger.Balance(main.Data,id,today)==3500 && main.Data.Entries.Count==2,"confirmed early withdrawal reduces locked funds exactly once");
            Modal(Named<Button>(main,"EditTermDeposit").PerformClick, f=>{
                Named<DateTimePicker>(f,"DepositMaturity").Value=today; f.AcceptButton.PerformClick();
            });
            Check(Named<DataGridView>(main,"TermDepositsPagerGrid").Rows[0].Cells["status"].Value.ToString()=="Matured" &&
                Ledger.Balance(main.Data,id,today)==3500,"on the maturity date the status changes and the funds remain visible");
            Modal(Named<Button>(main,"FundTermDeposit").PerformClick, f=>{
                Named<NumericUpDown>(f,"TransferSent").Value=600; f.CancelButton.PerformClick();
            });
            Check(main.Data.Entries.Count==2 && Ledger.Balance(main.Data,id,today)==3500,"cancelling a transfer leaves balances and history intact");
            Modal(Named<Button>(main,"WithdrawTermDeposit").PerformClick, f=>{
                Named<NumericUpDown>(f,"TransferSent").Value=3500;
                Check(!Named<CheckBox>(f,"ConfirmEarlyWithdrawal").Enabled,"matured principal can be returned directly");
                f.AcceptButton.PerformClick();
            });
            main.ShowPage(0);
            Check(Named<Label>(main,"GrandTotalAmount").Text==Theme.MoneyText(10000,"CZK") && main.Data.Entries.Count==3 &&
                Ledger.Balance(main.Data,"bank",today)==10000,"a full return restores the bank balance and the first-page grand total");
            main.ShowPage(1);
            Check(Named<DataGridView>(main,"EntriesPagerGrid").Rows.Count==3,"daily history displays outgoing and incoming term transfers");
            Named<Button>(main,"AllHistoryView").PerformClick();
            var grid = Named<DataGridView>(main,"EntriesPagerGrid");
            string lastReturn = main.Data.Entries.Last().Id;
            grid.CurrentCell = grid.Rows.Cast<DataGridViewRow>().Single(row => (string)row.Tag == lastReturn).Cells[0];
            Modal(Children(main).OfType<Button>().Single(button => button.Text == "Edit selected").PerformClick, f=>{
                Named<NumericUpDown>(f,"TransferSent").Value=3000; f.AcceptButton.PerformClick();
            });
            Check(main.Data.Entries.Count==3 && main.Data.Entries.Single(e=>e.Id==lastReturn).Amount==3000 &&
                Ledger.Balance(main.Data,id,today)==500 && Ledger.TotalBalance(main.Data,today)==10000,
                "editing a deposit transfer from all-history updates the selected posting and both balances");
            main.ShowPage(5);
            Modal(Named<Button>(main,"NewTermDeposit").PerformClick, f=>{
                Named<TextBox>(f,"DepositName").Text="Existing EUR";
                Icons.Choose(Named<ComboBox>(f,"DepositFunding"),""); Icons.Choose(Named<ComboBox>(f,"DepositCurrency"),"EUR");
                Named<NumericUpDown>(f,"DepositOpeningAmount").Value=100; f.AcceptButton.PerformClick();
            });
            Check(main.Data.Entries.Count==3 && Ledger.TotalBalance(main.Data,today)==12500,"a pre-existing EUR deposit adds its opening balance without debiting a second account");
            Modal(Named<Button>(main,"NewTermDeposit").PerformClick, f=>{
                Named<TextBox>(f,"DepositName").Text="New EUR";
                Icons.Choose(Named<ComboBox>(f,"DepositCurrency"),"EUR");
                Named<NumericUpDown>(f,"DepositOpeningAmount").Value=20;
                var debit=Named<NumericUpDown>(f,"DepositFundingDebit");
                Check(debit.Enabled && debit.Value==500,"FX funding displays the estimated bank debit in the bank currency");
                debit.Value=510; f.AcceptButton.PerformClick();
            });
            var fx=main.Data.TermDeposits.Single(t=>t.Name=="New EUR");
            Check(main.Data.Entries.Last().Amount==510 && main.Data.Entries.Last().ReceivedAmount==20 &&
                Ledger.Balance(main.Data,fx.Id,today)==20,"one-step FX funding saves the actual bank debit and deposit credit");
            main.Commit(d=>{d.Language="el"; d.Appearance="dark";});
            Check(Named<DataGridView>(main,"TermDepositsPagerGrid").Columns["maturity"].HeaderText=="Ημερομηνία λήξης" &&
                Named<DataGridView>(main,"TermDepositsPagerGrid").BackgroundColor==Theme.Surface,"Greek deposit controls work in dark mode");
            main.Commit(d=>d.Language="cs");
            Check(Named<DataGridView>(main,"TermDepositsPagerGrid").Columns["deposit"].HeaderText=="Termínovaný vklad","Czech deposit controls use the banking terminology");
            main.Close();
        }
    }

    [STAThread]
    private static int Main(string[] args)
    {
        string folder=Path.Combine(Path.GetTempPath(),"BudgetDesk-Term-"+Guid.NewGuid().ToString("N"));
        try {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            Accounting(Path.Combine(folder,"ledger")); Windows(Path.Combine(folder,"ui"));
            Console.WriteLine(checks+" term-deposit checks passed."); return 0;
        } catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { if(Directory.Exists(folder)) Directory.Delete(folder,true); }
    }
}
