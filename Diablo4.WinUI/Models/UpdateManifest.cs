namespace Diablo4.WinUI.Models;

public sealed class UpdateManifest
{
    public string LatestVersion { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string ReleaseNotes { get; set; } = string.Empty;
    public string MinimumVersion { get; set; } = string.Empty;
    public string ReleaseDate { get; set; } = string.Empty;
}
