using Diablo4.WinUI.Helpers;
using Diablo4.WinUI.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;

namespace Diablo4.WinUI.Services;

public class ProcessMonitor
{
    private DispatcherQueueTimer? _timer;
    private DispatcherQueueTimer? _webTimer;
    private readonly HashSet<string> _processNames;
    private readonly string _filePath;
    private DateTime? _processStartTime;
    private int _weekOfYear;
    private long _lastLogPosition = 0;
    private DateTime? _firstDetectionTime;
    private CancellationTokenSource? _cancellationTokenSource;
    private WeeklyDurations _cachedHistoricalDurations = new(TimeSpan.Zero, TimeSpan.Zero);
    private int _cachedWeekOfYear = -1;
    private int _cachedYear = -1;
    private long _cachedFileLength = -1;
    private bool _durationsDirty = true;

    private volatile bool _isWebRunning = false;
    private int _isCheckingTabsFlag;
    private volatile bool _isProcessing;
    private volatile bool _isRunning;
    public bool IsRunning => _isRunning;

    private readonly bool _shouldCheckWebContent;

    public ProcessMonitor(string filePath, bool checkWebContent, params string[] processNames)
    {
        _processNames = new HashSet<string>(processNames, StringComparer.OrdinalIgnoreCase);
        _filePath = filePath;
        _shouldCheckWebContent = checkWebContent;
    }

    public void Start(DispatcherQueue dispatcherQueue)
    {
        _cancellationTokenSource = new CancellationTokenSource();

        _timer = dispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(500);
        _timer.Tick += OnTimerTick;
        _timer.Start();

        if (_shouldCheckWebContent)
        {
            _webTimer = dispatcherQueue.CreateTimer();
            _webTimer.Interval = TimeSpan.FromMilliseconds(5000);
            _webTimer.Tick += OnWebTimerTick;
            _webTimer.Start();
        }
    }

    public void Stop()
    {
        _timer?.Stop();
        _webTimer?.Stop();
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    private void OnTimerTick(DispatcherQueueTimer sender, object args)
    {
        if (_isProcessing || _cancellationTokenSource is null)
        {
            return;
        }

        _isProcessing = true;
        var cancellationToken = _cancellationTokenSource.Token;

        _ = Task.Run(() =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var processes = Process.GetProcesses();

                try
                {
                    bool isAnyProcessRunning = IsAnyTrackedProcessRunning(processes);
                    FirstCheckProcess(isAnyProcessRunning);
                    WriteProcessDuration(isAnyProcessRunning);
                }
                finally
                {
                    foreach (var process in processes)
                    {
                        process.Dispose();
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                AppDiagnostics.LogError("Monitoring procesů selhal.", ex);
            }
            finally
            {
                _isProcessing = false;
            }
        }, cancellationToken);
    }

    private void OnWebTimerTick(DispatcherQueueTimer sender, object args)
    {
        if (_cancellationTokenSource is null)
        {
            return;
        }

        var cancellationToken = _cancellationTokenSource.Token;

        _ = Task.Run(() =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                CheckAllOpenTabsForUdemy();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                AppDiagnostics.LogError("Webový monitoring selhal.", ex);
            }
        }, cancellationToken);
    }

    private bool IsAnyTrackedProcessRunning(Process[] processes)
    {
        if (_isWebRunning)
        {
            return true;
        }

        foreach (var process in processes)
        {
            if (_processNames.Contains(process.ProcessName))
            {
                return true;
            }
        }

        return false;
    }

    private void FirstCheckProcess(bool isAnyProcessRunning)
    {
        if (!isAnyProcessRunning)
        {
            _firstDetectionTime = null;
            return;
        }

        if (!_firstDetectionTime.HasValue)
        {
            _firstDetectionTime = DateTime.Now;
            return;
        }

        if ((DateTime.Now - _firstDetectionTime.Value).TotalMilliseconds >= 200)
        {
            string dateTimeString = FileHelper.FormatLastPlayedTimestamp(DateTime.Now) + Environment.NewLine;

            if (TryWriteToFile(dateTimeString, FileMode.Open, FileAccess.Write))
            {
                _firstDetectionTime = null;
            }
        }
    }

    private void WriteProcessDuration(bool isAnyProcessRunning)
    {
        if (isAnyProcessRunning)
        {
            _isRunning = true;

            if (!_processStartTime.HasValue)
            {
                _processStartTime = DateTime.Now;
                _weekOfYear = GetIso8601WeekOfYear((DateTime)_processStartTime);
                _lastLogPosition = GetFileLength(_filePath);
                _durationsDirty = true;
                return;
            }

            var processEndTime = DateTime.Now;
            var duration = processEndTime - _processStartTime.Value;
            if (duration.TotalMilliseconds < 200)
            {
                return;
            }

            var logEntry = $"{_weekOfYear}||{_processStartTime.Value:dd-MM-yyyy HH:mm:ss}||{duration.TotalSeconds}\n";
            TryWriteToFileAtPosition(logEntry, _lastLogPosition);

            return;
        }

        _processStartTime = null;
        _isRunning = false;
        _durationsDirty = true;
    }

    public int GetIso8601WeekOfYear(DateTime time)
    {
        DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
        if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
        {
            time = time.AddDays(3);
        }

        return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }

    private int GetWeeksInYear(int year)
    {
        var dec31 = new DateTime(year, 12, 31);
        return GetIso8601WeekOfYear(dec31) != 1
            ? GetIso8601WeekOfYear(dec31)
            : GetIso8601WeekOfYear(new DateTime(year, 12, 27));
    }

    public WeeklyDurations GetDurations(string filePath, int actualWeekOfYear)
    {
        int actualYear = DateTime.Now.Year;
        long currentFileLength = GetFileLength(filePath);

        if (_durationsDirty
            || _cachedWeekOfYear != actualWeekOfYear
            || _cachedYear != actualYear
            || (!_processStartTime.HasValue && _cachedFileLength != currentFileLength))
        {
            _cachedHistoricalDurations = LoadHistoricalDurations(filePath, actualWeekOfYear, actualYear, _processStartTime);
            _cachedWeekOfYear = actualWeekOfYear;
            _cachedYear = actualYear;
            _cachedFileLength = currentFileLength;
            _durationsDirty = false;
        }

        var result = _cachedHistoricalDurations;

        if (_processStartTime.HasValue)
        {
            result = AddActiveSessionDuration(result, actualWeekOfYear, actualYear, _processStartTime.Value);
        }

        return result;
    }

    private WeeklyDurations LoadHistoricalDurations(string filePath, int actualWeekOfYear, int actualYear, DateTime? activeSessionStartTime)
    {
        var weeklyDurations = new TimeSpan();
        var lastWeekDuration = new TimeSpan();
        int weeksInLastYear = GetWeeksInYear(actualYear - 1);

        var lines = ReadAllLinesShared(filePath);

        foreach (var line in lines)
        {
            string[] separator = new string[] { "||" };
            var parts = line.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            {
                continue;
            }

            if (!int.TryParse(parts[0], out var weekOfYear))
            {
                continue;
            }

            if (!DateTime.TryParseExact(parts[1], "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startTime))
            {
                continue;
            }

            if (!FileHelper.TryParseDurationSeconds(parts[2], out var seconds))
            {
                continue;
            }

            var startTimeYear = startTime.Year;
            var duration = TimeSpan.FromSeconds(seconds);

            if (activeSessionStartTime.HasValue && AreSameLoggedStartTime(startTime, activeSessionStartTime.Value))
            {
                continue;
            }

            if (weekOfYear == actualWeekOfYear && startTimeYear == actualYear)
            {
                weeklyDurations += duration;
            }
            else if (actualWeekOfYear == 1 && weekOfYear == weeksInLastYear && startTimeYear == actualYear - 1) 
            {
                lastWeekDuration += duration;
            }
            else if (actualWeekOfYear != 1 && weekOfYear == actualWeekOfYear - 1 && startTimeYear == actualYear)
            {
                lastWeekDuration += duration;
            }
        }

        return new WeeklyDurations(weeklyDurations, lastWeekDuration);
    }

    private WeeklyDurations AddActiveSessionDuration(WeeklyDurations durations, int actualWeekOfYear, int actualYear, DateTime activeSessionStartTime)
    {
        var activeDuration = DateTime.Now - activeSessionStartTime;
        int activeWeekOfYear = GetIso8601WeekOfYear(activeSessionStartTime);

        if (activeWeekOfYear == actualWeekOfYear && activeSessionStartTime.Year == actualYear)
        {
            return durations with { ThisWeek = durations.ThisWeek + activeDuration };
        }

        int weeksInLastYear = GetWeeksInYear(actualYear - 1);

        if (actualWeekOfYear == 1 && activeWeekOfYear == weeksInLastYear && activeSessionStartTime.Year == actualYear - 1)
        {
            return durations with { LastWeek = durations.LastWeek + activeDuration };
        }

        if (actualWeekOfYear != 1 && activeWeekOfYear == actualWeekOfYear - 1 && activeSessionStartTime.Year == actualYear)
        {
            return durations with { LastWeek = durations.LastWeek + activeDuration };
        }

        return durations;
    }

    private static bool AreSameLoggedStartTime(DateTime loggedStartTime, DateTime activeSessionStartTime)
    {
        return loggedStartTime.ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture)
            == activeSessionStartTime.ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
    }

    private void CheckAllOpenTabsForUdemy()
    {
        if (Interlocked.CompareExchange(ref _isCheckingTabsFlag, 1, 0) != 0)
        {
            return;
        }

        Process[]? allProcesses = null;
        try
        {
            allProcesses = Process.GetProcesses();
            bool found = false;

            foreach (var process in allProcesses)
            {
                if (process.ProcessName is not ("firefox" or "chrome" or "msedge"))
                {
                    continue;
                }

                try
                {
                    if (process.MainWindowTitle.Contains("udemy", StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }
                catch (Exception)
                {
                    // Proces mohl skončit nebo přístup k titulku selhal – přeskočit..
                }
            }

            _isWebRunning = found;
        }
        catch (Exception ex)
        {
            _isWebRunning = false;
            AppDiagnostics.LogWarning("Kontrola prohlížečů pro Udemy selhala.", ex);
        }
        finally
        {
            if (allProcesses != null)
            {
                foreach (var p in allProcesses)
                {
                    p.Dispose();
                }
            }

            Interlocked.Exchange(ref _isCheckingTabsFlag, 0);
        }
    }

    private static long GetFileLength(string filePath)
    {
        try
        {
            return new FileInfo(filePath).Length;
        }
        catch (IOException ex)
        {
            AppDiagnostics.LogWarning($"Nepodařilo se získat délku souboru '{filePath}'.", ex);
            return 0;
        }
        catch (UnauthorizedAccessException ex)
        {
            AppDiagnostics.LogWarning($"Přístup k souboru '{filePath}' byl odmítnut při zjišťování délky.", ex);
            return 0;
        }
    }

    private bool TryWriteToFile(string content, FileMode mode, FileAccess access)
    {
        try
        {
            using var stream = new FileStream(_filePath, mode, access, FileShare.ReadWrite);
            using var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            return true;
        }
        catch (IOException ex)
        {
            AppDiagnostics.LogWarning("Nepodařilo se zapsat monitoring do log souboru.", ex);
            return false;
        }
    }

    private bool TryWriteToFileAtPosition(string content, long position)
    {
        try
        {
            using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            stream.Position = position;

            using var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.SetLength(stream.Position);
            return true;
        }
        catch (IOException ex)
        {
            AppDiagnostics.LogWarning("Nepodařilo se aktualizovat délku běhu procesu v logu.", ex);
            return false;
        }
    }

    private static string[] ReadAllLinesShared(string filePath)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            var lines = new List<string>();

            while (reader.ReadLine() is { } line)
            {
                lines.Add(line);
            }

            return [.. lines];
        }
        catch (IOException ex)
        {
            AppDiagnostics.LogWarning($"Nepodařilo se načíst log soubor '{filePath}'.", ex);
            return [];
        }
    }
}
