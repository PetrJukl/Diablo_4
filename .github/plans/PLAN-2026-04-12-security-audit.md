# Plán: Security audit – error handling & bezpečnost

**Soubor:** `.github/plans/PLAN-2026-04-12-security-audit.md`  
**Datum:** 2026-04-12  
**Stav:** `návrh`

---

## Cíl

Opravit všechna nalezená bezpečnostní rizika, nesprávné try-catch bloky a error-handling slabiny v `Diablo4.WinUI`. Žádná logická změna chování aplikace – pouze hardening a oprava defektů.

## Kontext

Kompletní ruční audit všech `.cs` souborů v projektu. Baseline: 21/21 testů zelených.

---

## Nálezy seřazené podle závažnosti

### 🔴 HIGH – spolehlivost / bezpečnost

| # | Soubor | Nález |
|---|--------|-------|
| H1 | `UpdateService.cs` | Sdílený `HttpClient` s `Timeout = 15s` – timeout platí pro celý HTTP request (headers + body). Stahování velkého installeru na pomalém připojení spolehlivě selže s `TaskCanceledException`. |
| H2 | `UpdateService.cs` | `CleanupDownloadedInstallers` – `Directory.GetFiles()` a vnější `Directory.CreateDirectory()` nejsou v try-catch. Volá se z konstruktoru → nekontrolovaná výjimka crashne inicializaci. |
| H3 | `FileHelper.cs` | `EnsureFileExists` nemá vůbec žádný error handling. Volá se z konstruktoru `MainViewModel`. Pokud je `%LOCALAPPDATA%` nepřístupný, aplikace crashne bez smysluplné chyby. |
| H4 | `App.xaml.cs` | `StartupErrorLogPath` jde do `%TEMP%` – veřejně přístupná složka (sdílený systém). Stack traces odhalují interní strukturu aplikace. |

### 🟡 MEDIUM – race conditions / error handling

| # | Soubor | Nález |
|---|--------|-------|
| M1 | `ProcessMonitor.cs` | `_isCheckingTabs` a `_isWebRunning` nejsou `volatile`. Přistupuje k nim DispatcherQueue thread i `Task.Run` background thread bez synchronizace. (`_isRunning` a `_isProcessing` jsou správně `volatile`.) |
| M2 | `ProcessMonitor.cs` | `CheckAllOpenTabsForUdemy` – bare `catch { return false; }` v lambda pro `p.MainWindowTitle` spolkne všechny výjimky bez logování. Navíc `Process.GetProcesses()` vrátí objekty, které nikdy nejsou `Dispose`d. |
| M3 | `ProcessMonitor.cs` | `LoadHistoricalDurations` a `AddActiveSessionDuration` používají `DateTime.Parse($"31.12.{year}")` bez explicitní kultury. Na anglickém systému formát `dd.MM.yyyy` selže. |
| M4 | `WeekendMotivationDialog.xaml.cs` | `IsProcessRunning` – `Process.GetProcessesByName(processName)` vrací `Process[]`, které nikdy nejsou dispose'd → resource leak (handles). |
| M5 | `MainViewModel.cs` | `UpdateStatsTimerTick` zachytává jen `IOException` a `InvalidOperationException`. Chybí fallback `catch (Exception ex)` pro případné jiné výjimky v computaci statistik. |
| M6 | `ProcessMonitor.cs` | `GetFileLength` zachytává pouze `IOException`. Chybí `UnauthorizedAccessException` a `SecurityException`. |

### 🔵 LOW – code quality / drobné aspekty

| # | Soubor | Nález |
|---|--------|-------|
| L1 | `UpdateNotificationWindow.cs` | `[DllImport("user32.dll")]` bez `SetLastError = true`. Return value `SetWindowPos` je ignorován bez logování. |
| L2 | `MachineContextHelper.cs` | Název počítače `"LEGION"` hardcoded ve zdrojovém kódu. |
| L3 | `UpdateSourcePolicy.cs` | `IsValidSha256` vrací `true` pro prázdné/null hodnoty, aniž by bylo logováno varování. Installer bez SHA-256 projde bez ověření integrity. |
| L4 | `App.xaml.cs` | `_singleInstanceMutex` není explicitně uvolněn při zavírání aplikace (OS to sice vyřeší, ale je to nekorektní pattern). |
| L5 | `WeekendMotivationDialog.xaml.cs` | Encoding issues – soubor obsahuje Mojibake v komentářích a string literálech (pravděpodobně špatná znaková sada při uložení). |

---

## Návrh řešení

### Varianta A – Inkrementální opravy (doporučeno)

Opravy přímo v dotčených souborech, bez změny architektury. Každá oprava je minimální a nezávislá.

**Výhody:** Minimální dopad, snadné review, žádný refactor risk.  
**Nevýhody:** Víc malých změn napříč soubory.

### Varianta B – Refactor error handling do centrálního wrapper

Centralizovat try-catch logiku pro filesystem operace. Větší zásah, větší risk.

**Zvolená varianta:** A

---

## Kroky implementace

### HIGH opravy
- [ ] H1 – `UpdateService`: oddělit HTTP klient pro manifest (15s) a download (bez timeoutu, spolehá jen na CancellationToken)
- [ ] H2 – `UpdateService.CleanupDownloadedInstallers`: obalit `Directory.GetFiles` + `Directory.CreateDirectory` do try-catch
- [ ] H3 – `FileHelper.EnsureFileExists`: přidat try-catch s logováním přes `AppDiagnostics`
- [ ] H4 – `App.xaml.cs`: přesunout `StartupErrorLogPath` z `%TEMP%` do `%LOCALAPPDATA%\Diablo Log\`

### MEDIUM opravy
- [ ] M1 – `ProcessMonitor`: přidat `volatile` na `_isCheckingTabs` a `_isWebRunning`
- [ ] M2 – `ProcessMonitor.CheckAllOpenTabsForUdemy`: opravit bare catch, přidat Dispose na `Process` objekty
- [ ] M3 – `ProcessMonitor`: nahradit `DateTime.Parse` za `DateTime.ParseExact` s `CultureInfo.InvariantCulture`
- [ ] M4 – `WeekendMotivationDialog.IsProcessRunning`: dispose `Process[]` z `GetProcessesByName`
- [ ] M5 – `MainViewModel.UpdateStatsTimerTick`: přidat fallback `catch (Exception ex)`
- [ ] M6 – `ProcessMonitor.GetFileLength`: přidat `UnauthorizedAccessException` catch

### LOW opravy
- [ ] L1 – `UpdateNotificationWindow`: přidat `SetLastError = true`, zkontrolovat return value
- [ ] L2 – `MachineContextHelper`: TODO komentář zůstane (machine name je záměrná personalní konfigurace)
- [ ] L3 – `UpdateSourcePolicy.IsValidSha256`: přidat `AppDiagnostics.LogWarning` pro prázdné SHA-256 (jen při volání z `DownloadAndInstallAsync`)
- [ ] L4 – `App.xaml.cs`: volat `_singleInstanceMutex.ReleaseMutex()` při ukončení aplikace
- [ ] L5 – `WeekendMotivationDialog.xaml.cs`: přeuložit soubor jako UTF-8

### Testy
- [ ] Přidat test pro `FileHelper.EnsureFileExists` při nepřístupném adresáři (mock)
- [ ] Přidat test pro `UpdateService` – download timeout se neaplikuje na body stream
- [ ] Ověřit všechny stávající testy prochází

---

## Dotčené soubory

| Soubor | Typ změny | Opravy |
|--------|-----------|--------|
| `Diablo4.WinUI/Services/UpdateService.cs` | úprava | H1, H2 |
| `Diablo4.WinUI/Helpers/FileHelper.cs` | úprava | H3 |
| `Diablo4.WinUI/App.xaml.cs` | úprava | H4, L4 |
| `Diablo4.WinUI/Services/ProcessMonitor.cs` | úprava | M1, M2, M3, M6 |
| `Diablo4.WinUI/Views/WeekendMotivationDialog.xaml.cs` | úprava | M4, L5 |
| `Diablo4.WinUI/ViewModels/MainViewModel.cs` | úprava | M5 |
| `Diablo4.WinUI/Views/UpdateNotificationWindow.cs` | úprava | L1 |
| `Diablo4.WinUI/Helpers/UpdateSourcePolicy.cs` | úprava | L3 |

## Rizika

| Riziko | Pravděpodobnost | Dopad | Mitigace |
|--------|-----------------|-------|----------|
| Změna HttpClient logiky rozbije stávající testy | nízká | střední | Spustit testy po každé změně |
| Přidání `volatile` nestačí pro full thread safety | nízká | nízká | `volatile` je správná oprava pro bool flag reads/writes |
| Encoding fix `WeekendMotivationDialog` zavede regresi | nízká | nízká | Porovnat chování před/po buildu |

## Ověření

- Build `Diablo4.WinUI` úspěšný
- `dotnet test ... -p:Platform=x64` – všechny testy zelené (21+)
- Aplikace se spustí, tray funguje, monitoring funguje

## Rollback

`git revert` na jednotlivé commity nebo `git checkout HEAD -- <soubor>`

## Poznámky po dokončení

> Doplnit po realizaci.
