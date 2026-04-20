namespace Diablo4.WinUI.Models;

/// <summary>
/// Výsledek pokusu o spuštění instalátoru aktualizace.
/// </summary>
public enum UpdateApplyResult
{
    /// <summary>Instalátor byl úspěšně spuštěn (běží proces installeru).</summary>
    Started,

    /// <summary>Uživatel odmítl UAC výzvu, instalace nebyla aplikována.</summary>
    UserCancelled
}
