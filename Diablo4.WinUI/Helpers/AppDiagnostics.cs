using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Diablo4.WinUI.Helpers;

public static class AppDiagnostics
{
    private static readonly object SyncRoot = new();
    private static readonly string LogDirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Diablo Log");
    private static readonly string LogFilePath = Path.Combine(LogDirectoryPath, "Diablo4.WinUI.log");

    public static void LogInfo(string message)
    {
        WriteEntry("INFO", message, null);
    }

    public static void LogWarning(string message, Exception? exception = null)
    {
        WriteEntry("WARN", message, exception);
    }

    public static void LogError(string message, Exception? exception = null)
    {
        WriteEntry("ERROR", message, exception);
    }

    private static void WriteEntry(string level, string message, Exception? exception)
    {
        try
        {
            Directory.CreateDirectory(LogDirectoryPath);

            lock (SyncRoot)
            {
                using var stream = new FileStream(LogFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                using var writer = new StreamWriter(stream, Encoding.UTF8);

                writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}");

                if (exception is not null)
                {
                    writer.WriteLine(exception);
                }

                writer.WriteLine();
            }
        }
        catch (IOException ioEx)
        {
            Debug.WriteLine(ioEx);
        }
        catch (UnauthorizedAccessException unauthorizedAccessException)
        {
            Debug.WriteLine(unauthorizedAccessException);
        }
    }
}
