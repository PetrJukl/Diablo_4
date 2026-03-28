# Plán: Inno Setup installer a GitHub update flow pro Diablo4.WinUI

**Soubor:** `.github/plans/PLAN-2026-03-28-inno-setup-update-flow.md`  
**Datum:** 2026-03-28  
**Stav:** `v realizaci`

---

## Cíl

> Připravit zdarma použitelný instalační balík `.exe`, který si uživatel stáhne z GitHub Releases, normálně nainstaluje a aplikace po spuštění automaticky zkontroluje novou verzi přes veřejný manifest na GitHubu.

## Kontext

> Veřejné `MSIX` bez placeného důvěryhodného podpisu není vhodné pro pohodlnou distribuci koncovým uživatelům. Proto je cílová distribuce změněna na `Inno Setup` installer nad publish výstupem aplikace.

## Rozsah

> Publish výstup `Diablo4.WinUI`, instalační skript `Inno Setup`, GitHub release artefakty, `update-manifest.json`, release workflow a dokumentace k vydání.

## Návrh řešení

### Zvolená varianta – `Inno Setup` + GitHub Releases + `update-manifest.json`

> Aplikace bude publikovaná jako `win-x64` self-contained výstup do složky. Nad tímto výstupem vznikne `Inno Setup` installer `.exe`, který bude nahrávaný do GitHub Releases. Aplikace si při startu stáhne `update-manifest.json` z GitHubu a při novější verzi stáhne nový installer ze stejného releasu.

**Výhody:** zdarma, normální instalace i odinstalace, bez nutnosti placeného signing certifikátu pro základní distribuci.  
**Nevýhody:** při instalaci se může zobrazit Windows SmartScreen varování, pokud installer nebude podepsaný.

## Kroky implementace

- [x] Ověřit publish výstup, který bude vstupem pro installer.
- [x] Navrhnout cílové instalační umístění a uninstall flow.
- [x] Přidat `Inno Setup` skript pro vytvoření `.exe` installeru.
- [x] Upravit release workflow a manifest pro `.exe` asset.
- [x] Doplnit release dokumentaci a pracovní log.
- [ ] Ověřit build projektu a konzistenci release souborů.

## Dotčené soubory

| Soubor | Typ změny | Poznámka |
|--------|-----------|----------|
| `.github/plans/PLAN-2026-03-28-inno-setup-update-flow.md` | přidání | plán implementace |
| `Diablo4.WinUI/Helpers/AppConfiguration.cs` | kontrola | manifest URL už je nastavená |
| `Diablo4.WinUI/Services/UpdateService.cs` | kontrola / případná úprava | update musí stahovat `.exe` installer |
| `update-manifest.json` | úprava | release asset bude `.exe` installer |
| `.github/workflows/update-release-manifest.yml` | úprava | workflow musí brát `.exe` jako primární asset |
| `.github/release-checklist.md` | úprava | release kroky pro installer |
| `.github/log.log` | úprava | pracovní záznam |
| `installer/Diablo4.WinUI.iss` | přidání | Inno Setup skript |

## Rizika

| Riziko | Pravděpodobnost | Dopad | Mitigace |
|--------|-----------------|-------|----------|
| Publish výstup nebude obsahovat všechny potřebné soubory | střední | vysoký | ověřit publish složku a explicitně ji zahrnout do installeru |
| Update installer poběží nad spuštěnou aplikací | střední | střední | v installeru použít zavření aplikace před aktualizací |
| Windows SmartScreen bude varovat před nepodepsaným installerem | vysoký | střední | popsat v dokumentaci, případně později doplnit code signing |

## Ověření

> Ověřit lokální build, publish výstup, vytvoření `.exe` installeru a konzistenci `update-manifest.json` pro GitHub release.

## Rollback

> Vrátit release flow zpět na čistý publish output bez installeru a ponechat manifest URL bez aktivního update kanálu.

## Poznámky po dokončení

> Doplnit finální cestu k installeru, release postup, očekávanou instalační složku a známá omezení kolem nepodepsaného `.exe` souboru.

---

## Průběžný výsledek

- Publish vstup pro installer je ověřený jako unpackaged `win-x64` self-contained output do `artifacts/publish/win-x64`.
- Instalační skript používá per-user instalaci do `{localappdata}\Programs\Kontrola parby` a standardní Inno Setup uninstall.
- Lokální build installeru je připravený přes `installer/build-installer.ps1 -Version X.Y.Z.W`.
- GitHub Actions workflow `Build release installer` po pushi tagu `vX.Y.Z.W` vytvoří installer `.exe`, založí GitHub release a aktualizuje `update-manifest.json`.
