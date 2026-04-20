using Diablo4.WinUI.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Diablo4.WinUI.Tests.Helpers;

[TestClass]
public class JsonPathCacheStoreTests
{
    [TestMethod]
    public void Set_AndTryGet_ReturnsStoredPath()
    {
        var path = CreateTempCachePath();
        try
        {
            var store = new JsonPathCacheStore(path);
            store.Set("Game.exe", @"C:\Games\Game\Game.exe");

            Assert.IsTrue(store.TryGet("Game.exe", out var result));
            Assert.AreEqual(@"C:\Games\Game\Game.exe", result);
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [TestMethod]
    public void TryGet_WithUnknownKey_ReturnsFalse()
    {
        var path = CreateTempCachePath();
        try
        {
            var store = new JsonPathCacheStore(path);

            Assert.IsFalse(store.TryGet("Missing.exe", out var result));
            Assert.AreEqual(string.Empty, result);
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [TestMethod]
    public void TryGet_IsCaseInsensitive()
    {
        var path = CreateTempCachePath();
        try
        {
            var store = new JsonPathCacheStore(path);
            store.Set("Game.exe", @"C:\Games\Game\Game.exe");

            Assert.IsTrue(store.TryGet("GAME.EXE", out var result));
            Assert.AreEqual(@"C:\Games\Game\Game.exe", result);
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [TestMethod]
    public void Set_PersistsAcrossInstances()
    {
        var path = CreateTempCachePath();
        try
        {
            var first = new JsonPathCacheStore(path);
            first.Set("Game.exe", @"C:\Games\Game\Game.exe");

            var second = new JsonPathCacheStore(path);

            Assert.IsTrue(second.TryGet("Game.exe", out var result));
            Assert.AreEqual(@"C:\Games\Game\Game.exe", result);
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [TestMethod]
    public void Set_WithEmptyKeyOrPath_IsIgnored()
    {
        var path = CreateTempCachePath();
        try
        {
            var store = new JsonPathCacheStore(path);
            store.Set(string.Empty, @"C:\Games\Game\Game.exe");
            store.Set("Game.exe", string.Empty);

            Assert.IsFalse(store.TryGet("Game.exe", out _));
            Assert.AreEqual(0, store.Snapshot().Count);
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [TestMethod]
    public void Constructor_WithCorruptedFile_StartsEmpty()
    {
        var path = CreateTempCachePath();
        try
        {
            File.WriteAllText(path, "{ this is not valid json");

            var store = new JsonPathCacheStore(path);

            Assert.AreEqual(0, store.Snapshot().Count);
        }
        finally
        {
            SafeDelete(path);
        }
    }

    private static string CreateTempCachePath()
    {
        return Path.Combine(Path.GetTempPath(), $"diablo4-paths-cache-{Guid.NewGuid():N}.json");
    }

    private static void SafeDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            var tempPath = path + ".tmp";
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
