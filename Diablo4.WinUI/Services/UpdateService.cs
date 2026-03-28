using Diablo4.WinUI.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Diablo4.WinUI.Services;

public sealed class UpdateService
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly string _manifestUrl;

    public UpdateService(string manifestUrl)
    {
        _manifestUrl = manifestUrl;
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var currentVersion = GetCurrentVersion();

        if (string.IsNullOrWhiteSpace(_manifestUrl) || _manifestUrl.Contains("example/diablo4", StringComparison.OrdinalIgnoreCase))
        {
            return UpdateCheckResult.Error("GitHub manifest URL není nakonfigurovaná.", currentVersion);
        }

        try
        {
            using var response = await HttpClient.GetAsync(_manifestUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return UpdateCheckResult.Error($"Manifest se nepodařilo načíst. HTTP {(int)response.StatusCode}.", currentVersion);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var manifest = await JsonSerializer.DeserializeAsync<UpdateManifest>(stream, cancellationToken: cancellationToken);
            if (manifest == null || string.IsNullOrWhiteSpace(manifest.LatestVersion) || string.IsNullOrWhiteSpace(manifest.DownloadUrl))
            {
                return UpdateCheckResult.Error("Manifest neobsahuje povinné údaje o verzi nebo URL balíčku.", currentVersion);
            }

            if (!Version.TryParse(manifest.LatestVersion, out var latestVersion))
            {
                return UpdateCheckResult.Error("Manifest obsahuje neplatný formát verze.", currentVersion);
            }

            return new UpdateCheckResult
            {
                IsUpdateAvailable = latestVersion > currentVersion,
                Manifest = manifest,
                CurrentVersion = currentVersion,
                LatestVersion = latestVersion
            };
        }
        catch (Exception ex)
        {
            return UpdateCheckResult.Error(ex.Message, currentVersion);
        }
    }

    public async Task<string> DownloadUpdateAsync(string downloadUrl, CancellationToken cancellationToken = default)
    {
        var fileName = Path.GetFileName(new Uri(downloadUrl).LocalPath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = "Diablo4.WinUI.msix";
        }

        var destinationPath = Path.Combine(Path.GetTempPath(), fileName);

        using var response = await HttpClient.GetAsync(downloadUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var target = File.Create(destinationPath);
        await source.CopyToAsync(target, cancellationToken);

        return destinationPath;
    }

    public Task ApplyUpdateAsync(string installerPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(installerPath))
        {
            throw new FileNotFoundException("Instalační balíček nebyl nalezen.", installerPath);
        }

        Process.Start(new ProcessStartInfo(installerPath)
        {
            UseShellExecute = true
        });

        return Task.CompletedTask;
    }

    public async Task DownloadAndInstallAsync(UpdateManifest manifest, CancellationToken cancellationToken = default)
    {
        var installerPath = await DownloadUpdateAsync(manifest.DownloadUrl, cancellationToken);
        await ApplyUpdateAsync(installerPath, cancellationToken);
    }

    private static Version GetCurrentVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);
    }
}
