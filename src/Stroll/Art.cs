using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

namespace Stroll
{
    // Decode at import/open, never on an animation tick. GIFs retain frame timings.
    public sealed class PetArt : IDisposable
    {
        private Image image;
        private MemoryStream bytes;
        private int frames = 1;
        private int[] delays;
        private int selected = -1;
        private double cycle;
        public bool IsBuiltIn { get; private set; }
        public PetArt(string path, bool jump)
        {
            IsBuiltIn = string.IsNullOrWhiteSpace(path) || path.StartsWith("builtin:") || !File.Exists(path);
            if (IsBuiltIn)
            {
                string pet = path == "builtin:john" ? "john" : path == "builtin:smooth" ? "smooth" : "";
                string name = pet.Length == 0 ? (jump ? "Stroll.jump.png" : "Stroll.walk.png") :
                    "Stroll." + pet + (jump ? ".jump.png" : pet == "john" ? ".walk.png" : ".walk.gif");
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
                {
                    if (stream == null) throw new FileNotFoundException("Missing bundled pet artwork: " + name);
                    bytes = new MemoryStream(); stream.CopyTo(bytes);
                }
            }
            else bytes = new MemoryStream(File.ReadAllBytes(path));
            bytes.Position = 0; image = Image.FromStream(bytes);
            if (Array.IndexOf(image.FrameDimensionsList, FrameDimension.Time.Guid) >= 0)
            {
                frames = image.GetFrameCount(FrameDimension.Time);
                delays = new int[frames]; byte[] data = null;
                try { data = image.GetPropertyItem(0x5100).Value; } catch (ArgumentException) { }
                for (int i = 0; i < frames; i++)
                {
                    int delay = data != null && data.Length >= (i + 1) * 4 ? BitConverter.ToInt32(data, i * 4) * 10 : 100;
                    delays[i] = Math.Max(40, delay); cycle += delays[i] / 1000.0;
                }
            }
        }
        public Image ImageAt(double seconds)
        {
            if (frames > 1)
            {
                double ms = seconds % cycle * 1000; int i = 0;
                while (i < frames - 1 && ms >= delays[i]) { ms -= delays[i]; i++; }
                if (selected != i) { image.SelectActiveFrame(FrameDimension.Time, i); selected = i; }
            }
            return image;
        }
        public Rectangle SourceAt(double seconds)
        {
            // Built-in walk is a compact three-frame horizontal strip.
            if (IsBuiltIn && image.Width == image.Height * 3)
            { int frame = ((int)(seconds * 5)) % 4; if (frame == 3) frame = 1;
              return new Rectangle(frame * image.Height, 0, image.Height, image.Height); }
            return new Rectangle(0, 0, image.Width, image.Height);
        }
        public void Dispose() { image.Dispose(); bytes.Dispose(); }

        public static string Import(string source, bool background)
        {
            var info = new FileInfo(source);
            if (info.Length > 20 * 1024 * 1024) throw new ArgumentException("Choose a file smaller than 20 MB.");
            using (var image = Image.FromFile(source))
            {
                if (image.Width > 8192 || image.Height > 8192 || (long)image.Width * image.Height > 24000000)
                    throw new ArgumentException("Choose an image no larger than 24 megapixels and 8192 pixels per side.");
                if (!background && (image.Width > 2048 || image.Height > 2048))
                    throw new ArgumentException("Pet images must be 2048 pixels per side or smaller.");
                int count = 1;
                if (Array.IndexOf(image.FrameDimensionsList, FrameDimension.Time.Guid) >= 0)
                    count = image.GetFrameCount(FrameDimension.Time);
                if ((long)image.Width * image.Height * count > 64000000)
                    throw new ArgumentException("This animation is too large. Reduce its resolution or number of frames.");
            }
            string folder = Path.Combine(Settings.Folder, "assets"); Directory.CreateDirectory(folder);
            string dest = Path.Combine(folder, Guid.NewGuid().ToString("N") + Path.GetExtension(source).ToLowerInvariant());
            File.Copy(source, dest); return dest;
        }
    }
}
