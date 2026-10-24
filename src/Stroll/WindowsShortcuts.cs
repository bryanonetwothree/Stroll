using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Windows.Forms;

namespace Stroll
{
    // Windows shell integration only: no installer, background service or packages.
    public static class WindowsShortcuts
    {
        public const string AppId = "Stroll.EyeBreak";
        public const string OpenEventName = @"Local\Stroll.OpenSettings.1";
        public static string StartMenuPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Stroll.lnk"); } }
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);
        public static void SetIdentity() { Marshal.ThrowExceptionForHR(SetCurrentProcessExplicitAppUserModelID(AppId)); }
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern void SHChangeNotify(uint change, uint flags,
            [MarshalAs(UnmanagedType.LPWStr)] string item, IntPtr other);
        public static void AddToStartMenu()
        {
            bool existed = File.Exists(StartMenuPath);
            CreateShortcut(StartMenuPath, Application.ExecutablePath);
            // Saving IShellLink alone does not notify the shell's app catalog.
            SHChangeNotify(existed ? 0x2000u : 2u, 0x1005, StartMenuPath, IntPtr.Zero);
            SHChangeNotify(0x1000, 0x1005, Path.GetDirectoryName(StartMenuPath), IntPtr.Zero);
        }
        public static void CreateShortcut(string shortcutPath, string executable)
        {
            executable = Path.GetFullPath(executable);
            if (!File.Exists(executable)) throw new FileNotFoundException("Stroll executable was not found.", executable);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(shortcutPath)));
            object shell = new ShellLink();
            try
            {
                var link = (IShellLink)shell;
                link.SetPath(executable); link.SetArguments("");
                link.SetWorkingDirectory(Path.GetDirectoryName(executable));
                link.SetDescription("Stroll - eye breaks and desktop pets"); link.SetIconLocation(executable, 0); link.SetShowCmd(1);
                var key = new PropertyKey { Format = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), Id = 5 };
                var value = new PropVariant { Type = 31, Pointer = Marshal.StringToCoTaskMemUni(AppId) };
                try { var store = (IPropertyStore)shell; store.SetValue(ref key, ref value); store.Commit(); }
                finally { Marshal.FreeCoTaskMem(value.Pointer); }
                ((IPersistFile)shell).Save(Path.GetFullPath(shortcutPath), true);
            }
            finally { Marshal.FinalReleaseComObject(shell); }
        }
        [ComImport, Guid("00021401-0000-0000-C000-000000000046")] private class ShellLink { }
        [StructLayout(LayoutKind.Sequential)] private struct PropertyKey { public Guid Format; public uint Id; }
        // PROPVARIANT has a 16-byte union at offset 8 on x64 (24 bytes total).
        [StructLayout(LayoutKind.Explicit, Size = 24)] private struct PropVariant
        { [FieldOffset(0)] public ushort Type; [FieldOffset(8)] public IntPtr Pointer; }
        [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPropertyStore
        {
            void GetCount(out uint count); void GetAt(uint index, out PropertyKey key);
            void GetValue(ref PropertyKey key, out PropVariant value);
            void SetValue(ref PropertyKey key, ref PropVariant value); void Commit();
        }
        [ComImport, Guid("000214F9-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellLink
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder path, int count, IntPtr findData, uint flags);
            void GetIDList(out IntPtr idList); void SetIDList(IntPtr idList);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder description, int count);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string description);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder path, int count);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string path);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder arguments, int count);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string arguments);
            void GetHotkey(out short hotkey); void SetHotkey(short hotkey); void GetShowCmd(out int show); void SetShowCmd(int show);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder path, int count, out int index);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string path, int index);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string path, uint reserved);
            void Resolve(IntPtr window, uint flags); void SetPath([MarshalAs(UnmanagedType.LPWStr)] string path);
        }
    }
}

