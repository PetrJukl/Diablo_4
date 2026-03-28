using Diablo4.WinUI.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Diablo4.WinUI.Tests.Helpers;

[TestClass]
public class ExecutableLaunchPolicyTests
{
    [TestMethod]
    public void IsTrustedExecutablePath_WhenFileIsUnderAllowedRoot_ReturnsTrue()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"allowed-root-{Guid.NewGuid():N}");
        var gameDirectory = Path.Combine(rootPath, "Game");
        Directory.CreateDirectory(gameDirectory);
        var executablePath = Path.Combine(gameDirectory, "Diablo IV.exe");
        File.WriteAllText(executablePath, "test");

        try
        {
            var result = ExecutableLaunchPolicy.IsTrustedExecutablePath(executablePath, [rootPath]);

            Assert.IsTrue(result);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [TestMethod]
    public void IsTrustedExecutablePath_WhenFileIsOutsideAllowedRoot_ReturnsFalse()
    {
        var allowedRoot = Path.Combine(Path.GetTempPath(), $"allowed-root-{Guid.NewGuid():N}");
        var otherRoot = Path.Combine(Path.GetTempPath(), $"other-root-{Guid.NewGuid():N}");
        Directory.CreateDirectory(allowedRoot);
        Directory.CreateDirectory(otherRoot);
        var executablePath = Path.Combine(otherRoot, "Diablo IV.exe");
        File.WriteAllText(executablePath, "test");

        try
        {
            var result = ExecutableLaunchPolicy.IsTrustedExecutablePath(executablePath, [allowedRoot]);

            Assert.IsFalse(result);
        }
        finally
        {
            Directory.Delete(allowedRoot, recursive: true);
            Directory.Delete(otherRoot, recursive: true);
        }
    }

    [TestMethod]
    public void IsTrustedExecutablePath_WhenFileHasUnsupportedExtension_ReturnsFalse()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"allowed-root-{Guid.NewGuid():N}");
        Directory.CreateDirectory(rootPath);
        var executablePath = Path.Combine(rootPath, "Diablo IV.bat");
        File.WriteAllText(executablePath, "test");

        try
        {
            var result = ExecutableLaunchPolicy.IsTrustedExecutablePath(executablePath, [rootPath]);

            Assert.IsFalse(result);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }
}