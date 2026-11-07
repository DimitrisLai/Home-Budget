using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BudgetDesk
{
    internal static class Theme
    {
        internal static readonly Color Ink = Color.FromArgb(28, 42, 61);
        internal static readonly Color Muted = Color.FromArgb(106, 119, 137);
        internal static readonly Color Teal = Color.FromArgb(0, 123, 116);
        internal static readonly Color Pale = Color.FromArgb(229, 245, 240);
        internal static readonly Color Canvas = Color.FromArgb(244, 247, 250);
        internal static readonly Color Line = Color.FromArgb(222, 229, 236);
        internal static readonly Color Red = Color.FromArgb(187, 58, 66);
        internal static Font Font(float size, bool bold) { return new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular); }
        internal static Label Label(string text, float size, bool bold, Color color)
        {
            return new Label { Text = text, Font = Font(size, bold), ForeColor = color, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill, Margin = new Padding(0), UseMnemonic = false };
        }
        internal static Button Button(string text, bool primary)
        {
            Button b = new Button { Text = text, Font = Font(10f, true), AutoSize = false, Height = 36,
                Width = Math.Max(100, text.Length * 8 + 24), FlatStyle = FlatStyle.Flat,
                BackColor = primary ? Teal : Color.White, ForeColor = primary ? Color.White : Ink,
                Cursor = Cursors.Hand, Margin = new Padding(5, 3, 0, 3), UseVisualStyleBackColor = false, UseMnemonic = false };
            b.FlatAppearance.BorderColor = primary ? Teal : Line;
            b.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(0, 101, 96) : Pale;
            return b;
        }
        internal static ComboBox Combo() { return new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = Font(10.5f, false), Dock = DockStyle.Fill, Margin = new Padding(0, 10, 0, 8) }; }
        internal static NumericUpDown Money(decimal value)
        {
            return new NumericUpDown { DecimalPlaces = 2, Minimum = -999999999999m, Maximum = 999999999999m,
                ThousandsSeparator = true, Value = value, Font = Font(11, false), Dock = DockStyle.Fill, Margin = new Padding(0, 10, 0, 8) };
        }
        internal static TextBox Text(string value)
        { return new TextBox { Text = value ?? "", Font = Font(11, false), Dock = DockStyle.Fill, Margin = new Padding(0, 10, 0, 8), MaxLength = 300 }; }
        internal static string MoneyText(decimal amount, string currency)
        { return amount.ToString("N2", CultureInfo.CurrentCulture) + " " + currency; }
        internal static DataGridView Grid()
        {
            DataGridView g = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                AllowUserToDeleteRows = false, AllowUserToResizeRows = false, AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Line, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, Font = Font(10, false), EnableHeadersVisualStyles = false, ScrollBars = ScrollBars.None,
                ColumnHeadersHeight = 30, ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing };
            g.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Canvas, ForeColor = Muted, Font = Font(9, true), Padding = new Padding(9, 0, 0, 0), SelectionBackColor = Canvas };
            g.DefaultCellStyle = new DataGridViewCellStyle { Font = Font(10, false), ForeColor = Ink, BackColor = Color.White, SelectionBackColor = Pale,
                SelectionForeColor = Ink, Padding = new Padding(8, 0, 5, 0), WrapMode = DataGridViewTriState.False };
            g.RowTemplate.Height = 32;
            return g;
        }
        internal static void Column(DataGridView g, string name, string label, float weight)
        { g.Columns.Add(new DataGridViewTextBoxColumn { Name = name, HeaderText = label, FillWeight = weight, MinimumWidth = 70, SortMode = DataGridViewColumnSortMode.NotSortable }); }
    }

    internal sealed class Pick
    {
        internal string Id; internal string Text;
        internal Pick(string id, string text) { Id = id; Text = text; }
        public override string ToString() { return Text; }
    }
    internal static class Icons
    {
        internal static readonly Dictionary<string, string> All = new Dictionary<string, string> {
            { "Bank", "▥" }, { "Wallet", "▣" }, { "Shield", "◆" }, { "Home", "⌂" },
            { "Basket", "▦" }, { "Coffee", "☕" }, { "Car", "▰" }, { "Health", "✚" },
            { "Bag", "▤" }, { "Bolt", "ϟ" }, { "Music", "♫" }, { "Phone", "▯" },
            { "Travel", "✈" }, { "Gift", "✦" }, { "Education", "▧" }, { "Heart", "♥" },
            { "Star", "★" }, { "Work", "▣" }, { "Sport", "●" }, { "Pets", "♣" },
            { "Internet", "◎" }, { "Food", "◒" }, { "Coins", "◉" }, { "Other", "◇" }
        };
        internal static string Glyph(string key) { return key != null && All.ContainsKey(key) ? All[key] : "◇"; }
        internal static ComboBox Picker(string key)
        {
            ComboBox c = Theme.Combo();
            foreach (var x in All) c.Items.Add(new Pick(x.Key, x.Value + "    " + x.Key));
            Choose(c, key); return c;
        }
        internal static void Choose(ComboBox c, string id)
        {
            for (int i = 0; i < c.Items.Count; i++)
                if (((Pick)c.Items[i]).Id == id) { c.SelectedIndex = i; return; }
            if (c.Items.Count > 0) c.SelectedIndex = 0;
        }
        internal static string Selected(ComboBox c)
        { return c.SelectedItem == null ? "" : ((Pick)c.SelectedItem).Id; }
    }

    internal sealed class Editor : Form
    {
        private readonly TableLayoutPanel fields;
        private readonly FlowLayoutPanel buttons;
        internal readonly Button Save;
        private int row;
        private readonly int fieldHeight;
        internal Editor(string title, string hint, int rowHeight = 49)
        {
            fieldHeight = rowHeight;
            Text = title; StartPosition = FormStartPosition.CenterParent; Width = 610;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
            Font = Theme.Font(10, false); BackColor = Color.White; AutoScaleMode = AutoScaleMode.Dpi;
            Padding = new Padding(24, 18, 24, 14);
            TableLayoutPanel shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = new Padding(0) };
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            Label info = Theme.Label(hint, 10f, false, Theme.Muted); info.TextAlign = ContentAlignment.TopLeft;
            shell.Controls.Add(info, 0, 0);
            fields = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 0, Margin = new Padding(0) };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 167));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            shell.Controls.Add(fields, 0, 1);
            buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 9, 0, 0), Margin = new Padding(0) };
            Save = Theme.Button("Save", true);
            Button cancel = Theme.Button("Cancel", false); cancel.DialogResult = DialogResult.Cancel;
            buttons.Controls.Add(Save); buttons.Controls.Add(cancel); shell.Controls.Add(buttons, 0, 2);
            Controls.Add(shell); AcceptButton = Save; CancelButton = cancel;
        }
        internal void Field(string label, Control control)
        {
            fields.RowCount++; fields.RowStyles.Add(new RowStyle(SizeType.Absolute, fieldHeight));
            fields.Controls.Add(Theme.Label(label, 10, true, Theme.Ink), 0, row);
            fields.Controls.Add(control, 1, row++);
        }
        internal void ExtraButton(string text, Action action)
        {
            Button b = Theme.Button(text, false); b.ForeColor = Theme.Red;
            b.Click += delegate { Run(action); }; buttons.Controls.Add(b);
        }
        internal void Finish(Action action)
        {
            Height = 204 + row * fieldHeight;
            Save.Click += delegate { Run(action); };
        }
        internal Button AuxiliaryButton(string text, Action action)
        {
            Button button = Theme.Button(text, false); buttons.Controls.Add(button);
            button.Click += delegate { try { action(); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Budget Desk", MessageBoxButtons.OK, MessageBoxIcon.Information); } };
            return button;
        }
        internal void Run(Action action)
        {
            try { action(); DialogResult = DialogResult.OK; Close(); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Please check", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        }
    }

    internal sealed class CategoryChart : Control
    {
        internal List<KeyValuePair<string, decimal>> Values = new List<KeyValuePair<string, decimal>>();
        internal string Currency = "CZK";
        internal CategoryChart() { DoubleBuffered = true; BackColor = Color.White; Dock = DockStyle.Fill; }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            if (Values.Count == 0)
            {
                using (Font f = Theme.Font(12, false)) using (Brush b = new SolidBrush(Theme.Muted))
                    g.DrawString("Your spending breakdown will appear here.", f, b, 18, 24);
                return;
            }
            decimal max = Values.Max(x => x.Value);
            int rowHeight = 48; int maxRows = Math.Max(1, (Height - 15) / rowHeight);
            var display = Values.Take(Math.Max(1, maxRows - (Values.Count > maxRows ? 1 : 0))).ToList();
            if (display.Count < Values.Count) display.Add(new KeyValuePair<string, decimal>("Other categories", Values.Skip(display.Count).Sum(x => x.Value)));
            max = display.Max(x => x.Value);
            int y = 10;
            using (Font small = Theme.Font(9, false)) using (Font name = Theme.Font(10, true))
            using (Brush ink = new SolidBrush(Theme.Ink)) using (Brush muted = new SolidBrush(Theme.Muted))
            using (Brush teal = new SolidBrush(Theme.Teal)) using (Brush pale = new SolidBrush(Theme.Pale))
            {
                foreach (var item in display)
                {
                    g.DrawString(item.Key, name, ink, 16, y);
                    string amount = Theme.MoneyText(item.Value, Currency);
                    SizeF size = g.MeasureString(amount, small);
                    g.DrawString(amount, small, muted, Math.Max(200, Width - size.Width - 20), y + 1);
                    int barWidth = Math.Max(50, Width - 34);
                    g.FillRectangle(pale, 17, y + 25, barWidth, 7);
                    g.FillRectangle(teal, 17, y + 25, max == 0 ? 0 : (float)(item.Value / max) * barWidth, 7);
                    y += rowHeight;
                }
            }
        }
    }

    public sealed partial class MainForm : Form
    {
        public BudgetData Data { get; private set; }
        public BudgetStore Store { get; private set; }
        private readonly bool demo;
        private readonly Panel pageHost;
        private readonly Button[] tabs;
        private readonly Label status;
        private Panel currentPage;
        private Action refreshPageLayout;
        private bool layoutPassQueued;
        private const string AccountDragFormat = "BudgetDesk.AccountOrder";
        private string accountDragId;
        private int pendingAccountDropIndex = -1;
        private DateTime accountClickBlockedUntil;
        private string selectedBillId;
        private PagedGridPanel entriesPager;
        private int entriesPageIndex;
        private bool showPlans;
        private int plansPageIndex;
        private int goalsPageIndex;
        private string planWhen = "All dates";
        private int selectedYear = DateTime.Today.Year;
        private int pageIndex;
        private DateTime selectedMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        private string accountFilter = "", categoryFilter = "", kindFilter = "All types", searchText = "";
        private readonly Timer clock;
        private DataGridView entriesGrid;
        private Label filteredTotal;
        private bool reportingTimerError;

        public MainForm(BudgetStore store, BudgetData data, bool isDemo)
        {
            Store = store; Data = data; demo = isDemo;
            Text = "Budget Desk 1.0.1" + (demo ? "  |  DEMO DATA" : "");
            MinimumSize = new Size(1000, 700);
            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
            Size = new Size(Math.Min(1320, Math.Max(1000, workingArea.Width - 36)), Math.Min(930, Math.Max(700, workingArea.Height - 36)));
            StartPosition = FormStartPosition.CenterScreen; BackColor = Theme.Canvas;
            Font = Theme.Font(10, false); ForeColor = Theme.Ink; AutoScaleMode = AutoScaleMode.Dpi;
            DoubleBuffered = true;
            TableLayoutPanel shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(26, 8, 26, 8), Margin = new Padding(0) };
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            TableLayoutPanel header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = new Padding(0) };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 192));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 166));
            Label title = Theme.Label("Budget Desk" + (demo ? "   /   DEMO" : ""), 24, true, Theme.Ink);
            header.Controls.Add(title, 0, 0);
            header.Controls.Add(Theme.Label(DateTime.Today.ToString("ddd, d MMM yyyy", CultureInfo.GetCultureInfo("en-GB")), 10, false, Theme.Muted), 1, 0);
            Button settings = Theme.Button("Settings & backup", false); settings.Anchor = AnchorStyles.Right;
            settings.Click += delegate { Settings(); }; header.Controls.Add(settings, 2, 0); shell.Controls.Add(header, 0, 0);
            FlowLayoutPanel nav = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            string[] labels = { "Accounts & bills", "Daily expenses", "Monthly overview", "Yearly overview", "Savings goals" }; tabs = new Button[5];
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i; tabs[i] = Theme.Button(labels[i], false); tabs[i].Width = 174;
                tabs[i].Margin = new Padding(0, 0, 8, 7); tabs[i].Click += delegate { ShowPage(index); }; nav.Controls.Add(tabs[i]);
            }
            shell.Controls.Add(nav, 0, 1);
            pageHost = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) }; shell.Controls.Add(pageHost, 0, 2);
            status = Theme.Label(demo ? "DEMO  /  Sample figures only. Your real budget is separate." : "Saved on this computer  /  Your next step: add an account.", 9, false, Theme.Muted);
            shell.Controls.Add(status, 0, 3); Controls.Add(shell);
            ShowPage(0);
            clock = new Timer { Interval = 60000 };
            clock.Tick += delegate { CheckDue(false); };
            clock.Start(); Shown += delegate { CheckDue(true); };
            FormClosed += delegate { clock.Stop(); clock.Dispose(); };
        }

        // Save a validated working copy before publishing the change to the UI.
        public void Commit(Action<BudgetData> edit)
        {
            BudgetData working = BudgetStore.Clone(Data); edit(working);
            Ledger.Validate(working); Store.Save(working); Data = working;
            status.Text = (demo ? "DEMO  /  " : "Saved on this computer  /  ") + "Last saved " + DateTime.Now.ToString("HH:mm:ss");
            ShowPage(pageIndex);
        }
        private void ActionSafely(Action action)
        {
            try { action(); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Budget Desk", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        }
        private bool Confirm(string message)
        { return MessageBox.Show(this, message, "Budget Desk", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes; }
        private void CheckDue(bool initial)
        {
            if (accountDragId != null) return;
            try
            {
                BudgetData d = BudgetStore.Clone(Data); int count = Ledger.PostDue(d, DateTime.Today);
                if (count > 0)
                {
                    Store.Save(d); Data = d; ShowPage(pageIndex);
                    status.Text = count + " recurring payment" + (count == 1 ? "" : "s") + " recorded. View them in Daily expenses.";
                }
                else if (initial && !File.Exists(Store.FilePath)) Store.Save(Data);
                reportingTimerError = false;
            }
            catch (Exception ex)
            {
                status.Text = "Recurring payments were not saved. " + ex.Message;
                if (!reportingTimerError) MessageBox.Show(this, ex.Message, "Recurring payment check", MessageBoxButtons.OK, MessageBoxIcon.Information);
                reportingTimerError = true;
            }
        }
        public void ShowPage(int index)
        {
            pageIndex = index; refreshPageLayout = null;
            SuspendLayout(); pageHost.SuspendLayout();
            try
            {
                if (currentPage != null) { pageHost.Controls.Remove(currentPage); currentPage.Dispose(); }
                // A rebuilt page must start at the viewport size, not Panel's
                // temporary default size while its parent layout is suspended.
                currentPage = new Panel { Size = pageHost.ClientSize, Dock = DockStyle.Fill, Margin = new Padding(0) };
                pageHost.Controls.Add(currentPage);
                for (int i = 0; i < tabs.Length; i++)
                {
                    tabs[i].BackColor = i == index ? Theme.Ink : Color.White;
                    tabs[i].ForeColor = i == index ? Color.White : Theme.Muted;
                }
                if (index == 0) AccountsPage(); else if (index == 1) EntriesPage(); else if (index == 2) OverviewPage(); else if (index == 3) YearlyPage(); else GoalsPage();
            }
            finally
            {
                pageHost.ResumeLayout(true); ResumeLayout(true);
            }
            RefreshVisiblePage(); QueuePageLayout();
        }
        private void RefreshVisiblePage()
        {
            if (pageHost == null || currentPage == null || IsDisposed || Disposing || WindowState == FormWindowState.Minimized) return;
            pageHost.PerformLayout(); currentPage.PerformLayout();
            if (refreshPageLayout != null) refreshPageLayout();
            foreach (PagedGridPanel pager in Descendants(currentPage).OfType<PagedGridPanel>()) pager.Reflow();
            currentPage.Invalidate(true);
        }
        private void QueuePageLayout()
        {
            if (layoutPassQueued || !IsHandleCreated || IsDisposed || Disposing || WindowState == FormWindowState.Minimized) return;
            layoutPassQueued = true;
            BeginInvoke((MethodInvoker)delegate {
                layoutPassQueued = false;
                RefreshVisiblePage();
            });
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (pageHost != null && WindowState != FormWindowState.Minimized) { RefreshVisiblePage(); QueuePageLayout(); }
        }
        protected override void OnActivated(EventArgs e) { base.OnActivated(e); QueuePageLayout(); }
        private static IEnumerable<Control> Descendants(Control parent)
        {
            foreach (Control child in parent.Controls) { yield return child; foreach (Control nested in Descendants(child)) yield return nested; }
        }
        private TableLayoutPanel PageLayout(params int[] heights)
        {
            TableLayoutPanel t = new TableLayoutPanel { Size = currentPage.ClientSize, Dock = DockStyle.Fill, ColumnCount = 1, RowCount = heights.Length, Margin = new Padding(0) };
            foreach (int h in heights) t.RowStyles.Add(new RowStyle(h < 0 ? SizeType.Percent : SizeType.Absolute, h < 0 ? 100 : h));
            currentPage.Controls.Add(t); return t;
        }
        private Panel Section(string title, string subtitle, params Button[] actions)
        {
            Panel panel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            FlowLayoutPanel right = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
            foreach (Button b in actions.Reverse()) right.Controls.Add(b);
            panel.Controls.Add(right);
            Label label = Theme.Label(title, 14, true, Theme.Ink); label.Dock = DockStyle.Fill;
            if (!String.IsNullOrEmpty(subtitle)) label.Text += "    " + subtitle;
            panel.Controls.Add(label); label.BringToFront(); right.BringToFront(); return panel;
        }
        private Panel Metric(string caption, string value, string hint, bool dark, string valueName = null)
        {
            Panel p = new Panel { Dock = DockStyle.Fill, BackColor = dark ? Theme.Ink : Color.White, Margin = new Padding(0, 0, 12, 0), Padding = new Padding(14, 4, 10, 4) };
            TableLayoutPanel t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = new Padding(0), BackColor = p.BackColor };
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 18)); t.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); t.RowStyles.Add(new RowStyle(SizeType.Absolute, 16));
            t.Controls.Add(Theme.Label(caption, 9, true, dark ? Color.FromArgb(178, 211, 213) : Theme.Muted), 0, 0);
            Label amount = Theme.Label(value, 18, true, dark ? Color.White : Theme.Ink); if (valueName != null) amount.Name = valueName;
            t.Controls.Add(amount, 0, 1);
            t.Controls.Add(Theme.Label(hint, 8.5f, false, dark ? Color.FromArgb(178, 211, 213) : Theme.Muted), 0, 2);
            p.Controls.Add(t); return p;
        }

        private void AccountsPage()
        {
            DashboardPanel page = new DashboardPanel { Name = "AccountsDashboard", Size = currentPage.ClientSize, Dock = DockStyle.Fill, Margin = new Padding(0), AutoScroll = false, AccountCount = Data.Accounts.Count, BillCount = Data.Bills.Count };
            Panel accounts = new Panel { Name = "AccountCards", AutoScroll = false, BackColor = Theme.Canvas };
            foreach (Account account in Data.Accounts) accounts.Controls.Add(AccountCard(account));
            if (Data.Accounts.Count == 0)
            {
                Label empty = Theme.Label("Add a bank account, savings account or cash. Give each its own name and icon.", 10.5f, false, Theme.Muted);
                empty.BackColor = Color.White; empty.Padding = new Padding(12, 0, 12, 0); accounts.Controls.Add(empty);
            }
            Button add = Theme.Button("+ Add account", true); add.Click += delegate { AccountEditor(null); };
            ConfigureAccountDrop(accounts, null);
            Panel accountHeader = Section("Your accounts", Data.Accounts.Count + " / 20 · Drag to reorder", add); accountHeader.Name = "AccountsHeader";
            Panel total = new Panel { Name = "GrandTotal", BackColor = Theme.Ink, Padding = new Padding(10, 0, 10, 0) };
            Label caption = Theme.Label("GRAND TOTAL  /  ALL ACCOUNTS", 9, true, Color.FromArgb(178, 211, 213)); caption.Dock = DockStyle.Left; caption.Width = 285;
            Label amount = Theme.Label(Theme.MoneyText(Ledger.TotalBalance(Data, DateTime.Today), Data.BaseCurrency), 16, true, Color.White); amount.TextAlign = ContentAlignment.MiddleRight; amount.AutoEllipsis = true;
            total.Controls.Add(amount); total.Controls.Add(caption);
            Panel bills = new Panel { Name = "RecurringCards", AutoScroll = false, BackColor = Theme.Canvas };
            if (!Data.Bills.Any(x => x.Id == selectedBillId)) selectedBillId = Data.Bills.OrderBy(x => Ledger.DueDate(x)).Select(x => x.Id).FirstOrDefault();
            foreach (RecurringBill bill in Data.Bills.OrderBy(x => Ledger.DueDate(x))) bills.Controls.Add(BillCard(bill, bills));
            if (Data.Bills.Count == 0)
            {
                Label empty = Theme.Label("Add rent, subscriptions or insurance. Each payment has its own account and repeat interval.", 10.5f, false, Theme.Muted);
                empty.BackColor = Color.White; empty.Padding = new Padding(12, 0, 12, 0); bills.Controls.Add(empty);
            }
            Func<RecurringBill> selected = delegate {
                RecurringBill bill = Data.Bills.FirstOrDefault(x => x.Id == selectedBillId);
                Ledger.Require(bill != null, "Select a recurring payment first."); return bill;
            };
            Button record = Theme.Button("Record payment", false), edit = Theme.Button("Edit", false), toggle = Theme.Button("Pause / resume", false), addBill = Theme.Button("+ Recurring payment", true);
            addBill.Click += delegate { BillEditor(null); };
            edit.Click += delegate { ActionSafely(delegate { BillEditor(selected()); }); };
            record.Click += delegate { ActionSafely(delegate {
                RecurringBill bill = selected(); string id = bill.Id;
                if (!Confirm("Record " + bill.Name + " for " + Theme.MoneyText(bill.Amount, Data.Accounts.First(x => x.Id == bill.AccountId).Currency) + "?\n\nIt will be deducted from its account and included in expenses.")) return;
                Commit(d => Ledger.RecordNext(d, d.Bills.First(x => x.Id == id), DateTime.Today));
            }); };
            toggle.Click += delegate { ActionSafely(delegate {
                RecurringBill bill = selected(); string id = bill.Id;
                if (!bill.Enabled && bill.AutoRecord && Ledger.DueDate(bill) <= DateTime.Today && !Confirm("Resuming this schedule will record its overdue payments. Continue?")) return;
                Commit(d => { RecurringBill value = d.Bills.First(x => x.Id == id); value.Enabled = !value.Enabled; Ledger.PostDue(d, DateTime.Today); });
            }); };
            Panel billHeader = Section("Recurring payments", Data.Bills.Count + " / 20", record, edit, toggle, addBill); billHeader.Name = "RecurringHeader";
            Label hint = Theme.Label("Click a payment to select · Double-click to edit · Hover for full details · Monthly figures are on Monthly overview.", 8, false, Theme.Muted); hint.Name = "DashboardHint"; hint.Dock = DockStyle.None;
            page.AccountHeader = accountHeader; page.AccountCards = accounts; page.Total = total; page.BillHeader = billHeader; page.BillCards = bills; page.Hint = hint;
            foreach (Control control in new Control[] { accountHeader, accounts, total, billHeader, bills, hint }) { control.Dock = DockStyle.None; page.Controls.Add(control); }
            currentPage.Controls.Add(page); refreshPageLayout = delegate { if (!page.IsDisposed) page.Reflow(); };
        }
        private Panel BillCard(RecurringBill bill, Panel host)
        {
            Account account = Data.Accounts.First(x => x.Id == bill.AccountId); DateTime due = Ledger.DueDate(bill);
            Panel card = new Panel { Name = "RecurringCard", Tag = bill.Id, Size = new Size(240, 64), Padding = new Padding(7, 3, 7, 3), Cursor = Cursors.Hand, BackColor = bill.Id == selectedBillId ? Theme.Pale : Color.White, TabStop = true, AccessibleName = bill.Name };
            TableLayoutPanel fields = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Margin = new Padding(0) };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 62));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 15)); fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 18)); fields.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Label title = Theme.Label(Icons.Glyph(bill.Icon) + " " + bill.Name, 9, true, bill.Enabled ? Theme.Ink : Theme.Muted); title.AutoEllipsis = true; fields.Controls.Add(title, 0, 0); fields.SetColumnSpan(title, 2);
            Label amount = Theme.Label(Theme.MoneyText(bill.Amount, account.Currency), 11, true, Theme.Teal); amount.AutoEllipsis = true; fields.Controls.Add(amount, 0, 1);
            Label date = Theme.Label(due.ToString("d MMM", CultureInfo.GetCultureInfo("en-GB")), 8.5f, true, bill.Enabled && due <= DateTime.Today ? Theme.Red : Theme.Muted); date.TextAlign = ContentAlignment.MiddleRight; fields.Controls.Add(date, 1, 1);
            string unit = bill.Unit == "Months" ? "mo" : bill.Unit == "Weeks" ? "wk" : bill.Unit == "Years" ? "yr" : "d";
            string shortAccount = account.Name.Length > 14 ? account.Name.Substring(0, 13) + "…" : account.Name;
            Label detail = Theme.Label(shortAccount + " · " + bill.Interval + unit + " · " + (!bill.Enabled ? "Paused" : bill.AutoRecord ? "Auto" : "Manual"), 7.5f, false, Theme.Muted); detail.AutoEllipsis = true; fields.Controls.Add(detail, 0, 2); fields.SetColumnSpan(detail, 2);
            card.Controls.Add(fields);
            ToolTip tip = new ToolTip { InitialDelay = 250, AutoPopDelay = 15000 };
            string text = bill.Name + "\n" + Theme.MoneyText(bill.Amount, account.Currency) + "\nFrom: " + account.Name + "\nNext due: " + due.ToString("d MMM yyyy") + "\nEvery " + bill.Interval + " " + bill.Unit.ToLowerInvariant() + "\n" + (!bill.Enabled ? "Paused" : bill.AutoRecord ? "Automatic recording" : "Manual recording");
            foreach (Control control in new Control[] { card, fields, title, amount, date, detail })
            {
                tip.SetToolTip(control, text);
                control.Click += delegate { selectedBillId = bill.Id; card.Focus(); foreach (Control sibling in host.Controls) { sibling.BackColor = (string)sibling.Tag == selectedBillId ? Theme.Pale : Color.White; sibling.Invalidate(); } };
                control.DoubleClick += delegate { ActionSafely(delegate { BillEditor(Data.Bills.First(x => x.Id == bill.Id)); }); };
            }
            card.Paint += delegate(object sender, PaintEventArgs e) { using (Pen pen = new Pen(bill.Id == selectedBillId ? Theme.Teal : Theme.Line)) e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1); };
            card.KeyDown += delegate(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) BillEditor(Data.Bills.First(x => x.Id == bill.Id)); };
            card.Disposed += delegate { tip.Dispose(); };
            return card;
        }
        private Panel AccountCard(Account a)
        {
            decimal balance = Ledger.Balance(Data, a.Id, DateTime.Today);
            Panel p = new Panel { Name = "AccountCard", Tag = a.Id, Width = 240, Height = 58, BackColor = Color.White, Margin = new Padding(0), Padding = new Padding(9, 4, 9, 4), Cursor = Cursors.Hand };
            TableLayoutPanel t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2, Margin = new Padding(0) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 23)); t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 18)); t.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Label icon = Theme.Label(Icons.Glyph(a.Icon), 18, true, Theme.Teal); t.Controls.Add(icon, 0, 0); t.SetRowSpan(icon, 2);
            Label name = Theme.Label(a.Name, 9.5f, true, Theme.Ink); name.AutoEllipsis = true; t.Controls.Add(name, 1, 0);
            Label value = Theme.Label(Theme.MoneyText(balance, a.Currency), 13, true, balance < 0 ? Theme.Red : Theme.Ink); value.AutoEllipsis = true; t.Controls.Add(value, 1, 1); t.SetColumnSpan(value, 2);
            Label edit = Theme.Label("•••", 9, true, Theme.Muted); t.Controls.Add(edit, 2, 0); p.Controls.Add(t);
            ToolTip details = new ToolTip { InitialDelay = 300, AutoPopDelay = 10000 };
            foreach (Control c in new Control[] { p, t, icon, name, value, edit }) details.SetToolTip(c, a.Name + "\n" + Theme.MoneyText(balance, a.Currency) + "\nDrag to reorder · Click to edit display position or account details");
            p.Disposed += delegate { details.Dispose(); };
            p.SizeChanged += delegate {
                bool compact = p.Height < 48;
                p.Padding = compact ? new Padding(7, 3, 7, 3) : new Padding(9, 4, 9, 4);
                t.RowStyles[0].Height = compact ? 16 : 18;
                float size = compact ? 11 : 13;
                if (value.Font.Size != size) { Font previous = value.Font; value.Font = Theme.Font(size, true); previous.Dispose(); }
            };
            bool pressed = false, dragged = false; Rectangle dragThreshold = Rectangle.Empty;
            foreach (Control part in new Control[] { p, t, icon, name, value, edit })
            {
                Control source = part; ConfigureAccountDrop(source, a.Id);
                source.MouseDown += delegate(object sender, MouseEventArgs e) {
                    if (e.Button != MouseButtons.Left) return;
                    pressed = true; dragged = false; accountClickBlockedUntil = DateTime.MinValue;
                    Point point = source.PointToScreen(e.Location); Size size = SystemInformation.DragSize;
                    dragThreshold = new Rectangle(point.X - size.Width / 2, point.Y - size.Height / 2, size.Width, size.Height);
                };
                source.MouseUp += delegate { pressed = false; };
                source.MouseMove += delegate(object sender, MouseEventArgs e) {
                    if (!pressed || e.Button != MouseButtons.Left || dragThreshold.Contains(source.PointToScreen(e.Location))) return;
                    pressed = false; dragged = true; StartAccountDrag(source, a.Id);
                };
                source.Click += delegate { if (!dragged && DateTime.UtcNow >= accountClickBlockedUntil) AccountEditor(a); };
            }
            return p;
        }

        private bool AcceptAccountDrag(DragEventArgs e)
        {
            if (accountDragId == null || e.Data == null || (e.AllowedEffect & DragDropEffects.Move) == 0) return false;
            try { return e.Data.GetDataPresent(AccountDragFormat, false) && e.Data.GetData(AccountDragFormat, false) as string == accountDragId && Data.Accounts.Any(a => a.Id == accountDragId); }
            catch (Exception) { return false; }
        }
        private void ConfigureAccountDrop(Control control, string targetId)
        {
            control.AllowDrop = true;
            DragEventHandler over = delegate(object sender, DragEventArgs e) { e.Effect = AcceptAccountDrag(e) ? DragDropEffects.Move : DragDropEffects.None; };
            control.DragEnter += over; control.DragOver += over;
            control.DragDrop += delegate(object sender, DragEventArgs e) {
                if (!AcceptAccountDrag(e)) { e.Effect = DragDropEffects.None; return; }
                pendingAccountDropIndex = targetId == null ? Data.Accounts.Count - 1 : Data.Accounts.FindIndex(a => a.Id == targetId);
                e.Effect = DragDropEffects.Move;
            };
        }
        private void StartAccountDrag(Control source, string id)
        {
            if (accountDragId != null) return;
            accountDragId = id; pendingAccountDropIndex = -1;
            try { source.DoDragDrop(new DataObject(AccountDragFormat, id), DragDropEffects.Move); }
            finally { FinishAccountDrag(); }
        }
        private void FinishAccountDrag()
        {
            string id = accountDragId; int position = pendingAccountDropIndex;
            accountDragId = null; pendingAccountDropIndex = -1; accountClickBlockedUntil = DateTime.UtcNow.AddMilliseconds(250);
            // Save only after the OS drag loop releases the source control.
            if (id != null && position >= 0 && Data.Accounts.FindIndex(a => a.Id == id) != position)
                ActionSafely(delegate { Commit(d => Ledger.MoveAccount(d, id, position)); });
            CheckDue(false);
        }

        private ComboBox AccountChoices(string selected, bool includeAll)
        {
            ComboBox c = Theme.Combo();
            if (includeAll) c.Items.Add(new Pick("", "All accounts"));
            foreach (Account a in Data.Accounts) c.Items.Add(new Pick(a.Id, Icons.Glyph(a.Icon) + "  " + a.Name + "  (" + a.Currency + ")"));
            Icons.Choose(c, selected); return c;
        }
        private ComboBox CategoryChoices(string selected, bool includeAll)
        {
            ComboBox c = Theme.Combo(); if (includeAll) c.Items.Add(new Pick("", "All categories"));
            foreach (Category cat in Data.Categories) c.Items.Add(new Pick(cat.Id, Icons.Glyph(cat.Icon) + "  " + cat.Name));
            Icons.Choose(c, selected); return c;
        }
        private ComboBox Strings(IEnumerable<string> values, string selected)
        {
            ComboBox c = Theme.Combo(); foreach (string x in values) c.Items.Add(new Pick(x, x)); Icons.Choose(c, selected); return c;
        }
        private DateTimePicker DatePicker(DateTime date)
        { return new DateTimePicker { Value = date, Format = DateTimePickerFormat.Custom, CustomFormat = "ddd, d MMM yyyy", Dock = DockStyle.Fill, Font = Theme.Font(10.5f, false), Margin = new Padding(0, 10, 0, 8), MinDate = new DateTime(1900, 1, 1), MaxDate = new DateTime(2199, 12, 31) }; }
        private Panel MonthBar(Action refresh, bool compact = false)
        {
            Panel p = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            Button prev = Theme.Button("‹", false); prev.Width = 43; prev.Location = new Point(0, 4);
            Button next = Theme.Button("›", false); next.Width = 43; next.Location = new Point(262, 4);
            Label label = Theme.Label(selectedMonth.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("en-GB")), 15, true, Theme.Ink); label.Dock = DockStyle.None; label.SetBounds(57, 2, 200, 43); label.TextAlign = ContentAlignment.MiddleCenter;
            DateTimePicker picker = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "MMMM yyyy", ShowUpDown = true, Value = selectedMonth, Width = compact ? 128 : 164, Height = 30, Location = new Point(compact ? 311 : 323, 11), Font = Theme.Font(10, false), MinDate = new DateTime(1900, 1, 1), MaxDate = new DateTime(2199, 12, 31) };
            prev.Click += delegate { if (selectedMonth.Year == 1900 && selectedMonth.Month == 1) return; selectedMonth = selectedMonth.AddMonths(-1); refresh(); };
            next.Click += delegate { if (selectedMonth.Year == 2199 && selectedMonth.Month == 12) return; selectedMonth = selectedMonth.AddMonths(1); refresh(); };
            picker.ValueChanged += delegate { selectedMonth = new DateTime(picker.Value.Year, picker.Value.Month, 1); refresh(); };
            p.Controls.Add(prev); p.Controls.Add(next); p.Controls.Add(label); p.Controls.Add(picker); return p;
        }

        private void EntriesPage()
        {
            TableLayoutPanel t = allHistory && !showPlans ? PageLayout(50, 70, 38, 40, 42, 42, -1, 28) : PageLayout(50, 70, 38, 40, 42, -1, 25);
            Panel top = showPlans || allHistory ? new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) } : MonthBar(delegate { entriesPageIndex = 0; ShowPage(1); }, true);
            if (showPlans || allHistory) top.Controls.Add(Theme.Label(showPlans ? "One-off plans · all dates" : "Search all recorded history", 15, true, Theme.Ink));
            FlowLayoutPanel actions = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
            Button expense = Theme.Button("+ Expense", true), income = Theme.Button("+ Income", false), transfer = Theme.Button("Transfer", false), future = Theme.Button("+ Future expense", false);
            expense.Width = income.Width = transfer.Width = 91; future.Width = 148; future.Name = "AddPlannedExpense";
            expense.Click += delegate { EntryEditor(null, "Expense"); }; income.Click += delegate { EntryEditor(null, "Income"); }; transfer.Click += delegate { EntryEditor(null, "Transfer"); };
            future.Click += delegate { PlannedExpenseEditor(null); };
            actions.Controls.Add(expense); actions.Controls.Add(income); actions.Controls.Add(transfer); actions.Controls.Add(future); top.Controls.Add(actions); actions.BringToFront(); t.Controls.Add(top, 0, 0);
            decimal incomeTotal = Ledger.MonthTotal(Data, selectedMonth, "Income"), expenseTotal = Ledger.MonthTotal(Data, selectedMonth, "Expense");
            TableLayoutPanel metrics = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = new Padding(0) };
            for (int i = 0; i < 3; i++) metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
            if (showPlans)
            {
                metrics.Controls.Add(Metric("ALL ONE-OFF PLANS", Theme.MoneyText(Data.PlannedExpenses.Sum(p => Ledger.PlanInBase(Data, p)), Data.BaseCurrency), "Not deducted · all dates and accounts", false), 0, 0);
                metrics.Controls.Add(Metric("DUE IN NEXT 30 DAYS", Theme.MoneyText(Ledger.OneOffTotal(Data, DateTime.Today, DateTime.Today.AddDays(30)), Data.BaseCurrency), "Today through the next 30 days", false), 1, 0);
                metrics.Controls.Add(Metric("OVERDUE", Theme.MoneyText(Data.PlannedExpenses.Where(p => Ledger.Day(p.DueDate) < DateTime.Today).Sum(p => Ledger.PlanInBase(Data, p)), Data.BaseCurrency), "Still planned · record only when paid", true), 2, 0);
            }
            else if (allHistory)
            {
                metrics.Controls.Add(Metric("INCOME · RESULTS", "", "Matching recorded income", false, "HistoryIncome"), 0, 0);
                metrics.Controls.Add(Metric("EXPENSES · RESULTS", "", "Matching spending · category shares", false, "HistoryExpenses"), 1, 0);
                metrics.Controls.Add(Metric("NET · RESULTS", "", "Income minus expenses · transfers excluded", true, "HistoryNet"), 2, 0);
            }
            else
            {
                metrics.Controls.Add(Metric("INCOME", Theme.MoneyText(incomeTotal, Data.BaseCurrency), "Money received this month", false), 0, 0);
                metrics.Controls.Add(Metric("EXPENSES", Theme.MoneyText(expenseTotal, Data.BaseCurrency), "Purchases + recorded payments · plans excluded", false), 1, 0);
                metrics.Controls.Add(Metric("MONTHLY NET", Theme.MoneyText(incomeTotal - expenseTotal, Data.BaseCurrency), "Income minus expenses · transfers excluded", true), 2, 0);
            }
            t.Controls.Add(metrics, 0, 1);
            FlowLayoutPanel views = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = new Padding(0), WrapContents = false };
            Button recorded = Theme.Button("Recorded entries", !showPlans && !allHistory), planned = Theme.Button("Future / planned (" + Data.PlannedExpenses.Count + ")", showPlans), history = Theme.Button("Search all history", !showPlans && allHistory);
            Button templates = Theme.Button("Templates", false), payees = Theme.Button("Payees", false);
            recorded.Name = "RecordedEntriesView"; planned.Name = "PlannedExpensesView"; history.Name = "AllHistoryView"; templates.Name = "QuickTemplates"; payees.Name = "ManagePayees";
            recorded.Width = 162; planned.Width = 202; history.Width = 174; templates.Width = 110; payees.Width = 100;
            foreach (Button button in new Button[] { recorded, planned, history, templates, payees }) { button.Height = 32; button.Margin = new Padding(0, 3, 7, 0); views.Controls.Add(button); }
            recorded.Click += delegate { showPlans = false; allHistory = false; entriesPageIndex = 0; ShowPage(1); }; planned.Click += delegate { showPlans = true; allHistory = false; ShowPage(1); };
            history.Click += delegate { showPlans = false; allHistory = true; entriesPageIndex = 0; ShowPage(1); };
            templates.Click += delegate { Templates(); }; payees.Click += delegate { Payees(); }; t.Controls.Add(views, 0, 2);
            Button cats = Theme.Button("Categories  " + Data.Categories.Count + "/20", false); cats.Click += delegate { Categories(); };
            Button addCategory = Theme.Button("+ Category", true); addCategory.AccessibleName = "Add expense category"; addCategory.Enabled = Data.Categories.Count < 20;
            addCategory.Click += delegate { CategoryEditor(null, this); };
            Button edit = Theme.Button("Edit selected", false), delete = Theme.Button("Delete", false); delete.ForeColor = Theme.Red;
            Button csv = Theme.Button(showPlans ? "Export plans CSV" : allHistory ? "Export results CSV" : "Export month CSV", false); csv.Click += delegate { ExportCsv(); };
            Button paid = Theme.Button("Record paid", true); paid.Name = "RecordPlannedPaid";
            t.Controls.Add(showPlans ? Section("Planned expenses", "", edit, delete, paid, csv) : Section(allHistory ? "History results" : "Daily entries", "", edit, delete, cats, addCategory, csv), 0, 3);
            TableLayoutPanel filters = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, Margin = new Padding(0) };
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28)); filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 19)); filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
            ComboBox account = AccountChoices(accountFilter, true), category = CategoryChoices(categoryFilter, true);
            ComboBox kind = showPlans ? Strings(new[] { "All dates", "Next 30 days", "Overdue" }, planWhen) : Strings(new[] { "All types", "Expense", "Income", "Transfer" }, kindFilter);
            kind.Name = showPlans ? "PlanDateFilter" : "EntryTypeFilter";
            TextBox search = Theme.Text(searchText); search.AccessibleName = "Search descriptions"; search.Name = "EntrySearchText";
            ToolTip tip = new ToolTip(); tip.SetToolTip(search, "Search payee, description, account or category"); currentPage.Disposed += delegate { tip.Dispose(); };
            foreach (Control c in new Control[] { account, category, kind, search }) c.Margin = new Padding(0, 6, 12, 10);
            filters.Controls.Add(account, 0, 0); filters.Controls.Add(category, 1, 0); filters.Controls.Add(kind, 2, 0); filters.Controls.Add(search, 3, 0); t.Controls.Add(filters, 0, 4);
            PagedGridPanel pager = new PagedGridPanel(showPlans ? "PlansPager" : "EntriesPager"); entriesPager = pager; entriesGrid = pager.Grid;
            pager.PageChanged += delegate { if (showPlans) plansPageIndex = pager.PageIndex; else entriesPageIndex = pager.PageIndex; };
            Theme.Column(entriesGrid, "date", showPlans ? "DUE DATE" : "DATE", 13); Theme.Column(entriesGrid, "description", "DESCRIPTION", 26); Theme.Column(entriesGrid, "category", "CATEGORY", 17);
            Theme.Column(entriesGrid, "account", "ACCOUNT", 22); Theme.Column(entriesGrid, "amount", "AMOUNT", 16); Theme.Column(entriesGrid, "type", showPlans ? "STATUS" : "TYPE", 14);
            bool extraFilters = allHistory && !showPlans;
            if (extraFilters) t.Controls.Add(HistoryFilters(), 0, 5);
            t.Controls.Add(pager, 0, extraFilters ? 6 : 5);
            filteredTotal = Theme.Label("", 9, false, Theme.Muted); t.Controls.Add(filteredTotal, 0, extraFilters ? 7 : 6);
            EventHandler filter = delegate { accountFilter = Icons.Selected(account); categoryFilter = Icons.Selected(category); if (showPlans) planWhen = Icons.Selected(kind); else kindFilter = Icons.Selected(kind); searchText = search.Text; FillEntries(true); };
            account.SelectedIndexChanged += filter; category.SelectedIndexChanged += filter; kind.SelectedIndexChanged += filter; search.TextChanged += filter;
            Func<string> selectedId = delegate { Ledger.Require(entriesGrid.SelectedRows.Count > 0, "Select an item first."); return (string)entriesGrid.SelectedRows[0].Tag; };
            Action open = delegate { string id = selectedId(); if (showPlans) PlannedExpenseEditor(Data.PlannedExpenses.First(x => x.Id == id)); else EntryEditor(Data.Entries.First(x => x.Id == id), "Expense"); };
            edit.Click += delegate { ActionSafely(open); };
            entriesGrid.CellDoubleClick += delegate(object sender, DataGridViewCellEventArgs e) { if (e.RowIndex >= 0) ActionSafely(open); };
            paid.Click += delegate { ActionSafely(delegate { RecordPlannedEditor(Data.PlannedExpenses.First(x => x.Id == selectedId())); }); };
            delete.Click += delegate { ActionSafely(delegate {
                string id = selectedId();
                if (showPlans)
                {
                    if (!Confirm("Delete this planned expense? It has not been deducted, so your balance will stay the same.")) return;
                    Commit(d => d.PlannedExpenses.RemoveAll(x => x.Id == id));
                }
                else
                {
                    Entry e = Data.Entries.First(x => x.Id == id);
                    if (!Confirm("Delete this " + e.Kind.ToLowerInvariant() + "? Its effect on the account balance will be reversed." + (!String.IsNullOrEmpty(e.BillId) ? "\n\nIts recurring schedule will stay advanced, so the payment will not be added again." : "") + (!String.IsNullOrEmpty(e.PlanId) ? "\n\nThe original one-off plan will stay removed." : ""))) return;
                    Commit(d => d.Entries.RemoveAll(x => x.Id == id));
                }
            }); };
            FillEntries();
        }
        private void FillPlans(bool resetPage)
        {
            IEnumerable<PlannedExpense> items = Data.PlannedExpenses;
            if (accountFilter != "") items = items.Where(p => p.AccountId == accountFilter);
            if (categoryFilter != "") items = items.Where(p => p.CategoryId == categoryFilter);
            if (!String.IsNullOrWhiteSpace(searchText)) items = items.Where(p => p.Description.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) >= 0);
            if (planWhen == "Next 30 days") items = items.Where(p => Ledger.Day(p.DueDate) >= DateTime.Today && Ledger.Day(p.DueDate) <= DateTime.Today.AddDays(30));
            else if (planWhen == "Overdue") items = items.Where(p => Ledger.Day(p.DueDate) < DateTime.Today);
            List<PlannedExpense> list = items.OrderBy(p => p.DueDate).ThenBy(p => p.Id).ToList();
            List<GridItem> rows = new List<GridItem>();
            foreach (PlannedExpense p in list)
            {
                Account a = Data.Accounts.First(x => x.Id == p.AccountId); Category c = Data.Categories.First(x => x.Id == p.CategoryId);
                int days = (Ledger.Day(p.DueDate) - DateTime.Today).Days;
                string when = days < 0 ? "Overdue " + (-days) + "d" : days == 0 ? "Due today" : "In " + days + " days";
                rows.Add(new GridItem(p.Id, Ledger.Day(p.DueDate).ToString("d MMM yyyy", CultureInfo.GetCultureInfo("en-GB")), p.Description, Icons.Glyph(c.Icon) + "  " + c.Name,
                    a.Name, Theme.MoneyText(p.Amount, a.Currency), when) { ColorColumn = 5, Color = days < 0 ? Theme.Red : Theme.Muted });
            }
            int wantedPage = resetPage ? 0 : plansPageIndex;
            entriesPager.SetRows(rows, resetPage); entriesPager.PageIndex = wantedPage;
            filteredTotal.Text = list.Count + " plans  /  Estimated total: " + Theme.MoneyText(list.Sum(p => Ledger.PlanInBase(Data, p)), Data.BaseCurrency) + "  /  Not deducted. Use Record paid after payment.";
        }
        private void FillEntries(bool resetPage = false)
        {
            if (showPlans) { FillPlans(resetPage); return; }
            List<GridItem> rows = new List<GridItem>();
            List<Entry> list;
            try { list = Ledger.SearchEntries(Data, BuildEntrySearch()).OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).ToList(); }
            catch (BudgetException ex) { entriesPager.SetRows(new GridItem[0], true); filteredTotal.Text = ex.Message; UpdateHistoryMetrics(0, 0); return; }
            foreach (Entry e in list)
            {
                Account a = Data.Accounts.First(x => x.Id == e.AccountId); Category c = Data.Categories.FirstOrDefault(x => x.Id == e.CategoryId);
                Account to = Data.Accounts.FirstOrDefault(x => x.Id == e.ToAccountId);
                bool split = Ledger.HasSplits(e), share = split && categoryFilter != "";
                string cat = e.Kind == "Transfer" ? "Between accounts" : split ? (share ? Data.Categories.First(x => x.Id == categoryFilter).Name + " [split]" : "Split · " + e.Splits.Count + " categories") : c == null ? "Income" : Icons.Glyph(c.Icon) + "  " + c.Name;
                decimal shownAmount = share ? Ledger.CategoryExpenseAmount(e, categoryFilter) : e.Amount;
                string amount = (e.Kind == "Expense" ? "− " : e.Kind == "Income" ? "+ " : "↔ ") + Theme.MoneyText(shownAmount, a.Currency);
                rows.Add(new GridItem(e.Id, Ledger.Day(e.Date).ToString("d MMM yyyy", CultureInfo.GetCultureInfo("en-GB")), (String.IsNullOrWhiteSpace(e.Payee) ? "" : e.Payee + " · ") + e.Description + (share ? "  [share of " + Theme.MoneyText(e.Amount, a.Currency) + "]" : split ? "  [split]" : "") + (!String.IsNullOrEmpty(e.BillId) ? "  [bill]" : !String.IsNullOrEmpty(e.PlanId) ? "  [one-off]" : ""), cat,
                    a.Name + (to == null ? "" : " → " + to.Name), amount, e.Kind) { ColorColumn = 4, Color = e.Kind == "Income" ? Theme.Teal : e.Kind == "Expense" ? Theme.Red : Theme.Muted });
            }
            int wantedPage = resetPage ? 0 : entriesPageIndex;
            entriesPager.SetRows(rows, resetPage); entriesPager.PageIndex = wantedPage;
            decimal filtered = list.Where(x => x.Kind == "Expense").Sum(x => categoryFilter == "" ? Ledger.EntryInBase(Data, x) : Ledger.CategoryExpenseInBase(Data, x, categoryFilter));
            filteredTotal.Text = list.Count + " matching entries  /  Expenses across these results: " + Theme.MoneyText(filtered, Data.BaseCurrency) + (allHistory ? "  /  Amount filters use the full entry in its account currency." : "  /  Search payee, description, account or category.");
            UpdateHistoryMetrics(list.Where(x => x.Kind == "Income").Sum(x => Ledger.EntryInBase(Data, x)), filtered);
        }

        private void OverviewPage()
        {
            TableLayoutPanel t = PageLayout(54, 80, 38, -1, 32);
            Panel top = MonthBar(delegate { ShowPage(2); }); Button export = Theme.Button("Export month CSV", false); export.Dock = DockStyle.Right; export.Click += delegate { ExportCsv(); }; top.Controls.Add(export); t.Controls.Add(top, 0, 0);
            decimal income = Ledger.MonthTotal(Data, selectedMonth, "Income"), expenses = Ledger.MonthTotal(Data, selectedMonth, "Expense");
            TableLayoutPanel metrics = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = new Padding(0) };
            for (int i = 0; i < 3; i++) metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
            metrics.Controls.Add(Metric("TOTAL INCOME", Theme.MoneyText(income, Data.BaseCurrency), "All accounts, in " + Data.BaseCurrency, false), 0, 0);
            metrics.Controls.Add(Metric("TOTAL EXPENSES", Theme.MoneyText(expenses, Data.BaseCurrency), "Includes bills already recorded", false), 1, 0);
            metrics.Controls.Add(Metric("NET CASH FLOW", Theme.MoneyText(income - expenses, Data.BaseCurrency), "Income minus expenses", true), 2, 0); t.Controls.Add(metrics, 0, 1);
            DateTime end = selectedMonth.AddMonths(1).AddDays(-1);
            decimal planned = Ledger.PlannedTotal(Data, selectedMonth, end), oneOff = Ledger.OneOffTotal(Data, selectedMonth, end);
            t.Controls.Add(Theme.Label("Unpaid one-off plans: " + Theme.MoneyText(oneOff, Data.BaseCurrency) + "    ·    Unrecorded bills: " + Theme.MoneyText(planned, Data.BaseCurrency), 10.5f, true, Theme.Ink), 0, 2);
            TableLayoutPanel detail = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
            detail.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 47)); detail.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 53));
            CategoryChart chart = new CategoryChart { Margin = new Padding(0, 0, 16, 0), Currency = Data.BaseCurrency };
            PagedGridPanel overviewPager = new PagedGridPanel("OverviewPager"); DataGridView grid = overviewPager.Grid; Theme.Column(grid, "category", "CATEGORY", 36); Theme.Column(grid, "actual", "SPENT", 24); Theme.Column(grid, "budget", "BUDGET", 23); Theme.Column(grid, "remaining", "LEFT", 23);
            List<GridItem> categoryRows = new List<GridItem>();
            List<Entry> entries = Ledger.MonthEntries(Data, selectedMonth).Where(e => e.Kind == "Expense").ToList();
            foreach (Category c in Data.Categories.OrderByDescending(c => entries.Sum(e => Ledger.CategoryExpenseInBase(Data, e, c.Id))))
            {
                decimal actual = entries.Sum(e => Ledger.CategoryExpenseInBase(Data, e, c.Id));
                decimal budget = c.MonthlyBudgetCzk / Ledger.Rate(Data, Data.BaseCurrency);
                if (actual > 0) chart.Values.Add(new KeyValuePair<string, decimal>(c.Name, actual));
                GridItem item = new GridItem(c.Id, Icons.Glyph(c.Icon) + "  " + c.Name, Theme.MoneyText(actual, Data.BaseCurrency), budget > 0 ? Theme.MoneyText(budget, Data.BaseCurrency) : "—", budget > 0 ? Theme.MoneyText(budget - actual, Data.BaseCurrency) : "—");
                if (budget > 0 && actual > budget) { item.ColorColumn = 3; item.Color = Theme.Red; } categoryRows.Add(item);
            }
            overviewPager.SetRows(categoryRows, true);
            detail.Controls.Add(chart, 0, 0); detail.Controls.Add(overviewPager, 1, 0); t.Controls.Add(detail, 0, 3);
            t.Controls.Add(Theme.Label("Budgets are optional; set them in Daily expenses → Categories. Transfers are excluded from income and expense totals. Foreign entries keep the rate saved when recorded.", 9, false, Theme.Muted), 0, 4);
        }

        private void YearlyPage()
        {
            TableLayoutPanel t = PageLayout(50, 74, 34, -1, 28);
            Panel top = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            Button previous = Theme.Button("‹", false), next = Theme.Button("›", false);
            previous.SetBounds(0, 4, 43, 36); next.SetBounds(204, 4, 43, 36);
            previous.Enabled = selectedYear > 1900; next.Enabled = selectedYear < 2199;
            Label yearTitle = Theme.Label(selectedYear.ToString(), 19, true, Theme.Ink); yearTitle.Dock = DockStyle.None; yearTitle.SetBounds(53, 2, 141, 43); yearTitle.TextAlign = ContentAlignment.MiddleCenter;
            NumericUpDown year = Theme.Money(selectedYear); year.Name = "OverviewYear"; year.Dock = DockStyle.None; year.DecimalPlaces = 0; year.ThousandsSeparator = false; year.Minimum = 1900; year.Maximum = 2199; year.SetBounds(269, 10, 100, 30);
            previous.Click += delegate { selectedYear--; ShowPage(3); }; next.Click += delegate { selectedYear++; ShowPage(3); };
            year.ValueChanged += delegate { selectedYear = (int)year.Value; ShowPage(3); };
            Button export = Theme.Button("Export year CSV", false); export.Dock = DockStyle.Right; export.Click += delegate { ExportYearCsv(); };
            top.Controls.Add(previous); top.Controls.Add(yearTitle); top.Controls.Add(next); top.Controls.Add(year); top.Controls.Add(export); t.Controls.Add(top, 0, 0);
            decimal income = Ledger.YearTotal(Data, selectedYear, "Income"), expense = Ledger.YearTotal(Data, selectedYear, "Expense");
            TableLayoutPanel metrics = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = new Padding(0) };
            for (int i = 0; i < 3; i++) metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
            metrics.Controls.Add(Metric("YEARLY INCOME", Theme.MoneyText(income, Data.BaseCurrency), "Recorded income · all accounts", false), 0, 0);
            metrics.Controls.Add(Metric("YEARLY EXPENSES", Theme.MoneyText(expense, Data.BaseCurrency), "Recorded expenses · unpaid plans excluded", false), 1, 0);
            metrics.Controls.Add(Metric("YEARLY NET", Theme.MoneyText(income - expense, Data.BaseCurrency), "Income minus expenses · transfers excluded", true), 2, 0); t.Controls.Add(metrics, 0, 1);
            DateTime start = new DateTime(selectedYear, 1, 1), end = new DateTime(selectedYear, 12, 31);
            decimal oneOff = Ledger.OneOffTotal(Data, start, end), bills = Ledger.PlannedTotal(Data, start, end);
            t.Controls.Add(Theme.Label("Unpaid one-off plans: " + Theme.MoneyText(oneOff, Data.BaseCurrency) + "    ·    Unrecorded bills: " + Theme.MoneyText(bills, Data.BaseCurrency), 10.5f, true, Theme.Ink), 0, 2);
            DataGridView grid = Theme.Grid(); grid.Name = "YearlyMonthsGrid"; grid.AccessibleName = "Twelve month yearly overview";
            Theme.Column(grid, "month", "MONTH", 16); Theme.Column(grid, "income", "INCOME", 17); Theme.Column(grid, "expense", "PAID EXPENSES", 18);
            Theme.Column(grid, "net", "NET", 17); Theme.Column(grid, "plans", "ONE-OFF PLANS", 18); Theme.Column(grid, "bills", "BILLS TO PAY", 18);
            grid.DefaultCellStyle.Font = Theme.Font(9, false);
            for (int month = 1; month <= 12; month++)
            {
                DateTime from = new DateTime(selectedYear, month, 1), through = new DateTime(selectedYear, month, DateTime.DaysInMonth(selectedYear, month));
                decimal received = Ledger.MonthTotal(Data, from, "Income"), spent = Ledger.MonthTotal(Data, from, "Expense");
                int row = grid.Rows.Add(from.ToString("MMMM", CultureInfo.GetCultureInfo("en-GB")), Theme.MoneyText(received, Data.BaseCurrency), Theme.MoneyText(spent, Data.BaseCurrency),
                    Theme.MoneyText(received - spent, Data.BaseCurrency), Theme.MoneyText(Ledger.OneOffTotal(Data, from, through), Data.BaseCurrency), Theme.MoneyText(Ledger.PlannedTotal(Data, from, through), Data.BaseCurrency));
                grid.Rows[row].Tag = month;
                grid.Rows[row].Cells[3].Style.ForeColor = received - spent < 0 ? Theme.Red : Theme.Teal;
                if (selectedYear == DateTime.Today.Year && month == DateTime.Today.Month) grid.Rows[row].DefaultCellStyle.BackColor = Theme.Pale;
            }
            Action fit = delegate {
                if (WindowState == FormWindowState.Minimized || grid.IsDisposed) return;
                int height = Math.Max(20, Math.Min(38, (grid.ClientSize.Height - grid.ColumnHeadersHeight - 2) / 12));
                foreach (DataGridViewRow row in grid.Rows) row.Height = height;
            };
            grid.SizeChanged += delegate { fit(); }; refreshPageLayout = fit;
            grid.CellDoubleClick += delegate(object sender, DataGridViewCellEventArgs e) { if (e.RowIndex >= 0) { selectedMonth = new DateTime(selectedYear, (int)grid.Rows[e.RowIndex].Tag, 1); ShowPage(2); } };
            t.Controls.Add(grid, 0, 3);
            t.Controls.Add(Theme.Label("Double-click a month for details. Plans are not deducted; their estimates use current rates. Year CSV exports recorded entries.", 9, false, Theme.Muted), 0, 4);
        }

        private void GoalsPage()
        {
            TableLayoutPanel t = PageLayout(74, 44, -1, 42);
            TableLayoutPanel metrics = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = new Padding(0) };
            for (int i = 0; i < 3; i++) metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
            metrics.Controls.Add(Metric("TOTAL TARGETS", Theme.MoneyText(Data.Goals.Sum(g => Ledger.ToBase(Data, g.TargetAmount, g.Currency)), Data.BaseCurrency), "Goal amounts · current reporting rates", false), 0, 0);
            metrics.Controls.Add(Metric("SAVED TOWARD GOALS", Theme.MoneyText(Data.Goals.Sum(g => Ledger.ToBase(Data, g.SavedAmount, g.Currency)), Data.BaseCurrency), "Amounts you have marked as set aside", false), 1, 0);
            metrics.Controls.Add(Metric("STILL TO SAVE", Theme.MoneyText(Data.Goals.Sum(g => Ledger.ToBase(Data, Ledger.GoalRemaining(g), g.Currency)), Data.BaseCurrency), "Remaining across your goals", true), 2, 0); t.Controls.Add(metrics, 0, 0);
            Button add = Theme.Button("+ New goal", true), savings = Theme.Button("Add savings", false), edit = Theme.Button("Edit selected", false), delete = Theme.Button("Delete", false); delete.ForeColor = Theme.Red;
            add.Name = "AddSavingsGoal"; savings.Name = "AddGoalSavings"; edit.Name = "EditSavingsGoal"; delete.Name = "DeleteSavingsGoal";
            t.Controls.Add(Section("Your goals", Data.Goals.Count.ToString(), edit, delete, savings, add), 0, 1);
            PagedGridPanel pager = new PagedGridPanel("GoalsPager"); DataGridView grid = pager.Grid;
            Theme.Column(grid, "name", "GOAL", 30); Theme.Column(grid, "target", "TARGET", 18); Theme.Column(grid, "saved", "SAVED", 18); Theme.Column(grid, "left", "LEFT", 18);
            Theme.Column(grid, "progress", "PROGRESS", 16); Theme.Column(grid, "date", "TARGET DATE", 19); Theme.Column(grid, "monthly", "PER MONTH", 18);
            List<GridItem> rows = new List<GridItem>();
            foreach (SavingsGoal goal in Data.Goals)
            {
                bool complete = Ledger.GoalRemaining(goal) == 0, hasDate = !String.IsNullOrEmpty(goal.TargetDate);
                string date = complete ? "Complete" : !hasDate ? "No target date" : Ledger.Day(goal.TargetDate).ToString("d MMM yyyy", CultureInfo.GetCultureInfo("en-GB"));
                if (!complete && hasDate && Ledger.Day(goal.TargetDate) < DateTime.Today) date += " · overdue";
                rows.Add(new GridItem(goal.Id, Icons.Glyph(goal.Icon) + "  " + goal.Name, Theme.MoneyText(goal.TargetAmount, goal.Currency), Theme.MoneyText(goal.SavedAmount, goal.Currency),
                    Theme.MoneyText(Ledger.GoalRemaining(goal), goal.Currency), Ledger.GoalPercent(goal) + "%", date,
                    hasDate && !complete ? Theme.MoneyText(Ledger.GoalMonthlyAmount(goal, DateTime.Today), goal.Currency) : "—") { ColorColumn = 5, Color = complete ? Theme.Teal : hasDate && Ledger.Day(goal.TargetDate) < DateTime.Today ? Theme.Red : Theme.Muted });
            }
            pager.PageChanged += delegate { goalsPageIndex = pager.PageIndex; }; int wantedPage = goalsPageIndex; pager.SetRows(rows, false); pager.PageIndex = wantedPage;
            grid.CellPainting += delegate(object sender, DataGridViewCellPaintingEventArgs e) {
                if (e.RowIndex < 0 || e.ColumnIndex != 4) return;
                e.PaintBackground(e.ClipBounds, true);
                SavingsGoal goal = Data.Goals.FirstOrDefault(g => g.Id == (string)grid.Rows[e.RowIndex].Tag); if (goal == null) return; int percent = Ledger.GoalPercent(goal);
                Rectangle bar = new Rectangle(e.CellBounds.X + 6, e.CellBounds.Y + 10, Math.Max(1, e.CellBounds.Width - 46), Math.Max(5, e.CellBounds.Height - 20));
                using (SolidBrush track = new SolidBrush(Theme.Line)) e.Graphics.FillRectangle(track, bar);
                using (SolidBrush fill = new SolidBrush(Theme.Teal)) e.Graphics.FillRectangle(fill, bar.X, bar.Y, bar.Width * percent / 100, bar.Height);
                TextRenderer.DrawText(e.Graphics, percent + "%", grid.DefaultCellStyle.Font, new Rectangle(bar.Right + 3, e.CellBounds.Y, 36, e.CellBounds.Height), Theme.Ink, TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
                e.Handled = true;
            };
            t.Controls.Add(pager, 0, 2);
            t.Controls.Add(Theme.Label("Goals track money you set aside; updating progress does not move money or change account balances. Per month divides the amount left across calendar months, including this month and the target month.", 9, false, Theme.Muted), 0, 3);
            Func<SavingsGoal> selected = delegate { Ledger.Require(grid.SelectedRows.Count > 0, "Select a savings goal first."); return Data.Goals.First(g => g.Id == (string)grid.SelectedRows[0].Tag); };
            add.Click += delegate { GoalEditor(null); }; edit.Click += delegate { ActionSafely(delegate { GoalEditor(selected()); }); };
            savings.Click += delegate { ActionSafely(delegate { GoalSavingsEditor(selected()); }); };
            grid.CellDoubleClick += delegate(object sender, DataGridViewCellEventArgs e) { if (e.RowIndex >= 0) ActionSafely(delegate { GoalEditor(selected()); }); };
            delete.Click += delegate { ActionSafely(delegate { string id = selected().Id; if (Confirm("Delete this savings goal tracker? Your accounts and transactions will be kept.")) Commit(d => d.Goals.RemoveAll(g => g.Id == id)); }); };
        }
        private void GoalEditor(SavingsGoal existing)
        {
            using (Editor f = new Editor(existing == null ? "New savings goal" : "Edit savings goal", "Set a target and enter how much you have already set aside. Add savings updates progress only; record any real transfer separately. You can edit Saved so far to adjust or withdraw an allocation."))
            {
                TextBox name = Theme.Text(existing == null ? "" : existing.Name); name.MaxLength = 80; name.Name = "GoalName";
                ComboBox icon = Icons.Picker(existing == null ? "Star" : existing.Icon);
                ComboBox currency = Strings(Ledger.Currencies.Where(c => Data.Rates.ContainsKey(c)), existing == null ? Data.BaseCurrency : existing.Currency); currency.Name = "GoalCurrency";
                currency.Enabled = existing == null || existing.SavedAmount == 0;
                NumericUpDown target = Theme.Money(existing == null ? 0 : existing.TargetAmount), saved = Theme.Money(existing == null ? 0 : existing.SavedAmount); target.Minimum = saved.Minimum = 0; target.Name = "GoalTarget"; saved.Name = "GoalSaved";
                CheckBox dated = new CheckBox { Text = "Use a target date", Checked = existing != null && !String.IsNullOrEmpty(existing.TargetDate), Dock = DockStyle.Fill, Name = "GoalUseDate" };
                DateTimePicker date = DatePicker(existing == null || String.IsNullOrEmpty(existing.TargetDate) ? DateTime.Today.AddMonths(6) : Ledger.Day(existing.TargetDate)); date.Enabled = dated.Checked; date.Name = "GoalTargetDate";
                dated.CheckedChanged += delegate { date.Enabled = dated.Checked; };
                f.Field("Goal name", name); f.Field("Icon", icon); f.Field("Currency", currency); f.Field("Target amount", target); f.Field("Saved so far", saved); f.Field("Deadline (optional)", dated); f.Field("Target date", date);
                f.Finish(delegate {
                    Commit(d => { SavingsGoal goal = existing == null ? new SavingsGoal { Id = Ledger.NewId() } : d.Goals.First(g => g.Id == existing.Id);
                        goal.Name = name.Text.Trim(); goal.Icon = Icons.Selected(icon); goal.Currency = Icons.Selected(currency); goal.TargetAmount = target.Value; goal.SavedAmount = saved.Value; goal.TargetDate = dated.Checked ? Ledger.Iso(date.Value) : null;
                        if (existing == null) d.Goals.Add(goal);
                    });
                    if (existing == null) { goalsPageIndex = Int32.MaxValue; ShowPage(4); }
                }); f.ShowDialog(this);
            }
        }
        private void GoalSavingsEditor(SavingsGoal goal)
        {
            using (Editor f = new Editor("Add savings to goal", "Enter money you have set aside for this goal. This updates its progress and does not make a transfer or reduce an account balance."))
            {
                NumericUpDown amount = Theme.Money(0); amount.Minimum = 0; amount.Name = "GoalContribution";
                f.Field("Goal", Theme.Label(goal.Name, 11, true, Theme.Ink)); f.Field("Amount (" + goal.Currency + ")", amount);
                f.Finish(delegate { Commit(d => Ledger.AddGoalSavings(d, goal.Id, amount.Value)); }); f.ShowDialog(this);
            }
        }

        private List<ExpenseSplit> SplitExpenseEditor(decimal total, string currency, List<ExpenseSplit> initial, IWin32Window owner)
        {
            Ledger.Require(Data.Categories.Count >= 2, "Add at least two expense categories before splitting a purchase.");
            List<ExpenseSplit> result = null;
            int rowCount = (Data.Categories.Count + 1) / 2;
            using (Form f = new Form { Text = "Split purchase", Name = "PurchaseSplitEditor", Width = 860, Height = 238 + rowCount * 40, StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, BackColor = Color.White, Font = Theme.Font(10, false), Padding = new Padding(20, 14, 20, 14) })
            {
                TableLayoutPanel shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Margin = new Padding(0) };
                shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 64)); shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
                shell.Controls.Add(Theme.Label("Divide this purchase between two or more categories. Amounts are in " + currency + ". Leave unused categories at zero. The assigned amounts must equal the purchase total.", 10, false, Theme.Muted), 0, 0);
                TableLayoutPanel fields = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = rowCount, Margin = new Padding(0) };
                fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30)); fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20)); fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30)); fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
                for (int row = 0; row < rowCount; row++) fields.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rowCount));
                Dictionary<string, NumericUpDown> amounts = new Dictionary<string, NumericUpDown>();
                for (int i = 0; i < Data.Categories.Count; i++)
                {
                    Category category = Data.Categories[i]; ExpenseSplit old = initial.FirstOrDefault(p => p.CategoryId == category.Id);
                    Label label = Theme.Label(Icons.Glyph(category.Icon) + "  " + category.Name, 9.5f, true, Theme.Ink); label.AutoEllipsis = true;
                    NumericUpDown amount = Theme.Money(old == null ? 0 : old.Amount); amount.Minimum = 0; amount.Name = "SplitAmount-" + category.Id; amount.Margin = new Padding(3, 6, 14, 6); amount.Font = Theme.Font(10, false);
                    fields.Controls.Add(label, i % 2 * 2, i / 2); fields.Controls.Add(amount, i % 2 * 2 + 1, i / 2); amounts.Add(category.Id, amount);
                }
                shell.Controls.Add(fields, 0, 1);
                Label summary = Theme.Label("", 11, true, Theme.Ink); summary.Name = "SplitRemainder"; shell.Controls.Add(summary, 0, 2);
                Action update = delegate { decimal assigned = amounts.Values.Sum(a => a.Value); summary.Text = "Purchase: " + Theme.MoneyText(total, currency) + "    ·    Assigned: " + Theme.MoneyText(assigned, currency) + "    ·    Left: " + Theme.MoneyText(total - assigned, currency); summary.ForeColor = assigned == total ? Theme.Teal : Theme.Red; };
                foreach (NumericUpDown amount in amounts.Values) amount.ValueChanged += delegate { update(); }; update();
                FlowLayoutPanel buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 9, 0, 0), Margin = new Padding(0) };
                Button save = Theme.Button("Use split", true), cancel = Theme.Button("Cancel", false); save.Name = "SavePurchaseSplit"; cancel.DialogResult = DialogResult.Cancel;
                buttons.Controls.Add(save); buttons.Controls.Add(cancel); shell.Controls.Add(buttons, 0, 3); f.Controls.Add(shell); f.AcceptButton = save; f.CancelButton = cancel;
                save.Click += delegate {
                    try { List<ExpenseSplit> parts = amounts.Where(p => p.Value.Value > 0).Select(p => new ExpenseSplit { CategoryId = p.Key, Amount = p.Value.Value }).ToList();
                        Ledger.ValidateExpenseParts(Data, parts, total); result = parts; f.DialogResult = DialogResult.OK; f.Close();
                    }
                    catch (BudgetException ex) { MessageBox.Show(f, ex.Message, "Check split amounts", MessageBoxButtons.OK, MessageBoxIcon.Information); }
                };
                f.ShowDialog(owner);
            }
            return result;
        }

        private void PlannedExpenseEditor(PlannedExpense existing)
        {
            if (Data.Accounts.Count == 0 || Data.Categories.Count == 0) { ActionSafely(delegate { throw new BudgetException("Add an account and at least one category first."); }); return; }
            using (Editor f = new Editor(existing == null ? "New future expense" : "Edit planned expense", "A one-off plan does not change your balance or paid spending. It stays in Future / planned until you record it as paid, even after its due date."))
            {
                TextBox description = Theme.Text(existing == null ? "" : existing.Description); description.Name = "PlanDescription";
                DateTimePicker date = DatePicker(existing == null ? DateTime.Today.AddDays(20) : Ledger.Day(existing.DueDate)); date.Name = "PlanDueDate";
                ComboBox account = AccountChoices(existing == null ? accountFilter : existing.AccountId, false), category = CategoryChoices(existing == null ? categoryFilter : existing.CategoryId, false);
                account.Name = "PlanAccount"; category.Name = "PlanCategory";
                NumericUpDown amount = Theme.Money(existing == null ? 0 : existing.Amount); amount.Minimum = 0; amount.Name = "PlanAmount";
                f.Field("Description", description); f.Field("Due date", date); f.Field("Pay from account", account); f.Field("Amount (account currency)", amount); f.Field("Expense category", category);
                f.Finish(delegate {
                    Commit(d => {
                        PlannedExpense p = existing == null ? new PlannedExpense { Id = Ledger.NewId() } : d.PlannedExpenses.First(x => x.Id == existing.Id);
                        p.Description = description.Text.Trim(); p.DueDate = Ledger.Iso(date.Value); p.AccountId = Icons.Selected(account); p.CategoryId = Icons.Selected(category); p.Amount = amount.Value;
                        if (existing == null) d.PlannedExpenses.Add(p);
                    });
                    if (existing == null) { accountFilter = categoryFilter = searchText = ""; planWhen = "All dates"; plansPageIndex = 0; }
                    showPlans = true; ShowPage(1);
                }); f.ShowDialog(this);
            }
        }
        private void RecordPlannedEditor(PlannedExpense plan)
        {
            using (Editor f = new Editor("Record planned expense as paid", "Record this only after payment. Saving adds one actual expense and removes the plan. Choose the date you paid; an overdue plan is not backdated automatically."))
            {
                Account account = Data.Accounts.First(a => a.Id == plan.AccountId);
                DateTimePicker date = DatePicker(DateTime.Today); date.MaxDate = DateTime.Today; date.Name = "ActualPaymentDate";
                f.Field("Description", Theme.Label(plan.Description, 10, false, Theme.Ink));
                f.Field("Account", Theme.Label(account.Name, 10, false, Theme.Ink));
                f.Field("Amount", Theme.Label(Theme.MoneyText(plan.Amount, account.Currency), 12, true, Theme.Ink));
                f.Field("Paid on", date);
                f.Finish(delegate { Commit(d => Ledger.RecordPlanned(d, plan.Id, date.Value, DateTime.Today)); status.Text = "One-off expense recorded on " + date.Value.ToString("d MMM yyyy") + ". View it in Recorded entries."; });
                f.Save.Text = "Record paid"; f.Save.Width = 125; f.ShowDialog(this);
            }
        }

        private void AccountEditor(Account existing)
        {
            if (existing == null && Data.Accounts.Count >= 20) { ActionSafely(delegate { throw new BudgetException("You already have 20 accounts."); }); return; }
            using (Editor f = new Editor(existing == null ? "Add account" : "Edit account", "Enter the balance at the START of the chosen date, before entries you add for that date. Earlier entries are blocked. Click any account card to edit it."))
            {
                TextBox name = Theme.Text(existing == null ? "" : existing.Name); name.MaxLength = 60;
                ComboBox icon = Icons.Picker(existing == null ? "Bank" : existing.Icon);
                ComboBox currency = Strings(Ledger.Currencies, existing == null ? Data.BaseCurrency : existing.Currency);
                currency.Enabled = existing == null || !Data.Entries.Any(e => e.AccountId == existing.Id || e.ToAccountId == existing.Id) && !Data.Bills.Any(b => b.AccountId == existing.Id) && !Data.PlannedExpenses.Any(p => p.AccountId == existing.Id) && !Data.Templates.Any(t => t.AccountId == existing.Id);
                NumericUpDown balance = Theme.Money(existing == null ? 0 : existing.OpeningBalance);
                DateTimePicker date = DatePicker(existing == null ? DateTime.Today : Ledger.Day(existing.OpeningDate)); date.MaxDate = DateTime.Today;
                NumericUpDown rate = Theme.Money(1); rate.DecimalPlaces = 6; rate.Minimum = 0; rate.Maximum = 1000000m;
                NumericUpDown position = Theme.Money(existing == null ? Data.Accounts.Count + 1 : Data.Accounts.FindIndex(a => a.Id == existing.Id) + 1);
                position.DecimalPlaces = 0; position.ThousandsSeparator = false; position.Minimum = 1; position.Maximum = existing == null ? Data.Accounts.Count + 1 : Data.Accounts.Count;
                Action setRate = delegate { string code = Icons.Selected(currency); rate.Value = Data.Rates.ContainsKey(code) ? Data.Rates[code] : 0; rate.Enabled = code != "CZK"; };
                currency.SelectedIndexChanged += delegate { setRate(); }; setRate();
                f.Field("Account name", name); f.Field("Display position", position); f.Field("Icon", icon); f.Field("Currency", currency); f.Field("Opening balance", balance); f.Field("Starting date", date); f.Field("1 currency unit = CZK", rate);
                if (existing != null) f.ExtraButton("Delete account", delegate {
                    if (!Confirm("Delete " + existing.Name + "? This is allowed only when it has no entries, bills or plans.")) throw new BudgetException("Account kept.");
                    Commit(d => Ledger.DeleteAccount(d, existing.Id));
                });
                f.Finish(delegate {
                    Ledger.Require(rate.Value > 0, "Enter a positive exchange rate. Example: if one EUR is worth 25 CZK, enter 25.");
                    Commit(d => {
                        string code = Icons.Selected(currency); d.Rates[code] = rate.Value;
                        Account a = existing == null ? new Account { Id = Ledger.NewId() } : d.Accounts.First(x => x.Id == existing.Id);
                        a.Name = name.Text.Trim(); a.Icon = Icons.Selected(icon); a.Currency = code; a.OpeningBalance = balance.Value; a.OpeningDate = Ledger.Iso(date.Value);
                        if (existing == null) Ledger.AddAccount(d, a);
                        Ledger.MoveAccount(d, a.Id, (int)position.Value - 1);
                    });
                }); f.ShowDialog(this);
            }
        }

        private void EntryEditor(Entry existing, string initialKind)
        {
            if (Data.Accounts.Count == 0) { ActionSafely(delegate { throw new BudgetException("Add an account on the first tab, then record your first entry."); }); return; }
            if ((existing == null ? initialKind : existing.Kind) == "Transfer" && Data.Accounts.Count < 2) { ActionSafely(delegate { throw new BudgetException("Add at least two accounts to record a transfer."); }); return; }
            Entry seed = existing ?? newEntryPreset;
            using (Editor f = new Editor(existing == null ? "New " + initialKind.ToLowerInvariant() : "Edit entry", "Known payees suggest an account and category until you choose your own. Split amounts must match the total. Transfers do not count as spending; enter the amount actually received for a currency exchange.", 44))
            {
                ComboBox kind = Strings(new[] { "Expense", "Income", "Transfer" }, seed == null ? initialKind : seed.Kind);
                if (existing != null && !String.IsNullOrEmpty(existing.PlanId)) kind.Enabled = false;
                DateTimePicker date = DatePicker(existing == null ? DateTime.Today : Ledger.Day(existing.Date)); date.MaxDate = DateTime.Today;
                TextBox description = Theme.Text(seed == null ? "" : seed.Description);
                ComboBox payee = PayeeBox(seed == null ? "" : seed.Payee); payee.Name = "EntryPayee";
                ComboBox account = AccountChoices(seed == null ? accountFilter : seed.AccountId, false);
                ComboBox category = CategoryChoices(seed == null ? categoryFilter : seed.CategoryId, false);
                ComboBox destination = AccountChoices(existing == null ? "" : existing.ToAccountId, false);
                if (existing == null && destination.Items.Count > 1) destination.SelectedIndex = 1;
                NumericUpDown amount = Theme.Money(seed == null ? 0 : seed.Amount); amount.Minimum = 0;
                NumericUpDown received = Theme.Money(existing == null ? 0 : existing.ReceivedAmount); received.Minimum = 0;
                List<ExpenseSplit> parts = seed != null && Ledger.HasSplits(seed) ? seed.Splits.Select(p => new ExpenseSplit { CategoryId = p.CategoryId, Amount = p.Amount }).ToList() : new List<ExpenseSplit>();
                FlowLayoutPanel splitControls = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = new Padding(0), WrapContents = false, Padding = new Padding(0, 4, 0, 0) };
                Button split = Theme.Button("Split purchase…", false), single = Theme.Button("One category", false); split.Name = "EditPurchaseSplit"; single.Name = "ClearPurchaseSplit"; split.Width = 154; single.Width = 130; split.Height = single.Height = 32;
                splitControls.Controls.Add(split); splitControls.Controls.Add(single);
                bool initializing = true, accountTouched = false, categoryTouched = false, applyingDefaults = false;
                Action configure = delegate {
                    bool isTransfer = Icons.Selected(kind) == "Transfer";
                    bool isExpense = Icons.Selected(kind) == "Expense"; payee.Enabled = !isTransfer;
                    category.Enabled = isExpense && parts.Count == 0; destination.Enabled = isTransfer;
                    split.Enabled = isExpense; single.Visible = isExpense && parts.Count > 0; split.Text = parts.Count > 0 ? "Edit split (" + parts.Count + ")" : "Split purchase…";
                    Account a = Data.Accounts.FirstOrDefault(x => x.Id == Icons.Selected(account));
                    Account to = Data.Accounts.FirstOrDefault(x => x.Id == Icons.Selected(destination));
                    bool same = a != null && to != null && a.Currency == to.Currency;
                    received.Enabled = isTransfer && !same;
                    if (isTransfer && same) received.Value = amount.Value;
                };
                kind.SelectedIndexChanged += delegate { configure(); }; account.SelectedIndexChanged += delegate { configure(); };
                destination.SelectedIndexChanged += delegate { configure(); }; amount.ValueChanged += delegate { configure(); }; configure();
                split.Click += delegate {
                    try {
                        Ledger.Require(amount.Value > 0, "Enter the full purchase amount before splitting it.");
                        Account selected = Data.Accounts.First(a => a.Id == Icons.Selected(account));
                        List<ExpenseSplit> initial = parts.Count > 0 ? parts : new List<ExpenseSplit> { new ExpenseSplit { CategoryId = Icons.Selected(category), Amount = amount.Value } };
                        List<ExpenseSplit> edited = SplitExpenseEditor(amount.Value, selected.Currency, initial, f); if (edited != null) parts = edited; configure();
                    }
                    catch (BudgetException ex) { MessageBox.Show(f, ex.Message, "Split purchase", MessageBoxButtons.OK, MessageBoxIcon.Information); }
                };
                single.Click += delegate { parts = new List<ExpenseSplit>(); configure(); };
                account.SelectedIndexChanged += delegate { if (!initializing && !applyingDefaults) accountTouched = true; };
                category.SelectedIndexChanged += delegate { if (!initializing && !applyingDefaults) categoryTouched = true; };
                account.SelectionChangeCommitted += delegate { if (!initializing && !applyingDefaults) accountTouched = true; };
                category.SelectionChangeCommitted += delegate { if (!initializing && !applyingDefaults) categoryTouched = true; };
                payee.TextChanged += delegate {
                    if (initializing || seed != null || Icons.Selected(kind) == "Transfer") return;
                    PayeeProfile profile = Ledger.FindPayee(Data, payee.Text); if (profile == null) return;
                    applyingDefaults = true;
                    try { if (!accountTouched && !String.IsNullOrEmpty(profile.DefaultAccountId)) Icons.Choose(account, profile.DefaultAccountId);
                        if (!categoryTouched && parts.Count == 0 && Icons.Selected(kind) == "Expense" && !String.IsNullOrEmpty(profile.DefaultCategoryId)) Icons.Choose(category, profile.DefaultCategoryId); }
                    finally { applyingDefaults = false; }
                };
                f.Field("Type", kind); f.Field("Date", date); f.Field("Payee / shop", payee); f.Field("Description", description); f.Field("Account / send from", account);
                f.Field("Amount (account currency)", amount); f.Field("Expense category", category); f.Field("Split purchase", splitControls); f.Field("Transfer to", destination); f.Field("Received (target currency)", received);
                f.Finish(delegate {
                    Commit(d => {
                        Entry e = existing == null ? new Entry { Id = Ledger.NewId() } : d.Entries.First(x => x.Id == existing.Id);
                        string accountId = Icons.Selected(account);
                        decimal fx = existing != null && existing.AccountId == accountId ? existing.RateToCzk : Ledger.Rate(d, d.Accounts.First(x => x.Id == accountId).Currency);
                        e.Kind = Icons.Selected(kind); e.Payee = e.Kind == "Transfer" ? null : payee.Text.Trim(); e.Date = Ledger.Iso(date.Value); e.Description = description.Text.Trim(); e.AccountId = accountId;
                        e.Splits = e.Kind == "Expense" && parts.Count > 0 ? parts.Select(p => new ExpenseSplit { CategoryId = p.CategoryId, Amount = p.Amount }).ToList() : null;
                        e.CategoryId = e.Kind == "Expense" && !Ledger.HasSplits(e) ? Icons.Selected(category) : null; e.Amount = amount.Value;
                        e.ToAccountId = e.Kind == "Transfer" ? Icons.Selected(destination) : null; e.ReceivedAmount = e.Kind == "Transfer" ? received.Value : 0;
                        e.RateToCzk = fx; if (existing == null) d.Entries.Add(e);
                        if (e.Kind != "Transfer") Ledger.RememberPayee(d, e.Payee, e.AccountId, e.CategoryId);
                    });
                    if (pageIndex == 1 && (existing == null || showPlans))
                    {
                        showPlans = false; allHistory = false; selectedMonth = new DateTime(date.Value.Year, date.Value.Month, 1);
                        accountFilter = categoryFilter = searchText = ""; kindFilter = "All types"; entriesPageIndex = 0; ShowPage(1);
                    }
                });
                Button saveTemplate = f.AuxiliaryButton("Save as template", delegate {
                    bool isExpense = Icons.Selected(kind) == "Expense";
                    Entry draft = new Entry { Kind = Icons.Selected(kind), Payee = payee.Text.Trim(), Description = description.Text.Trim(), AccountId = Icons.Selected(account),
                        CategoryId = isExpense && parts.Count == 0 ? Icons.Selected(category) : null, Amount = amount.Value,
                        Splits = isExpense && parts.Count > 0 ? parts.Select(p => new ExpenseSplit { CategoryId = p.CategoryId, Amount = p.Amount }).ToList() : null };
                    TemplateEditor(null, Ledger.TemplateFromEntry(draft), f);
                }); saveTemplate.Name = "SaveAsTemplate"; saveTemplate.Enabled = Icons.Selected(kind) != "Transfer";
                kind.SelectedIndexChanged += delegate { saveTemplate.Enabled = Icons.Selected(kind) != "Transfer"; };
                initializing = false; f.ShowDialog(this);
            }
        }

        private void BillEditor(RecurringBill existing)
        {
            if (Data.Accounts.Count == 0 || Data.Categories.Count == 0) { ActionSafely(delegate { throw new BudgetException("Add an account and at least one category first."); }); return; }
            if (existing == null && Data.Bills.Count >= 20) { ActionSafely(delegate { throw new BudgetException("You already have 20 recurring payments."); }); return; }
            using (Editor f = new Editor(existing == null ? "Add recurring payment" : "Edit recurring payment", "Automatic payments are recorded when due, including missed dates when you reopen the app. Choose the NEXT unpaid date to avoid entering old payments twice. Past entries stay unchanged."))
            {
                TextBox name = Theme.Text(existing == null ? "" : existing.Name); name.MaxLength = 80;
                ComboBox icon = Icons.Picker(existing == null ? "Bolt" : existing.Icon);
                ComboBox account = AccountChoices(existing == null ? "" : existing.AccountId, false);
                Category defaultCategory = Data.Categories.FirstOrDefault(c => c.Name.Equals("Bills", StringComparison.OrdinalIgnoreCase)) ?? Data.Categories.First();
                ComboBox category = CategoryChoices(existing == null ? defaultCategory.Id : existing.CategoryId, false);
                NumericUpDown amount = Theme.Money(existing == null ? 0 : existing.Amount); amount.Minimum = 0;
                DateTimePicker date = DatePicker(existing == null ? DateTime.Today.AddDays(1) : Ledger.DueDate(existing));
                FlowLayoutPanel intervalPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = new Padding(0), WrapContents = false };
                NumericUpDown interval = Theme.Money(existing == null ? 1 : existing.Interval); interval.DecimalPlaces = 0; interval.Minimum = 1; interval.Maximum = 120; interval.Dock = DockStyle.None; interval.Width = 80;
                ComboBox unit = Strings(Ledger.Units, existing == null ? "Months" : existing.Unit); unit.Dock = DockStyle.None; unit.Width = 175;
                intervalPanel.Controls.Add(interval); intervalPanel.Controls.Add(unit);
                CheckBox automatic = new CheckBox { Text = "Record automatically when due", Checked = existing == null || existing.AutoRecord, Dock = DockStyle.Fill };
                CheckBox active = new CheckBox { Text = "Schedule is active", Checked = existing == null || existing.Enabled, Dock = DockStyle.Fill };
                f.Field("Payment name", name); f.Field("Icon", icon); f.Field("Pay from", account); f.Field("Category", category); f.Field("Amount (account currency)", amount);
                f.Field("Next unpaid date", date); f.Field("Repeat every", intervalPanel); f.Field("Automatic recording", automatic); f.Field("Status", active);
                if (existing != null) f.ExtraButton("Delete schedule", delegate {
                    if (!Confirm("Delete this schedule? Previously recorded expenses will remain.")) throw new BudgetException("Schedule kept.");
                    Commit(d => d.Bills.RemoveAll(x => x.Id == existing.Id));
                });
                f.Finish(delegate {
                    Commit(d => {
                        RecurringBill b = existing == null ? new RecurringBill { Id = Ledger.NewId() } : d.Bills.First(x => x.Id == existing.Id);
                        string next = Ledger.Iso(date.Value);
                        bool scheduleChanged = existing == null || next != Ledger.Iso(Ledger.DueDate(b)) || b.Interval != (int)interval.Value || b.Unit != Icons.Selected(unit);
                        b.Name = name.Text.Trim(); b.Icon = Icons.Selected(icon); b.AccountId = Icons.Selected(account); b.CategoryId = Icons.Selected(category); b.Amount = amount.Value;
                        if (scheduleChanged) { b.AnchorDate = next; b.NextIndex = 0; }
                        b.Interval = (int)interval.Value; b.Unit = Icons.Selected(unit); b.AutoRecord = automatic.Checked; b.Enabled = active.Checked;
                        if (existing == null) Ledger.AddBill(d, b);
                        Ledger.Validate(d); Ledger.PostDue(d, DateTime.Today);
                    });
                }); f.ShowDialog(this);
            }
        }

        private void Categories()
        {
            using (Form f = new Form { Text = "Expense categories", Width = 760, Height = 590, MinimumSize = new Size(650, 440), StartPosition = FormStartPosition.CenterParent,
                Font = Theme.Font(10, false), BackColor = Theme.Canvas, Padding = new Padding(20) })
            {
                TableLayoutPanel t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
                t.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); t.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); t.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
                t.Controls.Add(Theme.Label("Up to 20 categories. Optional budgets apply to each month.", 11, true, Theme.Ink), 0, 0);
                PagedGridPanel categoryPager = new PagedGridPanel("CategoryPager"); DataGridView g = categoryPager.Grid; Theme.Column(g, "name", "CATEGORY", 55); Theme.Column(g, "budget", "MONTHLY BUDGET", 45); t.Controls.Add(categoryPager, 0, 1);
                Action fill = delegate {
                    categoryPager.SetRows(Data.Categories.Select(c => new GridItem(c.Id, Icons.Glyph(c.Icon) + "  " + c.Name, c.MonthlyBudgetCzk > 0 ? Theme.MoneyText(c.MonthlyBudgetCzk / Ledger.Rate(Data, Data.BaseCurrency), Data.BaseCurrency) : "No budget set")), false);
                }; fill();
                FlowLayoutPanel buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 9, 0, 0) };
                Button add = Theme.Button("+ Category", true), edit = Theme.Button("Edit selected", false), close = Theme.Button("Close", false);
                buttons.Controls.Add(add); buttons.Controls.Add(edit); buttons.Controls.Add(close); t.Controls.Add(buttons, 0, 2); f.Controls.Add(t);
                add.Click += delegate { CategoryEditor(null, f); fill(); };
                Action open = delegate { if (g.SelectedRows.Count > 0) { string id = (string)g.SelectedRows[0].Tag; CategoryEditor(Data.Categories.First(x => x.Id == id), f); fill(); } };
                edit.Click += delegate { open(); }; g.CellDoubleClick += delegate(object sender, DataGridViewCellEventArgs e) { if (e.RowIndex >= 0) open(); };
                close.Click += delegate { f.Close(); }; f.ShowDialog(this);
            }
        }
        private void CategoryEditor(Category existing, IWin32Window owner)
        {
            if (existing == null && Data.Categories.Count >= 20) { ActionSafely(delegate { throw new BudgetException("You already have 20 categories."); }); return; }
            using (Editor f = new Editor(existing == null ? "Add category" : "Edit category", "Give this category a name and an icon. A monthly budget is optional: leave it at zero to track spending without a limit."))
            {
                TextBox name = Theme.Text(existing == null ? "" : existing.Name); name.MaxLength = 60;
                ComboBox icon = Icons.Picker(existing == null ? "Other" : existing.Icon);
                NumericUpDown budget = Theme.Money(existing == null ? 0 : Decimal.Round(existing.MonthlyBudgetCzk / Ledger.Rate(Data, Data.BaseCurrency), 2)); budget.Minimum = 0;
                f.Field("Category name", name); f.Field("Icon", icon); f.Field("Monthly budget (" + Data.BaseCurrency + ")", budget);
                if (existing != null) f.ExtraButton("Delete category", delegate {
                    if (!Confirm("Delete " + existing.Name + "? Categories in use cannot be deleted.")) throw new BudgetException("Category kept.");
                    Commit(d => Ledger.DeleteCategory(d, existing.Id));
                });
                f.Finish(delegate { Commit(d => {
                    Category c = existing == null ? new Category { Id = Ledger.NewId() } : d.Categories.First(x => x.Id == existing.Id);
                    c.Name = name.Text.Trim(); c.Icon = Icons.Selected(icon); c.MonthlyBudgetCzk = Decimal.Round(budget.Value * Ledger.Rate(d, d.BaseCurrency), 2);
                    if (existing == null) Ledger.AddCategory(d, c);
                }); }); f.ShowDialog(owner);
            }
        }

        private void ExportCsv()
        {
            bool plans = pageIndex == 1 && showPlans, history = pageIndex == 1 && !showPlans && allHistory;
            ActionSafely(delegate { using (SaveFileDialog f = new SaveFileDialog { Filter = "CSV file (*.csv)|*.csv", FileName = plans ? "BudgetDesk-planned-expenses.csv" : history ? "BudgetDesk-history-results.csv" : "BudgetDesk-" + selectedMonth.ToString("yyyy-MM") + ".csv" })
                if (f.ShowDialog(this) == DialogResult.OK) { File.WriteAllText(f.FileName, plans ? BudgetStore.PlansCsv(Data) : history ? BudgetStore.SearchCsv(Data, BuildEntrySearch()) : BudgetStore.Csv(Data, selectedMonth), new UTF8Encoding(true)); status.Text = plans ? "All pending one-off plans exported, irrespective of filters." : history ? "All matching history entries exported, across every page." : "Monthly CSV exported. It includes all entries in the selected month."; }
            });
        }
        private void ExportYearCsv()
        {
            ActionSafely(delegate { using (SaveFileDialog f = new SaveFileDialog { Filter = "CSV file (*.csv)|*.csv", FileName = "BudgetDesk-" + selectedYear + ".csv" })
                if (f.ShowDialog(this) == DialogResult.OK) { File.WriteAllText(f.FileName, BudgetStore.YearCsv(Data, selectedYear), new UTF8Encoding(true)); status.Text = "Year CSV exported. It includes all recorded entries in " + selectedYear + "; unpaid plans are excluded."; }
            });
        }
        private void Settings()
        {
            using (Editor f = new Editor("Settings & backup", "All data stays on this computer. There is no bank connection. Enter your own exchange rates: 1 unit of each currency is worth the number of CZK below. Zero means unused. Demo rates are sample figures."))
            {
                ComboBox currency = Strings(Ledger.Currencies, Data.BaseCurrency); f.Field("Reporting currency", currency);
                Dictionary<string, NumericUpDown> rates = new Dictionary<string, NumericUpDown>();
                foreach (string code in Ledger.Currencies.Where(x => x != "CZK"))
                {
                    NumericUpDown n = Theme.Money(Data.Rates.ContainsKey(code) ? Data.Rates[code] : 0); n.Minimum = 0; n.Maximum = 1000000; n.DecimalPlaces = 6;
                    rates.Add(code, n); f.Field("1 " + code + " = CZK", n);
                }
                Label explanation = Theme.Label("New entries keep these rates. Balances use current rates.\nExisting category budgets stay in the same CZK value.", 9, false, Theme.Muted);
                f.Field("Rate behavior", explanation);
                FlowLayoutPanel backup = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = new Padding(0) };
                Button export = Theme.Button("Export backup", false), restore = Theme.Button("Restore backup", false); export.Width = 143; restore.Width = 143;
                backup.Controls.Add(export); backup.Controls.Add(restore); f.Field("JSON backups", backup);
                Button folder = Theme.Button("Open data folder", false); folder.Width = 180;
                folder.Click += delegate { ActionSafely(delegate { Directory.CreateDirectory(Store.Folder); Process.Start(new ProcessStartInfo { FileName = Store.Folder, UseShellExecute = true }); }); }; f.Field("Automatic backups", folder);
                Button openDemo = Theme.Button(demo ? "Demo is already open" : "Open a demo window", false); openDemo.Width = 210; openDemo.Enabled = !demo;
                openDemo.Click += delegate { ActionSafely(delegate { Process.Start(new ProcessStartInfo { FileName = Application.ExecutablePath, Arguments = "--demo", UseShellExecute = true }); }); }; f.Field("Try sample data", openDemo);
                export.Click += delegate { ActionSafely(delegate {
                    using (SaveFileDialog dialog = new SaveFileDialog { Filter = "Budget Desk backup (*.json)|*.json", FileName = "BudgetDesk-backup-" + DateTime.Today.ToString("yyyy-MM-dd") + ".json" })
                        if (dialog.ShowDialog(f) == DialogResult.OK) { BudgetStore.ExportBackup(Data, dialog.FileName); status.Text = "Backup exported."; }
                }); };
                restore.Click += delegate { ActionSafely(delegate {
                    using (OpenFileDialog dialog = new OpenFileDialog { Filter = "Budget Desk backup (*.json)|*.json" })
                    {
                        if (dialog.ShowDialog(f) != DialogResult.OK) return;
                        BudgetData restored = BudgetStore.ReadBackup(dialog.FileName);
                        if (!Confirm("Replace the current budget with this backup?\n\n" + restored.Accounts.Count + " accounts, " + restored.Entries.Count + " entries, " + restored.Bills.Count + " recurring payments, " + restored.PlannedExpenses.Count + " plans and " + restored.Goals.Count + " goals, " + restored.Templates.Count + " templates and " + restored.Payees.Count + " payees.\nYour current data will be kept in the automatic Backups folder.")) return;
                        Store.Save(restored); Data = restored; ShowPage(pageIndex); status.Text = "Backup restored. Automatic due payments will be checked when this window closes."; f.Close();
                    }
                }); };
                f.Finish(delegate {
                    Commit(d => {
                        d.BaseCurrency = Icons.Selected(currency);
                        foreach (var pair in rates) { if (pair.Value.Value > 0) d.Rates[pair.Key] = pair.Value.Value; else d.Rates.Remove(pair.Key); }
                    });
                }); f.ShowDialog(this); CheckDue(false);
            }
        }
    }
}
