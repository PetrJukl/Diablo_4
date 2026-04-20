using CommunityToolkit.Mvvm.ComponentModel;
using Diablo4.WinUI.Helpers;
using Diablo4.WinUI.Models;
using Diablo4.WinUI.Services;
using Microsoft.UI.Dispatching;
using System;
using System.IO;

namespace Diablo4.WinUI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly string _filePath;
    private ProcessMonitor? _processMonitor;
    private DispatcherQueueTimer? _messageUpdateTimer;
    private DispatcherQueueTimer? _statsUpdateTimer;
    private bool _lastKnownProcessRunningState;
    private bool _isUpdatingStats;
    private int _consecutiveStatsErrors;
    private bool _statsTimerStoppedDueToErrors;
    private const int MaxConsecutiveStatsErrors = 10;

    [ObservableProperty]
    public partial string MessageText { get; set; } = "Text";

    [ObservableProperty]
    public partial string WeekDurationText { get; set; } = "Doba pařby";

    [ObservableProperty]
    public partial string LastWeekDurationText { get; set; } = "Doba pařby minulý týden";

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
        if (_processMonitor is not null)
        {
            return;
        }

        LoadCachedLastPlayedDateTime();

        _messageUpdateTimer = dispatcherQueue.CreateTimer();
        _messageUpdateTimer.Interval = TimeSpan.FromMilliseconds(50);
        _messageUpdateTimer.Tick += UpdateMessageTimerTick;
        _messageUpdateTimer.Start();

        bool shouldCheckWeb = MachineContextHelper.ShouldCheckWebContent();
        string[] trackedProcessNames = [.. TrackedApplications.AllProcessNames];
        _processMonitor = new ProcessMonitor(
            _filePath,
            shouldCheckWeb,
            trackedProcessNames
        );
        _processMonitor.Start(dispatcherQueue);

        _statsUpdateTimer = dispatcherQueue.CreateTimer();
        _statsUpdateTimer.Interval = TimeSpan.FromSeconds(1);
        _statsUpdateTimer.Tick += UpdateStatsTimerTick;
        _statsUpdateTimer.Start();
    }

    private void UpdateMessageTimerTick(DispatcherQueueTimer sender, object args)
    {
        if (_cachedLastPlayedDateTime == null) return;
        TimeSpan timeSinceLastWrite = DateTime.Now - _cachedLastPlayedDateTime.Value;
        MessageText = $"Už jsi nepařila: {FormatDetailedDuration(timeSinceLastWrite)} :).";
    }

    private void UpdateStatsTimerTick(DispatcherQueueTimer sender, object args)
    {
        if (_processMonitor == null || _isUpdatingStats)
        {
            return;
        }

        _isUpdatingStats = true;

        try
        {
            LoadCachedLastPlayedDateTime();

            int currentWeekOfYear = ProcessMonitor.GetIso8601WeekOfYear(DateTime.Now);
            var durations = _processMonitor.GetDurations(_filePath, currentWeekOfYear);

            if (durations.ThisWeek != TimeSpan.Zero)
            {
                _totalDuration = durations.ThisWeek;
                string formattedDuration = FormatSummaryDuration(_totalDuration);
                WeekDurationText = $"Tento týden \n {formattedDuration}";
            }
            else
            {
                WeekDurationText = "Chtělo by to roztočit grafárnu.";
            }

            if (durations.LastWeek != TimeSpan.Zero)
            {
                TimeSpan lastWeekTotalDuration = durations.LastWeek;
                string formattedDuration = FormatSummaryDuration(lastWeekTotalDuration);
                LastWeekDurationText = $"Minulý týden \n {formattedDuration}";
            }
            else
            {
                LastWeekDurationText = "Minulý týden se nezadařilo.";
            }

            // Check weekend motivation
            CheckWeekendMotivation();
            NotifyProcessRunningState(_processMonitor.IsRunning);
            _consecutiveStatsErrors = 0;
        }
        catch (IOException ex)
        {
            HandleStatsError("Nepodařilo se načíst statistiky hraní.", ex, isWarning: true);
        }
        catch (InvalidOperationException ex)
        {
            HandleStatsError("Aktualizace statistik skončila v neplatném stavu.", ex, isWarning: false);
        }
        catch (Exception ex)
        {
            HandleStatsError("Neočekávaná chyba při aktualizaci statistik.", ex, isWarning: false);
        }
        finally
        {
            _isUpdatingStats = false;
        }
    }

    private void HandleStatsError(string message, Exception exception, bool isWarning)
    {
        if (isWarning)
        {
            AppDiagnostics.LogWarning(message, exception);
        }
        else
        {
            AppDiagnostics.LogError(message, exception);
        }

        _consecutiveStatsErrors++;
        if (_consecutiveStatsErrors >= MaxConsecutiveStatsErrors && !_statsTimerStoppedDueToErrors)
        {
            _statsTimerStoppedDueToErrors = true;
            _statsUpdateTimer?.Stop();
            AppDiagnostics.LogError($"Aktualizace statistik byla zastavena po {MaxConsecutiveStatsErrors} po sobě jdoucích chybách.", exception);
        }
    }

    private void LoadCachedLastPlayedDateTime()
    {
        if (!File.Exists(_filePath))
        {
            return;
        }

        try
        {
            using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            string? firstLine = reader.ReadLine();

            if (FileHelper.TryParseLastPlayedTimestamp(firstLine, out DateTime dt))
            {
                _cachedLastPlayedDateTime = dt;
            }
        }
        catch (IOException ex)
        {
            AppDiagnostics.LogWarning("Nepodařilo se načíst poslední čas hraní.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            AppDiagnostics.LogWarning("Přístup k log souboru byl odmítnut.", ex);
        }
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

    private static string FormatDetailedDuration(TimeSpan duration)
    {
        return $"{FormatTimeUnit(duration.Days, "den", "dny", "dní")}, {FormatTimeUnit(duration.Hours, "hodina", "hodiny", "hodin")},\n  {FormatTimeUnit(duration.Minutes, "minuta", "minuty", "minut")}, {FormatTimeUnit(duration.Seconds, "vteřina", "vteřiny", "vteřin")} a {duration.Milliseconds} miliseků";
    }

    private static string FormatSummaryDuration(TimeSpan duration)
    {
        int totalHours = (int)Math.Floor(duration.TotalHours);
        return $"{FormatTimeUnit(totalHours, "hodina", "hodiny", "hodin")}, {FormatTimeUnit(duration.Minutes, "minuta", "minuty", "minut")} a {FormatTimeUnit(duration.Seconds, "vteřina", "vteřiny", "vteřin")}";
    }

    private static string FormatTimeUnit(int value, string singular, string few, string many)
    {
        return $"{value} {GetCzechPluralForm(value, singular, few, many)}";
    }

    private static string GetCzechPluralForm(int value, string singular, string few, string many)
    {
        int absoluteValue = Math.Abs(value);
        int lastTwoDigits = absoluteValue % 100;

        if (lastTwoDigits is >= 11 and <= 14)
        {
            return many;
        }

        return (absoluteValue % 10) switch
        {
            1 => singular,
            2 or 3 or 4 => few,
            _ => many
        };
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
        if (_messageUpdateTimer is not null)
        {
            _messageUpdateTimer.Tick -= UpdateMessageTimerTick;
        }

        if (_statsUpdateTimer is not null)
        {
            _statsUpdateTimer.Tick -= UpdateStatsTimerTick;
        }

        _processMonitor?.Stop();
        _processMonitor = null;
    }
}
