using CommunityToolkit.Mvvm.Input;
using Diablo4.WinUI.Helpers;
using Diablo4.WinUI.ViewModels;
using Diablo4.WinUI.Views;
using H.NotifyIcon;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Graphics;
using WinRT.Interop;

namespace Diablo4.WinUI;

public sealed partial class MainWindow : Window
{
    private AppWindow? _appWindow;
    private TaskbarIcon? _trayIcon;
    private WeekendMotivationDialog? _weekendMotivationWindow;
    private bool _isInitialized;
    private bool _isClosed;
    private bool _isExitRequested;
    private bool _isWeekendMotivationOpen;
    private bool _isTrayAvailable;
    private bool _windowConfigured;

    public MainViewModel ViewModel { get; }
    private IRelayCommand ExitApplicationCommand { get; }
    public IRelayCommand RestoreWindowCommand { get; }
    public FrameworkElement DialogRoot => RootGrid;

    public MainWindow()
    {
        ViewModel = new MainViewModel();
        InitializeComponent();
        Bindings.Update();

        Title = "Kontrola pařby";
        ExitApplicationCommand = new RelayCommand(ExitApplication);
        RestoreWindowCommand = new RelayCommand(RestoreWindow);
        ViewModel.WeekendMotivationRequested += ViewModel_WeekendMotivationRequested;

        Activated += MainWindow_Activated;
        Closed += MainWindow_Closed;
    }

    private void ConfigureWindow()
    {
        if (_windowConfigured)
        {
            return;
        }

        try
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);

            try
            {
                _appWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "211668_controller_b_game_icon.ico"));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                AppDiagnostics.LogWarning("Nepodařilo se nastavit ikonu hlavního okna.", ex);
            }

            _appWindow.Resize(new SizeInt32(1245, 575));

            try
            {
                var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest);
                var workArea = displayArea.WorkArea;
                _appWindow.Move(new PointInt32(
                    workArea.X + (workArea.Width - 1245) / 2,
                    workArea.Y + (workArea.Height - 575) / 2));
            }
            catch (COMException ex)
            {
                AppDiagnostics.LogWarning("Nepodařilo se vycentrovat hlavní okno na obrazovku.", ex);
            }

            if (_appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
                presenter.IsMinimizable = false;
            }

            _appWindow.Closing += AppWindow_Closing;

            _windowConfigured = true;
        }
        catch (COMException ex)
        {
            AppDiagnostics.LogError("Konfigurace hlavního okna selhala. Aplikace pokračuje s výchozím rozložením.", ex);
        }
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (_isClosed)
        {
            return;
        }

        if (_isInitialized)
        {
            if (ViewModel.IsProcessRunning)
            {
                DispatcherQueue.TryEnqueue(HideToTrayOrMinimize);
            }

            return;
        }

        try
        {
            ConfigureWindow();
            ViewModel.ProcessRunningStateChanged += ViewModel_ProcessRunningStateChanged;
            _isTrayAvailable = TryInitializeTray();
            ViewModel.Initialize(DispatcherQueue);
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            ViewModel.ProcessRunningStateChanged -= ViewModel_ProcessRunningStateChanged;
            AppDiagnostics.LogError("Inicializace hlavního okna selhala během aktivace.", ex);
            return;
        }

        if (ViewModel.IsProcessRunning)
        {
            HideToTrayOrMinimize();
        }
    }

    private void ViewModel_ProcessRunningStateChanged(object? sender, bool isRunning)
    {
        if (!isRunning || _isClosed)
        {
            return;
        }

        CloseWeekendMotivationWindow();
        HideToTrayOrMinimize();
    }

    private async void ViewModel_WeekendMotivationRequested(object? sender, EventArgs e)
    {
        if (_isWeekendMotivationOpen || _isClosed)
        {
            return;
        }

        _isWeekendMotivationOpen = true;

        var window = new WeekendMotivationDialog();
        _weekendMotivationWindow = window;

        void closeWeekendWindow(object s, WindowEventArgs a)
        {
            CloseWeekendMotivationWindow();
        }

        Closed += closeWeekendWindow;

        try
        {
            await window.ShowAndWaitAsync();
        }
        catch (InvalidOperationException ex)
        {
            AppDiagnostics.LogError("Weekend dialog skončil v neplatném stavu.", ex);
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogError("Weekend dialog selhal.", ex);
        }
        finally
        {
            Closed -= closeWeekendWindow;
            if (ReferenceEquals(_weekendMotivationWindow, window))
            {
                _weekendMotivationWindow = null;
            }

            _isWeekendMotivationOpen = false;

            if (!_isClosed && ViewModel.IsProcessRunning)
            {
                DispatcherQueue.TryEnqueue(HideToTrayOrMinimize);
            }
        }
    }

    private void CloseWeekendMotivationWindow()
    {
        var weekendMotivationWindow = _weekendMotivationWindow;
        _weekendMotivationWindow = null;

        if (weekendMotivationWindow is null)
        {
            return;
        }

        weekendMotivationWindow.RequestClose();
    }

    private void ExitApplication()
    {
        if (_isClosed)
        {
            return;
        }

        _isExitRequested = true;
        Close();
    }

    private void RestoreWindow()
    {
        if (_isClosed)
        {
            return;
        }

        try
        {
            if (_isTrayAvailable)
            {
                WindowExtensions.Show(this);
            }

            Activate();
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogError("Obnovení hlavního okna selhalo.", ex);
            return;
        }

        if (ViewModel.IsProcessRunning)
        {
            DispatcherQueue.TryEnqueue(HideToTrayOrMinimize);
        }
    }

    private void MinimizeWindow()
    {
        if (_appWindow?.Presenter is OverlappedPresenter presenter)
        {
            presenter.Minimize();
        }
    }

    private void HideToTrayOrMinimize()
    {
        if (_isWeekendMotivationOpen || _isClosed)
        {
            return;
        }

        if (_isTrayAvailable)
        {
            WindowExtensions.Hide(this);
            return;
        }

        MinimizeWindow();
    }

    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_isClosed || _isExitRequested || !_isTrayAvailable)
        {
            return;
        }

        try
        {
            args.Cancel = true;
            WindowExtensions.Hide(this);
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogError("Skrytí hlavního okna do tray režimu selhalo.", ex);
        }
    }

    private bool TryInitializeTray()
    {
        if (_trayIcon != null)
        {
            return true;
        }

        try
        {
            _trayIcon = new TaskbarIcon
            {
                ToolTipText = "Kontrola pařby",
                ContextFlyout = CreateTrayMenu(),
                LeftClickCommand = RestoreWindowCommand,
                NoLeftClickDelay = true,
                IconSource = CreateTrayIconSource()
            };

            _trayIcon.ForceCreate();
            return true;
        }
        catch (COMException ex)
        {
            AppDiagnostics.LogWarning("Inicializace tray ikony selhala, aplikace poběží bez tray režimu.", ex);
            _trayIcon = null;
            return false;
        }
    }

    private MenuFlyout CreateTrayMenu()
    {
        var restoreItem = new MenuFlyoutItem
        {
            Text = "Obnovit",
            Command = RestoreWindowCommand
        };

        var exitItem = new MenuFlyoutItem
        {
            Text = "Ukončit",
            Command = ExitApplicationCommand
        };

        var menu = new MenuFlyout();
        menu.Items.Add(restoreItem);
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(exitItem);

        return menu;
    }

    private static ImageSource CreateTrayIconSource()
    {
        return new BitmapImage(new Uri("ms-appx:///Assets/211668_controller_b_game_icon.ico"));
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        _isClosed = true;
        ViewModel.Cleanup();
        ViewModel.WeekendMotivationRequested -= ViewModel_WeekendMotivationRequested;
        ViewModel.ProcessRunningStateChanged -= ViewModel_ProcessRunningStateChanged;
        if (_appWindow != null)
        {
            _appWindow.Closing -= AppWindow_Closing;
        }

        _trayIcon?.Dispose();
    }
}

