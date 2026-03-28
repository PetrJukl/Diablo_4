# Plán: Automatizace release manifestu a SHA-256

**Soubor:** `.github/plans/PLAN-2026-03-28-automatizace-release-manifestu-sha256.md`  
**Datum:** 2026-03-28  
**Stav:** `dokončeno`

---

## Cíl

> Doplnit do stávajícího release procesu plně automatické dopočítání `SHA-256` pro publikovaný installer, automatické vygenerování `update-manifest.json` a publikaci obou artefaktů bez ručních zásahů po vytvoření tagu/releasu.

## Kontext

> V repozitáři už existují dva workflow soubory: `build-release-installer.yml` a `update-release-manifest.yml`. První workflow dnes vytvoří installer, založí GitHub release a následně zapíše `update-manifest.json`. Druhé workflow po publikaci nebo editaci releasu znovu synchronizuje manifest podle release assetu. Aplikace už nyní umí `Sha256` z manifestu validovat, ale release pipeline ho zatím negeneruje. Cílem je dotáhnout to do stavu, kdy pro uživatele bude release prakticky automatický: push tagu spustí celý bezpečný update chain.

## Rozsah

> Dotčené oblasti:
> - `.github/workflows/build-release-installer.yml`
> - `.github/workflows/update-release-manifest.yml`
> - `update-manifest.json`
> - `.github/release-checklist.md`
> - případně pomocný PowerShell skript v repozitáři, pokud se ukáže vhodné část logiky sdílet

## Návrh řešení

### Varianta A – Jedno hlavní release workflow + jedno doplňkové resync workflow

> Zachovat `build-release-installer.yml` jako hlavní autoritativní workflow pro release. To po buildu spočítá `SHA-256`, vytvoří release asset, zapíše kompletní `update-manifest.json` včetně `Sha256` a pushne změnu do `main`. `update-release-manifest.yml` zůstane jako doplňkový fallback pro scénáře, kdy se release ručně upraví po publikaci, ale bude používat stejnou logiku a stejné schéma manifestu.

**Výhody**
- minimální zásah do současného řešení,
- zachování již fungujícího release triggeru přes tag,
- jasný autoritativní tok,
- malý diff a menší riziko regresí.

**Nevýhody**
- část odpovědnosti zůstane rozdělená mezi dvě workflow,
- je potřeba hlídat, aby obě workflow generovala manifest stejně.

### Varianta B – Jediné workflow pro release i následnou synchronizaci

> Sloučit odpovědnost do jednoho workflow a druhé odstranit. Veškerá logika buildu, releasu, výpočtu `SHA-256` a aktualizace manifestu bude jen v jednom souboru.

**Výhody**
- jedno místo pravdy,
- nižší dlouhodobá údržba.

**Nevýhody**
- větší změna proti současnému stavu,
- ztratí se jednoduchý fallback při ruční editaci release notes nebo assetů.

**Zvolená varianta:** A – navazuje na již fungující CI, drží malý diff a přitom přidá požadovanou automatiku.

## Kroky implementace

- [x] Upřesnit autoritativní release tok a zdroj pravdy pro `update-manifest.json`
- [x] Doplnit do `build-release-installer.yml` výpočet `SHA-256` pro publikovaný installer
- [x] Rozšířit generování `update-manifest.json` o pole `Sha256`
- [x] Sjednotit schéma manifestu mezi oběma workflow
- [x] Upravit `update-release-manifest.yml`, aby při resynchronizaci zachovávalo stejnou strukturu včetně `Sha256`
- [x] Rozhodnout, zda společnou logiku ponechat inline nebo přesunout do pomocného skriptu
- [x] Aktualizovat `update-manifest.json` sample / baseline v repozitáři
- [x] Aktualizovat `.github/release-checklist.md` pro nový automatický tok
- [x] Ověřit workflow syntaxi a lokální konzistenci souborů
- [x] Dopsat review a výsledný release postup

## Dotčené soubory

| Soubor | Typ změny | Poznámka |
|--------|-----------|----------|
| `.github/workflows/build-release-installer.yml` | úprava | výpočet `SHA-256`, generování manifestu, release automatika |
| `.github/workflows/update-release-manifest.yml` | úprava | fallback resync manifestu se stejným schématem |
| `update-manifest.json` | úprava | doplnění `Sha256` do baseline manifestu |
| `.github/release-checklist.md` | úprava | popis nového automatického release toku |
| `scripts/...` nebo `installer/...` | volitelné přidání | pouze pokud bude vhodné sdílet PowerShell logiku |

## Rizika

| Riziko | Pravděpodobnost | Dopad | Mitigace |
|--------|-----------------|-------|----------|
| Manifest v jednom workflow bude mít jiné schéma než ve druhém | střední | vysoký | sjednotit generování přes jednu sdílenou logiku nebo přesně stejnou strukturu |
| `SHA-256` se spočítá z jiného souboru než z publikovaného assetu | nízká | vysoký | hash počítat až z finálního souboru určeného pro upload |
| Release commit do `main` způsobí vedlejší trigger nebo kolizi | střední | střední | držet trigger jen na tagy/release a commit message mít jednoznačnou |
| Ručně upravený release ztratí synchronizaci s manifestem | střední | střední | ponechat fallback workflow `update-release-manifest.yml` |

## Ověření

> Ověření proběhne přes:
> - kontrolu YAML workflow souborů,
> - ověření, že `update-manifest.json` obsahuje `Sha256`,
> - testovací release přes tag `vX.Y.Z.W` v bezpečném scénáři,
> - kontrolu, že `DownloadUrl` a `Sha256` odpovídají stejnému release assetu,
> - následnou manuální kontrolu update flow v aplikaci.

## Rollback

> Pokud by automatizace dělala problémy, rollback spočívá ve vrácení workflow na současný stav bez `Sha256` a dočasném ponechání stávajícího release procesu. Aplikace už podporuje i prázdné `Sha256`, takže rollback neblokuje běh klienta.

## Poznámky po dokončení

> Autoritativní release tok zůstal v `build-release-installer.yml`. Po sestavení installeru workflow najde finální `.exe`, publikuje ho jako release asset a následně přes sdílený skript `./.github/scripts/Write-UpdateManifest.ps1` zapíše `update-manifest.json` včetně `Sha256`. Fallback workflow `update-release-manifest.yml` při ruční úpravě releasu stáhne publikovaný asset, z něj znovu spočítá `SHA-256` a zapíše stejnou strukturu manifestu.

> Společná logika byla záměrně přesunuta do pomocného PowerShell skriptu, aby obě workflow generovala manifest shodně a bez duplikace. Baseline `update-manifest.json` byl aktualizován o reálně ověřený `SHA-256` pro aktuální release `v1.0.0.8`.

## Review

> Upravené soubory:
> - `.github/workflows/build-release-installer.yml`
> - `.github/workflows/update-release-manifest.yml`
> - `.github/scripts/Write-UpdateManifest.ps1`
> - `.github/release-checklist.md`
> - `update-manifest.json`

> Ověření:
> - sdílený skript byl lokálně spuštěn proti aktuálnímu release installeru a vygeneroval manifest s očekávaným `Sha256`,
> - `run_build` nad řešením proběhl úspěšně,
> - baseline manifest nyní obsahuje `Sha256` hodnotu `C86AD42B01FFF615DCF8DD22C5AFA786AB2DCD4F2CFCB333C0BD64CE1F806F0D` pro release `v1.0.0.8`.

> Otevřený bod:
> - plné end-to-end ověření samotných GitHub Actions vyžaduje vytvoření testovacího tagu / releasu v repozitáři, protože lokálně nelze simulovat celý GitHub release životní cyklus beze zbytku.
