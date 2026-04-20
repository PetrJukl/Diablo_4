using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Diablo4.WinUI.Helpers;

public static class AppDiagnostics
{
    private const long MaxLogFileSizeBytes = 2 * 1024 * 1024;
    private const int MaxRotatedGenerations = 3;

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
                TryRotateLogFile();

                using var stream = new FileStream(LogFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
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

    private static void TryRotateLogFile()
    {
        try
        {
            var info = new FileInfo(LogFilePath);
            if (!info.Exists || info.Length < MaxLogFileSizeBytes)
            {
                return;
            }

            var oldestPath = GetRotatedPath(MaxRotatedGenerations);
            if (File.Exists(oldestPath))
            {
                File.Delete(oldestPath);
            }

            for (int generation = MaxRotatedGenerations - 1; generation >= 1; generation--)
            {
                var sourcePath = GetRotatedPath(generation);
                if (File.Exists(sourcePath))
                {
                    File.Move(sourcePath, GetRotatedPath(generation + 1), overwrite: true);
                }
            }

            File.Move(LogFilePath, GetRotatedPath(1), overwrite: true);
        }
        catch (IOException ioEx)
        {
            // Pokud rotace selže (např. soubor je dočasně zamčen), pokračujeme dál - log se rotuje příště.
            Debug.WriteLine(ioEx);
        }
        catch (UnauthorizedAccessException uaEx)
        {
            Debug.WriteLine(uaEx);
        }
    }

    private static string GetRotatedPath(int generation)
    {
        return Path.Combine(LogDirectoryPath, $"Diablo4.WinUI.{generation}.log");
    }
}
