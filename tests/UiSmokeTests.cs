// Optional smoke harness. Compile against Stroll.exe and run on a desktop/session.
// Does not change the user's startup registration or saved settings.
using System;
using System.Diagnostics;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Stroll;

public static class UiSmokeTests
{
    // Return a diagnostic instead of letting Windows show a crash-report dialog.
    [STAThread] public static int Main(string[] args)
    {
        try { return Run(args); }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static void Capture(Form form, Bitmap image)
    {
        form.Refresh(); form.Update(); Application.DoEvents();
        // Draw the actual controls without requiring desktop screen-capture access.
        form.DrawToBitmap(image, new Rectangle(Point.Empty, form.Size));
    }
    [STAThread]
    public static int Run(string[] args)
    {
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        string folder = args.Length > 0 ? args[0] : "smoke"; Directory.CreateDirectory(folder);
        var settings = new Settings();
        using (var form = new SettingsForm(settings, s => { }, s => { }, () => "Next break in 00:20:00"))
        {
            form.Show(); Application.DoEvents();
            var tabs = form.Controls.OfType<TabControl>().Single();
            for (int i = 0; i < tabs.TabPages.Count; i++)
            {
                tabs.SelectedIndex = i; Application.DoEvents();
                using (var image = new Bitmap(form.Width, form.Height))
                { Capture(form, image); image.Save(Path.Combine(folder, "settings-" + i + ".png"), ImageFormat.Png); }
            }
            form.Close();
        }
        using (var prompt = new PromptForm(settings, () => { }, () => { }, () => { }))
        {
            prompt.Show(); Application.DoEvents();
            using (var image = new Bitmap(prompt.Width, prompt.Height))
            { Capture(prompt, image); image.Save(Path.Combine(folder, "prompt.png")); }
            prompt.Dismiss();
        }
        using (var overlay = new BreakOverlay(Screen.PrimaryScreen, settings, true, true, () => 300, () => { }, () => { }))
        {
            overlay.Show(); var watch = Stopwatch.StartNew(); while (watch.ElapsedMilliseconds < 600) { Application.DoEvents(); Thread.Sleep(10); }
            using (var image = new Bitmap(overlay.Width, overlay.Height))
            { Capture(overlay, image); image.Save(Path.Combine(folder, "overlay.png")); }
            overlay.Dismiss();
        }
        using (var palette = new PaletteDialog(Color.FromArgb(23, 43, 41)))
        {
            palette.Show(); Application.DoEvents();
            using (var image = new Bitmap(palette.Width, palette.Height))
            { Capture(palette, image); image.Save(Path.Combine(folder, "colour-wheel.png")); }
            palette.Close();
        }
        // Exercise bundled artwork and its actual resource decoding.
        using (var walking = new PetArt("", false))
        using (var jumping = new PetArt("", true))
        {
            if (walking.SourceAt(0).Width != 256 || walking.SourceAt(.2).X != 256 || jumping.ImageAt(0).Width != 256)
                throw new Exception("Bundled pet resources did not decode as expected.");
        }
        Console.WriteLine("PASS · Settings tabs, prompt, overlay, colour wheel and pet resource smoke checks.");
        return 0;
    }
}
