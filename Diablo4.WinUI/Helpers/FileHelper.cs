using System;
using System.Globalization;
using System.IO;

namespace Diablo4.WinUI.Helpers;

public static class FileHelper
{

    /// <summary>
    /// Převede čas posledního hraní do původního formátu uloženého v log souboru.
    /// </summary>
    public static string FormatLastPlayedTimestamp(DateTime timestamp)
    {
        return timestamp.ToString();
    }

    /// <summary>
    /// Pokusí se načíst čas posledního hraní ze současného i starších formátů logu.
    /// </summary>
    public static bool TryParseLastPlayedTimestamp(string? value, out DateTime timestamp)
    {
        return DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out timestamp)
            || DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out timestamp);
    }

    /// <summary>
    /// Pokusí se načíst délku hraní v sekundách bez ohledu na desetinný oddělovač v logu.
    /// </summary>
    public static bool TryParseDurationSeconds(string? value, out double seconds)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            seconds = default;
            return false;
        }

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out seconds)
            || double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds))
        {
            return true;
        }

        var normalizedInvariant = value.Replace(',', '.');
        if (double.TryParse(normalizedInvariant, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds))
        {
            return true;
        }

        var normalizedCurrentCulture = value.Replace('.', ',');
        return double.TryParse(normalizedCurrentCulture, NumberStyles.Float, CultureInfo.CurrentCulture, out seconds);
    }

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
            File.WriteAllText(filePath, FormatLastPlayedTimestamp(DateTime.Now) + Environment.NewLine);
        }

        return filePath;
    }
}
