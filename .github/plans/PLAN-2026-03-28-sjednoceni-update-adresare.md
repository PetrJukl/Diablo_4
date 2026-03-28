# Plán: Sjednocení update adresáře

**Soubor:** `.github/plans/PLAN-2026-03-28-sjednoceni-update-adresare.md`  
**Datum:** 2026-03-28  
**Stav:** `dokončeno`

---

## Cíl

> Přesunout ukládání stažených update balíčků z `C:\Users\JuklP\AppData\Local\KontrolaParby\Updates` do `C:\Users\JuklP\AppData\Local\Diablo Log\Updates`, aby logy i update soubory byly pod jedním kořenem v `LocalApplicationData`.

## Kontext

> Požadavek je sjednotit aplikační data do jedné složky a odstranit roztříštěnost mezi `KontrolaParby` a `Diablo Log`. Aktuálně logování už používá `Diablo Log`, zatímco update služba stále zapisuje do `KontrolaParby`.

## Rozsah

> `Diablo4.WinUI/Services/UpdateService.cs`, `Diablo4.WinUI.Tests/Services/UpdateServiceTests.cs`, plánový soubor v `.github/plans/`.

## Návrh řešení

### Varianta A – změna kořenové složky v update službě

> Upravit konstantu nebo skládání cesty v `UpdateService`, aby používala kořen `Diablo Log` a zachovala podsložku `Updates`. Následně upravit testy, které tuto cestu explicitně ověřují.

**Výhody:**
- minimální zásah,
- beze změny logiky stahování a mazání installerů,
- sjednocení s existujícím logováním.

**Nevýhody:**
- staré soubory ve staré složce se automaticky nepřesunou.

### Varianta B – migrace staré složky při startu *(volitelné)*

> Kromě změny cílové cesty přidat i přesun obsahu ze staré složky do nové.

**Výhody:**
- uživatel nepřijde o již stažené instalační balíčky.

**Nevýhody:**
- mění chování,
- vyšší riziko chyb při přesunu souborů,
- není potřeba pro samotné sjednocení umístění.

**Zvolená varianta:** A – splní požadavek s minimálním dopadem a bez změny stávající logiky update procesu.

## Kroky implementace

- [x] Upravit kořen update adresáře v `UpdateService` na `Diablo Log`.
- [x] Upravit testy očekávající původní cestu `KontrolaParby\Updates`.
- [x] Spustit relevantní testy pro `UpdateService`.
- [x] Doplnit poznámku o realizaci a ověření.

## Dotčené soubory

| Soubor | Typ změny | Poznámka |
|--------|-----------|----------|
| `Diablo4.WinUI/Services/UpdateService.cs` | úprava | změna kořenové složky pro update soubory |
| `Diablo4.WinUI.Tests/Services/UpdateServiceTests.cs` | úprava | aktualizace očekávané cesty v testech |
| `.github/plans/PLAN-2026-03-28-sjednoceni-update-adresare.md` | přidání | plán změny |

## Rizika

| Riziko | Pravděpodobnost | Dopad | Mitigace |
|--------|-----------------|-------|----------|
| Některý test nebo kód očekává starou cestu | střední | střední | projít výskyty a upravit explicitní očekávání |
| Ve staré složce zůstanou historické instalátory | vysoký | nízký | ponechat beze změny, případnou migraci řešit samostatně |

## Ověření

> Ověření proběhne spuštěním testů pro `UpdateService` a kontrolou, že stažený instalační balíček cílí do `LocalApplicationData\Diablo Log\Updates`.

## Rollback

> Vrátit změnu kořenové složky v `UpdateService` a odpovídající testy na původní `KontrolaParby\Updates`.

## Poznámky po dokončení

> Realizováno úpravou `Diablo4.WinUI/Services/UpdateService.cs` a `Diablo4.WinUI.Tests/Services/UpdateServiceTests.cs`. Kořen update adresáře je nově `LocalApplicationData\Diablo Log\Updates`. Odchylka od původně popsaného rizika: migrace starých souborů se neřešila a podle upřesnění uživatele není potřeba, protože update soubory se průběžně uklízejí.

## Review

> Ověření proběhlo nad `Diablo4.WinUI.Tests.Services.UpdateServiceTests`: 7/7 testů prošlo. Následně proběhl úspěšný build celého workspace.
