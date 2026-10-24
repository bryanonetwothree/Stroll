using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Microsoft.Win32;

namespace Stroll
{
    [DataContract]
    public sealed class PetPreset
    {
        [DataMember] public string Name;
        [DataMember] public string WalkImage;
        [DataMember] public string JumpImage;
        public string Key { get { return ((Name ?? "") + "\n" + (WalkImage ?? "") + "\n" + (JumpImage ?? "")).ToUpperInvariant(); } }
        public PetPreset Copy() { return (PetPreset)MemberwiseClone(); }
        public override string ToString() { return Name; }
    }

    [DataContract]
    public sealed class Settings
    {
        [DataMember] public double WorkMinutes = 20;
        [DataMember] public double BreakMinutes = 5;
        [DataMember] public double SnoozeMinutes = 3;
        [DataMember] public bool WarningEnabled = true;
        [DataMember] public int WarningSeconds = 30;
        [DataMember] public string BackgroundColor = "#172B29";
        [DataMember] public string BackgroundImage = "";
        [DataMember] public string WalkImage = "builtin:john";
        [DataMember] public string JumpImage = "builtin:john";
        [DataMember] public string PetName = "John";
        [DataMember] public int Edges = 1;
        [DataMember] public int PetSize = 160;
        [DataMember] public int PetSpeed = 65;
        [DataMember] public bool StartWithWindows = false;
        [DataMember] public string Message = "Look far. Relax your eyes.";
        [DataMember] public int PetCount = 1;
        [DataMember] public bool FadeEnabled = true;
        [DataMember] public int FadeMilliseconds = 400;
        [DataMember] public List<PetPreset> ScreenPets;
        public double FadeSeconds { get { return FadeEnabled ? FadeMilliseconds / 1000.0 : 0; } }
        public PetPreset[] VisiblePets()
        {
            var selected = ScreenPets != null && ScreenPets.Count > 0 ? ScreenPets :
                new List<PetPreset> { new PetPreset { Name = PetName, WalkImage = WalkImage, JumpImage = JumpImage } };
            return Enumerable.Range(0, Math.Max(PetCount, selected.Count)).Take(5).Select(i => selected[i % selected.Count]).ToArray();
        }
        [DataMember] public int CardX = -1;
        [DataMember] public int CardY = -1;
        [DataMember] public List<PetPreset> Pets = new List<PetPreset>();
        [DataMember] public List<PetPreset> DeletedPets = new List<PetPreset>();
        public PetPreset CurrentPet()
        { return new PetPreset { Name = PetName, WalkImage = WalkImage, JumpImage = JumpImage }; }
        public PetPreset[] AvailablePets()
        {
            var deleted = new HashSet<string>(DeletedPets.Select(pet => pet.Key));
            return BuiltInPets().Concat(Pets).Concat(new[] { CurrentPet() }).Concat(ScreenPets ?? new List<PetPreset>())
                .Where(pet => !deleted.Contains(pet.Key)).GroupBy(pet => pet.Key).Select(group => group.First()).ToArray();
        }
        public bool DeletePet(PetPreset pet)
        {
            if (pet == null || AvailablePets().Length <= 1) return false;
            var visible = (ScreenPets ?? new List<PetPreset> { CurrentPet() }).Where(item => item.Key != pet.Key).ToList();
            Pets.RemoveAll(item => item.Key == pet.Key);
            if (!DeletedPets.Any(item => item.Key == pet.Key)) DeletedPets.Add(pet.Copy());
            var next = AvailablePets().First();
            if (CurrentPet().Key == pet.Key) UsePet(next);
            ScreenPets = visible.Count > 0 ? visible : new List<PetPreset> { next.Copy() };
            return true;
        }
        public void RestoreBuiltIns()
        {
            var bundled = new HashSet<string>(BuiltInPets().Select(pet => pet.Key));
            DeletedPets.RemoveAll(pet => bundled.Contains(pet.Key));
        }
        // DataContract does not run field initializers. Preserve v1 artwork/timings,
        // and explicitly default only the new fields when reading older settings.
        [OnDeserializing] private void BeforeLoad(StreamingContext context)
        { PetCount = 1; FadeEnabled = true; FadeMilliseconds = 400; CardX = CardY = -1; Pets = new List<PetPreset>(); DeletedPets = new List<PetPreset>(); }
        public static readonly string Folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Stroll");
        public static string FilePath { get { return Path.Combine(Folder, "settings.json"); } }
        public Settings Copy()
        { var copy = (Settings)MemberwiseClone(); copy.Pets = Pets.Select(p => p.Copy()).ToList(); copy.DeletedPets = DeletedPets.Select(p => p.Copy()).ToList(); copy.ScreenPets = ScreenPets == null ? null : ScreenPets.Select(p => p.Copy()).ToList(); return copy; }
        public void UsePet(PetPreset pet)
        { PetName = pet.Name; WalkImage = pet.WalkImage; JumpImage = pet.JumpImage; }
        public void SavePet(string name)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0) throw new ArgumentException("Give your pet a name first.");
            var pet = Pets.Find(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            if (pet == null) { pet = new PetPreset(); Pets.Add(pet); }
            pet.Name = name; pet.WalkImage = WalkImage; pet.JumpImage = JumpImage; PetName = name;
            DeletedPets.RemoveAll(item => item.Key == pet.Key);
        }
        public static PetPreset[] BuiltInPets()
        {
            return new[] {
                new PetPreset { Name = "John", WalkImage = "builtin:john", JumpImage = "builtin:john" },
                new PetPreset { Name = "S-Yosef", WalkImage = "", JumpImage = "" },
                new PetPreset { Name = "Smooth St Bernard", WalkImage = "builtin:smooth", JumpImage = "builtin:smooth" }
            };
        }
        public void Validate()
        {
            if (double.IsNaN(WorkMinutes) || WorkMinutes < 1 || WorkMinutes > 240) WorkMinutes = 20;
            if (double.IsNaN(BreakMinutes) || BreakMinutes < .1 || BreakMinutes > 60) BreakMinutes = 5;
            if (double.IsNaN(SnoozeMinutes) || SnoozeMinutes < 1 || SnoozeMinutes > 120) SnoozeMinutes = 3;
            WarningSeconds = Math.Max(1, Math.Min(300, WarningSeconds));
            PetSize = Math.Max(64, Math.Min(300, PetSize));
            PetSpeed = Math.Max(20, Math.Min(160, PetSpeed));
            FadeMilliseconds = Math.Max(100, Math.Min(3000, FadeMilliseconds));
            if (ScreenPets != null) ScreenPets = ScreenPets.Where(p => p != null && !string.IsNullOrWhiteSpace(p.Name)).Take(5).ToList();
            PetName = Renamed(PetName, WalkImage);
            if (ScreenPets != null) foreach (var pet in ScreenPets) pet.Name = Renamed(pet.Name, pet.WalkImage);
            PetCount = Math.Max(1, Math.Min(5, PetCount));
            CardX = Math.Max(-1, CardX); CardY = Math.Max(-1, CardY);
            if (Pets == null) Pets = new List<PetPreset>();
            Pets = Pets.Where(p => p != null && !string.IsNullOrWhiteSpace(p.Name)).ToList();
            foreach (var pet in Pets) pet.Name = Renamed(pet.Name, pet.WalkImage);
            if (DeletedPets == null) DeletedPets = new List<PetPreset>();
            DeletedPets = DeletedPets.Where(p => p != null && !string.IsNullOrWhiteSpace(p.Name)).ToList();
            foreach (var pet in DeletedPets) pet.Name = Renamed(pet.Name, pet.WalkImage);
            Pets = Pets.GroupBy(pet => pet.Key).Select(group => group.First()).ToList();
            Edges &= 15; if (Edges == 0) Edges = 1;
            try { ColorTranslator.FromHtml(BackgroundColor); } catch { BackgroundColor = "#172B29"; }
            if (string.IsNullOrWhiteSpace(BackgroundColor)) BackgroundColor = "#172B29";
            if (string.IsNullOrWhiteSpace(Message)) Message = "Look far. Relax your eyes.";
            if (string.IsNullOrWhiteSpace(PetName)) PetName = "S-Yosef";
        }
        private static string Renamed(string name, string walk)
        {
            if (walk == "builtin:john" && (name == "John" || name == "J-Yosef")) return "John";
            return name == "Bernie" ? "S-Yosef" : name == "Yosef" ? "J-Yosef" : name;
        }
        public static Settings Load()
        {
            if (!File.Exists(FilePath)) return new Settings();
            try
            {
                using (var stream = File.OpenRead(FilePath))
                {
                    var s = (Settings)new DataContractJsonSerializer(typeof(Settings)).ReadObject(stream);
                    s.Validate(); return s;
                }
            }
            catch (Exception e) when (e is IOException || e is SerializationException || e is ArgumentException)
            { return new Settings(); }
        }
        public void Save()
        {
            Validate(); Directory.CreateDirectory(Folder);
            string temporary = FilePath + ".tmp";
            using (var stream = File.Create(temporary))
                new DataContractJsonSerializer(typeof(Settings)).WriteObject(stream, this);
            if (File.Exists(FilePath)) File.Replace(temporary, FilePath, FilePath + ".bak");
            else File.Move(temporary, FilePath);
        }
        public static void SetStartup(bool enabled)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
            {
                if (enabled) key.SetValue("Stroll", "\"" + System.Windows.Forms.Application.ExecutablePath + "\" --quiet");
                else key.DeleteValue("Stroll", false);
            }
        }
    }
}
