using System;
using System.Windows.Forms;
#if DIAG_MODEL || DIAG_STORAGE || DIAG_UI || DIAG_STARTUP
using BudgetDesk;
#endif
#if DIAG_STORAGE || DIAG_UI || DIAG_STARTUP
using System.IO;
#endif

internal static class DiagnosticProgram
{
#if DIAG_WINDOW
    private const string Stage = "01_Window";
#elif DIAG_MODEL
    private const string Stage = "02_Model";
#elif DIAG_STORAGE
    private const string Stage = "03_Storage";
#elif DIAG_UI
    private const string Stage = "04_Demo_UI";
#elif DIAG_STARTUP
    private const string Stage = "05_Full_Startup";
#else
#error A documented diagnostic stage must be selected.
#endif

    [STAThread]
    private static int Main()
    {
        Console.WriteLine("DIAGNOSTIC_STAGE=" + Stage);
#if DIAG_STORAGE || DIAG_UI || DIAG_STARTUP
        string folder = null;
#endif
        try
        {
#if DIAG_STORAGE || DIAG_UI || DIAG_STARTUP
            folder = Path.Combine(Path.GetTempPath(), "BudgetDesk-Diagnostics", Stage + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            Console.WriteLine("TEMP_FOLDER=" + folder);
#endif
#if DIAG_STARTUP
            // The build changes only Main's visibility in a generated copy of
            // Program.cs, allowing this isolated launcher to call the real entry.
            return BudgetDesk.Program.Main(new[] { "--demo", "--data-dir=" + Path.Combine(folder, "Data"), "--startup-log=" + Path.Combine(folder, "startup.txt") });
#else
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
#if DIAG_MODEL || DIAG_STORAGE || DIAG_UI
            BudgetData data = Ledger.Demo(DateTime.Today);
            Ledger.Validate(data);
            Console.WriteLine("SAMPLE_ACCOUNTS=" + data.Accounts.Count);
            if (data.Accounts.Count != 3) throw new Exception("The diagnostic sample must contain three accounts.");
#endif
#if DIAG_STORAGE || DIAG_UI
            BudgetStore store = new BudgetStore(Path.Combine(folder, "Data"));
            store.Save(data);
            BudgetData reloaded = store.Load();
            if (BudgetStore.Serialize(data) != BudgetStore.Serialize(reloaded)) throw new Exception("Sample budget did not round-trip exactly.");
            Console.WriteLine("STORAGE_ROUND_TRIP=PASS");
#endif
#if DIAG_UI
            using (MainForm form = new MainForm(store, data, true))
            {
                form.Text = "Budget Desk diagnostic 04 | TEMPORARY DEMO DATA";
                Application.Run(form);
            }
#else
            using (Form form = new Form { Text = "Budget Desk diagnostic " + Stage, Width = 660, Height = 310, StartPosition = FormStartPosition.CenterScreen })
            {
                string detail = "A minimal Windows Forms window. No Budget Desk model, storage or startup code is compiled into this test.";
#if DIAG_MODEL
                detail = "The real Budget Desk financial model is included. Three synthetic accounts are validated in memory. No budget file is read or written.";
#elif DIAG_STORAGE
                detail = "The real model and JSON storage are included. A synthetic budget is saved and reloaded exactly, inside a temporary diagnostic folder.";
#endif
                Label label = new Label { Dock = DockStyle.Fill, AutoSize = false, Padding = new Padding(24), Text = Stage + "\r\n\r\n" + detail + "\r\n\r\nDiagnostic only. Close this window when finished." };
                form.Controls.Add(label);
                Application.Run(form);
            }
#endif
            Console.WriteLine("DIAGNOSTIC_CLOSED_NORMALLY=PASS");
            return 0;
#endif
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("DIAGNOSTIC_FAILURE=" + ex);
            MessageBox.Show(ex.Message, "Budget Desk diagnostic failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
#if DIAG_STORAGE || DIAG_UI || DIAG_STARTUP
        finally
        {
            if (folder != null && Directory.Exists(folder))
            {
                try { Directory.Delete(folder, true); }
                catch (IOException ex) { Console.Error.WriteLine("TEMP_CLEANUP_WARNING=" + ex.Message); }
                catch (UnauthorizedAccessException ex) { Console.Error.WriteLine("TEMP_CLEANUP_WARNING=" + ex.Message); }
            }
        }
#endif
    }
}
