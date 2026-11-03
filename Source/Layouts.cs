using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BudgetDesk
{
    // This page uses viewport coordinates rather than mutable table row heights.
    // A minimized window never changes its card geometry.
    internal sealed class DashboardPanel : Panel
    {
        internal Panel AccountHeader, AccountCards, Total, BillHeader, BillCards;
        internal Label Hint;
        internal int AccountCount, BillCount;
        private bool arranging;

        internal void Reflow()
        {
            Form owner = FindForm();
            if (arranging || IsDisposed || Disposing || owner != null && owner.WindowState == FormWindowState.Minimized || AccountCards == null || BillCards == null || ClientSize.Width < 600 || ClientSize.Height < 300) return;
            arranging = true;
            try
            {
                int width = ClientSize.Width, height = ClientSize.Height;
                int normalColumns = Math.Max(1, Math.Min(5, (width + 8) / 228));
                int accountRows = (AccountCount + normalColumns - 1) / normalColumns;
                int billRows = (BillCount + normalColumns - 1) / normalColumns;
                int accountHeight = AccountCount == 0 ? 68 : accountRows * 58 + Math.Max(0, accountRows - 1) * 8;
                int billHeight = BillCount == 0 ? 68 : billRows * 64 + Math.Max(0, billRows - 1) * 8;
                bool compact = accountHeight + billHeight + 168 > height;
                int gap = compact ? 3 : 8;
                int columns = compact ? Math.Max(1, Math.Min(5, (width + gap) / 180)) : normalColumns;
                accountRows = (AccountCount + columns - 1) / columns;
                billRows = (BillCount + columns - 1) / columns;
                int accountCardHeight = compact ? 40 : 58, billCardHeight = compact ? 54 : 64;
                accountHeight = AccountCount == 0 ? 52 : accountRows * accountCardHeight + Math.Max(0, accountRows - 1) * gap;
                billHeight = BillCount == 0 ? 52 : billRows * billCardHeight + Math.Max(0, billRows - 1) * gap;
                int headerHeight = compact ? 34 : 42, totalHeight = compact ? 26 : 40, hintHeight = compact ? 14 : 20;
                FitHeader(AccountHeader, compact); FitHeader(BillHeader, compact);
                int y = 0;
                AccountHeader.SetBounds(0, y, width, headerHeight); y += headerHeight;
                AccountCards.SetBounds(0, y, width, accountHeight); y += accountHeight + gap;
                Total.SetBounds(0, y, width, totalHeight); y += totalHeight + gap;
                BillHeader.SetBounds(0, y, width, headerHeight); y += headerHeight;
                BillCards.SetBounds(0, y, width, billHeight); y += billHeight + gap;
                Hint.SetBounds(0, Math.Min(y, height - hintHeight), width, hintHeight);
                PlaceCards(AccountCards, AccountCount, columns, gap, accountCardHeight);
                PlaceCards(BillCards, BillCount, columns, gap, billCardHeight);
            }
            finally { arranging = false; }
        }
        private static void FitHeader(Panel header, bool compact)
        {
            foreach (Control control in header.Controls)
                if (control is FlowLayoutPanel)
                    foreach (Button button in control.Controls.OfType<Button>())
                    {
                        button.Height = compact ? 30 : 34;
                        float size = compact ? 9 : 10;
                        if (button.Font.Size != size) { Font prior = button.Font; button.Font = Theme.Font(size, true); prior.Dispose(); }
                        button.Margin = new Padding(4, 2, 0, 2);
                    }
        }
        private static void PlaceCards(Panel host, int count, int columns, int gap, int cardHeight)
        {
            if (count == 0) return;
            int cardWidth = Math.Min(240, Math.Max(1, (host.ClientSize.Width - (columns - 1) * gap) / columns));
            for (int i = 0; i < host.Controls.Count; i++)
                host.Controls[i].SetBounds(i % columns * (cardWidth + gap), i / columns * (cardHeight + gap), cardWidth, cardHeight);
        }
        protected override void OnLayout(LayoutEventArgs e) { base.OnLayout(e); Reflow(); }
    }

    internal sealed class GridItem
    {
        internal object[] Cells;
        internal string Id;
        internal int ColorColumn = -1;
        internal Color Color = Color.Empty;
        internal GridItem(string id, params object[] cells) { Id = id; Cells = cells; }
    }

    // Only the rows that fit are materialized into the grid. Every other row is
    // available through page buttons; removing scrollbars never hides history.
    internal sealed class PagedGridPanel : Panel
    {
        internal readonly DataGridView Grid;
        private readonly Button previous, next;
        private readonly Label info;
        private List<GridItem> items = new List<GridItem>();
        private bool rendering;
        private int capacity = 1, pageIndex;
        internal event EventHandler PageChanged;
        internal int PageIndex { get { return pageIndex; } set { pageIndex = Math.Max(0, value); RenderRows(); if (PageChanged != null) PageChanged(this, EventArgs.Empty); } }
        internal int PageCount { get { return Math.Max(1, (items.Count + capacity - 1) / capacity); } }
        internal int ItemCount { get { return items.Count; } }

        internal PagedGridPanel(string name)
        {
            Name = name; Dock = DockStyle.Fill; Margin = new Padding(0); BackColor = Color.White;
            Grid = Theme.Grid(); Grid.Name = name + "Grid"; Grid.Dock = DockStyle.None;
            previous = Theme.Button("Previous", false); next = Theme.Button("Next", false);
            previous.Name = name + "Previous"; next.Name = name + "Next";
            previous.Margin = next.Margin = new Padding(0);
            info = Theme.Label("", 9, false, Theme.Muted); info.Dock = DockStyle.None;
            Controls.Add(Grid); Controls.Add(info); Controls.Add(previous); Controls.Add(next);
            previous.Click += delegate { MovePage(-1); }; next.Click += delegate { MovePage(1); };
            Grid.KeyDown += delegate(object sender, KeyEventArgs e) {
                if (e.KeyCode == Keys.PageDown || e.KeyCode == Keys.PageUp) { MovePage(e.KeyCode == Keys.PageDown ? 1 : -1); e.Handled = true; }
            };
        }
        private bool Minimized { get { Form owner = FindForm(); return owner != null && owner.WindowState == FormWindowState.Minimized; } }
        internal void SetRows(IEnumerable<GridItem> rows, bool resetPage)
        { items = rows.ToList(); if (resetPage) pageIndex = 0; RenderRows(); }
        private void MovePage(int amount)
        {
            pageIndex = Math.Max(0, Math.Min(PageCount - 1, pageIndex + amount)); RenderRows();
            if (PageChanged != null) PageChanged(this, EventArgs.Empty);
        }
        internal void Reflow()
        {
            if (Grid == null || rendering || IsDisposed || Minimized) return;
            Grid.SetBounds(0, 0, ClientSize.Width, Math.Max(0, ClientSize.Height - 34));
            info.SetBounds(8, Math.Max(0, ClientSize.Height - 32), Math.Max(0, ClientSize.Width - 214), 30);
            previous.SetBounds(Math.Max(0, ClientSize.Width - 198), Math.Max(0, ClientSize.Height - 29), 94, 26);
            next.SetBounds(Math.Max(0, ClientSize.Width - 98), Math.Max(0, ClientSize.Height - 29), 94, 26);
            int fit = Math.Max(1, (Grid.ClientSize.Height - Grid.ColumnHeadersHeight - 2) / Math.Max(1, Grid.RowTemplate.Height));
            if (fit != capacity) { capacity = fit; RenderRows(); }
        }
        private void RenderRows()
        {
            if (rendering || Grid == null || Grid.Columns.Count == 0 || IsDisposed || Minimized) return;
            int priorPage = pageIndex;
            rendering = true;
            try
            {
                pageIndex = Math.Max(0, Math.Min(PageCount - 1, pageIndex));
                Grid.Rows.Clear();
                foreach (GridItem item in items.Skip(pageIndex * capacity).Take(capacity))
                {
                    int row = Grid.Rows.Add(item.Cells); Grid.Rows[row].Tag = item.Id;
                    if (item.ColorColumn >= 0 && !item.Color.IsEmpty) Grid.Rows[row].Cells[item.ColorColumn].Style.ForeColor = item.Color;
                }
                int first = items.Count == 0 ? 0 : pageIndex * capacity + 1;
                int last = Math.Min(items.Count, (pageIndex + 1) * capacity);
                info.Text = "Page " + (pageIndex + 1) + " / " + PageCount + "    " + first + "–" + last + " of " + items.Count;
                previous.Enabled = pageIndex > 0; next.Enabled = pageIndex + 1 < PageCount;
            }
            finally { rendering = false; }
            if (priorPage != pageIndex && PageChanged != null) PageChanged(this, EventArgs.Empty);
        }
        protected override void OnLayout(LayoutEventArgs e) { base.OnLayout(e); Reflow(); }
    }
}
