using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Diablo4.WinUI.Helpers;

/// <summary>JSON implementace <see cref="IPathCacheStore"/>; ukládá do souboru v <c>%LocalAppData%\Diablo Log\</c>.</summary>
public sealed class JsonPathCacheStore : IPathCacheStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    private readonly string _filePath;
    private readonly ConcurrentDictionary<string, string> _entries;
    private readonly object _writeLock = new();

    public JsonPathCacheStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
        _entries = LoadFromDisk(filePath);
    }

    public static JsonPathCacheStore CreateDefault(string fileName = "paths.cache.json")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string directory = Path.Combine(appData, "Diablo Log");
        try
        {
            Directory.CreateDirectory(directory);
        }
        catch (IOException ex)
        {
            AppDiagnostics.LogWarning($"Nepodařilo se vytvořit adresář pro cache cest '{directory}'.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            AppDiagnostics.LogWarning($"Přístup k adresáři pro cache cest '{directory}' byl odmítnut.", ex);
        }

        return new JsonPathCacheStore(Path.Combine(directory, fileName));
    }

    public IReadOnlyDictionary<string, string> Snapshot()
    {
        return new Dictionary<string, string>(_entries, StringComparer.OrdinalIgnoreCase);
    }

    public bool TryGet(string key, out string path)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            path = string.Empty;
            return false;
        }

        if (_entries.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            path = value;
            return true;
        }

        path = string.Empty;
        return false;
    }

    public void Set(string key, string path)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        _entries[key] = path;
        Persist();
    }

    private void Persist()
    {
        lock (_writeLock)
        {
            try
            {
                var snapshot = new Dictionary<string, string>(_entries, StringComparer.OrdinalIgnoreCase);
                string json = JsonSerializer.Serialize(snapshot, SerializerOptions);
                string tempPath = _filePath + ".tmp";
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, _filePath, overwrite: true);
            }
            catch (IOException ex)
            {
                AppDiagnostics.LogWarning($"Nepodařilo se uložit cache cest '{_filePath}'.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                AppDiagnostics.LogWarning($"Přístup k cache souboru '{_filePath}' byl odmítnut při zápisu.", ex);
            }
        }
    }

    private static ConcurrentDictionary<string, string> LoadFromDisk(string filePath)
    {
        var result = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(filePath))
        {
            return result;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return result;
            }

            var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (loaded is null)
            {
                return result;
            }

            foreach (var pair in loaded)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
                {
                    continue;
                }

                result[pair.Key] = pair.Value;
            }
        }
        catch (JsonException ex)
        {
            AppDiagnostics.LogWarning($"Cache souboru '{filePath}' je poškozená a bude přeskočena.", ex);
        }
        catch (IOException ex)
        {
            AppDiagnostics.LogWarning($"Nepodařilo se načíst cache cest '{filePath}'.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            AppDiagnostics.LogWarning($"Přístup k cache souboru '{filePath}' byl odmítnut.", ex);
        }

        return result;
    }
}
