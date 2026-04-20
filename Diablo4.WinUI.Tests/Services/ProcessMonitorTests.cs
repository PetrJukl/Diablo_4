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

    [TestMethod]
    public void GetDurations_WhenInvokedRepeatedly_ReusesIncrementalAggregate()
    {
        var now = DateTime.Now;
        var currentWeek = ProcessMonitor.GetIso8601WeekOfYear(now);
        var firstEntry = TimeSpan.FromMinutes(10);
        var filePath = CreateLogFile(
            $"{currentWeek}||{now:dd-MM-yyyy HH:mm:ss}||{firstEntry.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

        var monitor = new ProcessMonitor(filePath, false, "Diablo IV");

        var first = monitor.GetDurations(filePath, currentWeek);
        var second = monitor.GetDurations(filePath, currentWeek);

        Assert.AreEqual(firstEntry, first.ThisWeek);
        Assert.AreEqual(firstEntry, second.ThisWeek, "Druhé volání nesmí počítat existující řádky znova.");
    }

    [TestMethod]
    public void GetDurations_WhenNewLineAppendedBetweenCalls_AddsOnlyTheDelta()
    {
        var now = DateTime.Now;
        var currentWeek = ProcessMonitor.GetIso8601WeekOfYear(now);
        var firstEntry = TimeSpan.FromMinutes(20);
        var secondEntry = TimeSpan.FromMinutes(5);
        var filePath = CreateLogFile(
            $"{currentWeek}||{now:dd-MM-yyyy HH:mm:ss}||{firstEntry.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

        var monitor = new ProcessMonitor(filePath, false, "Diablo IV");
        var initial = monitor.GetDurations(filePath, currentWeek);
        Assert.AreEqual(firstEntry, initial.ThisWeek);

        File.AppendAllText(
            filePath,
            $"{currentWeek}||{now:dd-MM-yyyy HH:mm:ss}||{secondEntry.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)}\n");

        var afterAppend = monitor.GetDurations(filePath, currentWeek);

        Assert.AreEqual(firstEntry + secondEntry, afterAppend.ThisWeek);
    }

    [TestMethod]
    public void GetDurations_WhenFileShrinks_ForcesFullRescan()
    {
        var now = DateTime.Now;
        var currentWeek = ProcessMonitor.GetIso8601WeekOfYear(now);
        var firstEntry = TimeSpan.FromMinutes(30);
        var rebuiltEntry = TimeSpan.FromMinutes(7);
        var filePath = CreateLogFile(
            $"{currentWeek}||{now:dd-MM-yyyy HH:mm:ss}||{firstEntry.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

        var monitor = new ProcessMonitor(filePath, false, "Diablo IV");
        var initial = monitor.GetDurations(filePath, currentWeek);
        Assert.AreEqual(firstEntry, initial.ThisWeek);

        File.WriteAllText(
            filePath,
            $"{currentWeek}||{now:dd-MM-yyyy HH:mm:ss}||{rebuiltEntry.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)}\n");

        var afterShrink = monitor.GetDurations(filePath, currentWeek);

        Assert.AreEqual(rebuiltEntry, afterShrink.ThisWeek);
    }

    private static string CreateLogFile(params string[] lines)
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"diablo4-winui-tests-{Guid.NewGuid():N}.log");
        File.WriteAllLines(filePath, lines.Length > 0 ? lines : [FileHelper.FormatLastPlayedTimestamp(DateTime.Now)]);
        return filePath;
    }
}
