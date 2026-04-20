using Diablo4.WinUI.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Diablo4.WinUI.Tests.Helpers;

[TestClass]
public class AppDiagnosticsTests
{
    private static readonly string LogDirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Diablo Log");
    private static readonly string LogFilePath = Path.Combine(LogDirectoryPath, "Diablo4.WinUI.log");
    private static readonly string RotatedFirstPath = Path.Combine(LogDirectoryPath, "Diablo4.WinUI.1.log");

    [TestMethod]
    public void LogInfo_WhenLogFileExceedsTwoMegabytes_RotatesToFirstGenerationFile()
    {
        Directory.CreateDirectory(LogDirectoryPath);

        if (File.Exists(RotatedFirstPath))
        {
            File.Delete(RotatedFirstPath);
        }

        var oversizedContent = new byte[(2 * 1024 * 1024) + 1];
        File.WriteAllBytes(LogFilePath, oversizedContent);

        AppDiagnostics.LogInfo("rotace logu test");

        Assert.IsTrue(File.Exists(RotatedFirstPath), "Po překročení limitu má vzniknout .1.log.");
        Assert.IsTrue(new FileInfo(LogFilePath).Length < 2 * 1024 * 1024, "Aktivní log má být po rotaci menší než limit.");
    }
}
