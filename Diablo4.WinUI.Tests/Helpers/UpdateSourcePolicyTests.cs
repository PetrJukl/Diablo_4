using Diablo4.WinUI.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Diablo4.WinUI.Tests.Helpers;

[TestClass]
public class UpdateSourcePolicyTests
{
    [TestMethod]
    public void TryCreateTrustedDownloadUri_WhenUrlUsesTrustedGitHubHost_ReturnsTrue()
    {
        var result = UpdateSourcePolicy.TryCreateTrustedDownloadUri(
            "https://github.com/PetrJukl/Diablo_4/releases/download/v1.0.0.8/KontrolaParbySetup-1.0.0.8.exe",
            out var uri,
            out var errorMessage);

        Assert.IsTrue(result);
        Assert.AreEqual("https://github.com/PetrJukl/Diablo_4/releases/download/v1.0.0.8/KontrolaParbySetup-1.0.0.8.exe", uri.AbsoluteUri);
        Assert.AreEqual(string.Empty, errorMessage);
    }

    [TestMethod]
    public void TryCreateTrustedDownloadUri_WhenUrlUsesHttp_ReturnsFalse()
    {
        var result = UpdateSourcePolicy.TryCreateTrustedDownloadUri(
            "http://github.com/PetrJukl/Diablo_4/releases/download/v1.0.0.8/KontrolaParbySetup-1.0.0.8.exe",
            out _,
            out var errorMessage);

        Assert.IsFalse(result);
        StringAssert.Contains(errorMessage, "HTTPS");
    }

    [TestMethod]
    public void TryCreateTrustedDownloadUri_WhenHostIsUntrusted_ReturnsFalse()
    {
        var result = UpdateSourcePolicy.TryCreateTrustedDownloadUri(
            "https://example.com/KontrolaParbySetup-1.0.0.8.exe",
            out _,
            out var errorMessage);

        Assert.IsFalse(result);
        StringAssert.Contains(errorMessage, "GitHub");
    }

    [TestMethod]
    public void TryCreateTrustedDownloadUri_WhenPathBelongsToDifferentRepository_ReturnsFalse()
    {
        var result = UpdateSourcePolicy.TryCreateTrustedDownloadUri(
            "https://github.com/Attacker/EvilRepo/releases/download/v1.0.0/KontrolaParbySetup.exe",
            out _,
            out var errorMessage);

        Assert.IsFalse(result);
        StringAssert.Contains(errorMessage, "GitHub");
    }

    [TestMethod]
    public void TryCreateTrustedManifestUri_WhenPathBelongsToDifferentRepository_ReturnsFalse()
    {
        var result = UpdateSourcePolicy.TryCreateTrustedManifestUri(
            "https://raw.githubusercontent.com/Attacker/EvilRepo/main/update-manifest.json",
            out _,
            out var errorMessage);

        Assert.IsFalse(result);
        StringAssert.Contains(errorMessage, "GitHub");
    }

    [TestMethod]
    public void TryCreateTrustedManifestUri_WhenPathBelongsToOwnRepository_ReturnsTrue()
    {
        var result = UpdateSourcePolicy.TryCreateTrustedManifestUri(
            "https://raw.githubusercontent.com/PetrJukl/Diablo_4/main/update-manifest.json",
            out var uri,
            out var errorMessage);

        Assert.IsTrue(result);
        Assert.AreEqual("https://raw.githubusercontent.com/PetrJukl/Diablo_4/main/update-manifest.json", uri.AbsoluteUri);
        Assert.AreEqual(string.Empty, errorMessage);
    }

    [TestMethod]
    public void IsTrustedDownloadUri_WhenRedirectTargetsReleaseAssetStorageBackend_ReturnsTrue()
    {
        // GitHub release assety přesměrovávají na storage backend, kde URL
        // obsahuje číselné repo ID místo owner/repo segmentu.
        var redirectTarget = new Uri("https://objects.githubusercontent.com/github-production-release-asset-2e65be/123456789/abcdef-uuid?token=xyz");

        var result = UpdateSourcePolicy.IsTrustedDownloadUri(redirectTarget);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsTrustedManifestUri_WhenHostIsStorageBackendButPathHasNoOwnerRepoPrefix_ReturnsFalse()
    {
        // Manifest URL musí vždy obsahovat path prefix vlastního repozitáře –
        // storage backend bez prefixu nesmí být přijat jako manifest source.
        var storageWithoutPrefix = new Uri("https://objects.githubusercontent.com/github-production-release-asset-2e65be/123456789/abcdef-uuid?token=xyz");

        var result = UpdateSourcePolicy.IsTrustedManifestUri(storageWithoutPrefix);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsTrustedDownloadUri_WhenHostIsUntrusted_ReturnsFalse()
    {
        var untrusted = new Uri("https://evil.example.com/PetrJukl/Diablo_4/setup.exe");

        var result = UpdateSourcePolicy.IsTrustedDownloadUri(untrusted);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsValidSha256_WhenValueHas64HexCharacters_ReturnsTrue()
    {
        var result = UpdateSourcePolicy.IsValidSha256("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsValidSha256_WhenValueContainsInvalidCharacter_ReturnsFalse()
    {
        var result = UpdateSourcePolicy.IsValidSha256("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdeg");

        Assert.IsFalse(result);
    }
}