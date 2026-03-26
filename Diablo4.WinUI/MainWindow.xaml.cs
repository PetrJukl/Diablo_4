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
    private bool _isInitialized;
    private bool _isExitRequested;
    private bool _isTrayAvailable;
    private bool _windowConfigured;

    public MainViewModel ViewModel { get; }
    public IRelayCommand RestoreWindowCommand { get; }
    public FrameworkElement DialogRoot => RootGrid;

    public MainWindow()
    {
        InitializeComponent();

        Title = "Kontrola pařby";
        RestoreWindowCommand = new RelayCommand(RestoreWindow);
        ViewModel = new MainViewModel();
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

        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);
        _appWindow.Resize(new SizeInt32(1245, 575));

        if (_appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
        }

        _appWindow.Closing += AppWindow_Closing;

        _windowConfigured = true;
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        ConfigureWindow();
        ProcessHelper.CloseOtherInstances("Diablo4.WinUI");
        ViewModel.ProcessRunningStateChanged += ViewModel_ProcessRunningStateChanged;
        _isTrayAvailable = TryInitializeTray();
        ViewModel.Initialize(DispatcherQueue);

        if (ViewModel.IsProcessRunning)
        {
            HideToTrayOrMinimize();
        }
    }

    private void ViewModel_ProcessRunningStateChanged(object? sender, bool isRunning)
    {
        if (!isRunning)
        {
            return;
        }

        HideToTrayOrMinimize();
    }

    private async void ViewModel_WeekendMotivationRequested(object? sender, EventArgs e)
    {
        var dialog = new WeekendMotivationDialog
        {
            XamlRoot = RootGrid.XamlRoot
        };

        await dialog.ShowAsync();
    }

    private void RestoreMenuItem_Click(object sender, RoutedEventArgs e)
    {
        RestoreWindow();
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _isExitRequested = true;
        Close();
    }

    private void RestoreWindow()
    {
        if (_isTrayAvailable)
        {
            WindowExtensions.Show(this);
        }

        Activate();
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
        if (_isTrayAvailable)
        {
            WindowExtensions.Hide(this);
            return;
        }

        MinimizeWindow();
    }

    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_isExitRequested || !_isTrayAvailable)
        {
            return;
        }

        args.Cancel = true;
        WindowExtensions.Hide(this);
    }

    private bool TryInitializeTray()
    {
        if (_trayIcon != null)
        {
            return true;
        }

        try
        {
            var trayMenu = RootGrid.Resources["TrayMenu"] as MenuFlyout;
            if (trayMenu == null)
            {
                return false;
            }

            _trayIcon = new TaskbarIcon
            {
                ToolTipText = "Kontrola pařby",
                ContextFlyout = trayMenu,
                LeftClickCommand = RestoreWindowCommand,
                NoLeftClickDelay = true,
                IconSource = CreateTrayIconSource()
            };

            _trayIcon.ForceCreate();
            return true;
        }
        catch (COMException ex)
        {
            Debug.WriteLine(ex);
            _trayIcon = null;
            return false;
        }
    }

    private static ImageSource CreateTrayIconSource()
    {
        return new BitmapImage(new Uri("ms-appx:///Assets/211668_controller_b_game_icon.ico"));
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
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

