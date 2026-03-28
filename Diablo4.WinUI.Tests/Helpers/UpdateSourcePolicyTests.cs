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