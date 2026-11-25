using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace BudgetDesk
{
    public sealed partial class MainForm
    {
        private const int DepositsPageId = 5;
        private int depositsPageIndex;
        private DateTime depositsAsOf;

        private static Pick AccountOption(Account account)
        {
            string name = Icons.Named(account.Name, account.Icon) + "  (" + account.Currency + ")";
            if (account is TermDeposit) name += " · " + UiText.Get("Term deposit");
            return new Pick(account.Id, name);
        }

        private void DepositsPage()
        {
            depositsAsOf = DateTime.Today;
            var balances = Ledger.Balances(Data, depositsAsOf);
            decimal held = Ledger.TotalInBase(Data, Data.TermDeposits, balances);
            decimal total = held + Ledger.TotalInBase(Data, Data.Accounts, balances);
            TableLayoutPanel layout = PageLayout(78, 50, -1, 38);
            var metrics = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = Padding.Empty };
            for (int i = 0; i < 3; i++) metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3));
            metrics.Controls.Add(Metric(UiText.Get("Regular accounts"), Theme.MoneyText(total - held, Data.BaseCurrency),
                UiText.Get("Balances on the first tab"), false, "RegularAccountsTotal"), 0, 0);
            metrics.Controls.Add(Metric(UiText.Get("Term deposits"), Theme.MoneyText(held, Data.BaseCurrency),
                UiText.Get("Still held in deposits"), false, "TermDepositsTotal"), 1, 0);
            metrics.Controls.Add(Metric(UiText.Get("Grand total"), Theme.MoneyText(total, Data.BaseCurrency),
                UiText.Get("Accounts and term deposits"), true, "DepositsGrandTotal"), 2, 0);
            layout.Controls.Add(metrics, 0, 0);

            Button add = Theme.Button(UiText.Get("+ New deposit"), true);
            Button edit = Theme.Button(UiText.Get("Edit"), false);
            Button fund = Theme.Button(UiText.Get("Transfer in"), false);
            Button withdraw = Theme.Button(UiText.Get("Transfer out"), false);
            add.Name = "NewTermDeposit"; edit.Name = "EditTermDeposit";
            fund.Name = "FundTermDeposit"; withdraw.Name = "WithdrawTermDeposit";
            layout.Controls.Add(Section(UiText.Get("Your term deposits"), "", edit, fund, withdraw, add), 0, 1);

            var pager = new PagedGridPanel("TermDepositsPager");
            var grid = pager.Grid;
            Theme.Column(grid, "deposit", UiText.Get("Term deposit"), 36);
            Theme.Column(grid, "balance", UiText.Get("Balance"), 22);
            Theme.Column(grid, "maturity", UiText.Get("Maturity date"), 23);
            Theme.Column(grid, "status", UiText.Get("Status"), 19);
            var rows = new List<GridItem>();
            foreach (TermDeposit deposit in Data.TermDeposits.OrderBy(d => d.Name, UiText.Comparer))
            {
                decimal balance = balances[deposit.Id];
                DateTime maturity = Ledger.Day(deposit.MaturityDate);
                string state = UiText.Get(balance == 0 ? "Empty deposit" : maturity <= depositsAsOf ? "Matured" : "Locked");
                rows.Add(new GridItem(deposit.Id, Icons.Named(deposit.Name, deposit.Icon), Theme.MoneyText(balance, deposit.Currency),
                    maturity.ToString("d MMM yyyy", UiText.Culture), state) {
                    SortKeys = new object[] { deposit.Name, Ledger.ToBase(Data, balance, deposit.Currency), maturity, state },
                    ColorIndex = deposit.ColorIndex });
            }
            pager.SetRows(rows, false);
            pager.PageIndex = depositsPageIndex;
            pager.PageChanged += delegate { depositsPageIndex = pager.PageIndex; };
            layout.Controls.Add(pager, 0, 2);
            refreshPageLayout = pager.Reflow;
            string hint = rows.Count == 0 ? "Create a term deposit and choose its maturity date." :
                "At maturity, record the return transfer when the money reaches your account.";
            layout.Controls.Add(Theme.Label(UiText.Get(hint), 10, false, Theme.Muted), 0, 3);

            Func<TermDeposit> selected = delegate {
                Ledger.Require(grid.CurrentRow != null, "Select a term deposit first.");
                return Data.TermDeposits.First(d => d.Id == (string)grid.CurrentRow.Tag);
            };
            edit.Enabled = rows.Count > 0;
            fund.Enabled = withdraw.Enabled = rows.Count > 0 && Data.Accounts.Count > 0;
            add.Click += delegate { DepositEditor(null); };
            edit.Click += delegate { ActionSafely(delegate { DepositEditor(selected()); }); };
            grid.CellDoubleClick += delegate(object sender, DataGridViewCellEventArgs e) {
                if (e.RowIndex >= 0) ActionSafely(delegate { DepositEditor(selected()); });
            };
            fund.Click += delegate { ActionSafely(delegate { TransferEditor(selected(), false, null); }); };
            withdraw.Click += delegate { ActionSafely(delegate { TransferEditor(selected(), true, null); }); };
        }

        private static NumericUpDown DepositMoney(string name, decimal value)
        {
            var field = Theme.Money(value);
            field.Name = name; field.Minimum = 0;
            return field;
        }

        private decimal ConvertedEstimate(decimal amount, string from, string to, decimal maximum)
        {
            try
            {
                decimal value = Decimal.Round(amount * Ledger.Rate(Data, from) / Ledger.Rate(Data, to), 2, MidpointRounding.AwayFromZero);
                return value <= maximum ? value : 0;
            }
            catch (OverflowException) { return 0; }
        }

        private void DepositEditor(TermDeposit existing)
        {
            bool creating = existing == null;
            using (var f = new Editor(UiText.Get(creating ? "New term deposit" : "Edit term deposit"),
                UiText.Get(creating ? "Choose a funding account to move money into this deposit. For money already held separately, choose Existing deposit balance." :
                "You can rename the deposit or change its maturity date. Use transfers to record movements."), 42))
            {
                var name = Theme.Text(creating ? "" : existing.Name); name.MaxLength = 60; name.Name = "DepositName";
                var currency = Strings(Ledger.Currencies.Where(Data.Rates.ContainsKey), creating ? Data.BaseCurrency : existing.Currency);
                currency.Name = "DepositCurrency";
                var funding = Theme.Combo(); funding.Name = "DepositFunding";
                funding.Items.Add(new Pick("", UiText.Get("Existing deposit balance")));
                foreach (Account a in Data.Accounts.OrderBy(a => a.Name, UiText.Comparer)) funding.Items.Add(AccountOption(a));
                Icons.Choose(funding, creating && Data.Accounts.Count > 0 ? Data.Accounts[0].Id : "");
                var opening = DatePicker(creating ? DateTime.Today : Ledger.Day(existing.OpeningDate));
                opening.Name = "DepositOpeningDate"; opening.MaxDate = DateTime.Today;
                var maturity = DatePicker(creating ? DateTime.Today.AddYears(1) : Ledger.Day(existing.MaturityDate)); maturity.Name = "DepositMaturity";
                var amount = DepositMoney("DepositOpeningAmount", creating ? 0 : existing.OpeningBalance);
                var debit = DepositMoney("DepositFundingDebit", 0);
                var depositCode = Theme.Label("", 10, true, Theme.Muted);
                var debitCode = Theme.Label("", 10, true, Theme.Muted);
                var icon = Icons.Picker(creating ? "Shield" : existing.Icon);
                bool hasHistory = !creating && Data.Entries.Any(e => e.AccountId == existing.Id || e.ToAccountId == existing.Id);
                currency.Enabled = !hasHistory;
                amount.Enabled = !hasHistory;
                Action refresh = delegate {
                    var bank = Data.Accounts.FirstOrDefault(a => a.Id == Icons.Selected(funding));
                    string code = Icons.Selected(currency);
                    depositCode.Text = code; debitCode.Text = bank == null ? "" : bank.Currency;
                    debit.Enabled = bank != null && bank.Currency != code;
                    debit.Value = bank == null ? 0 : ConvertedEstimate(amount.Value, code, bank.Currency, debit.Maximum);
                };
                Action chooseFunding = delegate {
                    Account bank = Data.Accounts.FirstOrDefault(a => a.Id == Icons.Selected(funding));
                    if (bank != null) Icons.Choose(currency, bank.Currency);
                    refresh();
                };
                funding.SelectedIndexChanged += delegate { chooseFunding(); };
                currency.SelectedIndexChanged += delegate { refresh(); }; amount.ValueChanged += delegate { refresh(); };
                chooseFunding();
                f.Field(UiText.Get("Deposit name"), name);
                f.Field(UiText.Get("Icon"), icon);
                var color = f.ColorField(creating ? 0 : existing.ColorIndex);
                if (creating) f.Field(UiText.Get("Funding source"), funding);
                f.Field(UiText.Get("Currency"), currency);
                f.Field(UiText.Get("Opening amount"), PaymentMoment.Row(amount, depositCode, 62));
                if (creating) f.Field(UiText.Get("Debit from account"), PaymentMoment.Row(debit, debitCode, 62));
                f.Field(UiText.Get("Starting date"), opening);
                f.Field(UiText.Get("Maturity date"), maturity);
                if (!creating && !hasHistory && existing.OpeningBalance == 0)
                    f.ExtraButton(UiText.Get("Delete"), delegate {
                        if (!Confirm(UiText.Get("Delete this empty term deposit?"))) return;
                        Commit(d => d.TermDeposits.RemoveAll(t => t.Id == existing.Id));
                    });
                f.Finish(delegate {
                    Commit(d => {
                        TermDeposit deposit = creating ? new TermDeposit { Id = Ledger.NewId() } : d.TermDeposits.First(t => t.Id == existing.Id);
                        deposit.Name = name.Text.Trim(); deposit.Icon = Icons.Selected(icon); deposit.ColorIndex = color.Index;
                        deposit.Currency = Icons.Selected(currency); deposit.OpeningDate = Ledger.Iso(opening.Value);
                        deposit.MaturityDate = Ledger.Iso(maturity.Value);
                        Account bank = creating ? d.Accounts.FirstOrDefault(a => a.Id == Icons.Selected(funding)) : null;
                        deposit.OpeningBalance = bank == null ? amount.Value : 0;
                        if (creating) d.TermDeposits.Add(deposit);
                        if (bank != null)
                            Ledger.SaveTransfer(d, null, bank, deposit, opening.Value,
                                opening.Value.Date == DateTime.Today ? Ledger.Clock(DateTime.Now) : null,
                                debit.Value, amount.Value, deposit.Name, color.Index);
                    });
                });
                f.ShowDialog(this);
            }
        }

        private void TransferEditor(TermDeposit pinned, bool outgoing, Entry existing)
        {
            using (var f = new Editor(UiText.Get(existing == null ? "New transfer" : "Edit transfer"),
                UiText.Get("Enter the amounts actually sent and received. Both balances update together. Transfers are excluded from income and expenses."), 43))
            {
                var from = AccountChoices(existing != null ? existing.AccountId : pinned != null && outgoing ? pinned.Id : "", false, pinned == null || outgoing);
                from.Name = "TransferFrom"; from.Enabled = pinned == null || !outgoing;
                var to = Theme.Combo(); to.Name = "TransferTo"; to.Enabled = pinned == null || outgoing;
                var moment = new PaymentMoment(DateTime.Now, existing == null ? null : existing.Date,
                    existing == null ? null : existing.Time, true, "TransferDate");
                var sent = DepositMoney("TransferSent", existing == null ? 0 : existing.Amount);
                var received = DepositMoney("TransferReceived", existing == null ? 0 : existing.ReceivedAmount);
                var fromCode = Theme.Label("", 10, true, Theme.Muted); var toCode = Theme.Label("", 10, true, Theme.Muted);
                var early = new CheckBox { Name = "ConfirmEarlyWithdrawal", Text = UiText.Get("Confirm early withdrawal"), Dock = DockStyle.Fill };
                var memo = Theme.Text(existing == null ? pinned == null ? "" : pinned.Name : existing.Description);
                Func<Account> source = () => Data.AllAccounts.First(a => a.Id == Icons.Selected(from));
                Func<Account> target = () => Data.AllAccounts.First(a => a.Id == Icons.Selected(to));
                Action updateEarly = delegate {
                    var deposit = source() as TermDeposit;
                    early.Enabled = deposit != null && moment.Value.Date < Ledger.Day(deposit.MaturityDate);
                    early.Checked = early.Enabled && existing != null && existing.AccountId == deposit.Id && existing.Date == Ledger.Iso(moment.Value);
                };
                Action updateAmounts = delegate {
                    if (to.SelectedItem == null) return;
                    string a = source().Currency, b = target().Currency;
                    fromCode.Text = a; toCode.Text = b; received.Enabled = a != b;
                    received.Value = ConvertedEstimate(sent.Value, a, b, received.Maximum);
                };
                Action destinations = delegate {
                    Account a = source(); string preferred = Icons.Selected(to);
                    to.Items.Clear();
                    IEnumerable<Account> choices = a is TermDeposit ? Data.Accounts : Data.AllAccounts;
                    foreach (Account other in choices.Where(x => x.Id != a.Id).OrderBy(x => x.Name, UiText.Comparer))
                        to.Items.Add(AccountOption(other));
                    if (pinned != null && !outgoing) preferred = pinned.Id;
                    Icons.Choose(to, preferred);
                    updateAmounts(); updateEarly();
                };
                destinations();
                if (existing != null) {
                    Icons.Choose(to, existing.ToAccountId); updateAmounts();
                    received.Value = existing.ReceivedAmount;
                }
                from.SelectedIndexChanged += delegate { destinations(); }; to.SelectedIndexChanged += delegate { updateAmounts(); };
                sent.ValueChanged += delegate { updateAmounts(); }; moment.Date.ValueChanged += delegate { updateEarly(); };
                f.Field(UiText.Get("From account"), from); f.Field(UiText.Get("Transfer to"), to);
                f.Field(UiText.Get("Date / time"), moment.Control);
                f.Field(UiText.Get("Amount sent"), PaymentMoment.Row(sent, fromCode, 62));
                f.Field(UiText.Get("Amount received"), PaymentMoment.Row(received, toCode, 62));
                f.Field(UiText.Get("Before maturity"), early);
                f.Field(UiText.Get("Description"), memo);
                var color = f.ColorField(existing != null ? existing.ColorIndex : pinned != null ? pinned.ColorIndex : 0);
                f.Finish(delegate {
                    Ledger.Require(!early.Enabled || early.Checked, "Confirm the early withdrawal before saving.");
                    Commit(d => {
                        var a = d.AllAccounts.First(x => x.Id == Icons.Selected(from));
                        var b = d.AllAccounts.First(x => x.Id == Icons.Selected(to));
                        Ledger.SaveTransfer(d, existing == null ? null : existing.Id, a, b, moment.Value, moment.ClockValue,
                            sent.Value, received.Value, memo.Text.Trim(), color.Index);
                    });
                    if (pageIndex == 1 && existing == null) {
                        selectedMonth = new DateTime(moment.Value.Year, moment.Value.Month, 1);
                        showPlans = allHistory = false; entriesPageIndex = 0;
                        accountFilter = categoryFilter = searchText = ""; kindFilter = "All types";
                        ShowPage(1);
                    }
                });
                f.ShowDialog(this);
            }
        }
    }
}
