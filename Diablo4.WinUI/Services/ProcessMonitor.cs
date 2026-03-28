using Diablo4.WinUI.Models;
using System;
using System.Collections.Generic;
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
    private readonly string[] _processNames;
    private readonly string _filePath;
    private DateTime? _processStartTime;
    private int _weekOfYear;
    private long _lastLogPosition = 0;
    private DateTime? _firstDetectionTime;
    
    private bool _isWebRunning = false;
    private bool _isCheckingTabs = false;
    private volatile bool _isProcessing;
    private volatile bool _isRunning;
    public bool IsRunning => _isRunning;

    private readonly bool _shouldCheckWebContent;

    public ProcessMonitor(string filePath, bool checkWebContent, params string[] processNames)
    {
        _processNames = processNames;
        _filePath = filePath;
        _shouldCheckWebContent = checkWebContent;
    }

    public void Start(DispatcherQueue dispatcherQueue)
    {
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
    }

    private void OnTimerTick(DispatcherQueueTimer sender, object args)
    {
        if (_isProcessing) return;
        _isProcessing = true;
        Task.Run(() =>
        {
            try
            {
                var processes = Process.GetProcesses();
                FirstCheckProcess(processes);
                WriteProcessDuration(processes);
            }
            finally
            {
                _isProcessing = false;
            }
        });
    }

    private void OnWebTimerTick(DispatcherQueueTimer sender, object args)
    {
        Task.Run(() => CheckAllOpenTabsForUdemy());
    }

    private void FirstCheckProcess(Process[] processes)
    {
        bool isAnyProcessRunning = false;

        foreach (var processName in _processNames)
        {
            var processExists = processes.Any(x => x.ProcessName == processName);
            if (processExists || _isWebRunning)
            {
                isAnyProcessRunning = true;

                if (!_firstDetectionTime.HasValue)
                {
                    _firstDetectionTime = DateTime.Now;
                }
                else if ((DateTime.Now - _firstDetectionTime.Value).TotalMilliseconds >= 200)
                {
                    string dateTimeString = DateTime.Now.ToString() + Environment.NewLine;
                    WriteToFile(dateTimeString, FileMode.Open, AccessMode.Write);
                    _firstDetectionTime = null;
                }
            }
        }

        if (!isAnyProcessRunning)
        {
            _firstDetectionTime = null;
        }
    }

    private void WriteProcessDuration(Process[] processes)
    {
        bool isAnyProcessRunning = false;

        foreach (var processName in _processNames)
        {
            var processExists = processes.Any(x => x.ProcessName == processName);
            if (processExists || _isWebRunning)
            {
                isAnyProcessRunning = true;
                _isRunning = true;

                if (!_processStartTime.HasValue)
                {
                    _processStartTime = DateTime.Now;
                    _weekOfYear = GetIso8601WeekOfYear((DateTime)_processStartTime);
                    _lastLogPosition = new FileInfo(_filePath).Length;
                }
                else if (_processStartTime.HasValue)
                {
                    var processEndTime = DateTime.Now;
                    var duration = processEndTime - _processStartTime.Value;
                    if (duration.TotalMilliseconds >= 200)
                    {
                        var logEntry = $"{_weekOfYear}||{_processStartTime.Value:dd-MM-yyyy HH:mm:ss}||{duration.TotalSeconds}\n";
                        WriteToFileAtPosition(logEntry, _lastLogPosition);
                    }
                }
            }
        }

        if (!isAnyProcessRunning)
        {
            _processStartTime = null;
            _isRunning = false;
        }
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

    public WeeklyDurations GetDurations(string filePath, int actualWeekOfYear)
    {
        var weeklyDurations = new TimeSpan();
        var lastWeekDuration = new TimeSpan();
        int actualYear = DateTime.Now.Year;
        int weeksInLastYear = GetIso8601WeekOfYear(DateTime.Parse($"31.12.{actualYear - 1}")) != 1 
            ? GetIso8601WeekOfYear(DateTime.Parse($"31.12.{actualYear - 1}")) 
            : GetIso8601WeekOfYear(DateTime.Parse($"27.12.{actualYear - 1}"));

        string[]? lines = null;
        const int maxRetries = 5;
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                lines = File.ReadAllLines(filePath);
                break;
            }
            catch (IOException) when (i < maxRetries - 1)
            {
                Thread.Sleep(200);
            }
        }

        if (lines == null)
            return new WeeklyDurations(TimeSpan.Zero, TimeSpan.Zero);

        foreach (var line in lines)
        {
            string[] separator = new string[] { "||" };
            var parts = line.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            {
                continue;
            }
            var weekOfYear = int.Parse(parts[0]);
            var startTimeYear = DateTime.Parse(parts[1]).Year;
            var duration = TimeSpan.FromSeconds(double.Parse(parts[2]));

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

    private void CheckAllOpenTabsForUdemy()
    {
        if (_isCheckingTabs) return;
        _isCheckingTabs = true;

        try
        {
            _isWebRunning = Process.GetProcesses()
                .Where(p => p.ProcessName is "firefox" or "chrome" or "msedge")
                .Any(p =>
                {
                    try { return p.MainWindowTitle.Contains("udemy", StringComparison.OrdinalIgnoreCase); }
                    catch { return false; }
                });
        }
        finally
        {
            _isCheckingTabs = false;
        }
    }

    private void WriteToFile(string content, FileMode mode, AccessMode access)
    {
        const int maxRetries = 5;
        FileStream? stream = null;
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var fileAccess = access == AccessMode.Write ? FileAccess.Write : FileAccess.Read;
                stream = new FileStream(_filePath, mode, fileAccess, FileShare.Read);
                break;
            }
            catch (IOException) when (i < maxRetries - 1)
            {
                Thread.Sleep(50);
            }
        }

        if (stream == null) return;

        using (stream)
        using (var writer = new StreamWriter(stream))
        {
            writer.Write(content);
        }
    }

    private void WriteToFileAtPosition(string content, long position)
    {
        const int maxRetries = 5;
        FileStream? stream = null;
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                stream = new FileStream(_filePath, FileMode.Open, FileAccess.Write, FileShare.Read);
                stream.Position = position;
                break;
            }
            catch (IOException) when (i < maxRetries - 1)
            {
                Thread.Sleep(70);
            }
        }

        if (stream == null) return;

        using (stream)
        using (var writer = new StreamWriter(stream))
        {
            writer.Write(content);
            writer.Flush();
            stream.SetLength(stream.Position);
        }
    }

    private enum AccessMode
    {
        Read,
        Write
    }
}
