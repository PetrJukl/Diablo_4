using System;
using System.Collections.Generic;
using System.Linq;

namespace Diablo4.WinUI.Helpers;

public static class MachineContextHelper
{
    // List of machine names that should have web content checking enabled
    // TODO: Configure this list based on actual requirements
    private static readonly List<string> _webCheckEnabledMachines = new()
    {
        "LEGION"
    };

    public static bool ShouldCheckWebContent()
    {
        string machineName = Environment.MachineName;
        return _webCheckEnabledMachines.Contains(machineName, StringComparer.OrdinalIgnoreCase);
    }

    public static string GetMachineName()
    {
        return Environment.MachineName;
    }
}
