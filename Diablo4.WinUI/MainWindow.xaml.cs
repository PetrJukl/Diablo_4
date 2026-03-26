using CommunityToolkit.Mvvm.Input;
using Diablo4.WinUI.Helpers;
using Diablo4.WinUI.ViewModels;
using Diablo4.WinUI.Views;
using H.NotifyIcon;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;
using WinRT.Interop;

namespace Diablo4.WinUI;

public sealed partial class MainWindow : Window
{
    private AppWindow? _appWindow;
    private bool _isInitialized;
    private bool _isExitRequested;

    private TaskbarIcon TrayIcon => (TaskbarIcon)RootGrid.Resources["TrayIcon"];

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
        ConfigureWindow();
    }

    private void ConfigureWindow()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);
        _appWindow.Resize(new SizeInt32(1245, 575));
        _appWindow.Closing += AppWindow_Closing;

        if (_appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
        }

        TrayIcon.ForceCreate();
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        ProcessHelper.CloseOtherInstances("Diablo4.WinUI");
        ViewModel.Initialize(DispatcherQueue);
    }

    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_isExitRequested)
        {
            return;
        }

        args.Cancel = true;
        WindowExtensions.Hide(this);
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
        TrayIcon.Dispose();
        Close();
    }

    private void RestoreWindow()
    {
        WindowExtensions.Show(this);
        Activate();
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        ViewModel.Cleanup();
        ViewModel.WeekendMotivationRequested -= ViewModel_WeekendMotivationRequested;
        _appWindow?.Closing -= AppWindow_Closing;
    }
}


