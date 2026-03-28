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
        return IsTrustedGitHubUri(uri);
    }

    internal static bool IsTrustedDownloadUri(Uri uri)
    {
        return IsTrustedGitHubUri(uri);
    }

    internal static bool IsTrustedGitHubUri(Uri uri)
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

        if (TrustedHosts.Contains(uri.Host))
        {
            return true;
        }

        foreach (var suffix in TrustedHostSuffixes)
        {
            if (uri.Host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
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