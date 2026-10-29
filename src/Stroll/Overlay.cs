using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Stroll
{
    public sealed class BreakOverlay : Form
    {
        private readonly Settings settings;
        private readonly bool primary, preview;
        private readonly Func<double> remaining;
        private readonly Action snooze, skip;
        private readonly Timer animation = new Timer { Interval = 40 };
        private readonly Stopwatch elapsed = Stopwatch.StartNew();
        private readonly PetArt[] walks = new PetArt[5], jumps = new PetArt[5];
        private readonly Dictionary<string, PetArt> artwork = new Dictionary<string, PetArt>();
        private readonly int petCount;
        private Bitmap background;
        private readonly Rectangle[] previousPets = new Rectangle[5];
        private readonly bool[] wasJumping = new bool[5];
        private readonly double[] jumpStarted = new double[5];
        private readonly Action<Point> rememberCard;
        private readonly Stopwatch fadeWatch = new Stopwatch();
        private bool fadingOut, fadingIn;
        private double fadeFrom;
        private Point dragStart, cardStart;
        private bool dragging;
        private Label counter;
        private Panel card;
        private bool intentionalClose;
        private string lastCounter = "";
        public BreakOverlay(Screen screen, Settings settings, bool primary, bool preview,
            Func<double> remaining, Action snooze, Action skip, Action<Point> rememberCard = null)
        {
            this.settings = settings; this.primary = primary; this.preview = preview;
            this.rememberCard = rememberCard;
            this.remaining = remaining; this.snooze = snooze; this.skip = skip;
            AutoScaleMode = AutoScaleMode.Dpi;
            FormBorderStyle = FormBorderStyle.None; StartPosition = FormStartPosition.Manual;
            Bounds = screen.Bounds; ShowInTaskbar = false; TopMost = true; KeyPreview = true;
            BackColor = ColorTranslator.FromHtml(settings.BackgroundColor);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            if (!string.IsNullOrWhiteSpace(settings.BackgroundImage))
            {
                try
                {
                    using (var image = Image.FromFile(settings.BackgroundImage))
                    {
                        background = new Bitmap(Width, Height);
                        using (var g = Graphics.FromImage(background))
                        {
                            g.Clear(BackColor);
                            double scale = Math.Max((double)Width / image.Width, (double)Height / image.Height);
                            int w = (int)(image.Width * scale), h = (int)(image.Height * scale);
                            g.DrawImage(image, (Width - w) / 2, (Height - h) / 2, w, h);
                        }
                    }
                }
                catch (Exception ex) when (ex is System.IO.IOException || ex is ArgumentException || ex is OutOfMemoryException) { }
            }
            var pets = settings.VisiblePets(); petCount = pets.Length;
            if (primary)
                for (int i = 0; i < petCount; i++)
                {
                    walks[i] = SharedArt(pets[i].WalkImage, false);
                    if (!string.IsNullOrWhiteSpace(pets[i].JumpImage)) jumps[i] = SharedArt(pets[i].JumpImage, true);
                    else if (walks[i].IsBuiltIn && string.IsNullOrEmpty(pets[i].WalkImage)) jumps[i] = SharedArt("", true);
                    if (!walks[i].IsBuiltIn && jumps[i] != null && jumps[i].IsBuiltIn) jumps[i] = null;
                }
            CreateCard();
            animation.Tick += (s, e) =>
            {
                if (fadingIn || fadingOut)
                {
                    double progress = Math.Min(1, fadeWatch.Elapsed.TotalSeconds / settings.FadeSeconds);
                    Opacity = fadingOut ? fadeFrom * (1 - progress) : progress;
                    if (progress >= 1)
                    {
                        if (fadingOut) { Close(); return; }
                        fadingIn = false; animation.Interval = primary ? 40 : 1000;
                    }
                }
                if (primary)
                    for (int i = 0; i < petCount; i++)
                    {
                        Rectangle next = PetBounds(Pose(i));
                        Invalidate(Rectangle.Union(previousPets[i], next)); previousPets[i] = next;
                    }
                UpdateCounter();
            };
            if (settings.FadeEnabled) { Opacity = 0; fadingIn = true; }
            else animation.Interval = primary ? 40 : 1000;
            Shown += (s, e) => { LayoutCard(); UpdateCounter(); fadeWatch.Restart(); elapsed.Restart(); animation.Start(); };
            Resize += (s, e) => LayoutCard();
            KeyDown += (s, e) => { if (!fadingOut && e.KeyCode == Keys.Escape) { e.Handled = true; snooze(); } };
            FormClosing += (s, e) => { if (!intentionalClose) { e.Cancel = true; snooze(); } };
        }
        private PetArt SharedArt(string path, bool jumping)
        {
            string key = (jumping ? "jump:" : "walk:") + (path ?? "");
            PetArt art;
            if (!artwork.TryGetValue(key, out art)) { art = OpenArt(path, jumping); artwork.Add(key, art); }
            return art;
        }
        private PetArt OpenArt(string path, bool jumping)
        {
            try { return new PetArt(path, jumping); }
            catch (Exception ex) when (ex is System.IO.IOException || ex is ArgumentException || ex is OutOfMemoryException)
            { return new PetArt("", jumping); }
        }
        private void CreateCard()
        {
            card = new Panel { Width = 620, Height = primary ? 270 : 190, BackColor = Color.FromArgb(24, 35, 36), Padding = new Padding(24) };
            var title = new Label { Text = Locale.BreakMessage(settings.Message, settings.Language), ForeColor = Color.White, Font = new Font("Segoe UI", 25, FontStyle.Bold), Dock = DockStyle.Top, Height = 88, TextAlign = ContentAlignment.MiddleCenter };
            counter = new Label { ForeColor = Color.FromArgb(171, 215, 204), Font = new Font("Segoe UI", 32), Dock = DockStyle.Top, Height = 64, TextAlign = ContentAlignment.MiddleCenter };
            var hint = new Label { Text = Locale.T(preview ? "PREVIEW · Esc to close" : "Let your gaze rest somewhere beyond the screen.", settings.Language), ForeColor = Color.FromArgb(188, 199, 197), Dock = DockStyle.Top, Height = 28, TextAlign = ContentAlignment.MiddleCenter };
            card.Controls.Add(hint); card.Controls.Add(counter); card.Controls.Add(title);
            if (primary)
            {
                var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 42, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
                actions.Controls.Add(Ui.Button(Locale.T(preview ? "Close preview" : "Snooze " + settings.SnoozeMinutes + " min", settings.Language), snooze));
                if (!preview) actions.Controls.Add(Ui.Button(Locale.T("Skip", settings.Language), skip));
                actions.Padding = new Padding(preview ? 180 : 100, 0, 0, 0);
                card.Controls.Add(actions);
            }
            Controls.Add(card);
            MakeDraggable(card); MakeDraggable(title); MakeDraggable(counter); MakeDraggable(hint);
        }
        private void LayoutCard()
        {
            if (card == null) return;
            card.Width = Math.Min(620, Math.Max(1, ClientSize.Width - 48));
            card.Height = Math.Min(primary ? 270 : 190, ClientSize.Height);
            card.Location = ClampCard(settings.CardX < 0 || settings.CardY < 0 ?
                new Point((ClientSize.Width - card.Width) / 2, (ClientSize.Height - card.Height) / 2) :
                new Point(settings.CardX, settings.CardY));
            using (var path = new GraphicsPath())
            {
                int r = Math.Min(28, Math.Min(card.Width, card.Height));
                path.AddArc(0, 0, r, r, 180, 90); path.AddArc(card.Width - r, 0, r, r, 270, 90);
                path.AddArc(card.Width - r, card.Height - r, r, r, 0, 90); path.AddArc(0, card.Height - r, r, r, 90, 90);
                path.CloseFigure(); var old = card.Region; card.Region = new Region(path); if (old != null) old.Dispose();
            }
        }
        private Point ClampCard(Point point)
        { return new Point(Math.Max(0, Math.Min(ClientSize.Width - card.Width, point.X)),
            Math.Max(0, Math.Min(ClientSize.Height - card.Height, point.Y))); }
        private void MakeDraggable(Control control)
        {
            control.Cursor = Cursors.SizeAll;
            control.MouseDown += (s, e) => {
                if (e.Button != MouseButtons.Left || fadingOut) return;
                dragging = true; dragStart = Cursor.Position; cardStart = card.Location; control.Capture = true;
            };
            control.MouseMove += (s, e) => {
                if (!dragging) return;
                Point mouse = Cursor.Position;
                card.Location = ClampCard(new Point(cardStart.X + mouse.X - dragStart.X, cardStart.Y + mouse.Y - dragStart.Y));
            };
            control.MouseUp += (s, e) => {
                if (!dragging) return;
                dragging = false; control.Capture = false;
                settings.CardX = card.Left; settings.CardY = card.Top;
                if (rememberCard != null) rememberCard(card.Location);
            };
            control.MouseCaptureChanged += (s, e) => { if (!control.Capture) dragging = false; };
        }
        private void UpdateCounter()
        {
            double seconds = Math.Max(0, remaining());
            string text = TimeSpan.FromSeconds(Math.Ceiling(seconds)).ToString(@"mm\:ss");
            if (text != lastCounter) { counter.Text = text; lastCounter = text; }
        }
        private PetPose Pose(int index)
        { return PetRoute.At(elapsed.Elapsed.TotalSeconds + index * (EffectiveSize + 36.0) / settings.PetSpeed, ClientSize.Width, ClientSize.Height,
            EffectiveSize, settings.PetSpeed, settings.Edges); }
        private int EffectiveSize { get { return Math.Min(settings.PetSize, Math.Max(48, Math.Min(Width, Height) / 3)); } }
        private Rectangle PetBounds(PetPose pose)
        { int r = (int)(EffectiveSize * .75) + 12; return new Rectangle((int)pose.X - r, (int)pose.Y - r, r * 2, r * 2); }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (background != null) e.Graphics.DrawImageUnscaled(background, 0, 0);
            else e.Graphics.Clear(BackColor);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); if (!primary || walks[0] == null) return;
            for (int i = 0; i < petCount; i++) DrawPet(e.Graphics, i);
        }
        private void DrawPet(Graphics graphics, int index)
        {
            PetPose pose = Pose(index); double t = elapsed.Elapsed.TotalSeconds;
            if (pose.Jumping && !wasJumping[index]) jumpStarted[index] = t;
            wasJumping[index] = pose.Jumping;
            PetArt art = pose.Jumping && jumps[index] != null ? jumps[index] : walks[index];
            double artTime = pose.Jumping && jumps[index] != null ? t - jumpStarted[index] : t;
            Image img = art.ImageAt(artTime); Rectangle src = art.SourceAt(artTime);
            double ratio = Math.Min((double)EffectiveSize / src.Width, (double)EffectiveSize / src.Height);
            float w = (float)(src.Width * ratio), h = (float)(src.Height * ratio);
            var state = graphics.Save();
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.TranslateTransform((float)pose.X, (float)pose.Y);
            graphics.RotateTransform((float)pose.Angle);
            if (pose.Reverse) graphics.ScaleTransform(-1, 1);
            // Small, inexpensive bob also gives static custom pictures some life.
            float bob = !pose.Jumping ? (float)Math.Sin(t * 8) * 2 : 0;
            graphics.DrawImage(img, new RectangleF(-w / 2, -h / 2 + bob, w, h), src, GraphicsUnit.Pixel);
            graphics.Restore(state);
        }
        public void Dismiss(bool immediately = false)
        {
            if (IsDisposed) return;
            intentionalClose = true;
            if (immediately || !settings.FadeEnabled) { Close(); return; }
            if (fadingOut) return;
            fadingIn = false; fadingOut = true; fadeFrom = Opacity;
            card.Enabled = false; fadeWatch.Restart(); animation.Interval = 40; animation.Start();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            { animation.Stop(); animation.Dispose(); foreach (var art in artwork.Values) art.Dispose(); artwork.Clear(); if (background != null) background.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
