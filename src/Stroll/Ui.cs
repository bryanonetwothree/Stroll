using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Stroll
{
    public static class Ui
    {
        public static readonly Color Ink = Color.FromArgb(28, 48, 45);
        public static readonly Color Paper = Color.FromArgb(247, 247, 240);
        public static readonly Color Green = Color.FromArgb(43, 102, 86);
        public static Button Button(string text, Action action, bool accent = false)
        {
            var b = new Button { Text = text, AutoSize = true, MinimumSize = new Size(122, 36),
                Padding = new Padding(12, 4, 12, 4), FlatStyle = FlatStyle.Flat,
                BackColor = accent ? Green : Color.FromArgb(236, 239, 231),
                ForeColor = accent ? Color.White : Ink, Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10), Margin = new Padding(4) };
            b.FlatAppearance.BorderSize = 0; b.Click += (s, e) => action(); return b;
        }
        public static Label Label(string text, int height = 28)
        { return new Label { Text = text, AutoSize = false, Height = height, Dock = DockStyle.Top, ForeColor = Ink, TextAlign = ContentAlignment.MiddleLeft }; }
    }

    public sealed class ColorWheel : Control
    {
        private Bitmap wheel;
        private double hue, saturation;
        private double value = 1;
        public event Action Changed;
        public Color Selection
        {
            get { return Hsv(hue, saturation, value); }
            set { hue = value.GetHue(); double max = Math.Max(value.R, Math.Max(value.G, value.B));
                double min = Math.Min(value.R, Math.Min(value.G, value.B));
                saturation = max == 0 ? 0 : (max - min) / max; this.value = max / 255; Invalidate(); }
        }
        public double Brightness { get { return value; } set { this.value = value; Fire(); } }
        public ColorWheel() { Size = new Size(220, 220); DoubleBuffered = true; Cursor = Cursors.Cross; }
        private void Fire() { Invalidate(); if (Changed != null) Changed(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (wheel == null)
            {
                wheel = new Bitmap(220, 220);
                for (int y = 0; y < 220; y++) for (int x = 0; x < 220; x++)
                {
                    double dx = x - 110, dy = y - 110, r = Math.Sqrt(dx * dx + dy * dy) / 106;
                    if (r <= 1) wheel.SetPixel(x, y, Hsv((Math.Atan2(dy, dx) * 180 / Math.PI + 360) % 360, r, 1));
                }
            }
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; e.Graphics.DrawImageUnscaled(wheel, 0, 0);
            double angle = hue * Math.PI / 180;
            float px = 110 + (float)(Math.Cos(angle) * saturation * 106);
            float py = 110 + (float)(Math.Sin(angle) * saturation * 106);
            using (var pen = new Pen(Color.White, 3)) e.Graphics.DrawEllipse(pen, px - 6, py - 6, 12, 12);
            using (var pen = new Pen(Color.Black, 1)) e.Graphics.DrawEllipse(pen, px - 8, py - 8, 16, 16);
        }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) Pick(e); }
        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); if (e.Button == MouseButtons.Left) Pick(e); }
        private void Pick(MouseEventArgs e)
        {
            double dx = e.X - 110, dy = e.Y - 110;
            hue = (Math.Atan2(dy, dx) * 180 / Math.PI + 360) % 360;
            saturation = Math.Min(1, Math.Sqrt(dx * dx + dy * dy) / 106); Fire();
        }
        public static Color Hsv(double h, double s, double v)
        {
            double c = v * s, x = c * (1 - Math.Abs(h / 60 % 2 - 1)), m = v - c;
            double r = 0, g = 0, b = 0;
            if (h < 60) { r = c; g = x; } else if (h < 120) { r = x; g = c; }
            else if (h < 180) { g = c; b = x; } else if (h < 240) { g = x; b = c; }
            else if (h < 300) { r = x; b = c; } else { r = c; b = x; }
            return Color.FromArgb((int)Math.Round((r + m) * 255), (int)Math.Round((g + m) * 255), (int)Math.Round((b + m) * 255));
        }
        protected override void Dispose(bool disposing) { if (disposing && wheel != null) wheel.Dispose(); base.Dispose(disposing); }
    }

    public sealed class PaletteDialog : Form
    {
        public Color Selection { get; private set; }
        public PaletteDialog(Color initial)
        {
            Text = "Choose a background colour"; ClientSize = new Size(430, 340); BackColor = Ui.Paper;
            Font = new Font("Segoe UI", 10); StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
            var wheel = new ColorWheel { Location = new Point(20, 18), Selection = initial };
            var swatch = new Panel { Location = new Point(270, 30), Size = new Size(130, 100), BackColor = initial };
            var hex = new TextBox { Location = new Point(270, 152), Width = 130, Text = ColorTranslator.ToHtml(initial) };
            var brightness = new TrackBar { Location = new Point(20, 260), Width = 240, Minimum = 0, Maximum = 100,
                Value = (int)(wheel.Brightness * 100), TickStyle = TickStyle.None };
            var label = new Label { Text = "Brightness", Location = new Point(25, 238), AutoSize = true };
            wheel.Changed += () => { swatch.BackColor = wheel.Selection; hex.Text = ColorTranslator.ToHtml(wheel.Selection); };
            brightness.ValueChanged += (s, e) => wheel.Brightness = brightness.Value / 100.0;
            var use = Ui.Button("Use colour", () =>
            {
                try { Selection = ColorTranslator.FromHtml(hex.Text); if (Selection.IsEmpty) throw new ArgumentException(); DialogResult = DialogResult.OK; }
                catch { MessageBox.Show(this, "Enter a colour such as #172B29.", "Choose a colour"); }
            }, true);
            use.Location = new Point(270, 270); AcceptButton = use;
            Controls.AddRange(new Control[] { wheel, swatch, hex, brightness, label, use });
        }
    }
}
