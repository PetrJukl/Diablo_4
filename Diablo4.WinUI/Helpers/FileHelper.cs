using System;
using System.IO;

namespace Diablo4.WinUI.Helpers;

public static class FileHelper
{
    public static string EnsureFileExists(string fileName)
    {
        string appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appDirectory = Path.Combine(appDataDirectory, "Diablo Log");
        Directory.CreateDirectory(appDirectory);
        string filePath = Path.Combine(appDirectory, fileName);

        if (!File.Exists(filePath))
        {
            File.Create(filePath).Close();
        }

        if (new FileInfo(filePath).Length == 0)
        {
            File.WriteAllText(filePath, DateTime.Now.ToString() + Environment.NewLine);
        }

        return filePath;
    }
}
