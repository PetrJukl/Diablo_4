using Diablo4.WinUI.Helpers;
using Diablo4.WinUI.Models;
using Diablo4.WinUI.Services;
using Diablo4.WinUI.Views;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Diablo4.WinUI;

public partial class App : Application
{
    private static readonly string StartupErrorLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Diablo Log",
        "Diablo4.WinUI.startup.log");
    private static Mutex? _singleInstanceMutex;

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
        LogStartupException("Application.UnhandledException", e.Exception);
        e.Handled = true;
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
            Directory.CreateDirectory(Path.GetDirectoryName(StartupErrorLogPath)!);
            File.AppendAllText(
                StartupErrorLogPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {source}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (IOException ioEx)
        {
            Debug.WriteLine(ioEx);
        }
        catch (UnauthorizedAccessException uae)
        {
            Debug.WriteLine(uae);
        }
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        _singleInstanceMutex = new Mutex(true, "Local\\Diablo4.WinUI.SingleInstance", out var isNewInstance);
        if (!isNewInstance)
        {
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
            Environment.Exit(0);
            return;
        }

        try
        {
            MainWindow = new MainWindow();
            MainWindow.Closed += (_, _) =>
            {
                try { _singleInstanceMutex?.ReleaseMutex(); } catch (ApplicationException) { }
                _singleInstanceMutex?.Dispose();
                _singleInstanceMutex = null;
            };
            MainWindow.Activate();
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogError("Inicializace hlavního okna selhala.", ex);
            LogStartupException("App.OnLaunched", ex);
            throw;
        }

        try
        {
            await TryRunUpdateFlowAsync();
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogError("Update flow selhal během spuštění aplikace.", ex);
            LogStartupException("App.TryRunUpdateFlowAsync", ex);
        }
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

        if (!updateResult.IsUpdateAvailable || updateResult.Manifest is null)
        {
            return;
        }

        bool shouldClose;

        try
        {
            var updateWindow = new UpdateNotificationWindow(updateResult, updateService);
            shouldClose = await updateWindow.ShowAndWaitAsync();
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogError("Zobrazení update okna selhalo.", ex);
            return;
        }

        if (shouldClose)
        {
            MainWindow.Close();
        }
    }
}
