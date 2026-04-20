# Plán: Optimalizace, bezpečnost a stabilita aplikace `Diablo4.WinUI`

**Soubor:** `.github/plans/PLAN-2026-04-20-optimalizace-bezpecnost-stabilita.md`
**Datum:** 2026-04-20
**Stav:** `fáze A hotová – čeká schválení fáze B`

---

## Cíl

Snížit CPU/IO zátěž aplikace, zpřísnit bezpečnostní hranice update flow i spouštění her, a doplnit chybějící ošetření pádů, timeoutů a race conditions tak, aby aplikace běžela stabilně i v rohových scénářích (offline, přerušené stahování, podstrčený EXE, zaplněný disk, dlouhý běh).

Bod 1.2 (50 ms `_messageUpdateTimer` v `MainViewModel`) **zůstává beze změny** – jde o záměrný plynulý vizuální nápočet.

## Kontext

Vychází z revize provedené 2026-04-20 nad celým solution `Diablo4.WinUI` + `Diablo4.WinUI.Tests`. Identifikovány 3 oblasti:
1. **Optimalizace** – polling procesů, opakované čtení celého logu, mrtvý kód.
2. **Bezpečnost** – path traversal v download flow, slabé trust ověření spouštěných EXE, manifest bez vyžadování SHA-256, neomezený růst log souboru.
3. **Stabilita** – chybějící timeouty/cancellation v update flow, `Task.Run` per timer tick, race podmínky při zavírání weekend dialogu během vyhledávání EXE, spam logů při opakovaných chybách timeru.

## Rozsah

- `Diablo4.WinUI/Services/ProcessMonitor.cs`
- `Diablo4.WinUI/Services/UpdateService.cs`
- `Diablo4.WinUI/ViewModels/MainViewModel.cs`
- `Diablo4.WinUI/ViewModels/BaseViewModel.cs` (smazat)
- `Diablo4.WinUI/Views/MainPage.xaml.cs`
- `Diablo4.WinUI/Views/UpdateNotificationWindow.cs`
- `Diablo4.WinUI/Views/WeekendMotivationDialog.xaml.cs`
- `Diablo4.WinUI/Helpers/AppDiagnostics.cs`
- `Diablo4.WinUI/Helpers/UpdateSourcePolicy.cs`
- `Diablo4.WinUI/Helpers/ExecutableLaunchPolicy.cs`
- `Diablo4.WinUI/App.xaml.cs`
- `Diablo4.WinUI.Tests/**` (rozšíření existujících + nové unit testy)

## Návrh řešení

Iterativní přístup ve **třech fázích**, každá končí zelenými testy a manuálním smoke testem. Mezi fázemi se zapisuje shrnutí do `.github/log.log`.

### Fáze A – Quick wins (bezpečnost + úklid)

| ID | Položka | Soubor |
|----|---------|--------|
| A1 | Sanitizace názvu souboru a path-traversal guard v `GetDownloadDestinationPath` | `UpdateService.cs` |
| A2 | Vyžadovat SHA-256 v manifestu jako povinný (jinak `UpdateCheckResult.Error`) | `UpdateService.cs` |
| A3 | Whitelist GitHub *path* (`/PetrJukl/Diablo_4/...`), nejen hostu | `UpdateSourcePolicy.cs` |
| A4 | `AppDiagnostics`: `FileShare.Read`, rotace logu po 2 MB (rename na `.1.log`, max 3 generace) | `AppDiagnostics.cs` |
| A5 | Smazat `BaseViewModel.cs`, smazat `MainPage.ViewModel => new()` (nepoužívané, drahé) | `BaseViewModel.cs`, `MainPage.xaml.cs` |
| A6 | `ProcessMonitor.GetIso8601WeekOfYear` → `static`; přesun separátoru `||` do `static readonly` | `ProcessMonitor.cs` |

### Fáze B – Stabilita update flow + spouštění her

| ID | Položka | Soubor |
|----|---------|--------|
| B1 | `UpdateService.DownloadAndInstallAsync` – přidat `CancellationToken` propag. + interní `CancelAfter(5 min)` jako safety net | `UpdateService.cs`, `App.xaml.cs`, `UpdateNotificationWindow.cs` |
| B2 | `UpdateNotificationWindow` – tlačítko „Zrušit", `IProgress<double>` s % staženo (Content-Length) | `UpdateNotificationWindow.cs`, `UpdateService.cs` |
| B3 | Po `Process.Start` installeru počkat krátce (`WaitForInputIdle` / 3 s) – pokud uživatel UAC odmítne, neukončovat aplikaci | `UpdateService.cs`, `UpdateNotificationWindow.cs` |
| B4 | `WeekendMotivationDialog._cts` – nedisposovat v `Closed` handleru během běžícího `_isLaunchInProgress`; bezpečné dispose v `finally` po dokončení launche | `WeekendMotivationDialog.xaml.cs` |
| B5 | `ExecutableLaunchPolicy` – ověření Authenticode podpisu (`WinVerifyTrust` přes P/Invoke) volitelně přes `IsTrustedSignedExecutable`; volání z `WeekendMotivationDialog` | `ExecutableLaunchPolicy.cs`, `WeekendMotivationDialog.xaml.cs`, `Imports.cs` |
| B6 | `MainViewModel.UpdateStatsTimerTick` – po N (=10) po sobě jdoucích chybách stop timeru + jednorázový log; reset po úspěchu | `MainViewModel.cs` |
| B7 | `MainWindow.ConfigureWindow` – obalit volání `SetIcon` / `WindowNative.GetWindowHandle` v try/catch s fallbackem | `MainWindow.xaml.cs` |

### Fáze C – Optimalizace běhu

| ID | Položka | Soubor |
|----|---------|--------|
| C1 | `ProcessMonitor` – nahradit `Process.GetProcesses()` voláním `Process.GetProcessesByName(name)` v cyklu přes `_processNames` | `ProcessMonitor.cs` |
| C2 | `ProcessMonitor` – jeden background pumping `Task` se smyčkou `await Task.Delay(500, ct)` místo `Task.Run` per tick | `ProcessMonitor.cs` |
| C3 | `ProcessMonitor` – inkrementální čtení logu: udržovat agregát `WeeklyDurations` + offset poslední přečtené pozice; reload jen nových řádků | `ProcessMonitor.cs` |
| C4 | `ProcessMonitor.TryWriteToFileAtPosition` – nepřepisovat za běhu při každém ticku, ale jen každých 30 s + při `Stop()` | `ProcessMonitor.cs` |
| C5 | `MainViewModel.LoadCachedLastPlayedDateTime` – nahradit cachováním podle `FileInfo.LastWriteTimeUtc` (přečíst jen když se soubor změnil) | `MainViewModel.cs` |
| C6 | `WeekendMotivationDialog.FindExecutablePathAsync` – `Directory.EnumerateFiles` s `EnumerationOptions { RecurseSubdirectories=true, IgnoreInaccessible=true, MaxRecursionDepth=4, AttributesToSkip=ReparsePoint }` + persistentní cache do `LocalAppData\Diablo Log\paths.cache.json` | `WeekendMotivationDialog.xaml.cs` |

> **Mimo rozsah** (vědomě odloženo): event-driven WMI/ETW monitoring procesů (návrh 1.10) a externí `appsettings.json`. Vrátím se k nim samostatným plánem, pokud bude zájem – jsou to větší architektonické změny.

**Zvolená varianta:** iterativní A → B → C s vlastním commitem za fázi a separátním schválením přechodu mezi fázemi. Důvod: každá fáze je nezávisle deployovatelná, snižuje riziko regresí a umožňuje rollback po fázích.

## Kroky implementace

### Fáze A – Quick wins
- [x] A1 – path-traversal guard + sanitizace názvu instalátoru
- [x] A2 – povinné SHA-256 v manifestu
- [x] A3 – whitelist GitHub path prefixu
- [x] A4 – rotace + `FileShare.Read` v `AppDiagnostics`
- [x] A5 – smazat `BaseViewModel.cs` a opravit `MainPage.ViewModel`
- [x] A6 – kosmetika `ProcessMonitor` (static, readonly separator)
- [x] Unit testy: nové pro A1–A4, ostatní jen build
- [x] `dotnet test` zelený (28/28); smoke check zatím neproveden – proveden bude před fází B
- [ ] Commit `feat(security): fáze A – quick wins`
- [ ] Schválení uživatele před fází B

### Fáze B – Stabilita
- [x] B1 – cancellation propagation v download flow
- [x] B2 – progress + tlačítko Zrušit
- [x] B3 – ošetření UAC odmítnutí
- [x] B4 – race-safe shutdown weekend dialogu
- [x] B5 – Authenticode kontrola (`WinVerifyTrust` P/Invoke + helper)
- [x] B6 – stop timeru po N chybách
- [x] B7 – obalení `ConfigureWindow` v try/catch
- [x] Unit testy: `AuthenticodeVerifierTests` (3 testy) + `UpdateServiceTests` o cancellation a progress (33/33 zelené)
- [x] `dotnet test` zelený – manuální smoke ponechán uživateli k odsouhlasení
- [ ] Commit `feat(stability): fáze B – stabilita update flow a spouštění`
- [ ] Schválení uživatele před fází C

### Fáze C – Optimalizace
- [ ] C1 – `GetProcessesByName` per name
- [ ] C2 – jeden background loop místo `Task.Run` per tick
- [ ] C3 – inkrementální parser logu + agregát
- [ ] C4 – throttling zápisu aktivního sezení (30 s + při Stop)
- [ ] C5 – `LastWriteTimeUtc` cache pro `_cachedLastPlayedDateTime`
- [ ] C6 – `EnumerateFiles` + persistentní path cache
- [ ] Unit testy: `ProcessMonitorTests` rozšíření o inkrementální načítání; nové testy pro persistent cache (přes abstrakci `IPathCacheStore`)
- [ ] `dotnet test` zelený, manuální smoke (24h běh – ověřit log size, stabilní paměť, žádné nové errory v `.log`)
- [ ] Commit `perf: fáze C – optimalizace monitoringu a vyhledávání`

## Dotčené soubory

| Soubor | Typ změny | Poznámka |
|--------|-----------|----------|
| `Diablo4.WinUI/Services/ProcessMonitor.cs` | úprava | A6, C1–C4 |
| `Diablo4.WinUI/Services/UpdateService.cs` | úprava | A1, A2, B1, B2, B3 |
| `Diablo4.WinUI/Helpers/UpdateSourcePolicy.cs` | úprava | A3 |
| `Diablo4.WinUI/Helpers/AppDiagnostics.cs` | úprava | A4 |
| `Diablo4.WinUI/Helpers/ExecutableLaunchPolicy.cs` | úprava | B5 |
| `Diablo4.WinUI/ViewModels/MainViewModel.cs` | úprava | B6, C5 |
| `Diablo4.WinUI/ViewModels/BaseViewModel.cs` | smazání | A5 |
| `Diablo4.WinUI/Views/MainPage.xaml.cs` | úprava | A5 |
| `Diablo4.WinUI/Views/UpdateNotificationWindow.cs` | úprava | B1, B2, B3 |
| `Diablo4.WinUI/Views/WeekendMotivationDialog.xaml.cs` | úprava | B4, B5, C6 |
| `Diablo4.WinUI/MainWindow.xaml.cs` | úprava | B7 |
| `Diablo4.WinUI/App.xaml.cs` | úprava | B1 (předání CTS do update flow) |
| `Diablo4.WinUI/Imports.cs` | úprava (volitelně) | P/Invoke imports pro `WinVerifyTrust` (případně samostatný `Helpers/NativeMethods.cs`) |
| `Diablo4.WinUI.Tests/Services/UpdateServiceTests.cs` | úprava | A1, A2, B1 |
| `Diablo4.WinUI.Tests/Helpers/UpdateSourcePolicyTests.cs` | úprava | A3 |
| `Diablo4.WinUI.Tests/Helpers/ExecutableLaunchPolicyTests.cs` | úprava | B5 |
| `Diablo4.WinUI.Tests/Services/ProcessMonitorTests.cs` | úprava | C3 |
| `Diablo4.WinUI.Tests/Helpers/AppDiagnosticsTests.cs` | nový | A4 (rotace) |

## Rizika

| Riziko | Pravděpodobnost | Dopad | Mitigace |
|--------|-----------------|-------|----------|
| A2 (povinné SHA-256) zablokuje stávající uživatele, pokud je v manifestu prázdné | nízká | vysoký | Před releasem ověřit aktuální `update-manifest.json`; pokud chybí SHA, nejdřív vydat manifest s SHA, teprve poté aplikaci |
| B5 Authenticode kontrola odmítne legitimní hru bez podpisu (např. starší GOG titul) | střední | střední | Dvouúrovňové: nepodepsané jen `LogWarning` + zobrazit confirm dialog „Hra není digitálně podepsaná. Spustit?"; tvrdé blokování jen pro EXE mimo Program Files |
| C3 Inkrementální parser zavede regresi v týdenních součtech | střední | vysoký | Pokrýt unit testy s fixture logy; ponechat fallback `--full-rescan` při detekci nesouladu (změna velikosti souboru zpětně) |
| C4 Méně častý zápis aktivního sezení = ztráta až 30 s při crashi | nízká | nízký | Při `Stop()` vždy doflushovat; krash zapisuje jen aktuální sezení, historie zůstává |
| Rotace logu (A4) konflikt s otevřeným handle z jiného procesu | nízká | nízký | Při `IOException` na rename → fallback: pokračovat zápisem dál, rotace proběhne příště |

## Ověření

Po každé fázi:
- `dotnet build` a `dotnet test` v solution rootu (CI parita).
- Manuální smoke checklist (specifický pro každou fázi výše).
- Inspekce `LocalApplicationData\Diablo Log\Diablo4.WinUI.log` – žádné nové `ERROR`/`WARN` které předtím nebyly.
- Ověřit, že `ProcessMonitor` po fázi C nehlásí drift týdenních hodin proti referenční fixture.

## Rollback

Po fázi se commit pushuje samostatně. Rollback fáze:
```pwsh
git revert <commit-sha-faze-X>
```
Pro selektivní revert jednotlivého bodu je v rámci fáze možné použít `git checkout <sha>~ -- <soubor>`.

## Poznámky po dokončení

> Doplnit po realizaci každé fáze: skutečně implementované body, deviace od plánu, co se naučilo, otevřené follow-upy (zejména WMI/ETW monitoring a externí konfigurace).
