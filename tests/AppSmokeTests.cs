using System;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;
using Stroll;

public static class AppSmokeTests
{
    // Return a diagnostic instead of letting Windows show a crash-report dialog.
    [STAThread] public static int Main(string[] args)
    {
        try { return Run(); }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static int assertions;
    private static object Field(object obj, string name)
    { return obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(obj); }
    private static void Check(bool ok, string label)
    { assertions++; if (!ok) throw new Exception(label); }
    private static void Prompt(BreakClock clock)
    { typeof(BreakClock).GetMethod("Set", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(clock, new object[] { Phase.Prompt, 0.0 }); Application.DoEvents(); }
    private static void Click(Form form, string text)
    {
        Button button = Find(form, text); if (button == null) throw new Exception("Missing button " + text);
        button.PerformClick(); Pump();
    }
    private static Button Find(Control parent, string text)
    {
        foreach (Control c in parent.Controls)
        {
            var b = c as Button; if (b != null && b.Text.StartsWith(text)) return b;
            var child = Find(c, text); if (child != null) return child;
        }
        return null;
    }
    private static void Pump()
    { var watch = Stopwatch.StartNew(); while (watch.ElapsedMilliseconds < 600) { Application.DoEvents(); Thread.Sleep(10); } }
    [STAThread]
    public static int Run()
    {
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        using (var launchRequest = new EventWaitHandle(false, EventResetMode.AutoReset))
        using (var app = new TrayApp(true, launchRequest))
        {
            typeof(TrayApp).GetField("settings", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(app, new Settings());
            var clock = (BreakClock)Field(app, "clock");
            Prompt(clock); Check(Application.OpenForms.OfType<PromptForm>().Count() == 1, "Prompt visible");
            Click(Application.OpenForms.OfType<PromptForm>().Single(), "Snooze");
            Check(clock.Phase == Phase.Snoozed && Application.OpenForms.OfType<PromptForm>().Count() == 0, "Snooze dismisses prompt");
            Prompt(clock); Click(Application.OpenForms.OfType<PromptForm>().Single(), "Snooze");
            Check(clock.Phase == Phase.Snoozed, "Second snooze");
            Prompt(clock); Click(Application.OpenForms.OfType<PromptForm>().Single(), "Break");
            Check(clock.Phase == Phase.Breaking && Application.OpenForms.OfType<BreakOverlay>().Count() == Screen.AllScreens.Length, "Break opens all overlays");
            Click(Application.OpenForms.OfType<BreakOverlay>().Last(), "Snooze");
            Check(clock.Phase == Phase.Snoozed && !Application.OpenForms.OfType<BreakOverlay>().Any(), "Snooze closes overlays");
            Prompt(clock); Click(Application.OpenForms.OfType<PromptForm>().Single(), "Skip");
            Check(clock.Phase == Phase.Working && clock.Remaining <= 1200 && clock.Remaining >= 1198, "Skip starts full interval");
            clock.StartBreak(); clock.Pause();
            Check(!Application.OpenForms.OfType<BreakOverlay>().Any(), "Pausing hides overlays");
            clock.Resume(); Check(Application.OpenForms.OfType<BreakOverlay>().Any(), "Resume restores overlay");
            var power = typeof(TrayApp).GetMethod("PowerChanged", BindingFlags.NonPublic | BindingFlags.Instance);
            power.Invoke(app, new object[] { null, new PowerModeChangedEventArgs(PowerModes.Resume) }); Pump();
            Check(clock.Phase == Phase.Working && !Application.OpenForms.OfType<BreakOverlay>().Any(), "Wake resets and closes overlays");
            launchRequest.Set(); Pump();
            Check(Application.OpenForms.OfType<SettingsForm>().Count() == 1 && clock.Phase == Phase.Working, "Pinned relaunch opens existing settings without resetting timer");
            launchRequest.Set(); Pump();
            Check(Application.OpenForms.OfType<SettingsForm>().Count() == 1, "Repeated pinned launch reuses settings window");
            app.ExitThread();
        }
        Console.WriteLine("PASS · " + assertions + " app integration checks: real prompt/overlay buttons, pause, resume and wake dispatch.");
        return 0;
    }
}
