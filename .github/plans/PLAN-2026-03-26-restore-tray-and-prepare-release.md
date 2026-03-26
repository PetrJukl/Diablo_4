# Plán: Obnovení tray integrace a příprava navazujících kroků

**Soubor:** `.github/plans/PLAN-2026-03-26-restore-tray-and-prepare-release.md`  
**Datum:** 2026-03-26  
**Stav:** `dokončeno`

---

## Cíl

> Vrátit do `Diablo4.WinUI` stabilní tray integraci bez návratu startup pádů, commitnout aktuálně ověřené změny a připravit navazující plán pro další den, aby šlo pokračovat na GitHub release/update flow bez ztráty kontextu.

## Kontext

> WinUI aplikace už běží stabilně v `Debug | x64` přes `Unpackaged` profil. Tray integrace byla během diagnostiky dočasně odebrána kvůli COM pádům při startu. Uživatel chce tray vrátit, commitnout rozpracované změny a mít připravený plán pro další pokračování.

## Rozsah

> `Diablo4.WinUI`, `.github/log.log`, `.github/plans/*`, Git stav repozitáře.

## Návrh řešení

### Varianta A – programová tray integrace po aktivaci okna

> Tray ikonu vytvořit až po inicializaci hlavního okna v code-behind, ne v XAML resources. Tím se oddělí WinUI start od inicializace `H.NotifyIcon`, zachová se možnost fallbacku a vrátí se menu/restore/hide-on-close chování.

**Výhody:** menší riziko startup pádu, jednodušší diagnostika, menší vazba na XAML init.  
**Nevýhody:** o něco víc code-behind logiky.

### Varianta B – návrat tray prvku do XAML

> Vrátit `TaskbarIcon` zpět do XAML resources a spoléhat, že po opravě Windows App SDK init už nebude padat.

**Výhody:** deklarativní řešení.  
**Nevýhody:** vyšší riziko návratu startup pádu a horší diagnostika.

**Zvolená varianta:** A – bezpečnější a lépe kontrolovatelná během dalšího ladění.

## Kroky implementace

- [x] Vrátit tray integraci programově po aktivaci okna.
- [x] Zachovat `restore`, `exit` a `hide-on-close` chování s fallbackem při selhání tray inicializace.
- [x] Ověřit build a základní spuštění aplikace.
- [x] Zapsat průběh a výsledek do `.github/log.log`.
- [x] Commitnout aktuální změny.
- [x] Připravit navazující plán pro GitHub release/update flow.

## Dotčené soubory

| Soubor | Typ změny | Poznámka |
|--------|-----------|----------|
| `Diablo4.WinUI/MainWindow.xaml.cs` | úprava | návrat tray integrace a životní cyklus okna |
| `Diablo4.WinUI/MainWindow.xaml` | úprava | případné zjednodušení resources |
| `Diablo4.WinUI/Diablo4.WinUI.csproj` | úprava | zachování stabilního debug/unpackaged režimu |
| `.github/log.log` | úprava | průběžný pracovní záznam |
| `.github/plans/PLAN-2026-03-26-restore-tray-and-prepare-release.md` | přidání | plán a navazující poznámky |

## Rizika

| Riziko | Pravděpodobnost | Dopad | Mitigace |
|--------|-----------------|-------|----------|
| Tray knihovna znovu vyvolá pád při startu | střední | vysoký | inicializovat až po aktivaci, přidat fallback bez tray |
| Hide-on-close znepřístupní okno bez funkční ikony | nízká | vysoký | zapnout hide-on-close jen při úspěšné tray inicializaci |
| Commit zahrne nechtěné artefakty | střední | střední | zkontrolovat `git status` a commitnout jen relevantní soubory |

## Ověření

> `run_build`, spuštění WinUI aplikace v `Debug | x64`, kontrola, že proces běží, okno lze schovat/obnovit a nedochází k návratu COMException.

## Rollback

> Vrátit tray změny posledním commitem nebo `git restore` na dotčených souborech; při nouzi ponechat aplikaci ve stabilním stavu bez tray integrace.

## Poznámky po dokončení

> Tray integrace byla vrácena programově v `MainWindow.xaml.cs`, build je úspěšný a automatická kontrola potvrdila, že po zavření okna proces běží dál. Navazující práce je připravena v samostatném plánu pro GitHub release/update flow. Aktuální změny byly commitnuty jako `Restore tray integration and add release plan`.
