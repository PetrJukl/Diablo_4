using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Diablo4.WinUI.Helpers;

public static class ProcessHelper
{
    public static void CloseOtherInstances(string processName)
    {
        Task.Run(() =>
        {
            var processes = Process.GetProcessesByName(processName);

            if (processes.Length > 1)
            {
                var processesToClose = processes.OrderByDescending(p => p.StartTime).Skip(1);

                foreach (var process in processesToClose)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception)
                    {
                        // Process might have already exited
                    }
                }
            }
        });
    }
}
