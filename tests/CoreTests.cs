using System;
using System.Linq;
using Stroll;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Json;

public static class CoreTests
{
    private static int assertions;
    private static void Check(bool ok, string message)
    { assertions++; if (!ok) throw new Exception(message); }
    private static void Near(double a, double b, string label)
    { Check(Math.Abs(a - b) < .00001, label + ": " + a + " != " + b); }
    public static int Main()
    { try { return Run(); } catch (Exception error) { Console.Error.WriteLine(error); return 1; } }
    public static int Run()
    {
        double t = 0; var s = new Settings();
        var clock = new BreakClock(() => t, () => s); int warnings = 0;
        clock.Warning += () => warnings++;
        Check(clock.Phase == Phase.Working, "Starts working"); Near(clock.Remaining, 1200, "20 minute default");
        t = 1170; clock.Tick(); Check(warnings == 1, "30 second warning");
        t = 1171; clock.Tick(); Check(warnings == 1, "No duplicate warning");
        t = 1200; clock.Tick(); Check(clock.Phase == Phase.Prompt, "Prompt after work interval");
        t = 1300; clock.Tick(); Check(clock.Phase == Phase.Prompt, "Prompt never auto-starts a break");
        clock.Snooze(); Near(clock.Remaining, 180, "Snooze 3 minutes");
        t += 180; clock.Tick(); Check(clock.Phase == Phase.Prompt, "Snooze prompts again");
        clock.Snooze(); t += 180; clock.Tick(); Check(clock.Phase == Phase.Prompt, "Repeated snooze");
        clock.StartBreak(); Near(clock.Remaining, 300, "Full 5 minute break");
        t += 299; clock.Tick(); Check(clock.Phase == Phase.Breaking, "Must complete break");
        t += 1; clock.Tick(); Check(clock.Phase == Phase.Working, "Completion restarts work");
        Near(clock.Remaining, 1200, "Fresh interval after completion");
        t += 1200; clock.Tick(); clock.Skip(); Near(clock.Remaining, 1200, "Skip restarts 20 minutes");
        t += 60; clock.Pause(); Near(clock.Remaining, 1140, "Pause accounts for elapsed time");
        t += 900; clock.Tick(); Near(clock.Remaining, 1140, "Paused time does not count");
        clock.Resume(); t += 10; clock.Tick(); Near(clock.Remaining, 1130, "Resume preserves interval");
        clock.FreshInterval(); Near(clock.Remaining, 1200, "Wake resets interval");
        Check(clock.Phase == Phase.Working, "Wake clears pause");
        clock.StartBreak(); t += 30; clock.Tick(); clock.Snooze();
        t += 180; clock.Tick(); clock.StartBreak(); Near(clock.Remaining, 300, "Interrupted break requires full duration");
        s.WarningEnabled = false; clock.FreshInterval(); int previous = warnings;
        t += 1199; clock.Tick(); Check(warnings == previous, "Disabled warning stays quiet");
        t += 500; clock.Tick(); Check(clock.Phase == Phase.Prompt, "Long delayed tick produces prompt");
        s.WorkMinutes = 1; s.BreakMinutes = .1; s.SnoozeMinutes = 1; clock.FreshInterval();
        Near(clock.Remaining, 60, "Custom work duration"); clock.StartBreak(); Near(clock.Remaining, 6, "Custom break duration");
        clock.Snooze(); Near(clock.Remaining, 60, "Custom snooze duration");
        clock.Pause(); clock.Pause(); clock.Resume(); Check(clock.Phase == Phase.Snoozed, "Repeated pause safe");
        foreach (int mask in new[] { 1, 2, 4, 8, 3, 6, 12, 9, 7, 11, 13, 14, 15, 5, 10 })
        {
            bool hop = false, reverse = false;
            for (int i = 0; i < 4000; i++)
            {
                PetPose p = PetRoute.At(i * .1, 1920, 1080, 160, 65, mask);
                Check(!double.IsNaN(p.X) && !double.IsNaN(p.Y), "Finite route " + mask);
                Check(p.X >= 80 && p.X <= 1840 && p.Y >= 80 && p.Y <= 1000, "Pet stays within screen " + mask);
                if (p.Jumping) hop = true; if (p.Reverse) reverse = true;
            }
            Check(hop == (mask == 5 || mask == 10), "Only opposite edges hop " + mask);
            Check(reverse == (mask != 15 && mask != 5 && mask != 10), "Connected partial routes reverse " + mask);
        }
        var top = PetRoute.At(0, 1920, 1080, 160, 65, 4); Near(top.Angle, -180, "Top is upside down");
        var right = PetRoute.At(0, 1920, 1080, 160, 65, 2); Near(right.Angle, -90, "Right walks up wall");
        for (int mask = 1; mask < 16; mask++)
        {
            var before = PetRoute.At(10, 1920, 1080, 160, 65, mask);
            var after = PetRoute.At(10.001, 1920, 1080, 160, 65, mask);
            Check(Math.Abs(after.X - before.X) < 2 && Math.Abs(after.Y - before.Y) < 2, "Smooth local motion");
        }
        var invalid = new Settings { WorkMinutes = double.NaN, Edges = 0, BackgroundColor = "nonsense", PetSize = 999 };
        invalid.Validate(); Near(invalid.WorkMinutes, 20, "Invalid duration repaired");
        Check(invalid.Edges == 1 && invalid.PetSize == 300 && invalid.BackgroundColor == Settings.DefaultBackgroundColor, "Invalid settings repaired");
        var fresh = new Settings();
        Check(fresh.PetName == "John" && fresh.WalkImage == "builtin:john" && fresh.BackgroundColor == Settings.DefaultBackgroundColor && fresh.Language == "en" && fresh.PetCount == 1 && fresh.FadeEnabled, "V3 defaults");
        Check(Settings.BuiltInPets().Any(pet => pet.Name == "J-Yosef" && pet.WalkImage == "builtin:jyosef") && !Settings.BuiltInPets().Any(pet => pet.Name == "Mudkip" || pet.Name == "Dorae"), "V3 built-ins exclude protected characters");
        fresh.SavePet("My John"); var copy = fresh.Copy(); copy.Pets[0].Name = "Draft only";
        Check(fresh.Pets[0].Name == "My John", "Draft preset edits are isolated");
        fresh.SavePet("my john"); Check(fresh.Pets.Count == 1, "Named preset update is case insensitive");
        fresh.UsePet(Settings.BuiltInPets()[1]); fresh.SavePet("My Bernie");
        fresh.UsePet(fresh.Pets[0]); Check(fresh.WalkImage == "builtin:john", "Preset restores artwork");
        fresh.CardX = 123; fresh.CardY = 456; fresh.PetCount = 5;
        var serializer = new DataContractJsonSerializer(typeof(Settings));
        using (var stream = new MemoryStream())
        {
            serializer.WriteObject(stream, fresh); stream.Position = 0;
            var loaded = (Settings)serializer.ReadObject(stream); loaded.Validate();
            Check(loaded.Pets.Count == 2 && loaded.PetCount == 5 && loaded.CardX == 123 && loaded.CardY == 456, "V2 settings round trip");
        }
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("{\"PetName\":\"Bernie\",\"WalkImage\":\"\",\"JumpImage\":\"\",\"WorkMinutes\":25}")))
        {
            var legacy = (Settings)serializer.ReadObject(stream); legacy.Validate();
            Check(legacy.PetName == "S-Yosef" && legacy.WalkImage == "" && legacy.WorkMinutes == 25 && legacy.PetCount == 1 && legacy.FadeEnabled && legacy.BackgroundColor == Settings.DefaultBackgroundColor && legacy.Language == "en" && legacy.CardX == -1 && legacy.Pets.Count == 0, "V1 migration preserves user choices with V3 defaults");
        }
        fresh.PetCount = 99; fresh.Validate(); Check(fresh.PetCount == 5, "Pet count upper limit");
        fresh.PetCount = 0; fresh.Validate(); Check(fresh.PetCount == 1, "Pet count lower limit");
        clock.StartBreak(); clock.HoldBreakFor(.4); t += .2; clock.Tick(); Near(clock.Remaining, 6, "Fade does not consume countdown");
        t += .3; clock.Tick(); Near(clock.Remaining, 5.9, "Countdown begins after fade hold");
        clock.Pause(); t += 10; clock.Resume(); t += 1; clock.Tick(); Near(clock.Remaining, 4.9, "Pause after fade preserves countdown");
        var mixed = new Settings { PetCount = 3, ScreenPets = new System.Collections.Generic.List<PetPreset> { Settings.BuiltInPets()[0], Settings.BuiltInPets()[1] }, FadeMilliseconds = 1500 };
        Check(mixed.VisiblePets().Length == 3 && mixed.VisiblePets()[0].Name == "John" && mixed.VisiblePets()[1].Name == "S-Yosef" && mixed.VisiblePets()[2].Name == "John", "Mixed choices repeat in order");
        var mixedCopy = mixed.Copy(); mixedCopy.ScreenPets[0].Name = "Draft";
        Check(mixed.ScreenPets[0].Name == "John", "Screen choices are isolated in draft");
        using (var stream = new MemoryStream())
        { serializer.WriteObject(stream, mixed); stream.Position = 0; var loaded = (Settings)serializer.ReadObject(stream); loaded.Validate(); Check(loaded.ScreenPets.Count == 2 && loaded.FadeSeconds == 1.5, "Checkbox choices and fade duration round trip"); }
        mixed.FadeMilliseconds = 99999; mixed.Validate(); Near(mixed.FadeSeconds, 3, "Fade duration upper bound");
        mixed.FadeEnabled = false; Near(mixed.FadeSeconds, 0, "Fade off overrides slider");
        mixed.FadeMilliseconds = 0; mixed.Validate(); Check(mixed.FadeMilliseconds == 100, "Fade duration lower bound");
        var oldName = new Settings { PetName = "Yosef" }; oldName.Validate(); Check(oldName.PetName == "J-Yosef", "Yosef name migration");
        var collection = new Settings { PetName = "Yosef", WalkImage = "imported.gif", JumpImage = "" };
        collection.SavePet("Yosef"); collection.Validate();
        Check(collection.AvailablePets().Count(p => p.Name == "J-Yosef") == 2 &&
            collection.AvailablePets().Any(p => p.Name == "J-Yosef" && p.WalkImage == "imported.gif") &&
            collection.AvailablePets().Any(p => p.Name == "J-Yosef" && p.WalkImage == "builtin:jyosef") &&
            collection.AvailablePets().Any(p => p.Name == "John" && p.WalkImage == "builtin:john"), "Imported Yosef, bundled J-Yosef and bundled John are distinct");
        var corrected = new Settings { PetName = "J-Yosef", WalkImage = "builtin:john" }; corrected.Validate(); Check(corrected.PetName == "John", "Correct earlier bundled John rename");
        collection.ScreenPets = new System.Collections.Generic.List<PetPreset> { collection.CurrentPet() };
        var removed = collection.CurrentPet(); Check(collection.DeletePet(removed), "Delete saved pet");
        Check(collection.Pets.Count == 0 && !collection.AvailablePets().Any(p => p.Key == removed.Key) && collection.VisiblePets().All(p => p.Key != removed.Key), "Deleted pet removed from catalog and screen with fallback");
        var john = Settings.BuiltInPets()[0]; Check(collection.DeletePet(john), "Delete bundled pet");
        using (var stream = new MemoryStream())
        { serializer.WriteObject(stream, collection); stream.Position = 0; var loaded = (Settings)serializer.ReadObject(stream); loaded.Validate(); Check(!loaded.AvailablePets().Any(p => p.Key == john.Key || p.Key == removed.Key), "Deleted pets remain absent after reload"); }
        var collectionCopy = collection.Copy(); collectionCopy.DeletedPets[0].Name = "Draft"; Check(collection.DeletedPets[0].Name == removed.Name, "Deleted list isolated in draft");
        collection.RestoreBuiltIns(); Check(collection.AvailablePets().Any(p => p.Key == john.Key) && !collection.AvailablePets().Any(p => p.Key == removed.Key), "Restore brings back only built-ins");
        collection.DeletePet(Settings.BuiltInPets()[0]); collection.DeletePet(Settings.BuiltInPets()[1]); Check(!collection.DeletePet(collection.AvailablePets()[0]), "Keep last available pet");
        Console.WriteLine("PASS · " + assertions + " assertions: timer transitions, warnings, pause, settings and all 15 edge combinations.");
        return 0;
    }
}
