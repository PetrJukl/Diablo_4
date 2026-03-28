using Diablo4.WinUI.Helpers;
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
    private const string DefaultInstallerFileName = "KontrolaParbySetup.exe";
    private const string UpdatesDirectoryName = "Updates";
    private const string UpdatesRootDirectoryName = "KontrolaParby";
    private readonly string _manifestUrl;

    public UpdateService(string manifestUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestUrl);
        _manifestUrl = manifestUrl;
        CleanupDownloadedInstallers();
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var currentVersion = GetCurrentVersion();

        if (!Uri.TryCreate(_manifestUrl, UriKind.Absolute, out var manifestUri))
        {
            return UpdateCheckResult.Error("GitHub manifest URL není platná.", currentVersion);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, manifestUri);
            using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
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

            if (!Uri.TryCreate(manifest.DownloadUrl, UriKind.Absolute, out _))
            {
                return UpdateCheckResult.Error("Manifest obsahuje neplatnou URL balíčku.", currentVersion);
            }

            if (!string.IsNullOrWhiteSpace(manifest.MinimumVersion)
                && !Version.TryParse(manifest.MinimumVersion, out _))
            {
                return UpdateCheckResult.Error("Manifest obsahuje neplatný formát minimální verze.", currentVersion);
            }

            return new UpdateCheckResult
            {
                IsUpdateAvailable = latestVersion > currentVersion,
                Manifest = manifest,
                CurrentVersion = currentVersion,
                LatestVersion = latestVersion
            };
        }
        catch (HttpRequestException ex)
        {
            return UpdateCheckResult.Error($"Manifest se nepodařilo načíst: {ex.Message}", currentVersion);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return UpdateCheckResult.Error("Načtení manifestu vypršelo.", currentVersion);
        }
        catch (JsonException ex)
        {
            return UpdateCheckResult.Error($"Manifest obsahuje neplatný JSON: {ex.Message}", currentVersion);
        }
        catch (NotSupportedException ex)
        {
            return UpdateCheckResult.Error($"Manifest používá nepodporovaný formát: {ex.Message}", currentVersion);
        }
        catch (UriFormatException ex)
        {
            return UpdateCheckResult.Error(ex.Message, currentVersion);
        }
    }

    public async Task<string> DownloadUpdateAsync(string downloadUrl, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(downloadUrl);

        if (!Uri.TryCreate(downloadUrl, UriKind.Absolute, out var downloadUri))
        {
            throw new ArgumentException("URL balíčku není platná.", nameof(downloadUrl));
        }

        var destinationPath = GetDownloadDestinationPath(downloadUri);
        CleanupDownloadedInstallers(destinationPath);

        using var request = new HttpRequestMessage(HttpMethod.Get, downloadUri);
        using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var target = new FileStream(destinationPath, new FileStreamOptions
        {
            Access = FileAccess.Write,
            Mode = FileMode.Create,
            Share = FileShare.None,
            Options = FileOptions.Asynchronous
        });
        await source.CopyToAsync(target, cancellationToken);

        return destinationPath;
    }

    public Task ApplyUpdateAsync(string installerPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(installerPath))
        {
            throw new FileNotFoundException("Instalační balíček nebyl nalezen.", installerPath);
        }

        Process.Start(new ProcessStartInfo(installerPath)
        {
            WorkingDirectory = Path.GetDirectoryName(installerPath),
            UseShellExecute = true
        });

        return Task.CompletedTask;
    }

    public async Task DownloadAndInstallAsync(UpdateManifest manifest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var installerPath = await DownloadUpdateAsync(manifest.DownloadUrl, cancellationToken);
        await ApplyUpdateAsync(installerPath, cancellationToken);
    }

    private static Version GetCurrentVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);
    }

    private static string GetDownloadDestinationPath(Uri downloadUri)
    {
        ArgumentNullException.ThrowIfNull(downloadUri);

        var updatesDirectory = GetUpdatesDirectoryPath();
        Directory.CreateDirectory(updatesDirectory);

        var fileName = Path.GetFileName(downloadUri.LocalPath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = DefaultInstallerFileName;
        }

        return Path.Combine(updatesDirectory, fileName);
    }

    private static string GetUpdatesDirectoryPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            UpdatesRootDirectoryName,
            UpdatesDirectoryName);
    }

    private static void CleanupDownloadedInstallers(string? preservedInstallerPath = null)
    {
        var updatesDirectory = GetUpdatesDirectoryPath();
        Directory.CreateDirectory(updatesDirectory);

        foreach (var filePath in Directory.GetFiles(updatesDirectory))
        {
            if (!string.IsNullOrWhiteSpace(preservedInstallerPath)
                && string.Equals(filePath, preservedInstallerPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                File.Delete(filePath);
            }
            catch (IOException ex)
            {
                AppDiagnostics.LogWarning($"Nepodařilo se smazat starý update soubor '{filePath}'.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                AppDiagnostics.LogWarning($"Přístup ke smazání starého update souboru '{filePath}' byl odmítnut.", ex);
            }
        }
    }
}
