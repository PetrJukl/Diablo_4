using CommunityToolkit.Mvvm.ComponentModel;
using Diablo4.WinUI.Helpers;
using Diablo4.WinUI.Models;
using Diablo4.WinUI.Services;
using Microsoft.UI.Dispatching;
using System;
using System.Diagnostics;
using System.IO;

namespace Diablo4.WinUI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly string _filePath;
    private ProcessMonitor? _processMonitor;
    private DispatcherQueueTimer? _messageUpdateTimer;
    private DispatcherQueueTimer? _statsUpdateTimer;
    private bool _lastKnownProcessRunningState;

    [ObservableProperty]
    private string _messageText = "Text";

    [ObservableProperty]
    private string _weekDurationText = "Doba pařby";

    [ObservableProperty]
    private string _lastWeekDurationText = "Doba pařby minulý týden";

    private TimeSpan _totalDuration = new TimeSpan();
    private bool _isWeekend = false;
    private DateTime? _cachedLastPlayedDateTime;

    public event EventHandler? WeekendMotivationRequested;
    public event EventHandler<bool>? ProcessRunningStateChanged;

    public MainViewModel()
    {
        _filePath = FileHelper.EnsureFileExists("Diablo IV.txt");
    }

    public void Initialize(DispatcherQueue dispatcherQueue)
    {
        LoadCachedLastPlayedDateTime();

        // Start message update timer (50ms)
        _messageUpdateTimer = dispatcherQueue.CreateTimer();
        _messageUpdateTimer.Interval = TimeSpan.FromMilliseconds(50);
        _messageUpdateTimer.Tick += UpdateMessageTimerTick;
        _messageUpdateTimer.Start();

        // Initialize process monitor
        bool shouldCheckWeb = MachineContextHelper.ShouldCheckWebContent();
        _processMonitor = new ProcessMonitor(
            _filePath,
            shouldCheckWeb,
            "Diablo IV",
            "DragonAgeInquisition",
            "Diablo III64",
            "devenv",
            "Code",
            "Dragon Age The Veilguard",
            "DragonAge2",
            "daorigins",
            "WindowsTerminal",
            "OpenConsole"
        );
        _processMonitor.Start(dispatcherQueue);

        // Start stats update timer (800ms)
        _statsUpdateTimer = dispatcherQueue.CreateTimer();
        _statsUpdateTimer.Interval = TimeSpan.FromMilliseconds(800);
        _statsUpdateTimer.Tick += UpdateStatsTimerTick;
        _statsUpdateTimer.Start();
    }

    private void UpdateMessageTimerTick(DispatcherQueueTimer sender, object args)
    {
        if (_cachedLastPlayedDateTime == null) return;
        TimeSpan timeSinceLastWrite = DateTime.Now - _cachedLastPlayedDateTime.Value;
        MessageText = $"Už jsi nepařila: {timeSinceLastWrite.Days} dní, {timeSinceLastWrite.Hours} hodiny,\n  {timeSinceLastWrite.Minutes} minuty, {timeSinceLastWrite.Seconds} sekund a {timeSinceLastWrite.Milliseconds} miliseků :).";
    }

    private void UpdateStatsTimerTick(DispatcherQueueTimer sender, object args)
    {
        if (_processMonitor == null) return;

        LoadCachedLastPlayedDateTime();

        try
        {
            int currentWeekOfYear = _processMonitor.GetIso8601WeekOfYear(DateTime.Now);
            var durations = _processMonitor.GetDurations(_filePath, currentWeekOfYear);

            if (durations.ThisWeek != TimeSpan.Zero)
            {
                _totalDuration = durations.ThisWeek;
                string formattedDuration = $"{Math.Floor(_totalDuration.TotalHours)} hodin, {_totalDuration.Minutes} minut a {_totalDuration.Seconds} vteřin";
                WeekDurationText = $"Tento týden \n {formattedDuration}";
            }
            else
            {
                WeekDurationText = "Chtělo by to roztočit grafárnu.";
            }

            if (durations.LastWeek != TimeSpan.Zero)
            {
                TimeSpan lastWeekTotalDuration = durations.LastWeek;
                string formattedDuration = $"{Math.Floor(lastWeekTotalDuration.TotalHours)} hodin, {lastWeekTotalDuration.Minutes} minut a {lastWeekTotalDuration.Seconds} vteřin";
                LastWeekDurationText = $"Minulý týden \n {formattedDuration}";
            }
            else
            {
                LastWeekDurationText = "Minulý týden se nezadařilo.";
            }

            // Check weekend motivation
            CheckWeekendMotivation();
            NotifyProcessRunningState(_processMonitor.IsRunning);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[UpdateStatsTimerTick] {ex}");
        }
    }

    private void LoadCachedLastPlayedDateTime()
    {
        if (!File.Exists(_filePath)) return;
        try
        {
            using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            string? firstLine = reader.ReadLine();
            if (DateTime.TryParse(firstLine, out DateTime dt))
                _cachedLastPlayedDateTime = dt;
        }
        catch (IOException) { }
    }

    private void CheckWeekendMotivation()
    {
        if (!_isWeekend && _totalDuration.TotalHours < 10 &&
            (DateTime.Now.DayOfWeek == DayOfWeek.Friday ||
             DateTime.Now.DayOfWeek == DayOfWeek.Saturday ||
             DateTime.Now.DayOfWeek == DayOfWeek.Sunday))
        {
            _isWeekend = true;
            WeekendMotivationRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool IsProcessRunning => _processMonitor?.IsRunning ?? false;

    private void NotifyProcessRunningState(bool isRunning)
    {
        if (_lastKnownProcessRunningState == isRunning)
        {
            return;
        }

        _lastKnownProcessRunningState = isRunning;
        ProcessRunningStateChanged?.Invoke(this, isRunning);
    }

    public void Cleanup()
    {
        _messageUpdateTimer?.Stop();
        _statsUpdateTimer?.Stop();
        _processMonitor?.Stop();
    }
}
