using Diablo4.WinUI.Helpers;
using Diablo4.WinUI.Models;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.UI;
using WinRT.Interop;

namespace Diablo4.WinUI.Views;

public sealed partial class WeekendMotivationDialog : Window
{
    private static readonly ConcurrentDictionary<string, string> ExecutablePathCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Lazy<IPathCacheStore> PersistentPathCache = new(() => JsonPathCacheStore.CreateDefault());
    private static readonly EnumerationOptions ExecutableEnumerationOptions = new()
    {
        RecurseSubdirectories = true,
        IgnoreInaccessible = true,
        MaxRecursionDepth = 4,
        AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.System,
    };

    private readonly TaskCompletionSource _tcs = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly object _ctsLifetimeLock = new();
    private bool _isClosed;
    private bool _isLaunchInProgress;
    private bool _ctsDisposed;
    private bool _ctsDisposalPending;

    private static readonly string[] SearchRoots =
    [
        @"C:\Program Files",
        @"C:\Program Files (x86)",
        @"C:\Games",
        @"C:\GOG Games",
    ];

    public WeekendMotivationDialog()
    {
        InitializeComponent();
        Title = "WeekendMotivation";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        GamesComboBox.ItemsSource = TrackedApplications.WeekendMotivationGames;
        GamesComboBox.SelectedIndex = 0;
        Closed += (_, _) =>
        {
            _isClosed = true;

            try
            {
                _cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }

            DisposeCancellationSourceIfSafe();
            _tcs.TrySetResult();
        };
    }

    /// <summary>Zobrazí okno a čeká na jeho zavření.</summary>
    public Task ShowAndWaitAsync()
    {
        ConfigureWindow();
        Activate();
        return _tcs.Task;
    }

    internal void RequestClose()
    {
        if (_isClosed)
        {
            return;
        }

        GamesComboBox.IsDropDownOpen = false;

        DispatcherQueue.TryEnqueue(() =>
        {
            if (_isClosed)
            {
                return;
            }

            try
            {
                Close();
            }
            catch (InvalidOperationException ex)
            {
                AppDiagnostics.LogWarning("Nepodařilo se korektně zavřít weekend dialog.", ex);
            }
        });
    }

    private void ConfigureWindow()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new SizeInt32(750, 750));

        var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest);
        var workArea = displayArea.WorkArea;
        appWindow.Move(new PointInt32(
            workArea.X + (workArea.Width - 750) / 2,
            workArea.Y + (workArea.Height - 750) / 2));

        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
        }

        // Průhledná caption tlačítka – splývají s obrázkem
        var titleBar = appWindow.TitleBar;
        titleBar.ButtonBackgroundColor = new Color { A = 0, R = 0, G = 0, B = 0 };
        titleBar.ButtonHoverBackgroundColor = new Color { A = 60, R = 255, G = 255, B = 255 };
        titleBar.ButtonPressedBackgroundColor = new Color { A = 100, R = 255, G = 255, B = 255 };
        titleBar.ButtonForegroundColor = new Color { A = 255, R = 255, G = 255, B = 255 };
        titleBar.ButtonHoverForegroundColor = new Color { A = 255, R = 255, G = 255, B = 255 };
        titleBar.ButtonPressedForegroundColor = new Color { A = 255, R = 255, G = 255, B = 255 };
    }

    private async void AnoButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isLaunchInProgress || _isClosed)
        {
            return;
        }

        _isLaunchInProgress = true;
        AnoButton.IsEnabled = false;
        GamesComboBox.IsEnabled = false;
        LaunchProgressRing.Visibility = Visibility.Visible;
        LaunchProgressRing.IsActive = true;

        TrackedApplicationDefinition selectedGame = GamesComboBox.SelectedItem as TrackedApplicationDefinition
            ?? TrackedApplications.WeekendMotivationGames[0];
        string executableName = selectedGame.ExecutableName ?? string.Empty;
        string processName = selectedGame.LaunchProcessName ?? Path.GetFileNameWithoutExtension(executableName);

        string executablePath;
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(15));
            executablePath = await FindExecutablePathAsync(executableName, timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            if (!_isClosed)
            {
                await ShowMessageAsync("Vyhledání hry bylo přerušeno", "Nepodařilo se včas najít spustitelný soubor vybrané hry.");
            }

            return;
        }
        finally
        {
            _isLaunchInProgress = false;
            LaunchProgressRing.IsActive = false;
            LaunchProgressRing.Visibility = Visibility.Collapsed;
            AnoButton.IsEnabled = true;
            GamesComboBox.IsEnabled = true;
            DisposeCancellationSourceIfSafe();
        }

        if (_isClosed)
        {
            return;
        }

        if (!string.IsNullOrEmpty(executablePath) && !IsProcessRunning(processName))
        {
            if (!ExecutableLaunchPolicy.IsTrustedExecutablePath(executablePath, SearchRoots))
            {
                AppDiagnostics.LogWarning($"Spuštění hry bylo zablokováno kvůli nedůvěryhodné cestě '{executablePath}'.");
                await ShowMessageAsync("Spuštění zablokováno", "Nalezený spustitelný soubor není v důvěryhodném umístění.");
                return;
            }

            if (!AuthenticodeVerifier.IsTrustedAuthenticodeSigned(executablePath))
            {
                AppDiagnostics.LogWarning($"Spustitelný soubor '{executablePath}' není digitálně podepsaný.");
                bool userApproved = await ConfirmUnsignedExecutableAsync(selectedGame.DisplayName, executablePath);
                if (!userApproved || _isClosed)
                {
                    return;
                }
            }

            try
            {
                var startedProcess = Process.Start(new ProcessStartInfo(executablePath) { UseShellExecute = true });
                if (startedProcess is null)
                {
                    throw new InvalidOperationException("Vybranou hru se nepodařilo spustit.");
                }

                Close();
            }
            catch (Win32Exception ex)
            {
                AppDiagnostics.LogError($"Spuštění hry '{selectedGame.DisplayName}' selhalo.", ex);
                await ShowMessageAsync("Spuštění selhalo", "Vybranou hru se nepodařilo spustit.");
            }
            catch (InvalidOperationException ex)
            {
                AppDiagnostics.LogError($"Spuštění hry '{selectedGame.DisplayName}' skončilo v neplatném stavu.", ex);
                await ShowMessageAsync("Spuštění selhalo", "Vybranou hru se nepodařilo spustit.");
            }
        }
        else if (IsProcessRunning(processName))
        {
            Close();
        }
        else
        {
            var dialog = new ContentDialog
            {
                Title = "Chyba",
                Content = "Spustitelný soubor pro vybranou hru nebyl nalezen.",
                CloseButtonText = "OK",
                XamlRoot = Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    private void NeButton_Click(object sender, RoutedEventArgs e)
    {
        RequestClose();
    }

    private static async Task<string> FindExecutablePathAsync(string executableName, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(executableName))
        {
            return string.Empty;
        }

        if (ExecutablePathCache.TryGetValue(executableName, out var cachedPath) && File.Exists(cachedPath))
        {
            return cachedPath;
        }

        var persistent = PersistentPathCache.Value;
        if (persistent.TryGet(executableName, out var persistedPath) && File.Exists(persistedPath))
        {
            ExecutablePathCache[executableName] = persistedPath;
            return persistedPath;
        }

        var result = await Task.Run(() =>
        {
            foreach (var root in SearchRoots)
            {
                if (!Directory.Exists(root)) continue;

                ct.ThrowIfCancellationRequested();
                var match = SearchRoot(root, executableName, ct);
                if (!string.IsNullOrEmpty(match))
                {
                    return match;
                }
            }

            return string.Empty;
        }, ct);

        if (!string.IsNullOrWhiteSpace(result))
        {
            ExecutablePathCache[executableName] = result;
            persistent.Set(executableName, result);
        }

        return result;
    }

    private static string SearchRoot(string root, string executableName, CancellationToken ct)
    {
        try
        {
            foreach (var path in Directory.EnumerateFiles(root, executableName, ExecutableEnumerationOptions))
            {
                ct.ThrowIfCancellationRequested();
                return path;
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (UnauthorizedAccessException) { }
        catch (DirectoryNotFoundException) { }
        catch (PathTooLongException) { }
        catch (IOException) { }

        return string.Empty;
    }

    private static bool IsProcessRunning(string processName)
    {
        var processes = Process.GetProcessesByName(processName);
        try
        {
            return processes.Length > 0;
        }
        finally
        {
            foreach (var p in processes) p.Dispose();
        }
    }

    private void DisposeCancellationSourceIfSafe()
    {
        lock (_ctsLifetimeLock)
        {
            if (_ctsDisposed)
            {
                return;
            }

            if (_isLaunchInProgress)
            {
                _ctsDisposalPending = true;
                return;
            }

            if (!_isClosed && !_ctsDisposalPending)
            {
                return;
            }

            try
            {
                _cts.Dispose();
                _ctsDisposed = true;
            }
            catch (ObjectDisposedException)
            {
                _ctsDisposed = true;
            }
        }
    }

    private async Task<bool> ConfirmUnsignedExecutableAsync(string displayName, string executablePath)
    {
        if (_isClosed)
        {
            return false;
        }

        var dialog = new ContentDialog
        {
            Title = "Hra není digitálně podepsaná",
            Content = $"Spustitelný soubor hry '{displayName}' nemá platný Authenticode podpis.\n\nCesta: {executablePath}\n\nPřesto spustit?",
            PrimaryButtonText = "Spustit",
            CloseButtonText = "Zrušit",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = Content.XamlRoot
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    private async Task ShowMessageAsync(string title, string content)
    {
        if (_isClosed)
        {
            return;
        }

        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            CloseButtonText = "OK",
            XamlRoot = Content.XamlRoot
        };

        await dialog.ShowAsync();
    }

}

