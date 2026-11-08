using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

internal static class VerifyWindows
{
    private static string Snapshot(string path)
    {
        if (!Directory.Exists(path)) return "ABSENT";
        List<string> files = new List<string>(Directory.GetFiles(path, "*", SearchOption.AllDirectories));
        files.Sort(StringComparer.OrdinalIgnoreCase);
        List<string> result = new List<string>();
        foreach (string file in files)
            using (SHA256 hash = SHA256.Create())
            using (FileStream stream = File.OpenRead(file)) result.Add(file + "=" + BitConverter.ToString(hash.ComputeHash(stream)));
        return String.Join("\n", result);
    }

    private static void Run(string root, string stage)
    {
        ProcessStartInfo info = new ProcessStartInfo(Path.Combine(root, stage, "BudgetDesk.exe")) {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true
        };
        using (Process process = Process.Start(info))
        {
            try
            {
                if (!process.WaitForInputIdle(15000)) throw new Exception(stage + ": window did not become idle.");
                Stopwatch timeout = Stopwatch.StartNew();
                bool shown = false;
                while (timeout.ElapsedMilliseconds < 15000)
                {
                    process.Refresh();
                    if (process.HasExited) throw new Exception(stage + ": exited before its window could be checked.");
                    if (process.MainWindowHandle != IntPtr.Zero && process.MainWindowTitle.StartsWith("Budget Desk", StringComparison.Ordinal) && process.Responding)
                    { shown = true; break; }
                    Thread.Sleep(100);
                }
                if (!shown) throw new Exception(stage + ": responsive Budget Desk window was not found.");
                if (!process.CloseMainWindow()) throw new Exception(stage + ": normal close was refused.");
                if (!process.WaitForExit(15000)) throw new Exception(stage + ": normal close timed out.");
                process.WaitForExit();
                string output = process.StandardOutput.ReadToEnd();
                string errors = process.StandardError.ReadToEnd();
                Console.Write(output); Console.Write(errors);
                if (process.ExitCode != 0 || errors.Contains("DIAGNOSTIC_FAILURE=") || errors.Contains("TEMP_CLEANUP_WARNING="))
                    throw new Exception(stage + ": diagnostic returned a failure.");
                if (!output.Contains("DIAGNOSTIC_STAGE=" + stage)) throw new Exception(stage + ": stage identity missing.");
                if ((stage == "02_Model" || stage == "03_Storage" || stage == "04_Demo_UI") && !output.Contains("SAMPLE_ACCOUNTS=3"))
                    throw new Exception(stage + ": sample model was not checked.");
                if ((stage == "03_Storage" || stage == "04_Demo_UI") && !output.Contains("STORAGE_ROUND_TRIP=PASS"))
                    throw new Exception(stage + ": storage round trip was not checked.");
                foreach (string line in output.Replace("\r", "").Split('\n'))
                    if (line.StartsWith("TEMP_FOLDER=", StringComparison.Ordinal) && Directory.Exists(line.Substring(12)))
                        throw new Exception(stage + ": temporary sample data was not cleaned up.");
                Console.WriteLine("PASS " + stage + ": actual EXE opens, responds, closes and retains only isolated test data");
            }
            finally { if (!process.HasExited) { process.Kill(); process.WaitForExit(); } }
        }
    }

    private static int Main(string[] args)
    {
        try
        {
            string data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BudgetDesk", "Data");
            string demo = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BudgetDesk", "Demo");
            string beforeData = Snapshot(data), beforeDemo = Snapshot(demo);
            foreach (string stage in new[] { "01_Window", "02_Model", "03_Storage", "04_Demo_UI", "05_Full_Startup" }) Run(Path.GetFullPath(args[0]), stage);
            if (Snapshot(data) != beforeData || Snapshot(demo) != beforeDemo) throw new Exception("A normal Budget Desk data folder changed.");
            Console.WriteLine("PASS default real/demo budget folders are untouched by all five diagnostic launches");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
