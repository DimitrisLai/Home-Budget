using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

// Drives the actual release executable from a separate process. This harness
// is compiled into the CI checks directory and is never shipped in the ZIP.
internal static class StartupSmokeTests
{
    private static string ReadLog(string path)
    {
        using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (StreamReader reader = new StreamReader(stream)) return reader.ReadToEnd();
    }

    private static void Run(string executable, string root, string mode)
    {
        string folder = Path.Combine(root, mode);
        Directory.CreateDirectory(folder);
        string data = Path.Combine(folder, "data");
        string capture = Path.Combine(folder, "unused-capture");
        string log = Path.Combine(folder, "startup.txt");
        if (mode == "log-fallback")
        {
            string occupied = Path.Combine(folder, "occupied");
            File.WriteAllText(occupied, "Leave this file intact.");
            log = Path.Combine(occupied, "startup.txt");
        }
        string arguments = "\"--data-dir=" + data + "\" \"--startup-log=" + log + "\"";
        if (mode == "demo") arguments += " --demo";
        // Older screenshot flags must no longer capture or auto-close the app.
        if (mode == "empty") arguments += " \"--capture=" + capture + "\"";
        using (Process process = Process.Start(new ProcessStartInfo(executable, arguments) { UseShellExecute = false }))
        {
            try
            {
                if (!process.WaitForInputIdle(15000)) throw new Exception(mode + ": app did not become responsive.");
                Stopwatch timeout = Stopwatch.StartNew();
                bool shown = false;
                while (timeout.ElapsedMilliseconds < 15000)
                {
                    process.Refresh();
                    if (process.HasExited) throw new Exception(mode + ": app exited before it could be checked.");
                    if (process.MainWindowHandle != IntPtr.Zero &&
                        (mode == "log-fallback" || (File.Exists(log) && ReadLog(log).Contains("Window shown"))))
                    { shown = true; break; }
                    Thread.Sleep(100);
                }
                if (!shown || !process.Responding) throw new Exception(mode + ": main window was not shown and responsive.");
                if (!File.Exists(Path.Combine(data, "budget.json"))) throw new Exception(mode + ": isolated budget was not saved.");
                if (Directory.Exists(capture)) throw new Exception("Release executable still created a screenshot directory.");
                if (!process.CloseMainWindow()) throw new Exception(mode + ": main window refused normal close.");
                if (!process.WaitForExit(15000)) throw new Exception(mode + ": app did not close normally.");
                process.WaitForExit();
                if (process.ExitCode != 0) throw new Exception(mode + ": exit code " + process.ExitCode);
                if (mode != "log-fallback" && !ReadLog(log).Contains("Window closed normally"))
                    throw new Exception(mode + ": normal shutdown was not logged.");
                if (mode == "log-fallback" && File.ReadAllText(Path.Combine(folder, "occupied")) != "Leave this file intact.")
                    throw new Exception("Log fallback altered an unrelated file.");
                Console.WriteLine("PASS actual executable opens a responsive " + mode + " budget and closes normally");
            }
            finally
            {
                if (!process.HasExited) { process.Kill(); process.WaitForExit(); }
            }
        }
    }

    private static int Main(string[] args)
    {
        string root = Path.Combine(Path.GetTempPath(), "budgetdesk-startup-check-" + Guid.NewGuid().ToString("N"));
        try
        {
            string executable = Path.GetFullPath(args[0]);
            foreach (string mode in new[] { "empty", "demo", "log-fallback" }) Run(executable, root, mode);
            Console.WriteLine("3 actual-executable startup checks passed.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine("FAIL " + ex); return 1; }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
