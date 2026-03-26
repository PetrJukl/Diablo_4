using Diablo4.WinUI.Helpers;
using Diablo4.WinUI.Models;
using Diablo4.WinUI.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Diablo4.WinUI;

public partial class App : Application
{
    public static MainWindow? MainWindow { get; private set; }

    public App()
    {
        InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();

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
