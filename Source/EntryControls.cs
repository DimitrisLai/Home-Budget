using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace BudgetDesk
{
    internal sealed class NamePicker : ComboBox
    {
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (DropDownStyle == ComboBoxStyle.DropDownList && Char.IsLetter(e.KeyChar))
            {
                for (int i = 0; i < Items.Count; i++)
                {
                    Pick item = Items[i] as Pick;
                    if (item != null && item.Id.Length > 0 && CultureInfo.CurrentCulture.CompareInfo.IsPrefix(item.Text,
                        e.KeyChar.ToString(), CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace))
                    {
                        SelectedIndex = i;
                        break;
                    }
                }
                e.Handled = true;
            }
            base.OnKeyPress(e);
        }
        protected override void OnDropDown(EventArgs e)
        {
            int width = Width;
            foreach (object item in Items)
                width = Math.Max(width, TextRenderer.MeasureText(GetItemText(item), Font).Width + SystemInformation.VerticalScrollBarWidth + 12);
            DropDownWidth = Math.Min(width, Screen.FromControl(this).WorkingArea.Width);
            base.OnDropDown(e);
        }
    }

    // Two separate pickers keep calendar selection and editable local time usable in either Windows date format.
    internal sealed class PaymentMoment
    {
        internal readonly TableLayoutPanel Control;
        internal readonly DateTimePicker Date, Time;
        internal PaymentMoment(DateTime value, string oldDate, string oldTime, bool pastOnly, string name)
        {
            bool dated = !String.IsNullOrEmpty(oldDate);
            DateTime initial = dated ? Ledger.Moment(oldDate, oldTime) : value;
            Date = new DateTimePicker { Name = name, Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short,
                MinDate = new DateTime(1900, 1, 1), MaxDate = pastOnly ? DateTime.Today : new DateTime(2199, 12, 31), Value = initial.Date };
            Time = new DateTimePicker { Name = name + "Time", Dock = DockStyle.Fill, Format = DateTimePickerFormat.Custom,
                CustomFormat = "HH:mm:ss", ShowUpDown = true, ShowCheckBox = dated && String.IsNullOrEmpty(oldTime),
                Value = DateTime.Today.Add(initial.TimeOfDay) };
            Time.Checked = !Time.ShowCheckBox;
            Control = Row(Date, Time, 130);
        }
        internal DateTime Value { get { return Date.Value.Date.Add((!Time.ShowCheckBox || Time.Checked) ? Time.Value.TimeOfDay : TimeSpan.Zero); } }
        internal string ClockValue { get { return (!Time.ShowCheckBox || Time.Checked) ? Ledger.Clock(Time.Value) : null; } }
        internal static TableLayoutPanel Row(System.Windows.Forms.Control left, System.Windows.Forms.Control right, int rightWidth)
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Margin = Padding.Empty, ColumnCount = 2, RowCount = 1 };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, rightWidth));
            left.Margin = new Padding(0, 7, 6, 4); right.Margin = new Padding(0, 7, 0, 4);
            panel.Controls.Add(left, 0, 0); panel.Controls.Add(right, 1, 0);
            return panel;
        }
    }

    internal sealed class PurchaseFields
    {
        internal readonly TableLayoutPanel AmountRow, RateRow;
        internal readonly ComboBox Currency;
        private readonly NumericUpDown rate, amount;
        private readonly Label preview;
        private readonly BudgetData data;
        private readonly ComboBox account;
        private string accountCode;
        private bool setting, followingAccount, transfer;
        private string AccountCurrency { get { return data.Accounts.First(a => a.Id == Icons.Selected(account)).Currency; } }
        internal string Code { get { return Icons.Selected(Currency); } }
        internal string ForeignCurrency { get { return Code == AccountCurrency ? null : Code; } }
        internal decimal Quote { get { return ForeignCurrency == null ? 0 : rate.Value; } }

        internal PurchaseFields(BudgetData data, ComboBox account, NumericUpDown amount, string original, decimal savedRate)
        {
            this.data = data; this.account = account; this.amount = amount; accountCode = AccountCurrency;
            followingAccount = String.IsNullOrEmpty(original) || original == accountCode;
            Currency = Theme.Combo(); Currency.Name = "PurchaseCurrency";
            foreach (string code in Ledger.Currencies.OrderBy(x => x)) Currency.Items.Add(new Pick(code, code));
            Icons.Choose(Currency, followingAccount ? accountCode : original);
            AmountRow = PaymentMoment.Row(amount, Currency, 92);
            rate = Theme.Money(0); rate.Name = "PurchaseExchangeRate"; rate.Minimum = 0; rate.Maximum = 1000000;
            rate.DecimalPlaces = 8; rate.Increment = 0.01m;
            preview = Theme.Label("", 9, false, Theme.Ink); preview.Name = "AccountChargePreview";
            RateRow = PaymentMoment.Row(preview, rate, 128);
            SetQuote(savedRate > 0 ? savedRate : Ledger.SuggestedQuote(data, Code, accountCode));
            Currency.SelectedIndexChanged += delegate {
                if (setting) return;
                followingAccount = Code == accountCode;
                SetQuote(Ledger.SuggestedQuote(data, Code, accountCode));
            };
            account.SelectedIndexChanged += delegate {
                if (Icons.Selected(account).Length == 0) return;
                string next = AccountCurrency;
                if (next == accountCode) return;
                accountCode = next;
                if (followingAccount || transfer) ChooseCurrency(next);
                followingAccount = Code == next;
                SetQuote(Ledger.SuggestedQuote(data, Code, accountCode));
            };
            rate.ValueChanged += delegate { UpdatePreview(); };
            amount.ValueChanged += delegate { UpdatePreview(); };
        }
        private void ChooseCurrency(string code)
        {
            setting = true;
            try { Icons.Choose(Currency, code); }
            finally { setting = false; }
        }
        internal void SetTransfer(bool value)
        {
            if (value == transfer) return;
            transfer = value; Currency.Enabled = !transfer;
            if (value) { followingAccount = true; ChooseCurrency(accountCode); SetQuote(0); }
        }
        private void SetQuote(decimal value)
        {
            rate.Value = ForeignCurrency == null ? 1 : value;
            rate.Enabled = ForeignCurrency != null;
            UpdatePreview();
        }
        private void UpdatePreview()
        {
            if (ForeignCurrency == null) { preview.Text = "Account amount  " + Theme.MoneyText(amount.Value, accountCode); return; }
            string charge = rate.Value > 0 ? Theme.MoneyText(Ledger.AccountAmount(amount.Value, rate.Value), accountCode) : "enter a rate";
            preview.Text = "Account amount  " + charge + Environment.NewLine + "Rate: " + accountCode + " per 1 " + Code;
        }
    }
}
