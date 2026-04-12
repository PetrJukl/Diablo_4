using Diablo4.WinUI.Helpers;
using Diablo4.WinUI.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Diablo4.WinUI.Services;

public sealed class UpdateService
{
    private static readonly HttpClient ManifestHttpClient = new() { Timeout = TimeSpan.FromSeconds(15) };
    private static readonly HttpClient DownloadHttpClient = new() { Timeout = System.Threading.Timeout.InfiniteTimeSpan };
    private const string DefaultInstallerFileName = "KontrolaParbySetup.exe";
    private const string UpdatesDirectoryName = "Updates";
    private const string UpdatesRootDirectoryName = "Diablo Log";
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

        if (!UpdateSourcePolicy.TryCreateTrustedManifestUri(_manifestUrl, out var manifestUri, out var manifestUriErrorMessage))
        {
            return UpdateCheckResult.Error(manifestUriErrorMessage, currentVersion);
        }

        try
        {
            using var manifestRequest = new HttpRequestMessage(HttpMethod.Get, manifestUri);
            using var response = await ManifestHttpClient.SendAsync(manifestRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (response.RequestMessage?.RequestUri is not { } finalManifestUri
                || !UpdateSourcePolicy.IsTrustedManifestUri(finalManifestUri))
            {
                return UpdateCheckResult.Error("Manifest byl přesměrován na nedůvěryhodný zdroj.", currentVersion);
            }

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

            if (!UpdateSourcePolicy.TryCreateTrustedDownloadUri(manifest.DownloadUrl, out _, out var downloadUriErrorMessage))
            {
                return UpdateCheckResult.Error(downloadUriErrorMessage, currentVersion);
            }

            if (!string.IsNullOrWhiteSpace(manifest.MinimumVersion)
                && !Version.TryParse(manifest.MinimumVersion, out _))
            {
                return UpdateCheckResult.Error("Manifest obsahuje neplatný formát minimální verze.", currentVersion);
            }

            if (!UpdateSourcePolicy.IsValidSha256(manifest.Sha256))
            {
                return UpdateCheckResult.Error("Manifest obsahuje neplatný SHA-256 otisk balíčku.", currentVersion);
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

        if (!UpdateSourcePolicy.TryCreateTrustedDownloadUri(downloadUrl, out var downloadUri, out var downloadUriErrorMessage))
        {
            throw new ArgumentException(downloadUriErrorMessage, nameof(downloadUrl));
        }

        var destinationPath = GetDownloadDestinationPath(downloadUri);
        CleanupDownloadedInstallers(destinationPath);

        using var downloadRequest = new HttpRequestMessage(HttpMethod.Get, downloadUri);
        using var response = await DownloadHttpClient.SendAsync(downloadRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        if (response.RequestMessage?.RequestUri is not { } finalDownloadUri
            || !UpdateSourcePolicy.IsTrustedDownloadUri(finalDownloadUri))
        {
            throw new InvalidOperationException("Stažený balíček pochází z nedůvěryhodného zdroje.");
        }

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var target = new FileStream(destinationPath, new FileStreamOptions
        {
            Access = FileAccess.Write,
            Mode = FileMode.Create,
            Share = FileShare.None,
            Options = FileOptions.Asynchronous
        });
        await source.CopyToAsync(target, cancellationToken);
        await target.FlushAsync(cancellationToken);

        var downloadedInstaller = new FileInfo(destinationPath);
        if (!downloadedInstaller.Exists || downloadedInstaller.Length == 0)
        {
            throw new IOException("Stažený instalační balíček je prázdný nebo nebyl vytvořen.");
        }

        return destinationPath;
    }

    public Task ApplyUpdateAsync(string installerPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(installerPath))
        {
            throw new FileNotFoundException("Instalační balíček nebyl nalezen.", installerPath);
        }

        if (!UpdateSourcePolicy.HasSupportedInstallerExtension(installerPath))
        {
            throw new InvalidOperationException("Instalační balíček má nepodporovanou příponu.");
        }

        var startedProcess = Process.Start(new ProcessStartInfo(installerPath)
        {
            WorkingDirectory = Path.GetDirectoryName(installerPath),
            UseShellExecute = true
        });

        if (startedProcess is null)
        {
            throw new InvalidOperationException("Instalační balíček se nepodařilo spustit.");
        }

        return Task.CompletedTask;
    }

    public async Task DownloadAndInstallAsync(UpdateManifest manifest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var installerPath = await DownloadUpdateAsync(manifest.DownloadUrl, cancellationToken);

        if (!string.IsNullOrWhiteSpace(manifest.Sha256))
        {
            await VerifyInstallerChecksumAsync(installerPath, manifest.Sha256, cancellationToken);
        }
        else
        {
            AppDiagnostics.LogWarning("Manifest neobsahuje SHA-256 otisk. Integrita installeru nebyla ověřena.");
        }

        await ApplyUpdateAsync(installerPath, cancellationToken);
    }

    private static async Task VerifyInstallerChecksumAsync(string installerPath, string expectedSha256, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(installerPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
        var actualSha256 = Convert.ToHexString(hashBytes);

        if (!string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Stažený instalační balíček neodpovídá očekávanému SHA-256 otisku.");
        }
    }

    private static Version GetCurrentVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);
    }

    private static string GetDownloadDestinationPath(Uri downloadUri)
    {
        ArgumentNullException.ThrowIfNull(downloadUri);

        if (!UpdateSourcePolicy.HasSupportedInstallerExtension(downloadUri))
        {
            throw new InvalidOperationException("URL instalačního balíčku používá nepodporovanou příponu.");
        }

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

        try
        {
            Directory.CreateDirectory(updatesDirectory);
        }
        catch (IOException ex)
        {
            AppDiagnostics.LogWarning($"Nepodařilo se vytvořit adresář pro aktualizace '{updatesDirectory}'.", ex);
            return;
        }
        catch (UnauthorizedAccessException ex)
        {
            AppDiagnostics.LogWarning($"Přístup k adresáři pro aktualizace '{updatesDirectory}' byl odmítnut.", ex);
            return;
        }

        string[] files;
        try
        {
            files = Directory.GetFiles(updatesDirectory);
        }
        catch (IOException ex)
        {
            AppDiagnostics.LogWarning("Nepodařilo se načíst soubory v adresáři pro aktualizace.", ex);
            return;
        }
        catch (UnauthorizedAccessException ex)
        {
            AppDiagnostics.LogWarning("Přístup k adresáři pro aktualizace byl odmítnut.", ex);
            return;
        }

        foreach (var filePath in files)
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
            catch (IOException)
            {
                // Soubor může být dočasně zamčen (např. installer ještě běží) – přeskočíme, smažeme při příštím startu.
                AppDiagnostics.LogInfo($"Update soubor '{filePath}' je dočasně zamčen, bude smazán při příštím startu.");
            }
            catch (UnauthorizedAccessException ex)
            {
                AppDiagnostics.LogWarning($"Přístup ke smazání starého update souboru '{filePath}' byl odmítnut.", ex);
            }
        }
    }
}
