using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Diablo4.WinUI.Views;

public sealed partial class WeekendMotivationDialog : ContentDialog
{
    private readonly List<string> _games = new() 
    { 
        "Diablo IV", 
        "Diablo III64", 
        "Dragon Age The Veilguard", 
        "DragonAgeInquisition" 
    };

    public WeekendMotivationDialog()
    {
        this.InitializeComponent();
        GamesComboBox.ItemsSource = _games;
        GamesComboBox.SelectedIndex = 0;
    }

    private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Get deferral to allow async operations
        var deferral = args.GetDeferral();

        try
        {
            string selectedGame = GamesComboBox.SelectedItem as string ?? "Diablo IV";
            string executableName = GetExecutableName(selectedGame);
            string executablePath = await FindExecutablePathAsync(executableName);
            string processName = Path.GetFileNameWithoutExtension(executableName);

            if (!string.IsNullOrEmpty(executablePath) && !IsProcessRunning(processName))
            {
                Process.Start(new ProcessStartInfo(executablePath) { UseShellExecute = true });
            }
            else if (IsProcessRunning(processName))
            {
                var dialog = new ContentDialog
                {
                    Title = "Information",
                    Content = $"{selectedGame} is already running.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
                args.Cancel = true; // Don't close the dialog
            }
            else
            {
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = "Executable path not found for the selected game.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
                args.Cancel = true; // Don't close the dialog
            }
        }
        finally
        {
            deferral.Complete();
        }
    }

    private string GetExecutableName(string gameName)
    {
        return gameName switch
        {
            "Diablo IV" => "Diablo IV.exe",
            "Diablo III64" => "Diablo III64.exe",
            "Dragon Age The Veilguard" => "Dragon Age The Veilguard.exe",
            "DragonAgeInquisition" => "DragonAgeInquisition.exe",
            _ => string.Empty
        };
    }

    private async Task<string> FindExecutablePathAsync(string executableName)
    {
        return await Task.Run(() =>
        {
            if (string.IsNullOrEmpty(executableName))
                return string.Empty;

            Queue<string> directories = new Queue<string>();
            directories.Enqueue("C:\\");

            while (directories.Count > 0)
            {
                string currentDir = directories.Dequeue();
                try
                {
                    string[] files = Directory.GetFiles(currentDir, executableName);
                    if (files.Length > 0)
                    {
                        return files[0];
                    }

                    foreach (string dir in Directory.GetDirectories(currentDir))
                    {
                        directories.Enqueue(dir);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                }
                catch (DirectoryNotFoundException)
                {
                }
                catch (Exception)
                {
                }
            }

            return string.Empty;
        });
    }

    private bool IsProcessRunning(string processName)
    {
        return Process.GetProcessesByName(processName).Length > 0;
    }
}
