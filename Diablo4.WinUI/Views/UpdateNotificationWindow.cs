using Diablo4.WinUI.Helpers;
using Diablo4.WinUI.Models;
using Diablo4.WinUI.Services;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.UI;
using WinRT.Interop;

namespace Diablo4.WinUI.Views;

/// <summary>Topmost okno pro notifikaci o dostupné aktualizaci. Spravuje celý flow: zobrazení info, stahování, chyba.</summary>
internal sealed class UpdateNotificationWindow : Window
{
    private static readonly IntPtr HwndTopmost = new(-1);
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoActivate = 0x0010;

    private readonly UpdateCheckResult _updateResult;
    private readonly UpdateService _updateService;
    private readonly TaskCompletionSource<bool> _tcs = new();
    private Button? _installButton;
    private Button? _laterButton;
    private ProgressRing? _progressRing;
    private TextBlock? _errorText;
    private bool _installSucceeded;
    private bool _zOrderReassertQueued;

    internal UpdateNotificationWindow(UpdateCheckResult updateResult, UpdateService updateService)
    {
        _updateResult = updateResult;
        _updateService = updateService;
        Title = "Kontrola pařby";
        Content = BuildContent();
        Closed += (_, _) => _tcs.TrySetResult(_installSucceeded);
    }

    /// <summary>Zobrazí topmost okno a vrátí true, pokud instalace proběhla úspěšně (aplikace se má zavřít).</summary>
    internal Task<bool> ShowAndWaitAsync()
    {
        ConfigureWindow();
        Activate();
        return _tcs.Task;
    }

    private void ConfigureWindow()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "211668_controller_b_game_icon.ico"));
        appWindow.Resize(new SizeInt32(480, 320));

        var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest);
        var workArea = displayArea.WorkArea;
        appWindow.Move(new PointInt32(
            workArea.X + (workArea.Width - 480) / 2,
            workArea.Y + (workArea.Height - 320) / 2));

        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
        }

        SetWindowPos(hwnd, HwndTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize);

        appWindow.Changed += (_, args) =>
        {
            if (!args.DidZOrderChange || _zOrderReassertQueued)
                return;

            _zOrderReassertQueued = true;
            DispatcherQueue.TryEnqueue(() =>
            {
                _zOrderReassertQueued = false;
                SetWindowPos(hwnd, HwndTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate);
            });
        };
    }

    private UIElement BuildContent()
    {
        var root = new Grid { Margin = new Thickness(24) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new TextBlock
        {
            Text = $"Dostupná aktualizace {_updateResult.LatestVersion}",
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 12)
        };
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        var infoPanel = new StackPanel { Spacing = 4 };
        infoPanel.Children.Add(new TextBlock { Text = $"Aktuální verze: {_updateResult.CurrentVersion}" });
        infoPanel.Children.Add(new TextBlock { Text = $"Nová verze: {_updateResult.LatestVersion}" });

        if (!string.IsNullOrWhiteSpace(_updateResult.Manifest?.ReleaseNotes))
        {
            infoPanel.Children.Add(new TextBlock
            {
                Text = _updateResult.Manifest.ReleaseNotes,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0)
            });
        }

        _errorText = new TextBlock
        {
            Text = "Instalaci se nepodařilo dokončit. Zkuste to znovu.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 100, 100)),
            Visibility = Visibility.Collapsed,
            Margin = new Thickness(0, 8, 0, 0)
        };
        infoPanel.Children.Add(_errorText);

        var scrollViewer = new ScrollViewer { Content = infoPanel };
        Grid.SetRow(scrollViewer, 1);
        root.Children.Add(scrollViewer);

        _progressRing = new ProgressRing
        {
            Width = 20,
            Height = 20,
            IsActive = false,
            Visibility = Visibility.Collapsed,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };

        _installButton = new Button { Content = "Instalovat" };
        if (Application.Current.Resources.TryGetValue("AccentButtonStyle", out var accentStyle) && accentStyle is Style style)
        {
            _installButton.Style = style;
        }

        _laterButton = new Button { Content = "Později", Margin = new Thickness(8, 0, 0, 0) };

        _installButton.Click += InstallButton_Click;
        _laterButton.Click += (_, _) => Close();

        var buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0)
        };
        buttonRow.Children.Add(_progressRing);
        buttonRow.Children.Add(_installButton);
        buttonRow.Children.Add(_laterButton);

        Grid.SetRow(buttonRow, 2);
        root.Children.Add(buttonRow);

        return root;
    }

    private async void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        _installButton!.IsEnabled = false;
        _laterButton!.IsEnabled = false;
        _progressRing!.Visibility = Visibility.Visible;
        _progressRing.IsActive = true;
        _errorText!.Visibility = Visibility.Collapsed;

        try
        {
            await _updateService.DownloadAndInstallAsync(_updateResult.Manifest!);
            _installSucceeded = true;
            Close();
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogError("Stažení nebo instalace aktualizace selhalo.", ex);
            _progressRing.IsActive = false;
            _progressRing.Visibility = Visibility.Collapsed;
            _installButton.IsEnabled = true;
            _laterButton.IsEnabled = true;
            _errorText.Visibility = Visibility.Visible;
        }
    }

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
}
