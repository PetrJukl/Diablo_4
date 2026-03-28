# Plán: Hardening stability, error handlingu a testů

**Soubor:** `.github/plans/PLAN-2026-03-28-hardening-stability-a-testy.md`  
**Datum:** 2026-03-28  
**Stav:** `dokončeno`

---

## Cíl

> Zvýšit odolnost aplikace proti pádům, zpřesnit řízení chyb v kritických tocích a omezit riziko spuštění nedůvěryhodného kódu při update flow a při spouštění her. Součástí je doplnění cílených testů pro bezpečnostní a stabilitní scénáře včetně orchestrace oken tam, kde to architektura dovolí bez neúměrného zásahu do chování aplikace.

## Kontext

> Audit ukázal, že aplikace má základní logování a několik ochranných `try/catch`, ale zůstávají otevřená rizika v updateru a ve vyhledání/spouštění her podle názvu `exe`. Dále chybí systematičtější ochrana kritických UI vstupních bodů a testy pro pořadí a orchestrace oken. Uživatel výslovně požaduje kontrolu stability proti pádům, řízení errorů a návrh/doplnění vhodných testů.

## Rozsah

> Dotčené oblasti:
> - `Diablo4.WinUI/App.xaml.cs`
> - `Diablo4.WinUI/MainWindow.xaml.cs`
> - `Diablo4.WinUI/Services/UpdateService.cs`
> - `Diablo4.WinUI/Views/WeekendMotivationDialog.xaml.cs`
> - `Diablo4.WinUI/ViewModels/MainViewModel.cs`
> - `Diablo4.WinUI/Helpers/AppDiagnostics.cs`
> - `Diablo4.WinUI.Tests/Services/UpdateServiceTests.cs`
> - `Diablo4.WinUI.Tests/Services/ProcessMonitorTests.cs`
> - případně nové testy pro view model / UI orchestration helper

## Návrh řešení

### Varianta A – Minimální hardening v existující architektuře

> Zachovat stávající strukturu aplikace, doplnit guardy, přesnější validace a lépe oddělit rozhodovací logiku od UI tam, kde je to nutné pro testování. Kritické body zabezpečit s minimálním dopadem na existující chování.

**Výhody**
- malý diff,
- nízké riziko regresí,
- respektuje současnou architekturu,
- lze rychle ověřit testy.

**Nevýhody**
- UI orchestrace zůstane testovatelná jen částečně,
- některé WinUI specifické scénáře půjdou pokrýt spíš nepřímo než plnohodnotným UI automation testem.

### Varianta B – Větší refactor pro plnou testovatelnost UI toku

> Zavést další abstrahované služby pro otevírání oken, spouštění procesů a rozhodování o tray/update flow, aby bylo možné přesně testovat pořadí událostí a oken čistě unit testy.

**Výhody**
- lepší testovatelnost UI orchestrace,
- čistší oddělení odpovědností.

**Nevýhody**
- větší zásah do více souborů,
- vyšší riziko změny chování,
- horší poměr cena/přínos pro aktuální rozsah.

**Zvolená varianta:** A – minimalizuje zásahy do chování aplikace a cílí přímo na zjištěná rizika.

## Kroky implementace

- [x] Upřesnit bezpečnostní politiku pro update balíček a spouštění her
- [x] Zpevnit `UpdateService` validací zdroje a integrity balíčku
- [x] Zpevnit `WeekendMotivationDialog` proti spuštění nedůvěryhodného `exe`
- [x] Doplnit guardy a error handling do startup/UI vstupních bodů
- [x] Zpřesnit logování kritických selhání bez tichého polykání chyb
- [x] Vyčlenit testovatelnou rozhodovací logiku tam, kde to bude nutné
- [x] Doplnit unit testy pro updater, monitoring a error-handling větve
- [x] Navrhnout rozumné pokrytí pořadí oken bez nadměrného refactoru
- [x] Ověřit build a všechny relevantní testy
- [x] Dopsat poznámky po dokončení a review sekci

## Dotčené soubory

| Soubor | Typ změny | Poznámka |
|--------|-----------|----------|
| `Diablo4.WinUI/App.xaml.cs` | úprava | guardy startupu, logování kritických chyb |
| `Diablo4.WinUI/MainWindow.xaml.cs` | úprava | stabilita aktivace, tray flow, ochrana UI toku |
| `Diablo4.WinUI/Services/UpdateService.cs` | úprava | validace URL, integrity a bezpečnější spuštění updateru |
| `Diablo4.WinUI/Views/WeekendMotivationDialog.xaml.cs` | úprava | omezení vyhledání/spuštění `exe`, error handling |
| `Diablo4.WinUI/ViewModels/MainViewModel.cs` | úprava | zpřesnění guardů a chování při selháních |
| `Diablo4.WinUI/Helpers/AppDiagnostics.cs` | úprava | případné zpřesnění diagnostiky a kontextu logů |
| `Diablo4.WinUI.Tests/Services/UpdateServiceTests.cs` | úprava | nové testy na bezpečnostní a chybové scénáře |
| `Diablo4.WinUI.Tests/Services/ProcessMonitorTests.cs` | úprava | edge cases a chybové větve |
| `Diablo4.WinUI.Tests/ViewModels/MainViewModelTests.cs` | přidání | testy notifikací, guardů a error handlingu |
| `Diablo4.WinUI.Tests/...` | přidání / úprava | případný helper test pro pořadí oken a rozhodovací logiku |

## Rizika

| Riziko | Pravděpodobnost | Dopad | Mitigace |
|--------|-----------------|-------|----------|
| Zpřísnění validace updateru zablokuje legitimní update | střední | vysoký | zavést jasná pravidla a pokrýt je testy |
| Ochrany kolem spouštění her změní očekávané chování na některých PC | střední | střední | zachovat fallback a detailně logovat důvod odmítnutí |
| WinUI okna nepůjde rozumně pokrýt čistým unit testem | vysoká | střední | otestovat alespoň rozhodovací logiku mimo UI vrstvu |
| Přidání guardů zakryje původní chybu bez dostatečné diagnostiky | nízká | střední | logovat přesný kontext a přesné typy výjimek |

## Ověření

> Ověření proběhne přes:
> - `run_build` pro celé řešení,
> - spuštění celé testovací sady `Diablo4.WinUI.Tests`,
> - doplněné testy na validaci updateru, guardy a vybrané rozhodovací scénáře,
> - manuální kontrolu, že hlavní okno, update okno a víkendový dialog stále fungují ve stejném základním toku.

## Rollback

> Pokud se změny neosvědčí, rollback bude proveden revertováním commitů s hardeningem a testy. Proto budou změny drženy v co nejmenších logických blocích, aby šlo vrátit bezpečnostní zpřísnění odděleně od testů.

## Poznámky po dokončení

> Zavedena byla validace trusted GitHub HTTPS zdrojů pro manifest i download URL, kontrola podporovaných instalačních přípon, kontrola finální redirect URL po stažení a volitelná validace `SHA-256` checksumu z manifestu. U spouštění her byla doplněna validace absolutní cesty pod povolenými root adresáři a robustnější ošetření selhání `Process.Start`. Ve startupu a hlavním okně byly doplněny guardy, aby chyby v update flow, aktivaci okna nebo restore/hide toku neshodily aplikaci bez logu.

> Testy nově pokrývají trusted-host policy, validaci `SHA-256`, trusted executable path a stávající integrační downloader scénáře. Skutečné pořadí WinUI oken zůstává primárně manuální / integrační záležitost, protože bez většího refactoru by čisté unit testy testovaly jen technické detaily UI frameworku, ne business rozhodnutí.

## Review

> Implementace byla provedena s minimálním zásahem do stávající architektury. Přibyly helpery `UpdateSourcePolicy` a `ExecutableLaunchPolicy`, hardening `UpdateService`, lepší guardy v `App.xaml.cs`, `MainWindow.xaml.cs` a `WeekendMotivationDialog.xaml.cs` a nové MSTest testy.

> Ověřené scénáře:
> - `run_build` prošel úspěšně,
> - `Diablo4.WinUI.Tests`: `20/20 passed`,
> - updater odmítá neplatné / nedůvěryhodné URL,
> - downloader stále funguje v integračních testech,
> - validace cesty k `exe` blokuje soubory mimo povolené rooty.

> Otevřené body pro další iteraci:
> - pokud má být integrita update balíčku vynucená vždy, je vhodné doplnit `Sha256` i do produkčního `update-manifest.json`,
> - pokud má být testované i skutečné pořadí WinUI oken, dává smysl až samostatná UI/integration test vrstva nebo lehká orchestration služba.
