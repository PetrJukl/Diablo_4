# Instrukce pro revizi kódu
- Senior standard ve všech bodech.

## Formátování kódu

### C# Coding Conventions
- Dodržuj Microsoft C# Coding Conventions
- **Naming conventions**:
  - `PascalCase` pro třídy, metody, vlastnosti, události
  - `camelCase` pro parametry, lokální proměnné
  - `_camelCase` (s podtržítkem) pro private fields
  - `IPascalCase` pro interface (prefix I)
- **Struktura**:
  - Používej závorky `{}` i pro jednořádkové bloky
  - Jeden řádek mezi metodami
  - `#region` bloky nepoužívej — preferuj menší třídy a logické členění kódu (výjimka: legacy kód, kde regiony již existují)
- **Komentáře**:
  - XML dokumentační komentáře pro public API
  - Inline komentáře jen pro složitou logiku
  - Komentáře v angličtině

### SQL Conventions
- SQL klíčová slova velkými písmeny: `SELECT`, `FROM`, `WHERE`, `JOIN`
- Názvy tabulek a sloupců: `PascalCase`
- Používej aliasy pro lepší čitelnost dotazů
- Preferuj stored procedures pro složité operace

### Obecné zásady
- **DRY** (Don't Repeat Yourself) - vyvaruj se duplicitního kódu
- **SOLID** principy
- **Error handling**: Vždy ošetřuj výjimky, používej `try-catch-finally`
- **Null safety**: Kontroluj null hodnoty, používej null-conditional operátory (`?.`, `??`)

## Best Practices

### Windows Forms
- Dodržuj pattern Presentation-Logic separation
- Databázovou logiku odděl od UI (použij Repository pattern nebo similar)
- Používej async/await pro DB operace, aby UI nezamrzlo
- Dispose resources správně (using statements, IDisposable pattern)

### SQL Server
- Používej parametrizované dotazy (ochrana proti SQL injection)
- Indexy na foreign keys a často používané sloupce
- Transakce pro operace, které musí být atomické
- Stored procedures pro opakované nebo složité operace

### Bezpečnost
- Nikdy neukládej hesla jako plain text (hash + salt)
- Validuj všechny vstupy od uživatelů
- Používej parametrizované SQL dotazy
- Chraň connection strings (configuration encryption)

### Práce s .NET projektem
- Respektuj existující strukturu Solution (`.sln`) a projektů (`.csproj`)
- Při přidávání NuGet balíčků ověř kompatibilitu s target frameworkem projektu
- Neměň `.csproj` ručně, pokud to není nezbytné — preferuj NuGet tooling
- Při vytváření nových projektů dodržuj stávající adresářovou konvenci v Solution
- Connection strings a konfigurace patří do `appsettings.json` / `app.config` — nikdy ne natvrdo v kódu

## Generování testů
- Pro kritickou business logiku automaticky navrhni unit testy
- Preferuj MSTest, NUnit nebo xUnit framework (podle toho, co projekt již používá)
- Pokryj edge cases a validaci vstupů
- Používej AAA pattern (Arrange-Act-Assert)

## Core Principles

- Před odevzdáním ověř funkčnost řešení/kódu -> podle aktuálních ověřených dat a dokumentace přes dostupné MCP servery, nebo gitHub repository, ne odhadem.
- Simplicity First: Každou změnu drž co nejjednodušší.
- No Laziness: Hledej root cause, nepoužívej dočasné záplaty, drž seniorní standard.
- Když je to relevantní, porovnej chovaní původní verze a změn.
- Spusť testy, zkontroluj logy a předveď korektnost, když nejsou logy -> vytvoř senior standard pro záchyt a zápis logů a pak otestuj.

---

## Meta

- **Verze:** 4.0
- **Poslední aktualizace:** 2026-03-07