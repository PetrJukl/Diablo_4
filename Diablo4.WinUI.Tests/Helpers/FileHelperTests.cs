using Diablo4.WinUI.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Diablo4.WinUI.Tests.Helpers;

[TestClass]
public class FileHelperTests
{
    [TestMethod]
    public void TryParseDurationSeconds_WhenValueUsesDecimalComma_ReturnsParsedNumber()
    {
        var parsed = FileHelper.TryParseDurationSeconds("8,180373", out var seconds);

        Assert.IsTrue(parsed);
        Assert.AreEqual(8.180373d, seconds, 0.000001d);
    }

    [TestMethod]
    public void TryParseDurationSeconds_WhenValueUsesDecimalDot_ReturnsParsedNumber()
    {
        var parsed = FileHelper.TryParseDurationSeconds("8.180373", out var seconds);

        Assert.IsTrue(parsed);
        Assert.AreEqual(8.180373d, seconds, 0.000001d);
    }
}
