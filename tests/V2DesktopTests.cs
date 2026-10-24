// Optional interactive Windows validation. Never saves settings or changes startup.
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Stroll;

public static class V2DesktopTests
{
    // Return a diagnostic instead of letting Windows show a crash-report dialog.
    [STAThread] public static int Main(string[] args)
    {
        try { return Run(args); }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    static int checks;
    static void Check(bool ok, string message) { checks++; if (!ok) throw new Exception(message); }
    static object Field(object obj, string name) { return obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(obj); }
    static void Pump(int milliseconds)
    { var watch = Stopwatch.StartNew(); while (watch.ElapsedMilliseconds < milliseconds) { Application.DoEvents(); Thread.Sleep(10); } }
    static Button Button(Control control, string text)
    {
        foreach (Control child in control.Controls) { var button = child as Button; if (button != null && button.Text == text) return button; var found = Button(child, text); if (found != null) return found; }
        return null;
    }
    [STAThread] public static int Run(string[] args)
    {
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        WindowsShortcuts.CreateShortcut(Path.Combine("dist", "smoke", "Stroll.lnk"), typeof(Settings).Assembly.Location);
        Check(File.Exists(Path.Combine("dist", "smoke", "Stroll.lnk")), "Native Windows shortcut created");
        foreach (var preset in Settings.BuiltInPets())
        using (var walk = new PetArt(preset.WalkImage, false))
        using (var jump = new PetArt(preset.JumpImage, true))
        { Check(walk.ImageAt(.2).Width > 0 && walk.SourceAt(.4).Width == 256 && jump.ImageAt(0).Width == 256, "Decode " + preset.Name); }
        var settings = new Settings { CardX = int.MaxValue, CardY = int.MaxValue, PetCount = 5 };
        Point remembered = new Point(-1, -1);
        using (var overlay = new BreakOverlay(Screen.PrimaryScreen, settings, true, true, () => 300, () => {}, () => {}, p => remembered = p))
        {
            overlay.Show(); Check(overlay.Opacity < 1, "Fade starts transparent"); Pump(600);
            Check(overlay.Opacity == 1, "Fade reaches full opacity");
            var card = (Panel)Field(overlay, "card");
            Check(overlay.ClientRectangle.Contains(card.Bounds) && card.Region != null && !card.Region.IsVisible(0, 0), "Rounded card clamped to screen");
            var clamp = typeof(BreakOverlay).GetMethod("ClampCard", BindingFlags.NonPublic | BindingFlags.Instance);
            Check((Point)clamp.Invoke(overlay, new object[] { new Point(-50, -50) }) == Point.Empty, "Negative drag clamped");
            typeof(Control).GetMethod("OnMouseDown", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(card, new object[] { new MouseEventArgs(MouseButtons.Left, 1, 20, 20, 0) });
            typeof(Control).GetMethod("OnMouseUp", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(card, new object[] { new MouseEventArgs(MouseButtons.Left, 1, 20, 20, 0) });
            Check(remembered == card.Location, "Drag release remembers location");
            overlay.Dismiss(); Check(!overlay.IsDisposed, "Dismiss starts fade"); Pump(150);
            Check(overlay.Opacity > 0 && overlay.Opacity < 1, "Intermediate fade opacity"); Pump(450);
            Check(overlay.IsDisposed, "Fade disposes overlay");
        }
        settings.FadeEnabled = false;
        using (var overlay = new BreakOverlay(Screen.PrimaryScreen, settings, false, true, () => 300, () => {}, () => {}))
        { overlay.Show(); Check(overlay.Opacity == 1 && ((System.Windows.Forms.Timer)Field(overlay, "animation")).Interval == 1000, "Secondary display updates once a second"); overlay.Dismiss(); Check(overlay.IsDisposed, "Fade off closes immediately"); }
        Settings saved = null;
        using (var form = new SettingsForm(new Settings(), s => saved = s, s => {}, () => "Test"))
        {
            form.Show(); Application.DoEvents();
            var tabs = form.Controls.OfType<TabControl>().Single(); tabs.SelectedIndex = 3; Application.DoEvents();
            var presets = (ComboBox)Field(form, "presets"); presets.SelectedIndex = 1;
            Button(form, "Edit selected").PerformClick();
            ((TextBox)Field(form, "name")).Text = "My Bernie"; Button(form, "Save named pet").PerformClick();
            Check(presets.Items.Count == 4, "Save named preset adds selection");
            var roster = (CheckedListBox)Field(form, "screenPets");
            Check(((PetPreset)roster.Items[0]).Name == "John" && ((PetPreset)roster.Items[1]).Name == "S-Yosef", "Renamed checkbox choices");
            roster.SetItemChecked(1, true);
            ((NumericUpDown)Field(form, "count")).Value = 3;
            var slider = (TrackBar)Field(form, "fadeDuration"); slider.Value = 12; Button(form, "Save settings").PerformClick();
            Check(saved != null && saved.Pets.Single().Name == "My Bernie" && saved.PetCount == 3 && saved.ScreenPets.Count == 2 && saved.FadeMilliseconds == 1200, "Preset and count saved by real settings controls");
        }
        Settings afterDelete = null;
        var deletionDraft = new Settings { PetName = "Yosef", WalkImage = "saved.gif", JumpImage = "" };
        deletionDraft.SavePet("Yosef"); deletionDraft.Validate();
        using (var form = new SettingsForm(deletionDraft, s => afterDelete = s, s => {}, () => "Test"))
        {
            form.Show(); Application.DoEvents(); var tabs = form.Controls.OfType<TabControl>().Single(); tabs.SelectedIndex = 2;
            var roster = (CheckedListBox)Field(form, "screenPets");
            Check(roster.Items.Cast<PetPreset>().Count(p => p.Name == "J-Yosef") == 1 && roster.Items.Cast<PetPreset>().Any(p => p.Name == "John"), "John and imported J-Yosef shown separately");
            roster.SelectedIndex = roster.Items.Cast<PetPreset>().ToList().FindIndex(p => p.Name == "J-Yosef");
            Button(form, "Delete selected").PerformClick();
            Check(!roster.Items.Cast<PetPreset>().Any(p => p.Name == "J-Yosef") && roster.CheckedItems.Count > 0, "Roster delete removes saved pet and selects fallback");
            tabs.SelectedIndex = 3; var presets = (ComboBox)Field(form, "presets");
            presets.SelectedIndex = presets.Items.Cast<PetPreset>().ToList().FindIndex(p => p.Name == "John"); Button(form, "Delete pet").PerformClick();
            Check(!presets.Items.Cast<PetPreset>().Any(p => p.Name == "John") && !roster.Items.Cast<PetPreset>().Any(p => p.Name == "John"), "Preset delete removes built-in from both lists");
            Button(form, "Save settings").PerformClick();
            Check(afterDelete != null && afterDelete.DeletedPets.Count == 2 && afterDelete.Pets.Count == 0 && afterDelete.VisiblePets().All(p => p.Name != "John" && p.Name != "J-Yosef"), "Deletion is included in saved settings");
        }
        var mixed = new Settings { PetCount = 3, ScreenPets = Settings.BuiltInPets().Take(2).ToList(), FadeMilliseconds = 1000 };
        using (var overlay = new BreakOverlay(Screen.PrimaryScreen, mixed, true, true, () => 30, () => {}, () => {}))
        {
            overlay.Show(); Pump(500); Check(overlay.Opacity > .1 && overlay.Opacity < .9, "Slider duration controls fade-in");
            var walks = (PetArt[])Field(overlay, "walks");
            Check(!object.ReferenceEquals(walks[0], walks[1]) && object.ReferenceEquals(walks[0], walks[2]), "Different pets render together and copies share artwork");
            Pump(650); Check(overlay.Opacity == 1, "Longer fade completes");
            overlay.Dismiss(); Pump(500); Check(!overlay.IsDisposed && overlay.Opacity > .1 && overlay.Opacity < .9, "Same slider duration controls fade-out");
            Pump(650); Check(overlay.IsDisposed, "Longer fade-out disposes");
        }
        Console.WriteLine("PASS: " + checks + " v2 desktop checks");
        if (args.Contains("--performance"))
        {
            Console.WriteLine("Windows: " + Environment.OSVersion + "; logical processors: " + Environment.ProcessorCount + "; screens: " + string.Join(", ", Screen.AllScreens.Select(s => s.Bounds.ToString())));
            Console.WriteLine("pets,cpu_percent_machine,private_MiB,working_set_MiB,paint_events");
            foreach (int count in new[] { 1, 3, 5 })
            {
                using (var overlay = new BreakOverlay(Screen.PrimaryScreen, new Settings { PetCount = count }, true, false, () => 300, () => {}, () => {}))
                {
                    int paints = 0; overlay.Paint += (sender, e) => paints++;
                    overlay.Show(); Pump(2000); GC.Collect(); GC.WaitForPendingFinalizers();
                    using (var process = Process.GetCurrentProcess())
                    {
                        paints = 0; var cpu = process.TotalProcessorTime; var sample = Stopwatch.StartNew(); Pump(10000); process.Refresh();
                        double percent = (process.TotalProcessorTime - cpu).TotalSeconds / sample.Elapsed.TotalSeconds / Environment.ProcessorCount * 100;
                        Console.WriteLine(count + "," + percent.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "," + (process.PrivateMemorySize64 / 1048576.0).ToString("F1") + "," + (process.WorkingSet64 / 1048576.0).ToString("F1") + "," + paints);
                        Check(paints > 100, "Performance sample must actually paint");
                    }
                    overlay.Dismiss(true);
                }
            }
        }
        return 0;
    }
}
