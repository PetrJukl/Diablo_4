using System;

namespace Diablo4.WinUI.Models;

public sealed class UpdateCheckResult
{
    public bool IsUpdateAvailable { get; init; }
    public UpdateManifest? Manifest { get; init; }
    public Version? CurrentVersion { get; init; }
    public Version? LatestVersion { get; init; }
    public string? ErrorMessage { get; init; }

    public static UpdateCheckResult NoUpdate(Version? currentVersion) => new()
    {
        IsUpdateAvailable = false,
        CurrentVersion = currentVersion
    };

    public static UpdateCheckResult Error(string message, Version? currentVersion) => new()
    {
        IsUpdateAvailable = false,
        CurrentVersion = currentVersion,
        ErrorMessage = message
    };
}
