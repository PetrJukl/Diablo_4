using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Diablo4.WinUI.Helpers;

public static class ProcessHelper
{
    public static void CloseOtherInstances(string processName)
    {
        _ = Task.Run(() =>
        {
            var currentProcess = Process.GetCurrentProcess();
            var processes = Process.GetProcessesByName(processName)
                .Where(process => process.Id != currentProcess.Id)
                .OrderByDescending(process => SafeGetStartTime(process))
                .ToArray();

            try
            {
                foreach (var process in processes)
                {
                    try
                    {
                        if (process.HasExited)
                        {
                            continue;
                        }

                        if (process.CloseMainWindow())
                        {
                            process.WaitForExit(2000);
                            continue;
                        }

                        AppDiagnostics.LogWarning($"Jinou instanci '{process.ProcessName}' se nepodařilo korektně zavřít, protože nemá hlavní okno.");
                    }
                    catch (InvalidOperationException ex)
                    {
                        AppDiagnostics.LogWarning("Nepodařilo se získat stav druhé instance aplikace.", ex);
                    }
                    catch (Win32Exception ex)
                    {
                        AppDiagnostics.LogWarning("Korektní zavření druhé instance aplikace selhalo.", ex);
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
            finally
            {
                currentProcess.Dispose();
            }
        });
    }

    private static DateTime SafeGetStartTime(Process process)
    {
        try
        {
            return process.StartTime;
        }
        catch (InvalidOperationException)
        {
            return DateTime.MinValue;
        }
        catch (Win32Exception)
        {
            return DateTime.MinValue;
        }
    }
}
