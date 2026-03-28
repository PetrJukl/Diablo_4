using Diablo4.WinUI.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Diablo4.WinUI.Tests.Services;

[TestClass]
public class UpdateServiceTests
{
    [TestMethod]
    public async Task CheckForUpdatesAsync_WhenManifestUrlIsInvalid_ReturnsErrorMessage()
    {
        var service = new UpdateService("neplatna-url");

        var result = await service.CheckForUpdatesAsync();

        StringAssert.Contains(result.ErrorMessage ?? string.Empty, "není platná");
    }

    [TestMethod]
    public async Task DownloadUpdateAsync_WhenDownloadUrlIsInvalid_ThrowsArgumentException()
    {
        var service = new UpdateService("https://example.invalid/manifest.json");

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => service.DownloadUpdateAsync("neplatna-url"));
    }

    [TestMethod]
    public async Task ApplyUpdateAsync_WhenInstallerIsMissing_ThrowsFileNotFoundException()
    {
        var service = new UpdateService("https://example.invalid/manifest.json");
        var installerPath = Path.Combine(Path.GetTempPath(), $"missing-installer-{Guid.NewGuid():N}.msix");

        await Assert.ThrowsExactlyAsync<FileNotFoundException>(() => service.ApplyUpdateAsync(installerPath));
    }
}
