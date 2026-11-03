using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("Budget Desk")]
[assembly: AssemblyDescription("Offline personal accounts, recurring payments and daily expenses")]
[assembly: AssemblyCompany("Dimitris L.")]
[assembly: AssemblyProduct("Budget Desk")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.5", FrameworkDisplayName = ".NET Framework 4.5")]

namespace BudgetDesk
{
    internal static class Program
    {
        private static bool uiFailed;

        [STAThread]
        private static int Main(string[] args)
        {
            // Keep the entry point independent of WinForms. Run must be JITted
            // inside this try, so a missing UI assembly also produces a report.
            try
            {
                StartupDiagnostics.Initialize(args);
                AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e) {
                    StartupDiagnostics.Fail(e.ExceptionObject as Exception ?? new Exception("Unhandled runtime failure."), false);
                };
                return Run(args);
            }
            catch (Exception ex)
            {
                StartupDiagnostics.Fail(ex, !StartupDiagnostics.Automated);
                return 1;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int Run(string[] args)
        {
            StartupDiagnostics.Stage("Initialize Windows controls");
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            bool demo = args.Contains("--demo");
            string capture = args.FirstOrDefault(a => a.StartsWith("--capture="));
            string customData = args.FirstOrDefault(a => a.StartsWith("--data-dir="));
            if (capture == null)
            {
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e) {
                    uiFailed = true;
                    StartupDiagnostics.Fail(e.Exception, true);
                    Application.Exit();
                };
            }
            string folder = customData != null ? customData.Substring(11) : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BudgetDesk", demo ? "Demo" : "Data");
            StartupDiagnostics.Stage("Acquire budget lock");
            bool acquired;
            string lockName;
            // Framework has TrimEnd(char[]); Mono's runtime also exposes
            // TrimEnd(char), which must never be bound into this Windows build.
            string canonicalFolder = Path.GetFullPath(folder).TrimEnd(new char[] { Path.DirectorySeparatorChar }).ToUpperInvariant();
            using (SHA256 sha = SHA256.Create())
                lockName = "BudgetDesk-" + BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(canonicalFolder))).Replace("-", "").Substring(0, 24);
            using (Mutex mutex = new Mutex(true, lockName, out acquired))
            {
                if (!acquired) {
                    StartupDiagnostics.Stage("Budget is already open in another process");
                    MessageBox.Show("Budget Desk is already open. Switch to its existing window.", "Budget Desk"); return 0;
                }
                try
                {
                    StartupDiagnostics.Stage("Load saved budget");
                    BudgetStore store = new BudgetStore(folder); BudgetData data;
                    if (demo && !File.Exists(store.FilePath)) { data = Ledger.Demo(DateTime.Today); store.Save(data); }
                    else
                    {
                        try { data = store.Load(); }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Your data file could not be opened. It has been left untouched.\n\n" + ex.Message + "\n\nSelect a JSON backup in the next window, or Cancel to exit.\nAutomatic backups are in:\n" + Path.Combine(folder, "Backups"), "Budget Desk recovery", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            using (OpenFileDialog dialog = new OpenFileDialog { Title = "Choose a Budget Desk backup", Filter = "Budget Desk backup (*.json)|*.json", InitialDirectory = Path.Combine(folder, "Backups") })
                            {
                                if (dialog.ShowDialog() != DialogResult.OK) return 1;
                                data = BudgetStore.ReadBackup(dialog.FileName);
                                Directory.CreateDirectory(folder);
                                if (File.Exists(store.FilePath)) File.Copy(store.FilePath, Path.Combine(folder, "unreadable-" + DateTime.UtcNow.Ticks + ".json"));
                                store.Save(data);
                            }
                        }
                    }
                    StartupDiagnostics.Stage("Build main window");
                    using (MainForm form = new MainForm(store, data, demo))
                    {
                        if (capture != null) SetupCapture(form, capture.Substring(10));
                        form.Shown += delegate { StartupDiagnostics.Stage("Window shown"); };
                        StartupDiagnostics.Stage("Open main window");
                        Application.Run(form);
                    }
                    StartupDiagnostics.Stage(uiFailed ? "Exit after UI failure" : "Window closed normally");
                    return uiFailed ? 1 : 0;
                }
                finally { mutex.ReleaseMutex(); }
            }
        }
        private static void SetupCapture(MainForm form, string folder)
        {
            Directory.CreateDirectory(folder);
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 700 }; int step = 0;
            timer.Tick += delegate {
                if (step < 5)
                {
                    form.ShowPage(step); form.Refresh(); Application.DoEvents();
                    using (Bitmap b = new Bitmap(form.Width, form.Height))
                    {
                        using (Graphics g = Graphics.FromImage(b)) g.CopyFromScreen(form.Location, Point.Empty, form.Size);
                        b.Save(Path.Combine(folder, new[] { "accounts.png", "daily-expenses.png", "monthly-overview.png", "yearly-overview.png", "savings-goals.png" }[step]), System.Drawing.Imaging.ImageFormat.Png);
                    }
                    step++;
                }
                else { timer.Stop(); timer.Dispose(); form.Close(); }
            };
            form.Shown += delegate { timer.Start(); };
        }
    }
}
