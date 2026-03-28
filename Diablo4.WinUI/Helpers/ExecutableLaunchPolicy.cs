using System;
using System.Collections.Generic;
using System.IO;

namespace Diablo4.WinUI.Helpers;

internal static class ExecutableLaunchPolicy
{
    internal static bool IsTrustedExecutablePath(string? executablePath, IEnumerable<string> allowedRoots)
    {
        if (string.IsNullOrWhiteSpace(executablePath) || allowedRoots is null)
        {
            return false;
        }

        if (!Path.IsPathRooted(executablePath) || !string.Equals(Path.GetExtension(executablePath), ".exe", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string normalizedExecutablePath;

        try
        {
            normalizedExecutablePath = NormalizeFilePath(executablePath);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }

        if (!File.Exists(normalizedExecutablePath))
        {
            return false;
        }

        foreach (var allowedRoot in allowedRoots)
        {
            if (string.IsNullOrWhiteSpace(allowedRoot) || !Path.IsPathRooted(allowedRoot))
            {
                continue;
            }

            try
            {
                var normalizedRoot = NormalizeDirectoryPath(allowedRoot);
                if (normalizedExecutablePath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (ArgumentException)
            {
            }
            catch (NotSupportedException)
            {
            }
        }

        return false;
    }

    private static string NormalizeFilePath(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static string NormalizeDirectoryPath(string path)
    {
        return NormalizeFilePath(path) + Path.DirectorySeparatorChar;
    }
}