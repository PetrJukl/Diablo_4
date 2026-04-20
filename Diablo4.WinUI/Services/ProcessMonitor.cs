using Diablo4.WinUI.Helpers;
using Diablo4.WinUI.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;

namespace Diablo4.WinUI.Services;

public class ProcessMonitor
{
    private static readonly string[] LogFieldSeparator = ["||"];
    private static readonly TimeSpan ActiveSessionFlushInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MonitorTickInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan WebTickInterval = TimeSpan.FromMilliseconds(5000);
    private static readonly TimeSpan StopWaitTimeout = TimeSpan.FromSeconds(2);

    private DispatcherQueueTimer? _webTimer;
    private Task? _monitorLoopTask;
    private readonly HashSet<string> _processNames;
    private readonly string _filePath;
    private DateTime? _processStartTime;
    private int _weekOfYear;
    private long _lastLogPosition = 0;
    private DateTime _lastDurationFlushUtc = DateTime.MinValue;
    private DateTime? _firstDetectionTime;
    private CancellationTokenSource? _cancellationTokenSource;

    // Inkrementální agregát historických dob hraní (C3).
    private TimeSpan _aggregatedThisWeek;
    private TimeSpan _aggregatedLastWeek;
    private int _aggregatedWeekOfYear = -1;
    private int _aggregatedYear = -1;
    private long _logReadPosition;
    private bool _durationsDirty = true;
    private readonly object _aggregateLock = new();

    private volatile bool _isWebRunning = false;
    private int _isCheckingTabsFlag;
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
        ArgumentNullException.ThrowIfNull(dispatcherQueue);

        if (_monitorLoopTask is not null)
        {
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        _monitorLoopTask = Task.Run(() => RunMonitorLoopAsync(token), token);

        if (_shouldCheckWebContent)
        {
            _webTimer = dispatcherQueue.CreateTimer();
            _webTimer.Interval = WebTickInterval;
            _webTimer.Tick += OnWebTimerTick;
            _webTimer.Start();
        }
    }

    public void Stop()
    {
        var cts = _cancellationTokenSource;
        var loop = _monitorLoopTask;

        _webTimer?.Stop();

        if (cts is not null)
        {
            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        if (loop is not null)
        {
            try
            {
                loop.Wait(StopWaitTimeout);
            }
            catch (AggregateException)
            {
            }
        }

        // C4: doflushovat aktivní sezení, aby se neztratilo při ukončení aplikace.
        FlushActiveSessionFinal();

        if (cts is not null)
        {
            try
            {
                cts.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        _cancellationTokenSource = null;
        _monitorLoopTask = null;
    }

    private async Task RunMonitorLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                DoMonitorTick();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                AppDiagnostics.LogError("Monitoring procesů selhal.", ex);
            }

            try
            {
                await Task.Delay(MonitorTickInterval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private void DoMonitorTick()
    {
        bool isAnyProcessRunning = IsAnyTrackedProcessRunning();
        FirstCheckProcess(isAnyProcessRunning);
        WriteProcessDuration(isAnyProcessRunning);
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

    private bool IsAnyTrackedProcessRunning()
    {
        if (_isWebRunning)
        {
            return true;
        }

        // C1: dotaz cíleně podle jména procesu místo enumerace všech procesů.
        foreach (var name in _processNames)
        {
            var processes = Process.GetProcessesByName(name);
            try
            {
                if (processes.Length > 0)
                {
                    return true;
                }
            }
            finally
            {
                foreach (var process in processes)
                {
                    process.Dispose();
                }
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
                _lastDurationFlushUtc = DateTime.MinValue;
                return;
            }

            var processEndTime = DateTime.Now;
            var duration = processEndTime - _processStartTime.Value;
            if (duration.TotalMilliseconds < 200)
            {
                return;
            }

            // C4: flushovat běžící sezení nejvýše jednou za 30 s.
            var nowUtc = DateTime.UtcNow;
            if ((nowUtc - _lastDurationFlushUtc) < ActiveSessionFlushInterval)
            {
                return;
            }

            if (FlushActiveSessionEntry(_processStartTime.Value, duration))
            {
                _lastDurationFlushUtc = nowUtc;
            }

            return;
        }

        // Hra právě skončila – doflushovat finální dobu, aby se neztratilo až 30 s.
        if (_processStartTime.HasValue)
        {
            var finalDuration = DateTime.Now - _processStartTime.Value;
            if (finalDuration.TotalMilliseconds >= 200)
            {
                FlushActiveSessionEntry(_processStartTime.Value, finalDuration);
            }
        }

        _processStartTime = null;
        _isRunning = false;
        _lastDurationFlushUtc = DateTime.MinValue;
        _durationsDirty = true;
    }

    private bool FlushActiveSessionEntry(DateTime startTime, TimeSpan duration)
    {
        var logEntry = $"{_weekOfYear}||{startTime:dd-MM-yyyy HH:mm:ss}||{duration.TotalSeconds}\n";
        return TryWriteToFileAtPosition(logEntry, _lastLogPosition);
    }

    private void FlushActiveSessionFinal()
    {
        if (!_processStartTime.HasValue)
        {
            return;
        }

        var finalDuration = DateTime.Now - _processStartTime.Value;
        if (finalDuration.TotalMilliseconds >= 200)
        {
            FlushActiveSessionEntry(_processStartTime.Value, finalDuration);
        }

        _processStartTime = null;
        _isRunning = false;
        _lastDurationFlushUtc = DateTime.MinValue;
        _durationsDirty = true;
    }

    public static int GetIso8601WeekOfYear(DateTime time)
    {
        DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
        if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
        {
            time = time.AddDays(3);
        }

        return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }

    private static int GetWeeksInYear(int year)
    {
        var dec31 = new DateTime(year, 12, 31);
        return GetIso8601WeekOfYear(dec31) != 1
            ? GetIso8601WeekOfYear(dec31)
            : GetIso8601WeekOfYear(new DateTime(year, 12, 27));
    }

    public WeeklyDurations GetDurations(string filePath, int actualWeekOfYear)
    {
        int actualYear = DateTime.Now.Year;

        WeeklyDurations result;
        DateTime? activeSessionStart;

        lock (_aggregateLock)
        {
            EnsureAggregatesUpToDate(filePath, actualWeekOfYear, actualYear);
            result = new WeeklyDurations(_aggregatedThisWeek, _aggregatedLastWeek);
            activeSessionStart = _processStartTime;
        }

        if (activeSessionStart.HasValue)
        {
            result = AddActiveSessionDuration(result, actualWeekOfYear, actualYear, activeSessionStart.Value);
        }

        return result;
    }

    /// <summary>C3: Načte jen nově přidané řádky logu a inkrementálně doplní agregát.</summary>
    private void EnsureAggregatesUpToDate(string filePath, int actualWeekOfYear, int actualYear)
    {
        // Během aktivního sezení čteme jen do _lastLogPosition, protože za ním leží
        // přepisovaný záznam aktuální session (nepatří do historického agregátu).
        long limitPosition = _processStartTime.HasValue ? _lastLogPosition : GetFileLength(filePath);

        bool weekChanged = _aggregatedWeekOfYear != actualWeekOfYear || _aggregatedYear != actualYear;
        bool fileShrunk = limitPosition < _logReadPosition;

        if (_durationsDirty || weekChanged || fileShrunk)
        {
            _aggregatedThisWeek = TimeSpan.Zero;
            _aggregatedLastWeek = TimeSpan.Zero;
            _logReadPosition = 0;
            _aggregatedWeekOfYear = actualWeekOfYear;
            _aggregatedYear = actualYear;
            _durationsDirty = false;
        }

        if (_logReadPosition >= limitPosition)
        {
            return;
        }

        long newPosition = AppendLogToAggregates(filePath, _logReadPosition, limitPosition, actualWeekOfYear, actualYear);
        if (newPosition >= 0)
        {
            _logReadPosition = newPosition;
        }
        else
        {
            // Při chybě I/O vyžádat full rescan při dalším volání.
            _durationsDirty = true;
        }
    }

    private long AppendLogToAggregates(string filePath, long startPosition, long endPosition, int actualWeekOfYear, int actualYear)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
            if (endPosition > stream.Length)
            {
                endPosition = stream.Length;
            }

            if (startPosition >= endPosition)
            {
                return endPosition;
            }

            stream.Position = startPosition;
            int remaining = checked((int)(endPosition - startPosition));
            var buffer = new byte[remaining];
            int total = 0;
            while (total < remaining)
            {
                int read = stream.Read(buffer, total, remaining - total);
                if (read <= 0)
                {
                    break;
                }

                total += read;
            }

            int weeksInLastYear = GetWeeksInYear(actualYear - 1);
            string text = Encoding.UTF8.GetString(buffer, 0, total);
            int lineStart = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] != '\n')
                {
                    continue;
                }

                ProcessLogLine(text, lineStart, i, actualWeekOfYear, actualYear, weeksInLastYear);
                lineStart = i + 1;
            }

            // Cokoli za posledním '\n' je neúplný řádek – přečteme ho příště.
            return startPosition + lineStart;
        }
        catch (IOException ex)
        {
            AppDiagnostics.LogWarning($"Inkrementální načtení log souboru '{filePath}' selhalo, vyžádán full rescan.", ex);
            return -1;
        }
        catch (UnauthorizedAccessException ex)
        {
            AppDiagnostics.LogWarning($"Přístup k log souboru '{filePath}' byl odmítnut při inkrementálním čtení.", ex);
            return -1;
        }
    }

    private void ProcessLogLine(string text, int start, int endExclusive, int actualWeekOfYear, int actualYear, int weeksInLastYear)
    {
        int lineEnd = endExclusive;
        if (lineEnd > start && text[lineEnd - 1] == '\r')
        {
            lineEnd--;
        }

        if (lineEnd <= start)
        {
            return;
        }

        var line = text.Substring(start, lineEnd - start);
        var parts = line.Split(LogFieldSeparator, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            return;
        }

        if (!int.TryParse(parts[0], out var weekOfYear))
        {
            return;
        }

        if (!DateTime.TryParseExact(parts[1], "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startTime))
        {
            return;
        }

        if (!FileHelper.TryParseDurationSeconds(parts[2], out var seconds))
        {
            return;
        }

        var duration = TimeSpan.FromSeconds(seconds);
        int startTimeYear = startTime.Year;

        if (weekOfYear == actualWeekOfYear && startTimeYear == actualYear)
        {
            _aggregatedThisWeek += duration;
        }
        else if (actualWeekOfYear == 1 && weekOfYear == weeksInLastYear && startTimeYear == actualYear - 1)
        {
            _aggregatedLastWeek += duration;
        }
        else if (actualWeekOfYear != 1 && weekOfYear == actualWeekOfYear - 1 && startTimeYear == actualYear)
        {
            _aggregatedLastWeek += duration;
        }
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
                    // Proces mohl skončit nebo přístup k titulku selhal – přeskočit.
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
}
