using Diablo4.WinUI.Services;
using Diablo4.WinUI.Helpers;
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
    public async Task DownloadUpdateAsync_WhenDownloadUrlUsesUntrustedHost_ThrowsArgumentException()
    {
        var service = new UpdateService("https://raw.githubusercontent.com/PetrJukl/Diablo_4/main/update-manifest.json");

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => service.DownloadUpdateAsync("https://example.com/KontrolaParbySetup.exe"));
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

        UpdateSourcePolicy.TrustOverride = uri => uri.IsLoopback;

        try
        {
            var downloadedPath = await service.DownloadUpdateAsync(server.Url);

            var expectedDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KontrolaParby",
                "Updates");

            Assert.AreEqual(Path.Combine(expectedDirectory, fileName), downloadedPath, ignoreCase: true);
        }
        finally
        {
            UpdateSourcePolicy.TrustOverride = null;
        }
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

        UpdateSourcePolicy.TrustOverride = uri => uri.IsLoopback;

        try
        {
            var downloadedPath = await service.DownloadUpdateAsync(server.Url);
            var downloadedContent = await File.ReadAllBytesAsync(downloadedPath);

            Assert.AreEqual(currentFilePath, downloadedPath, ignoreCase: true);
            CollectionAssert.AreEqual(newContent, downloadedContent);
            Assert.IsFalse(File.Exists(obsoleteFilePath));
        }
        finally
        {
            UpdateSourcePolicy.TrustOverride = null;
        }
    }

    [TestMethod]
    public async Task DownloadUpdateAsync_WhenTrustedRedirectTargetHasNoExtension_StoresInstallerSuccessfully()
    {
        var fileName = $"KontrolaParbySetup-{Guid.NewGuid():N}.exe";
        var content = "redirected-installer-content"u8.ToArray();
        using var server = await RedirectingHttpServer.StartAsync(fileName, "/download", content);
        var service = new UpdateService("https://example.invalid/manifest.json");

        UpdateSourcePolicy.TrustOverride = uri => uri.IsLoopback;

        try
        {
            var downloadedPath = await service.DownloadUpdateAsync(server.EntryUrl);
            var downloadedContent = await File.ReadAllBytesAsync(downloadedPath);

            CollectionAssert.AreEqual(content, downloadedContent);
        }
        finally
        {
            UpdateSourcePolicy.TrustOverride = null;
        }
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
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
            var port = UpdateServiceTests.GetFreePort();
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
    }

    private sealed class RedirectingHttpServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly Task _serveTask;

        private RedirectingHttpServer(HttpListener listener, string entryUrl, Task serveTask)
        {
            _listener = listener;
            EntryUrl = entryUrl;
            _serveTask = serveTask;
        }

        public string EntryUrl { get; }

        public static async Task<RedirectingHttpServer> StartAsync(string entryFileName, string redirectPath, byte[] content)
        {
            var port = UpdateServiceTests.GetFreePort();
            var prefix = $"http://127.0.0.1:{port}/";
            var entryUrl = $"{prefix}{entryFileName}";
            var redirectUrl = $"{prefix}{redirectPath.TrimStart('/')}";
            var listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();

            var serveTask = Task.Run(async () =>
            {
                var redirectContext = await listener.GetContextAsync();
                redirectContext.Response.StatusCode = (int)HttpStatusCode.Redirect;
                redirectContext.Response.RedirectLocation = redirectUrl;
                redirectContext.Response.Close();

                var contentContext = await listener.GetContextAsync();
                contentContext.Response.StatusCode = (int)HttpStatusCode.OK;
                contentContext.Response.ContentLength64 = content.Length;
                await contentContext.Response.OutputStream.WriteAsync(content);
                contentContext.Response.OutputStream.Close();
            });

            return new RedirectingHttpServer(listener, entryUrl, serveTask);
        }

        public void Dispose()
        {
            _listener.Stop();
            _listener.Close();
            _serveTask.GetAwaiter().GetResult();
        }
    }
}
