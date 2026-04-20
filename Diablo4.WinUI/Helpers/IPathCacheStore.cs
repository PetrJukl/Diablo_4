using System.Collections.Generic;

namespace Diablo4.WinUI.Helpers;

/// <summary>Abstrakce nad persistentní cache cest k spustitelným souborům her (C6).</summary>
public interface IPathCacheStore
{
    /// <summary>Vrátí pohled na všechny aktuálně načtené záznamy.</summary>
    IReadOnlyDictionary<string, string> Snapshot();

    /// <summary>Pokusí se získat cestu pro daný klíč (typicky název EXE).</summary>
    bool TryGet(string key, out string path);

    /// <summary>Uloží/aktualizuje záznam a flushne na disk (best-effort).</summary>
    void Set(string key, string path);
}
