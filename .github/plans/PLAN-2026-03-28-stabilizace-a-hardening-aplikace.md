# Plán: Stabilizace a hardening aplikace

**Soubor:** `.github/plans/PLAN-2026-03-28-stabilizace-a-hardening-aplikace.md`  
**Datum:** 2026-03-28  
**Stav:** `návrh`

---

## Cíl

> Zvýšit odolnost `Diablo4.WinUI` proti pádům, zamrzání UI a neřízenému chování tak, aby běžné chyby v aplikaci nevedly k negativnímu dopadu na chod systému. Není reálně možné garantovat, že uživatelská aplikace nikdy neovlivní celý počítač, ale je možné výrazně snížit riziko přes řízené time-outy, zrušitelné operace, jednotné exception handling strategie, omezení blokujících smyček, bezpečné ukončování a diagnostiku.

## Kontext

> Audit řešení ukázal, že hlavní aktivní aplikace `Diablo4.WinUI` (.NET 8) obsahuje rizikové vzory jako nekonečné retry smyčky s `Thread.Sleep`, tiché `catch` bloky, neřízené spuštění background práce a chybějící automatické testy. Aktuální build řešení je zelený. Legacy projekt `Diablo 4` je mimo rozsah tohoto plánu.

## Rozsah

> Audit a návrh se týká zejména složek `Diablo4.WinUI/`, případného test projektu pro WinUI logiku a `.github/plans/`.

## Návrh řešení

### Varianta A – Fázovaný hardening `Diablo4.WinUI`

> Postupně odstranit kritická místa, která mohou vést k zamrzání, ztrátě diagnostiky nebo nekorektnímu ukončení v `Diablo4.WinUI`. Stabilizační zásahy rozdělit do menších fází: exception handling, bezpečné I/O a retry, řízené background operace, testy a ověření.

**Výhody**
- menší a lépe řiditelný rozsah,
- rychlejší cesta k bezpečnější produkční aplikaci,
- nižší riziko regresí mimo WinUI část řešení.

**Nevýhody**
- legacy projekt zůstává mimo tento konkrétní plán,
- sdílené poznatky nebude možné automaticky promítnout do druhé aplikace bez samostatné práce.

### Varianta B – Jednorázový refactor WinUI monitoringu

> V rámci jedné větší změny přepracovat monitoring procesů, práci se soubory, update flow a dialogové chování v `Diablo4.WinUI`.

**Výhody**
- rychlejší dosažení cílového stavu,
- méně mezikroků v implementaci.

**Nevýhody**
- větší diff,
- vyšší riziko regresí,
- horší rollback při problémech.

**Zvolená varianta:** A – seniorní a bezpečnější postup je stabilizovat `Diablo4.WinUI` po menších, dobře ověřitelných krocích.

## Kroky implementace

- [ ] Zavést centrální crash-handling a nouzové logování pro UI i background chyby bez tichého pohlcení výjimek.
- [ ] Nahradit blokující retry smyčky s `Thread.Sleep` za omezené retry politiky s timeoutem, cancellation tokenem a bezpečným fallbackem.
- [ ] Omezit rizikové operace nad procesy, soubory a hledáním EXE tak, aby měly timeout, throttling a diagnostiku.
- [ ] Oddělit monitorování procesů od UI timerů a minimalizovat práci na UI threadu.
- [ ] Přidat guardy proti duplicitním oknům/dialogům a proti race conditions při zavírání aplikace.
- [ ] Přidat automatické testy alespoň pro parsování logu, výpočet týdnů, retry chování a update validaci.
- [ ] Ověřit scénáře: start aplikace, update check, ztráta internetu, zamčený log soubor, zavírání okna, tray režim, spuštění víc instancí, weekend dialog.

## Dotčené soubory

| Soubor | Typ změny | Poznámka |
|--------|-----------|----------|
| `Diablo4.WinUI/App.xaml.cs` | úprava | centrální exception handling a bezpečné spuštění update flow |
| `Diablo4.WinUI/MainWindow.xaml.cs` | úprava | řízené zavírání, tray režim, ochrana proti race conditions |
| `Diablo4.WinUI/ViewModels/MainViewModel.cs` | úprava | omezení chyb v timer tick logice a méně práce na UI threadu |
| `Diablo4.WinUI/Services/ProcessMonitor.cs` | úprava | retry, timeouty, IO, konkurence, throttling |
| `Diablo4.WinUI/Services/UpdateService.cs` | úprava | bezpečnější síťové a instalační operace |
| `Diablo4.WinUI/Views/WeekendMotivationDialog.xaml.cs` | úprava | timeouty, bezpečné hledání EXE, lepší error handling |
| `Diablo4.WinUI/Helpers/ProcessHelper.cs` | úprava | bezpečné řešení víc instancí bez tvrdého kill přístupu |
| `Diablo4.WinUI.Tests/*` nebo ekvivalent | přidání | testy stabilitní a validační logiky |

## Rizika

| Riziko | Pravděpodobnost | Dopad | Mitigace |
|--------|-----------------|-------|----------|
| Změna monitoringu rozbije současné chování zápisu času | střední | vysoký | nejdřív přidat testy na formát a výpočty, potom refaktor |
| Příliš agresivní retry/timeout omezí funkčnost na pomalejších strojích | střední | střední | nastavitelné limity, logování a manuální ověření |
| Hledání herních EXE zůstane pomalé | vysoká | střední | cache nalezených cest, omezení search roots, možnost konfigurace |
| Update flow může zavřít aplikaci v nevhodný okamžik | nízká | vysoký | zablokovat souběh s kritickými akcemi a zavádět explicitní stav update operace |

## Ověření

> 1. Build projektu `Diablo4.WinUI` bez chyb.
> 2. Automatické testy pro ne-UI logiku.
> 3. Manuální scénáře: spuštění bez internetu, zamčený log soubor, spuštěná hra, více instancí, zavření okna při tray režimu, otevření a zavření weekend dialogu, neexistující EXE, neplatný update manifest.
> 4. Ověřit, že při simulované chybě dojde k řízenému logování a aplikace se neukončí nekorektně ani nenechá běžet blokující práci.

## Rollback

> Změny vrátit po menších commitech přes `git revert` po jednotlivých fázích. Kritické refaktory dělat odděleně od testů a od diagnostiky, aby rollback zůstal levný.

## Poznámky po dokončení

> Doplnit po realizaci: které rizikové vzory byly odstraněny, jaké timeouty a retry limity se osvědčily a jaké další kroky ještě zbývají pro `Diablo4.WinUI`.
