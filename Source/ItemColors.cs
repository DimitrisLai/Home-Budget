using System;
using System.Drawing;
using System.Windows.Forms;

namespace BudgetDesk
{
    // Index zero uses the theme. Other indices are stable in saved budgets.
    internal static class ItemPalette
    {
        internal struct Swatch
        {
            internal readonly string Name;
            internal readonly Color Color;
            internal Swatch(string name, int rgb)
            {
                Name = name;
                Color = System.Drawing.Color.FromArgb((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);
            }
        }
        internal static readonly Swatch[] Colors = {
            new Swatch("Default color", 0x000000),
            new Swatch("Burgundy", 0x87263F),
            new Swatch("Red", 0xC73842),
            new Swatch("Coral", 0xE76E61),
            new Swatch("Pink", 0xED97B4),
            new Swatch("Magenta", 0xB52D82),
            new Swatch("Plum", 0x82416F),
            new Swatch("Violet", 0x794EAF),
            new Swatch("Lavender", 0xBCA5DB),
            new Swatch("Brown", 0x855536),
            new Swatch("Rust", 0xB2532A),
            new Swatch("Orange", 0xEB812A),
            new Swatch("Peach", 0xF3B38C),
            new Swatch("Amber", 0xD79924),
            new Swatch("Yellow", 0xF2D53F),
            new Swatch("Olive", 0x7E842D),
            new Swatch("Lime", 0xA7C944),
            new Swatch("Forest", 0x276441),
            new Swatch("Green", 0x39854E),
            new Swatch("Mint", 0x91D8B0),
            new Swatch("Teal", 0x147A78),
            new Swatch("Turquoise", 0x39BDB0),
            new Swatch("Cyan", 0x50CBDF),
            new Swatch("Sky blue", 0x89BFE9),
            new Swatch("Blue", 0x3475CC),
            new Swatch("Navy", 0x294573),
            new Swatch("Indigo", 0x54529C),
            new Swatch("Sand", 0xD8BE95),
            new Swatch("White", 0xFFFFFF),
            new Swatch("Silver", 0xBAC4CC),
            new Swatch("Gray", 0x858A90),
            new Swatch("Slate", 0x526578),
            new Swatch("Black", 0x212329),
        };

        internal static Color Surface(int index, bool selected)
        {
            if (index == 0) return selected ? Theme.Pale : Theme.Surface;
            Color basis = selected ? Theme.Pale : Theme.Surface;
            Color accent = Colors[index].Color;
            int percent = selected ? 16 : 14;
            return Color.FromArgb((basis.R * (100 - percent) + accent.R * percent) / 100,
                (basis.G * (100 - percent) + accent.G * percent) / 100,
                (basis.B * (100 - percent) + accent.B * percent) / 100);
        }

        internal static Color DetailColor(int index, Color semantic)
        {
            if (index == 0) return semantic;
            if (semantic == Theme.Muted) return Theme.Dark ? Color.FromArgb(214, 225, 234) : Color.FromArgb(62, 75, 89);
            if (semantic == Theme.Teal) return Theme.Dark ? Color.FromArgb(152, 239, 220) : Color.FromArgb(0, 88, 82);
            if (semantic == Theme.Red) return Theme.Dark ? Color.FromArgb(255, 214, 217) : Color.FromArgb(145, 36, 46);
            return semantic;
        }
        private static double Linear(byte component)
        {
            double s = component / 255.0;
            return s <= .04045 ? s / 12.92 : Math.Pow((s + .055) / 1.055, 2.4);
        }
        internal static Color SwatchText(Color color)
        {
            double luminance = .2126 * Linear(color.R) + .7152 * Linear(color.G) + .0722 * Linear(color.B);
            return luminance > .179 ? Color.Black : Color.White;
        }
        internal static void DrawStripe(Graphics graphics, Rectangle bounds, int index)
        {
            if (index == 0 || bounds.Width < 6 || bounds.Height < 4) return;
            using (Brush fill = new SolidBrush(Colors[index].Color))
                graphics.FillRectangle(fill, bounds.X + 1, bounds.Y + 1, 4, bounds.Height - 2);
            using (Pen edge = new Pen(Theme.Line))
                graphics.DrawLine(edge, bounds.X + 5, bounds.Y + 1, bounds.X + 5, bounds.Bottom - 2);
        }
    }

    internal sealed class ItemColorButton : Button
    {
        internal int Index { get; private set; }

        internal ItemColorButton(int initial)
        {
            Name = "ItemColor"; Dock = DockStyle.Fill; FlatStyle = FlatStyle.Flat;
            Font = Theme.Font(9, true); UseVisualStyleBackColor = false; UseMnemonic = false;
            FlatAppearance.BorderColor = Theme.Line;
            SetColor(initial);
            Click += delegate {
                using (var dialog = new ItemColorDialog(Index))
                    if (dialog.ShowDialog(FindForm()) == DialogResult.OK) SetColor(dialog.Choice);
            };
        }
        private void SetColor(int index)
        {
            Index = index;
            Text = UiText.Get(ItemPalette.Colors[index].Name);
            AccessibleName = UiText.Get("Item color") + " · " + Text;
            BackColor = index == 0 ? Theme.Surface : ItemPalette.Colors[index].Color;
            ForeColor = index == 0 ? Theme.Ink : ItemPalette.SwatchText(BackColor);
            FlatAppearance.MouseOverBackColor = index == 0 ? Theme.Pale : BackColor;
        }
    }

    internal sealed class ItemColorDialog : Form
    {
        internal int Choice { get; private set; }
        private readonly Button[] swatches = new Button[Ledger.CardColorCount + 1];

        internal ItemColorDialog(int current)
        {
            Choice = current;
            Name = "ItemColorDialog"; Text = UiText.Get("Item color"); Font = Theme.Font(10, false);
            StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false; AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(438, 294); Padding = new Padding(16);
            BackColor = Theme.Surface; ForeColor = Theme.Ink;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = Padding.Empty };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            Label name = Theme.Label(UiText.Get(ItemPalette.Colors[current].Name), 12, true, Theme.Ink);
            name.Name = "ColorName"; layout.Controls.Add(name, 0, 0);
            var tiles = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 8, RowCount = 4, Margin = Padding.Empty };
            for (int i = 0; i < 8; i++) tiles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5f));
            for (int i = 0; i < 4; i++) tiles.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            var hints = new ToolTip();
            Disposed += delegate { hints.Dispose(); };
            for (int i = 1; i < ItemPalette.Colors.Length; i++)
            {
                int index = i;
                ItemPalette.Swatch swatch = ItemPalette.Colors[i];
                string title = UiText.Get(swatch.Name);
                var b = new Button { Name = "Swatch" + i, AccessibleName = title, Dock = DockStyle.Fill,
                    Margin = new Padding(3), FlatStyle = FlatStyle.Flat, BackColor = swatch.Color,
                    ForeColor = ItemPalette.SwatchText(swatch.Color), Font = Theme.Font(12, true),
                    UseVisualStyleBackColor = false, Text = i == current ? "✓" : "", TabIndex = i - 1 };
                b.FlatAppearance.BorderColor = i == current ? b.ForeColor : Theme.Line;
                b.FlatAppearance.BorderSize = i == current ? 3 : 1;
                b.FlatAppearance.MouseOverBackColor = swatch.Color;
                b.MouseEnter += delegate { name.Text = title; };
                b.Enter += delegate { name.Text = title; };
                b.Click += delegate { Choose(index); };
                b.PreviewKeyDown += delegate(object sender, PreviewKeyDownEventArgs e) {
                    if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right) e.IsInputKey = true;
                };
                b.KeyDown += delegate(object sender, KeyEventArgs e) {
                    int offset;
                    switch (e.KeyCode) {
                        case Keys.Left: offset = -1; break; case Keys.Right: offset = 1; break;
                        case Keys.Up: offset = -8; break; case Keys.Down: offset = 8; break; default: return;
                    }
                    swatches[Math.Max(1, Math.Min(Ledger.CardColorCount, index + offset))].Focus(); e.Handled = true;
                };
                hints.SetToolTip(b, title);
                swatches[i] = b; tiles.Controls.Add(b, (i - 1) % 8, (i - 1) / 8);
            }
            layout.Controls.Add(tiles, 0, 1);
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Margin = Padding.Empty };
            Button cancel = Theme.Button(UiText.Get("Cancel"), false); cancel.DialogResult = DialogResult.Cancel;
            Button reset = Theme.Button(UiText.Get("Default color"), false); reset.Name = "DefaultItemColor";
            reset.Click += delegate { Choose(0); }; reset.Enter += delegate { name.Text = UiText.Get("Default color"); };
            buttons.Controls.Add(cancel); buttons.Controls.Add(reset); layout.Controls.Add(buttons, 0, 2);
            Controls.Add(layout); CancelButton = cancel;
            Shown += delegate { if (current == 0) reset.Focus(); else swatches[current].Focus(); };
        }
        private void Choose(int index) { Choice = index; DialogResult = DialogResult.OK; Close(); }
    }
}
