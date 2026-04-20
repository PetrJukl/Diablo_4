using Diablo4.WinUI.Helpers;
using Diablo4.WinUI.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Diablo4.WinUI.Tests.Services;

[TestClass]
public class ProcessMonitorTests
{
    [TestMethod]
    public void GetIso8601WeekOfYear_WhenDateIsInFirstIsoWeek_ReturnsOne()
    {
        var result = ProcessMonitor.GetIso8601WeekOfYear(new DateTime(2026, 1, 1));

        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public void GetDurations_WhenLogContainsCurrentWeekEntry_ReturnsCurrentWeekDuration()
    {
        var now = DateTime.Now;
        var monitor = new ProcessMonitor(CreateLogFile(), false, "Diablo IV");
        var currentWeek = ProcessMonitor.GetIso8601WeekOfYear(now);
        var expected = TimeSpan.FromMinutes(30);
        var filePath = CreateLogFile($"{currentWeek}||{now:dd-MM-yyyy HH:mm:ss}||{expected.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

        var result = monitor.GetDurations(filePath, currentWeek);

        Assert.AreEqual(expected, result.ThisWeek);
    }

    [TestMethod]
    public void GetDurations_WhenLogContainsPreviousWeekEntry_ReturnsLastWeekDuration()
    {
        var now = DateTime.Now;
        var previousWeekDate = now.AddDays(-7);
        var monitor = new ProcessMonitor(CreateLogFile(), false, "Diablo IV");
        var currentWeek = ProcessMonitor.GetIso8601WeekOfYear(now);
        var previousWeek = ProcessMonitor.GetIso8601WeekOfYear(previousWeekDate);
        var expected = TimeSpan.FromMinutes(45);
        var filePath = CreateLogFile($"{previousWeek}||{previousWeekDate:dd-MM-yyyy HH:mm:ss}||{expected.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

        var result = monitor.GetDurations(filePath, currentWeek);

        Assert.AreEqual(expected, result.LastWeek);
    }

    [TestMethod]
    public void GetDurations_WhenLogContainsCommaDecimalSeparator_ReturnsCurrentWeekDuration()
    {
        var now = DateTime.Now;
        var monitor = new ProcessMonitor(CreateLogFile(), false, "Diablo IV");
        var currentWeek = ProcessMonitor.GetIso8601WeekOfYear(now);
        var expected = TimeSpan.FromMinutes(15);
        var culture = System.Globalization.CultureInfo.GetCultureInfo("cs-CZ");
        var filePath = CreateLogFile($"{currentWeek}||{now:dd-MM-yyyy HH:mm:ss}||{expected.TotalSeconds.ToString(culture)}");

        var result = monitor.GetDurations(filePath, currentWeek);

        Assert.AreEqual(expected, result.ThisWeek);
    }

    private static string CreateLogFile(params string[] lines)
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"diablo4-winui-tests-{Guid.NewGuid():N}.log");
        File.WriteAllLines(filePath, lines.Length > 0 ? lines : [FileHelper.FormatLastPlayedTimestamp(DateTime.Now)]);
        return filePath;
    }
}
