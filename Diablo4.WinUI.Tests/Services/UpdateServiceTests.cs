using Diablo4.WinUI.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Sockets;

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

    [TestMethod]
    public async Task DownloadUpdateAsync_WhenDownloadSucceeds_StoresInstallerInLocalApplicationDataUpdatesFolder()
    {
        var fileName = $"KontrolaParbySetup-{Guid.NewGuid():N}.exe";
        var content = "installer-content"u8.ToArray();
        using var server = await SingleResponseHttpServer.StartAsync(fileName, content);
        var service = new UpdateService("https://example.invalid/manifest.json");

        var downloadedPath = await service.DownloadUpdateAsync(server.Url);

        var expectedDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KontrolaParby",
            "Updates");

        Assert.AreEqual(Path.Combine(expectedDirectory, fileName), downloadedPath, ignoreCase: true);
    }

    [TestMethod]
    public async Task DownloadUpdateAsync_WhenOldInstallerExists_RemovesOldInstallerAndOverwritesCurrentInstaller()
    {
        var updatesDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KontrolaParby",
            "Updates");
        Directory.CreateDirectory(updatesDirectory);

        var obsoleteFilePath = Path.Combine(updatesDirectory, $"obsolete-{Guid.NewGuid():N}.exe");
        var currentFileName = $"KontrolaParbySetup-{Guid.NewGuid():N}.exe";
        var currentFilePath = Path.Combine(updatesDirectory, currentFileName);
        await File.WriteAllTextAsync(obsoleteFilePath, "obsolete-content");
        await File.WriteAllTextAsync(currentFilePath, "old-installer-content");

        var newContent = "new-installer-content"u8.ToArray();
        using var server = await SingleResponseHttpServer.StartAsync(currentFileName, newContent);
        var service = new UpdateService("https://example.invalid/manifest.json");

        var downloadedPath = await service.DownloadUpdateAsync(server.Url);
        var downloadedContent = await File.ReadAllBytesAsync(downloadedPath);

        Assert.AreEqual(currentFilePath, downloadedPath, ignoreCase: true);
        CollectionAssert.AreEqual(newContent, downloadedContent);
        Assert.IsFalse(File.Exists(obsoleteFilePath));
    }

    private sealed class SingleResponseHttpServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly Task _serveTask;

        private SingleResponseHttpServer(HttpListener listener, string url, Task serveTask)
        {
            _listener = listener;
            Url = url;
            _serveTask = serveTask;
        }

        public string Url { get; }

        public static async Task<SingleResponseHttpServer> StartAsync(string fileName, byte[] content)
        {
            var port = GetFreePort();
            var prefix = $"http://127.0.0.1:{port}/";
            var url = $"{prefix}{fileName}";
            var listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();

            var serveTask = Task.Run(async () =>
            {
                var context = await listener.GetContextAsync();
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.ContentLength64 = content.Length;
                await context.Response.OutputStream.WriteAsync(content);
                context.Response.OutputStream.Close();
            });

            return new SingleResponseHttpServer(listener, url, serveTask);
        }

        public void Dispose()
        {
            _listener.Stop();
            _listener.Close();
            _serveTask.GetAwaiter().GetResult();
        }

        private static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
