using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Stroll
{
    public static class Program
    {
        [DllImport("user32.dll")] private static extern bool SetProcessDpiAwarenessContext(IntPtr context);
        [STAThread]
        public static void Main(string[] args)
        {
            WindowsShortcuts.SetIdentity();
            if (args.Contains("--add-shortcut"))
            {
                try { WindowsShortcuts.AddToStartMenu(); }
                catch (Exception error) when (error is System.IO.IOException || error is UnauthorizedAccessException || error is COMException)
                { Environment.ExitCode = 1; }
                return;
            }
            using (var openSettings = new EventWaitHandle(false, EventResetMode.AutoReset, WindowsShortcuts.OpenEventName))
            using (var mutex = new Mutex(true, @"Local\Stroll.EyeBreak.1", out bool created))
            {
                if (!created) { if (!args.Contains("--quiet")) openSettings.Set(); return; }
                try { SetProcessDpiAwarenessContext(new IntPtr(-4)); } catch (EntryPointNotFoundException) { }
                Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
                if (!args.Contains("--quiet"))
                {
                    try { WindowsShortcuts.AddToStartMenu(); }
                    catch (Exception error) when (error is System.IO.IOException || error is UnauthorizedAccessException || error is COMException) { }
                }
                Application.Run(new TrayApp(args.Contains("--quiet"), openSettings));
            }
        }
    }

    public sealed class TrayApp : ApplicationContext
    {
        private Settings settings = Settings.Load();
        private readonly BreakClock clock;
        private readonly Stopwatch monotonic = Stopwatch.StartNew();
        private readonly System.Windows.Forms.Timer heartbeat = new System.Windows.Forms.Timer { Interval = 1000 };
        private readonly Control dispatcher = new Control();
        private NotifyIcon tray;
        private ToolStripMenuItem statusItem, pauseItem;
        private ToolStripMenuItem breakItem, restartItem, settingsItem, exitItem;
        private PromptForm prompt;
        private SettingsForm settingsForm;
        private readonly List<BreakOverlay> closingOverlays = new List<BreakOverlay>();
        private readonly List<BreakOverlay> overlays = new List<BreakOverlay>();
        private bool previewing, shuttingDown, suspended;
        private Stopwatch previewWatch;
        private Phase previousPhase;
        private readonly RegisteredWaitHandle openRequest;
        public TrayApp(bool quiet, EventWaitHandle openSettings = null)
        {
            var handle = dispatcher.Handle;
            clock = new BreakClock(() => monotonic.Elapsed.TotalSeconds, () => settings);
            var menu = new ContextMenuStrip();
            statusItem = new ToolStripMenuItem { Enabled = false }; menu.Items.Add(statusItem);
            menu.Items.Add(new ToolStripSeparator());
            breakItem = new ToolStripMenuItem("Take a break now", null, (s, e) => { if (!previewing) clock.StartBreak(); }); menu.Items.Add(breakItem);
            pauseItem = new ToolStripMenuItem("Pause", null, (s, e) =>
            { if (previewing) return; if (clock.Phase == Phase.Paused) clock.Resume(); else clock.Pause(); });
            menu.Items.Add(pauseItem);
            restartItem = new ToolStripMenuItem("Restart work interval", null, (s, e) => { if (!previewing) clock.FreshInterval(); }); menu.Items.Add(restartItem);
            settingsItem = new ToolStripMenuItem("Settings…", null, (s, e) => OpenSettings()); menu.Items.Add(settingsItem);
            menu.Items.Add(new ToolStripSeparator());
            exitItem = new ToolStripMenuItem("Exit", null, (s, e) => ExitThread()); menu.Items.Add(exitItem);
            tray = new NotifyIcon { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath), Text = "Stroll", Visible = true, ContextMenuStrip = menu };
            tray.DoubleClick += (s, e) => OpenSettings();
            clock.Changed += Synchronize;
            clock.Warning += () =>
            {
                tray.BalloonTipTitle = Locale.T("A little rest is coming", settings.Language);
                tray.BalloonTipText = Locale.T("Your break prompt will appear in ", settings.Language) + (int)Math.Ceiling(clock.Remaining) + Locale.T(" seconds.", settings.Language);
                tray.ShowBalloonTip(5000);
            };
            heartbeat.Tick += (s, e) => { if (suspended) return; clock.Tick(); UpdateStatus(); }; heartbeat.Start();
            SystemEvents.PowerModeChanged += PowerChanged;
            SystemEvents.SessionSwitch += SessionChanged;
            SystemEvents.DisplaySettingsChanged += DisplaysChanged;
            UpdateStatus();
            if (openSettings != null) openRequest = ThreadPool.RegisterWaitForSingleObject(openSettings, (state, timedOut) => OnUi(OpenSettings), null, Timeout.Infinite, false);
            if (!quiet) dispatcher.BeginInvoke(new Action(OpenSettings));
        }
        public string StatusText()
        {
            return Locale.T(StatusTextEnglish(), settings.Language);
        }
        public string StatusTextEnglish()
        {
            string time = TimeSpan.FromSeconds(Math.Ceiling(clock.Remaining)).ToString(@"hh\:mm\:ss");
            switch (clock.Phase)
            {
                case Phase.Breaking: return "Resting · " + time + " remaining";
                case Phase.Prompt: return "Time for a break · waiting for you";
                case Phase.Snoozed: return "Snoozed · asking again in " + time;
                case Phase.Paused: return "Paused · resume from the tray";
                default: return "Next break in " + time;
            }
        }
        private void UpdateStatus()
        {
            string text = StatusText(); statusItem.Text = text;
            tray.Text = ("Stroll · " + text).Substring(0, Math.Min(63, ("Stroll · " + text).Length));
            breakItem.Text = Locale.T("Take a break now", settings.Language);
            pauseItem.Text = Locale.T(clock.Phase == Phase.Paused ? "Resume" : "Pause", settings.Language);
            restartItem.Text = Locale.T("Restart work interval", settings.Language);
            settingsItem.Text = Locale.T("Settings…", settings.Language); exitItem.Text = Locale.T("Exit", settings.Language);
        }
        private void Synchronize()
        {
            if (shuttingDown || previewing) return;
            Phase phase = clock.Phase;
            if (phase != Phase.Prompt && prompt != null) { prompt.Dismiss(); prompt = null; }
            if (phase != Phase.Breaking) CloseOverlays(phase == Phase.Paused);
            if (phase == Phase.Prompt && prompt == null)
            {
                prompt = new PromptForm(settings, clock.StartBreak, clock.Snooze, clock.Skip);
                prompt.Show(); prompt.Activate();
            }
            if (phase == Phase.Breaking && overlays.Count == 0)
                OpenOverlays(settings, false, () => clock.Remaining, clock.Snooze, clock.Skip);
            if (previousPhase == Phase.Breaking && phase == Phase.Working)
            { tray.BalloonTipTitle = Locale.T("Ready when you are", settings.Language); tray.BalloonTipText = Locale.T("Your next work interval has started.", settings.Language); tray.ShowBalloonTip(3000); }
            previousPhase = phase; UpdateStatus();
        }
        private void OpenSettings()
        {
            if (previewing) return;
            if (settingsForm != null && !settingsForm.IsDisposed)
            { settingsForm.WindowState = FormWindowState.Normal; settingsForm.Show(); settingsForm.Activate(); return; }
            settingsForm = new SettingsForm(settings, SaveSettings, Preview, StatusTextEnglish);
            settingsForm.Show(); settingsForm.Activate();
        }
        private void SaveSettings(Settings next)
        {
            // Roll startup back if the durable settings write fails.
            Settings.SetStartup(next.StartWithWindows);
            try { next.Save(); }
            catch { Settings.SetStartup(settings.StartWithWindows); throw; }
            settings = next; clock.FreshInterval();
        }
        private void Preview(Settings draft)
        {
            if (previewing) return;
            bool wasPaused = clock.Phase == Phase.Paused;
            clock.Pause(); previewing = true; previewWatch = Stopwatch.StartNew();
            Action stop = null;
            stop = () =>
            {
                if (!previewing) return;
                previewing = false; previewWatch.Stop(); CloseOverlays();
                if (!wasPaused) clock.Resume(); else Synchronize();
                if (settingsForm != null && !settingsForm.IsDisposed) settingsForm.Activate();
            };
            OpenOverlays(draft, true, () => Math.Max(0, 30 + draft.FadeSeconds - previewWatch.Elapsed.TotalSeconds), stop, stop);
            var previewTimer = new System.Windows.Forms.Timer { Interval = 250 };
            previewTimer.Tick += (s, e) => { if (!previewing || previewWatch.Elapsed.TotalSeconds >= 30 + draft.FadeSeconds) { previewTimer.Stop(); previewTimer.Dispose(); stop(); } };
            previewTimer.Start();
        }
        private void OpenOverlays(Settings s, bool preview, Func<double> remaining, Action snooze, Action skip)
        {
            // Finish any old fade before constructing replacement overlays.
            foreach (var old in closingOverlays.ToArray()) old.Dismiss(true);
            var screens = Screen.AllScreens.OrderBy(screen => screen.Primary ? 1 : 0).ToArray();
            foreach (var screen in screens)
            {
                var overlay = new BreakOverlay(screen, s, screen.Primary, preview, remaining, snooze, skip,
                    screen.Primary ? (Action<Point>)(point => {
                        s.CardX = point.X; s.CardY = point.Y;
                        if (!preview) try { settings.Save(); }
                        catch (System.IO.IOException) { }
                        catch (UnauthorizedAccessException) { }
                    }) : null);
                overlays.Add(overlay); overlay.Show();
            }
            if (!preview && s.FadeEnabled) clock.HoldBreakFor(s.FadeSeconds);
            if (overlays.Count > 0) overlays[overlays.Count - 1].Activate();
        }
        private void CloseOverlays(bool immediately = false)
        {
            foreach (var overlay in overlays.ToArray())
            {
                closingOverlays.Add(overlay);
                overlay.FormClosed += (sender, args) => closingOverlays.Remove(overlay);
                overlay.Dismiss(immediately);
            }
            overlays.Clear();
            if (immediately) foreach (var overlay in closingOverlays.ToArray()) overlay.Dismiss(true);
        }
        private void OnUi(Action action)
        { if (!shuttingDown && dispatcher.IsHandleCreated) try { dispatcher.BeginInvoke(action); } catch (InvalidOperationException) { } }
        private void PowerChanged(object sender, PowerModeChangedEventArgs e)
        {
            OnUi(() =>
            {
                if (e.Mode == PowerModes.Suspend) { suspended = true; clock.Pause(); }
                if (e.Mode == PowerModes.Resume)
                {
                    suspended = false; CloseOverlays(true);
                    if (previewing) { previewing = false; CloseOverlays(true); }
                    clock.FreshInterval();
                }
            });
        }
        private void SessionChanged(object sender, SessionSwitchEventArgs e)
        { if (e.Reason == SessionSwitchReason.SessionUnlock) OnUi(() => { CloseOverlays(true); if (previewing) { previewing = false; CloseOverlays(true); } clock.FreshInterval(); }); }
        private void DisplaysChanged(object sender, EventArgs e)
        {
            OnUi(() =>
            {
                if (previewing) { previewing = false; CloseOverlays(true); clock.FreshInterval(); return; }
                if (clock.Phase == Phase.Breaking)
                { CloseOverlays(true); OpenOverlays(settings, false, () => clock.Remaining, clock.Snooze, clock.Skip); }
            });
        }
        protected override void ExitThreadCore()
        {
            shuttingDown = true; if (openRequest != null) openRequest.Unregister(null); heartbeat.Stop(); heartbeat.Dispose();
            SystemEvents.PowerModeChanged -= PowerChanged; SystemEvents.SessionSwitch -= SessionChanged;
            SystemEvents.DisplaySettingsChanged -= DisplaysChanged;
            CloseOverlays(true); if (prompt != null) prompt.Dismiss();
            if (settingsForm != null) settingsForm.Dispose();
            tray.Visible = false; tray.Icon.Dispose(); tray.ContextMenuStrip.Dispose(); tray.Dispose(); dispatcher.Dispose();
            base.ExitThreadCore();
        }
    }

    public sealed class PromptForm : Form
    {
        private bool intentionalClose;
        public PromptForm(Settings settings, Action begin, Action snooze, Action skip)
        {
            Text = "Stroll · Time for a break"; ClientSize = new Size(530, 280);
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen; TopMost = true;
            BackColor = Ui.Paper; ForeColor = Ui.Ink; Font = new Font("Segoe UI", 10); AutoScaleMode = AutoScaleMode.Dpi;
            var heading = new Label { Text = Locale.T("A little distance. A little rest.", settings.Language), Location = new Point(28, 24), Size = new Size(480, 44), Font = new Font("Segoe UI", 21, FontStyle.Bold) };
            string petNames = string.Join(", ", settings.VisiblePets().Select(pet => pet.Name).Distinct());
            var body = new Label { Text = Locale.IsCantonese(settings.Language) ? "準備休息 " + settings.BreakMinutes + " 分鐘，讓眼睛放鬆嗎？\n" + petNames + " 會陪伴你。" : "Ready for a " + settings.BreakMinutes + "-minute eye break?\n" + petNames + " will keep you company.", Location = new Point(30, 87), Size = new Size(460, 66) };
            var actions = new FlowLayoutPanel { Location = new Point(24, 185), Size = new Size(490, 60), WrapContents = false };
            var start = Ui.Button("Break", begin, true); actions.Controls.Add(start);
            actions.Controls.Add(Ui.Button(Locale.T("Snooze " + settings.SnoozeMinutes + " min", settings.Language), snooze));
            actions.Controls.Add(Ui.Button(Locale.T("Skip", settings.Language), skip));
            Controls.AddRange(new Control[] { heading, body, actions }); AcceptButton = start;
            Locale.Apply(this, settings.Language);
            KeyPreview = true; KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) { e.Handled = true; snooze(); } };
            FormClosing += (s, e) => { if (!intentionalClose) { e.Cancel = true; snooze(); } };
        }
        public void Dismiss() { intentionalClose = true; Close(); }
    }
}
