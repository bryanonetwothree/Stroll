using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Stroll
{
    public sealed class SettingsForm : Form
    {
        private readonly Settings draft;
        private readonly Action<Settings> save;
        private NumericUpDown work, rest, snooze, warning, size, speed, count;
        private CheckBox warn, startup, fade;
        private ComboBox presets;
        private CheckedListBox screenPets;
        private TrackBar fadeDuration;
        private bool refreshingRoster;
        private readonly CheckBox[] edges = new CheckBox[4];
        private TextBox name, message;
        private Label walkFile, jumpFile, backgroundFile, status;
        private Button colour;
        private Timer statusTimer = new Timer { Interval = 1000 };
        public SettingsForm(Settings current, Action<Settings> save, Action<Settings> preview, Func<string> getStatus)
        {
            draft = current.Copy(); this.save = save;
            Text = "Stroll · A little space for your eyes";
            ClientSize = new Size(820, 650); MinimumSize = new Size(780, 650);
            AutoScaleMode = AutoScaleMode.Dpi; StartPosition = FormStartPosition.CenterScreen;
            BackColor = Ui.Paper; ForeColor = Ui.Ink; Font = new Font("Segoe UI", 10);
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            var header = new Panel { Dock = DockStyle.Top, Height = 122, Padding = new Padding(28, 16, 28, 8), BackColor = Ui.Ink };
            var title = new Label { Text = "stroll", Dock = DockStyle.Top, Height = 53, Font = new Font("Segoe UI", 30, FontStyle.Bold), ForeColor = Color.FromArgb(239, 235, 218) };
            status = new Label { Text = getStatus(), Dock = DockStyle.Top, Height = 29, ForeColor = Color.FromArgb(172, 211, 197) };
            header.Controls.Add(status); header.Controls.Add(title);
            var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(20, 9) };
            var timings = Page(tabs, "Timing"); var appearance = Page(tabs, "Break screen"); var roster = Page(tabs, "Pets on screen"); var pet = Page(tabs, "Pet && path");
            BuildTiming(timings); BuildAppearance(appearance); BuildPet(pet); BuildRoster(roster);
            var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 69, Padding = new Padding(20, 12, 12, 8), FlowDirection = FlowDirection.RightToLeft };
            footer.Controls.Add(Ui.Button("Save settings", Save, true));
            footer.Controls.Add(Ui.Button("Preview break", () => { if (ReadDraft()) preview(draft.Copy()); }));
            footer.Controls.Add(Ui.Button("Close", Close));
            Controls.Add(tabs); Controls.Add(footer); Controls.Add(header);
            statusTimer.Tick += (s, e) => status.Text = getStatus(); statusTimer.Start();
        }
        private TableLayoutPanel Page(TabControl tabs, string title)
        {
            var page = new TabPage(title) { BackColor = Ui.Paper, Padding = new Padding(22, 18, 22, 12), AutoScroll = true };
            var table = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, GrowStyle = TableLayoutPanelGrowStyle.AddRows };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 208));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            page.Controls.Add(table); tabs.TabPages.Add(page); return table;
        }
        private void Row(TableLayoutPanel table, string label, Control input, int height = 47)
        {
            int row = table.RowCount++; table.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            var caption = Ui.Label(label); caption.Dock = DockStyle.Fill;
            table.Controls.Add(caption, 0, row); input.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            input.Margin = new Padding(3, 7, 3, 7); table.Controls.Add(input, 1, row);
        }
        private void Note(TableLayoutPanel table, string text, int height = 58)
        {
            int row = table.RowCount++; table.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            var note = Ui.Label(text, height); note.Dock = DockStyle.Fill; note.ForeColor = Color.FromArgb(92, 109, 102);
            table.Controls.Add(note, 0, row); table.SetColumnSpan(note, 2);
        }
        private NumericUpDown Number(double value, decimal min, decimal max, int decimals = 0)
        { return new NumericUpDown { Minimum = min, Maximum = max, DecimalPlaces = decimals, Increment = decimals > 0 ? .1M : 1,
            Value = Math.Max(min, Math.Min(max, (decimal)value)), Width = 140 }; }
        private FlowLayoutPanel Group(params Control[] controls)
        { var p = new FlowLayoutPanel { Height = 42, WrapContents = false, AutoSize = false }; p.Controls.AddRange(controls); return p; }
        private void BuildTiming(TableLayoutPanel table)
        {
            Note(table, "A quieter rhythm. Work, take a proper break, then start fresh.", 40);
            work = Number(draft.WorkMinutes, 1, 240); rest = Number(draft.BreakMinutes, .1M, 60, 1);
            snooze = Number(draft.SnoozeMinutes, 1, 120); warning = Number(draft.WarningSeconds, 1, 300);
            Row(table, "Work interval · minutes", work); Row(table, "Break duration · minutes", rest);
            Row(table, "Snooze delay · minutes", snooze);
            warn = new CheckBox { Text = "Show a warning before the prompt", Checked = draft.WarningEnabled, AutoSize = true };
            Row(table, "Advance warning", warn); Row(table, "Warning lead · seconds", warning);
            warning.Enabled = warn.Checked; warn.CheckedChanged += (s, e) => warning.Enabled = warn.Checked;
            startup = new CheckBox { Text = "Start quietly in the tray when I sign in", Checked = draft.StartWithWindows, AutoSize = true };
            Row(table, "Windows startup", startup);
            Row(table, "Start / taskbar pinning", Ui.Button("Add to Start menu", () => {
                try
                {
                    WindowsShortcuts.AddToStartMenu();
                    System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + WindowsShortcuts.StartMenuPath + "\"");
                }
                catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is System.Runtime.InteropServices.COMException || error is System.ComponentModel.Win32Exception)
                { MessageBox.Show(this, "Could not add the shortcut. " + error.Message, "Start menu"); }
            }));
            Note(table, "In Start, search for Stroll and right-click to pin to Start or taskbar.\nA pinned launch opens settings even when Stroll is already in the tray.", 64);
            Note(table, "Snooze asks again later. Skip starts a fresh work interval.\nSleep/wake starts fresh. Pause is manual; there is no activity monitoring.", 67);
        }
        private void BuildAppearance(TableLayoutPanel table)
        {
            fade = new CheckBox { Text = "Gently fade in and out", Checked = draft.FadeEnabled, AutoSize = true };
            Row(table, "Transitions", fade);
            fadeDuration = new TrackBar { Minimum = 1, Maximum = 30, Value = Math.Max(1, Math.Min(30, draft.FadeMilliseconds / 100)), TickFrequency = 5, Height = 36, Enabled = fade.Checked };
            var duration = new Label { Dock = DockStyle.Bottom, Height = 22, TextAlign = ContentAlignment.MiddleLeft };
            Action refreshDuration = () => duration.Text = (fadeDuration.Value / 10.0).ToString("0.0") + " seconds each way";
            refreshDuration(); fadeDuration.ValueChanged += (sender, args) => refreshDuration();
            fade.CheckedChanged += (sender, args) => fadeDuration.Enabled = fade.Checked;
            var transition = new Panel { Height = 64 }; fadeDuration.Dock = DockStyle.Top;
            transition.Controls.Add(fadeDuration); transition.Controls.Add(duration);
            Row(table, "Fade in / out duration", transition, 78);
            message = new TextBox { Text = draft.Message, MaxLength = 100 };
            Row(table, "Break reminder", message);
            colour = Ui.Button("Choose colour…", () =>
            {
                using (var dialog = new PaletteDialog(ColorTranslator.FromHtml(draft.BackgroundColor)))
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    { draft.BackgroundColor = ColorTranslator.ToHtml(dialog.Selection); colour.Text = draft.BackgroundColor; }
            });
            colour.Text = draft.BackgroundColor + "  ·  Choose…";
            Row(table, "Background colour", colour);
            Row(table, "Background image", Group(Ui.Button("Upload image…", () => Import(true, false)),
                Ui.Button("Use solid colour", () => { draft.BackgroundImage = ""; RefreshFiles(); })), 52);
            backgroundFile = Ui.Label(""); Row(table, "Selected image", backgroundFile);
            Note(table, "Colour wheel, brightness and hex entry are supported.\nImages fill the screen and are cropped to fit. The reminder stays readable.", 66);
            Note(table, "All monitors are covered during the break. Your pet and controls appear\non the primary display; other displays show the reminder and countdown.", 66);
            Note(table, "Esc snoozes a live break. The overlay can also be closed with Alt+F4.\nNo keyboard hooks, screen capture, accounts, uploads to a server or telemetry.", 66);
            RefreshFiles();
        }
        private void BuildPet(TableLayoutPanel table)
        {
            presets = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };
            RefreshPresets();
            Row(table, "Pet presets", presets);
            Row(table, "", Group(Ui.Button("Edit selected", () => {
                var selected = presets.SelectedItem as PetPreset;
                if (selected != null) { CaptureRoster(); draft.UsePet(selected); name.Text = draft.PetName; RefreshFiles(); RefreshRoster(); }
            }), Ui.Button("Save named pet", () => {
                if (!ReadDraft()) return;
                draft.SavePet(name.Text); RefreshPresets(); RefreshRoster();
            }), Ui.Button("Delete pet", () => DeletePet(presets.SelectedItem as PetPreset))), 50);
            name = new TextBox { Text = draft.PetName, MaxLength = 40 }; Row(table, "Pet name", name);
            Row(table, "Walking image / GIF", Group(Ui.Button("Upload pet…", () => Import(false, false))), 50);
            walkFile = Ui.Label(""); Row(table, "Selected pet", walkFile, 34);
            int separatorRow = table.RowCount++; table.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
            var separator = new Panel { Height = 1, Dock = DockStyle.Fill, BackColor = Color.FromArgb(196, 207, 199), Margin = new Padding(0, 9, 0, 10) };
            table.Controls.Add(separator, 0, separatorRow); table.SetColumnSpan(separator, 2);
            Row(table, "Jump image / GIF · optional", Group(Ui.Button("Upload jump…", () => Import(false, true)),
                Ui.Button("Clear jump", () => { CaptureRoster(); draft.JumpImage = ""; RefreshFiles(); })), 50);
            jumpFile = Ui.Label(""); Row(table, "Selected jump", jumpFile, 34);
            var path = new FlowLayoutPanel { Height = 34, WrapContents = false };
            string[] labels = { "Bottom", "Right", "Top", "Left" };
            for (int i = 0; i < 4; i++)
            { edges[i] = new CheckBox { Text = labels[i], AutoSize = true, Checked = (draft.Edges & (1 << i)) != 0 }; path.Controls.Add(edges[i]); }
            Row(table, "Walk along", path, 42);
            size = Number(draft.PetSize, 64, 300); speed = Number(draft.PetSpeed, 20, 160);
            Row(table, "Pet size · pixels", size, 42); Row(table, "Walking speed · pixels/sec", speed, 42);
            Note(table, "Pet faces right in your source image. Transparent PNGs and GIFs work best.\nOpposite edges trigger a visible hop; a jump file is optional. Try Preview break.", 62);
            RefreshFiles();
        }
        private void BuildRoster(TableLayoutPanel table)
        {
            count = Number(draft.PetCount, 1, 5); Row(table, "Total pets", count, 42);
            screenPets = new CheckedListBox { CheckOnClick = true, Height = 100, IntegralHeight = false, BorderStyle = BorderStyle.FixedSingle };
            draft.ScreenPets = draft.VisiblePets().GroupBy(PetKey).Select(group => group.First().Copy()).ToList();
            RefreshRoster();
            screenPets.ItemCheck += (sender, args) => {
                if (refreshingRoster) return;
                int checkedCount = screenPets.CheckedItems.Count + (args.NewValue == CheckState.Checked ? 1 : -1);
                if (checkedCount > 5) { args.NewValue = CheckState.Unchecked; return; }
                if (checkedCount > count.Value) count.Value = checkedCount;
            };
            Row(table, "Show these pets", screenPets, 114);
            Row(table, "Manage pets", Group(Ui.Button("Delete selected", () => DeletePet(screenPets.SelectedItem as PetPreset)),
                Ui.Button("Restore built-ins", () => { CaptureRoster(); draft.RestoreBuiltIns(); RefreshPresets(); RefreshRoster(); })), 50);
            Note(table, "Check the pets to show together. Extra copies repeat the checked pets.\nUse Pet & path to edit artwork and save more named pets.", 56);
        }
        private static string PetKey(PetPreset pet)
        { return pet.Key; }
        private void CaptureRoster()
        {
            if (screenPets != null) draft.ScreenPets = screenPets.CheckedItems.Cast<PetPreset>().Select(pet => pet.Copy()).ToList();
        }
        private void DeletePet(PetPreset pet)
        {
            if (pet == null) return;
            CaptureRoster();
            if (!draft.DeletePet(pet)) { MessageBox.Show(this, "Keep at least one pet in your collection.", "Delete pet"); return; }
            name.Text = draft.PetName; RefreshPresets(); RefreshRoster(); RefreshFiles();
        }
        private void RefreshRoster()
        {
            if (screenPets == null) return;
            var keys = new HashSet<string>((draft.ScreenPets ?? new List<PetPreset>()).Select(PetKey));
            refreshingRoster = true; screenPets.BeginUpdate(); screenPets.Items.Clear();
            foreach (var pet in draft.AvailablePets()) screenPets.Items.Add(pet.Copy(), keys.Contains(PetKey(pet)));
            screenPets.EndUpdate(); refreshingRoster = false;
        }
        private void RefreshPresets()
        {
            presets.Items.Clear(); presets.Items.AddRange(draft.AvailablePets());
            if (presets.Items.Count == 0) return;
            presets.SelectedIndex = 0;
            for (int i = 0; i < presets.Items.Count; i++)
                if (PetKey((PetPreset)presets.Items[i]) == PetKey(draft.CurrentPet())) presets.SelectedIndex = i;
        }
        private void RefreshFiles()
        {
            if (backgroundFile != null) backgroundFile.Text = string.IsNullOrEmpty(draft.BackgroundImage) ? "Solid colour" : Path.GetFileName(draft.BackgroundImage);
            if (walkFile != null) walkFile.Text = string.IsNullOrEmpty(draft.WalkImage) ? "S-Yosef · original cartoon St Bernard" : draft.WalkImage.StartsWith("builtin:") ? draft.PetName + " · bundled" : Path.GetFileName(draft.WalkImage);
            if (jumpFile != null) jumpFile.Text = string.IsNullOrEmpty(draft.JumpImage) ? (string.IsNullOrEmpty(draft.WalkImage) ? "S-Yosef's bundled jump pose" : "None · use main image during hop") : draft.JumpImage.StartsWith("builtin:") ? "Bundled jump pose" : Path.GetFileName(draft.JumpImage);
        }
        private void Import(bool background, bool jumping)
        {
            using (var dialog = new OpenFileDialog { Title = background ? "Choose a background" : "Choose pet artwork", Filter = "Images|*.png;*.gif;*.jpg;*.jpeg;*.bmp" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    CaptureRoster();
                    string path = PetArt.Import(dialog.FileName, background);
                    if (background) draft.BackgroundImage = path;
                    else if (jumping) draft.JumpImage = path;
                    else { draft.WalkImage = path; draft.JumpImage = ""; name.Text = Path.GetFileNameWithoutExtension(dialog.FileName); }
                    draft.PetName = name.Text; RefreshFiles(); RefreshRoster();
                }
                catch (Exception ex) when (ex is IOException || ex is ArgumentException || ex is OutOfMemoryException || ex is System.Runtime.InteropServices.ExternalException)
                { MessageBox.Show(this, "That image could not be imported. " + ex.Message, "Import image", MessageBoxButtons.OK, MessageBoxIcon.Information); }
            }
        }
        private bool ReadDraft()
        {
            int mask = 0; for (int i = 0; i < 4; i++) if (edges[i].Checked) mask |= 1 << i;
            if (mask == 0) { MessageBox.Show(this, "Choose at least one edge for your pet.", "Pet path"); return false; }
            draft.Edges = mask; draft.WorkMinutes = (double)work.Value; draft.BreakMinutes = (double)rest.Value;
            draft.SnoozeMinutes = (double)snooze.Value; draft.WarningEnabled = warn.Checked; draft.WarningSeconds = (int)warning.Value;
            draft.StartWithWindows = startup.Checked; draft.PetSize = (int)size.Value; draft.PetSpeed = (int)speed.Value;
            if (screenPets.CheckedItems.Count == 0) { MessageBox.Show(this, "Check at least one pet to show on screen.", "Pets on screen"); return false; }
            draft.ScreenPets = screenPets.CheckedItems.Cast<PetPreset>().Select(pet => pet.Copy()).ToList();
            draft.PetCount = Math.Max((int)count.Value, draft.ScreenPets.Count); count.Value = draft.PetCount;
            draft.FadeEnabled = fade.Checked; draft.FadeMilliseconds = fadeDuration.Value * 100;
            draft.PetName = name.Text; draft.Message = message.Text; draft.Validate(); return true;
        }
        private void Save()
        {
            if (!ReadDraft()) return;
            try { save(draft.Copy()); Close(); }
            catch (Exception ex) { MessageBox.Show(this, "Could not save settings. " + ex.Message, "Save settings", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        protected override void Dispose(bool disposing)
        { if (disposing) statusTimer.Dispose(); base.Dispose(disposing); }
    }
}
