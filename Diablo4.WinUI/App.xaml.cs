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
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        InitializeComponent();
    }

    private static void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LogStartupException("AppDomain.UnhandledException", ex);
        }
    }

    private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogStartupException("TaskScheduler.UnobservedTaskException", e.Exception);
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
            LogStartupException("App.OnLaunched", ex);
            throw;
        }

        var updateService = new UpdateService(AppConfiguration.GitHubUpdateManifestUrl);
        var updateResult = await updateService.CheckForUpdatesAsync();
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

        await updateService.DownloadAndInstallAsync(updateResult.Manifest);
        MainWindow.Close();
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
