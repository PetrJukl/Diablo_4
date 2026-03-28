using Diablo4.WinUI.Helpers;
using Diablo4.WinUI.Models;
using Diablo4.WinUI.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Diablo4.WinUI;

public partial class App : Application
{
    private static readonly string StartupErrorLogPath = Path.Combine(Path.GetTempPath(), "Diablo4.WinUI.startup.log");

    public static MainWindow? MainWindow { get; private set; }

    public App()
    {
        UnhandledException += App_UnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        InitializeComponent();
    }

    private static void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        AppDiagnostics.LogError("Neošetřená UI výjimka.", e.Exception);
    }

    private static void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            AppDiagnostics.LogError("Neošetřená AppDomain výjimka.", ex);
            LogStartupException("AppDomain.UnhandledException", ex);
        }
    }

    private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        AppDiagnostics.LogError("Neošetřená TaskScheduler výjimka.", e.Exception);
        LogStartupException("TaskScheduler.UnobservedTaskException", e.Exception);
        e.SetObserved();
    }

    private static void LogStartupException(string source, Exception ex)
    {
        try
        {
            File.AppendAllText(
                StartupErrorLogPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {source}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (IOException ioEx)
        {
            Debug.WriteLine(ioEx);
        }
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            MainWindow = new MainWindow();
            MainWindow.Activate();
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogError("Inicializace hlavního okna selhala.", ex);
            LogStartupException("App.OnLaunched", ex);
            throw;
        }

        await TryRunUpdateFlowAsync();
    }

    private static async Task TryRunUpdateFlowAsync()
    {
        if (MainWindow is null)
        {
            return;
        }

        var updateService = new UpdateService(AppConfiguration.GitHubUpdateManifestUrl);
        UpdateCheckResult updateResult;

        try
        {
            updateResult = await updateService.CheckForUpdatesAsync();
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogError("Kontrola aktualizací selhala.", ex);
            return;
        }

        if (!string.IsNullOrWhiteSpace(updateResult.ErrorMessage))
        {
            AppDiagnostics.LogWarning($"Kontrola aktualizací skončila chybou: {updateResult.ErrorMessage}");
        }

        if (!updateResult.IsUpdateAvailable || updateResult.Manifest is null || MainWindow.DialogRoot.XamlRoot is null)
        {
            return;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = MainWindow.DialogRoot.XamlRoot,
            Title = $"Dostupná aktualizace {updateResult.LatestVersion}",
            Content = BuildUpdateDialogContent(updateResult),
            PrimaryButtonText = "Instalovat",
            CloseButtonText = "Později"
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        try
        {
            await updateService.DownloadAndInstallAsync(updateResult.Manifest);
            MainWindow.Close();
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogError("Stažení nebo instalace aktualizace selhalo.", ex);
            await ShowUpdateFailureDialogAsync(MainWindow.DialogRoot.XamlRoot);
        }
    }

    private static async Task ShowUpdateFailureDialogAsync(Microsoft.UI.Xaml.XamlRoot xamlRoot)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = xamlRoot,
            Title = "Aktualizaci se nepodařilo nainstalovat",
            Content = "Kontrola nebo instalace aktualizace selhala. Aplikace bude pokračovat bez aktualizace.",
            CloseButtonText = "OK"
        };

        await dialog.ShowAsync();
    }

    private static UIElement BuildUpdateDialogContent(UpdateCheckResult updateResult)
    {
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock
        {
            Text = $"Aktuální verze: {updateResult.CurrentVersion}",
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"Nová verze: {updateResult.LatestVersion}",
            TextWrapping = TextWrapping.Wrap
        });

        if (!string.IsNullOrWhiteSpace(updateResult.Manifest?.ReleaseNotes))
        {
            panel.Children.Add(new TextBlock
            {
                Text = updateResult.Manifest.ReleaseNotes,
                TextWrapping = TextWrapping.Wrap
            });
        }

        return panel;
    }
}
