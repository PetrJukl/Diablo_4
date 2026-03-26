using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Diablo_4
{
    public partial class WeekendMotivation : Form
    {
        public WeekendMotivation()
        {
            InitializeComponent();
            gamesComboBox.Items.AddRange(new string[] { "Diablo IV", "Diablo III64", "Dragon Age The Veilguard", "DragonAgeInquisition" });
            gamesComboBox.SelectedIndex = 0;
        }

        private void WeekendMotivation_Load(object sender, EventArgs e)
        {

        }

        private async void YesBtn_Click(object sender, EventArgs e)
        {
            string selectedGame = gamesComboBox.SelectedItem.ToString();
            string executableName = GetExecutableName(selectedGame);
            string executablePath = await FindExecutablePathAsync(executableName);
            string processName = Path.GetFileNameWithoutExtension(executableName);

            if (!string.IsNullOrEmpty(executablePath) && !IsProcessRunning(processName))
            {
                Process.Start(new ProcessStartInfo(executablePath));
                this.Close();
            }
            else if (IsProcessRunning(processName))
            {
                MessageBox.Show($"{selectedGame} is already running.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Executable path not found for the selected game.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void NoBtn_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private string GetExecutableName(string gameName)
        {
            switch (gameName)
            {
                case "Diablo IV":
                    return "Diablo IV.exe";
                case "Diablo III64":
                    return "Diablo III64.exe";
                case "Dragon Age The Veilguard":
                    return "Dragon Age The Veilguard.exe";
                case "DragonAgeInquisition":
                    return "DragonAgeInquisition.exe";
                default:
                    return string.Empty;
            }
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
                        // Skip directories where access is denied
                    }
                    catch (DirectoryNotFoundException)
                    {
                        // Skip directories that are not found
                    }
                    catch (Exception)
                    {
                        // Skip directories that throw exceptions
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
}
