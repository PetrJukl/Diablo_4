using System;
using System.Collections.Generic;
using System.IO;

namespace Diablo4.WinUI.Helpers;

internal static class UpdateSourcePolicy
{
#if DEBUG
    internal static Func<Uri, bool>? TrustOverride { get; set; }
#endif

    private static readonly HashSet<string> TrustedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "github.com"
    };

    private static readonly string[] TrustedHostSuffixes =
    [
        ".githubusercontent.com"
    ];

    /// <summary>
    /// Hostname suffixy storage backendu pro release assety. Tyto adresy
    /// neobsahují owner/repo segment v cestě (místo toho číselné repo ID),
    /// takže se na nich path prefix whitelist NEUPLATŇUJE. Použít jen pro
    /// stahování binárky chráněné povinným SHA-256 ověřením.
    /// </summary>
    private static readonly string[] StorageBackendHostSuffixes =
    [
        ".githubusercontent.com"
    ];

    /// <summary>
    /// Whitelist povolených prefixů cest na důvěryhodných GitHub hostech.
    /// Brání zneužití přes jiný uživatelský repozitář na stejném hostu.
    /// </summary>
    private static readonly string[] TrustedPathPrefixes =
    [
        "/PetrJukl/Diablo_4/"
    ];

    private static readonly HashSet<string> SupportedInstallerExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe",
        ".msi",
        ".msix",
        ".appinstaller"
    };

    internal static bool TryCreateTrustedManifestUri(string value, out Uri uri, out string errorMessage)
    {
        return TryCreateTrustedUri(value, requiresInstallerExtension: false, out uri, out errorMessage);
    }

    internal static bool TryCreateTrustedDownloadUri(string value, out Uri uri, out string errorMessage)
    {
        return TryCreateTrustedUri(value, requiresInstallerExtension: true, out uri, out errorMessage);
    }

    internal static bool IsTrustedManifestUri(Uri uri)
    {
        return IsTrustedGitHubUri(uri, allowStorageBackendWithoutPathPrefix: false);
    }

    internal static bool IsTrustedDownloadUri(Uri uri)
    {
        // Stažení release assetu redirectuje GitHub na storage backend
        // (objects.githubusercontent.com), kde URL místo "/owner/repo/" obsahuje
        // číselné repo ID. Path prefix whitelist tam nelze uplatnit – integritu
        // zajišťuje povinné SHA-256 ověření v UpdateService.DownloadAndInstallAsync.
        return IsTrustedGitHubUri(uri, allowStorageBackendWithoutPathPrefix: true);
    }

    internal static bool IsTrustedGitHubUri(Uri uri)
    {
        return IsTrustedGitHubUri(uri, allowStorageBackendWithoutPathPrefix: false);
    }

    private static bool IsTrustedGitHubUri(Uri uri, bool allowStorageBackendWithoutPathPrefix)
    {
        ArgumentNullException.ThrowIfNull(uri);

#if DEBUG
        if (TrustOverride?.Invoke(uri) == true)
        {
            return true;
        }
#endif

        if (!uri.IsAbsoluteUri || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        bool isHostTrusted = false;
        bool isStorageBackendHost = false;

        if (TrustedHosts.Contains(uri.Host))
        {
            isHostTrusted = true;
        }
        else
        {
            foreach (var suffix in TrustedHostSuffixes)
            {
                if (uri.Host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    isHostTrusted = true;
                    break;
                }
            }
        }

        if (!isHostTrusted)
        {
            return false;
        }

        if (allowStorageBackendWithoutPathPrefix)
        {
            foreach (var suffix in StorageBackendHostSuffixes)
            {
                if (uri.Host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    isStorageBackendHost = true;
                    break;
                }
            }

            if (isStorageBackendHost)
            {
                return true;
            }
        }

        foreach (var prefix in TrustedPathPrefixes)
        {
            if (uri.AbsolutePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool HasSupportedInstallerExtension(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        return HasSupportedInstallerExtension(uri.LocalPath);
    }

    internal static bool HasSupportedInstallerExtension(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var extension = Path.GetExtension(path);
        return !string.IsNullOrWhiteSpace(extension) && SupportedInstallerExtensions.Contains(extension);
    }

    internal static bool IsValidSha256(string? sha256)
    {
        if (string.IsNullOrWhiteSpace(sha256))
        {
            return true;
        }

        var trimmedValue = sha256.Trim();
        if (trimmedValue.Length != 64)
        {
            return false;
        }

        foreach (var character in trimmedValue)
        {
            if (!Uri.IsHexDigit(character))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryCreateTrustedUri(string value, bool requiresInstallerExtension, out Uri uri, out string errorMessage)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var parsedUri))
        {
            uri = null!;
            errorMessage = "GitHub URL není platná.";
            return false;
        }

        if (!IsTrustedGitHubUri(parsedUri))
        {
            uri = null!;
            errorMessage = "URL musí používat HTTPS a důvěryhodný GitHub zdroj.";
            return false;
        }

        if (requiresInstallerExtension && !HasSupportedInstallerExtension(parsedUri))
        {
            uri = null!;
            errorMessage = "URL balíčku musí odkazovat na podporovaný instalační soubor.";
            return false;
        }

        uri = parsedUri;
        errorMessage = string.Empty;
        return true;
    }
}