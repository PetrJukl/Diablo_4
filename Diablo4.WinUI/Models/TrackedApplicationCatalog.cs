using System;
using System.Collections.Generic;
using System.Linq;

namespace Diablo4.WinUI.Models;

internal sealed record TrackedApplicationDefinition(
    string DisplayName,
    string[] TrackedProcessNames,
    string? ExecutableName = null,
    string? LaunchProcessName = null,
    bool ShowInWeekendDialog = false)
{
    public bool CanLaunchFromWeekendDialog => ShowInWeekendDialog
        && !string.IsNullOrWhiteSpace(ExecutableName)
        && !string.IsNullOrWhiteSpace(LaunchProcessName);

    public override string ToString() => DisplayName;
}

internal static class TrackedApplications
{
    private static readonly IReadOnlyList<TrackedApplicationDefinition> LaunchableGames =
    [
        new("Diablo IV", ["Diablo IV"], "Diablo IV.exe", "Diablo IV", true),
        new("Diablo III64", ["Diablo III64"], "Diablo III64.exe", "Diablo III64", true),
        new("Dragon Age The Veilguard", ["Dragon Age The Veilguard"], "Dragon Age The Veilguard.exe", "Dragon Age The Veilguard", true),
        new("DragonAgeInquisition", ["DragonAgeInquisition"], "DragonAgeInquisition.exe", "DragonAgeInquisition", true),
        new("Dragon Age II", ["DragonAge2"], "DragonAge2.exe", "DragonAge2", true),
        new("Dragon Age: Origins", ["daorigins"], "daorigins.exe", "daorigins", true),
        new("Dragon's Dogma 2", ["DD2"], "DD2.exe", "DD2", true),
        new("The Elder Scrolls IV: Oblivion Remastered", ["OblivionRemastered"], "OblivionRemastered.exe", "OblivionRemastered", true),
        new("MassEffectLauncher", ["MassEffectLauncher", "MassEffect1", "MassEffect2", "MassEffect3"], "MassEffectLauncher.exe", "MassEffectLauncher", true)
    ];

    private static readonly IReadOnlyList<TrackedApplicationDefinition> BackgroundTrackedApps =
    [
        new("Code", ["Code"]),
        new("devenv", ["devenv"]),
        new("GitHub", ["github"]),
        new("ChatGPT", ["ChatGPT"]),
        new("WindowsTerminal", ["WindowsTerminal"]),
        new("OpenConsole", ["OpenConsole"])
    ];

    public static IReadOnlyList<TrackedApplicationDefinition> WeekendMotivationGames { get; } =
        [.. LaunchableGames.Where(application => application.CanLaunchFromWeekendDialog)];

    public static string[] AllProcessNames { get; } =
        [.. LaunchableGames
            .Concat(BackgroundTrackedApps)
            .SelectMany(application => application.TrackedProcessNames)
            .Distinct(StringComparer.OrdinalIgnoreCase)];
}
