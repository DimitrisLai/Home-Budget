using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace BudgetDesk
{
    internal static class StartupDiagnostics
    {
        private static readonly object Sync = new object();
        private static string logPath;
        private static string stage = "Enter application";
        internal static bool Automated { get; private set; }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int MessageBoxW(IntPtr owner, string text, string caption, uint flags);

        internal static void Initialize(string[] args)
        {
            string requested = null;
            foreach (string arg in args)
            {
                if (arg.StartsWith("--startup-log=", StringComparison.Ordinal)) requested = arg.Substring(14);
                if (arg.StartsWith("--capture=", StringComparison.Ordinal)) Automated = true;
            }
            string defaultFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BudgetDesk", "Logs");
            string name = "startup-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff") + "-" + Process.GetCurrentProcess().Id + ".txt";
            string[] candidates = { requested, Path.Combine(defaultFolder, name), Path.Combine(Path.GetTempPath(), "BudgetDesk-" + name) };
            foreach (string candidate in candidates)
            {
                if (String.IsNullOrWhiteSpace(candidate)) continue;
                try
                {
                    string full = Path.GetFullPath(candidate);
                    Directory.CreateDirectory(Path.GetDirectoryName(full));
                    File.WriteAllText(full, "Budget Desk 1.0.0 startup report\r\n" +
                        "UTC: " + DateTime.UtcNow.ToString("o") + "\r\n" +
                        "OS: " + Environment.OSVersion + "\r\n" +
                        "CLR: " + Environment.Version + "\r\n" +
                        "Process: " + (IntPtr.Size == 8 ? "64-bit" : "32-bit") + "\r\n" +
                        "Budget data and account balances are not included.\r\n", new UTF8Encoding(false));
                    logPath = full;
                    break;
                }
                catch (Exception) { /* A read-only program folder must not prevent startup. */ }
            }
            Stage("Enter application");
        }

        internal static void Stage(string value)
        {
            stage = value;
            Write(DateTime.UtcNow.ToString("HH:mm:ss.fff") + "  " + value);
        }

        private static void Write(string text)
        {
            try
            {
                lock (Sync)
                {
                    if (logPath != null) File.AppendAllText(logPath, text + "\r\n", new UTF8Encoding(false));
                }
            }
            catch (Exception) { }
        }

        internal static void Fail(Exception error, bool showDialog)
        {
            string details = "FAILED during: " + stage + "\r\n" + error;
            Write(details);
            try { Console.Error.WriteLine(details); } catch (Exception) { }
            if (!showDialog) return;
            string message = "Budget Desk could not open or continue.\r\n\r\n" + error.Message +
                "\r\n\r\nStep: " + stage + (logPath == null ? "" : "\r\n\r\nReport:\r\n" + logPath) +
                "\r\n\r\nPlease send the startup report so the exact failure can be fixed.";
            try
            {
                // A native dialog still works when System.Windows.Forms cannot load.
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    MessageBoxW(IntPtr.Zero, message, "Budget Desk startup error", 0x00000010 | 0x00010000 | 0x00040000);
            }
            catch (Exception) { }
        }
    }
}
