# Plán: GitHub release a update flow pro Diablo4.WinUI

**Soubor:** `.github/plans/PLAN-2026-03-26-github-release-update-flow.md`  
**Datum:** 2026-03-26  
**Stav:** `návrh`

---

## Cíl

> Připravit repozitář, release artefakty a update mechaniku tak, aby `Diablo4.WinUI` po publikaci uměla kontrolovat a stahovat aktualizace z GitHubu bez ručních zásahů do kódu při každém release.

## Kontext

> Aplikace už běží stabilně v `Debug | x64`, tray integrace byla vrácena a další krok je přenést projekt do vlastního GitHub repozitáře, doplnit reálné URL pro update manifest a ověřit distribuční scénář.

## Rozsah

> `Diablo4.WinUI/Helpers/AppConfiguration.cs`, `Diablo4.WinUI/Services/UpdateService.cs`, release/publish konfigurace projektu, GitHub repozitář a release artefakty.

## Návrh řešení

### Varianta A – manifest a release assety ve stejném GitHub repozitáři

> Zdrojový kód, `manifest.json` i instalační balíčky budou v jednom repozitáři. Aplikace bude číst manifest z veřejné URL a stahovat release asset ze stejného repa.

**Výhody:** jednoduchá správa, minimum moving parts, snadné verzování.  
**Nevýhody:** release artefakty a kód jsou svázané do jednoho workflow.

### Varianta B – samostatné release repo

> Kód zůstane v jednom repu a manifest/release assety budou hostované odděleně.

**Výhody:** oddělení zdrojáků od distribuce.  
**Nevýhody:** vyšší režie a více konfigurace.

**Zvolená varianta:** A – nejjednodušší start, vhodný pro první funkční release.

## Kroky implementace

- [ ] Založit cílový GitHub repozitář pro `Diablo4.WinUI` a nastavit remote.
- [ ] Navrhnout finální strukturu release artefaktů a `update-manifest.json`.
- [ ] Doplnit reálnou URL manifestu do `AppConfiguration`.
- [ ] Ověřit packaged publish/build scénář pro distribuční balíček.
- [ ] Upravit `UpdateService`, pokud bude potřeba pro finální GitHub Releases workflow.
- [ ] Manuálně ověřit dostupnost manifestu a stažení balíčku.
- [ ] Zapsat výsledky a vytvořit release checklist.

## Dotčené soubory

| Soubor | Typ změny | Poznámka |
|--------|-----------|----------|
| `Diablo4.WinUI/Helpers/AppConfiguration.cs` | úprava | reálná GitHub URL manifestu |
| `Diablo4.WinUI/Services/UpdateService.cs` | úprava | případné doladění release/update logiky |
| `Diablo4.WinUI/Diablo4.WinUI.csproj` | úprava | publish/release nastavení podle zvolené distribuce |
| `.github/log.log` | úprava | pracovní záznam průběhu |
| `.github/plans/PLAN-2026-03-26-github-release-update-flow.md` | přidání | navazující plán |

## Rizika

| Riziko | Pravděpodobnost | Dopad | Mitigace |
|--------|-----------------|-------|----------|
| Neveřejné URL manifestu nebude z aplikace dostupné | střední | vysoký | použít veřejné release assety nebo veřejný manifest |
| Packaged build bude mít jiné požadavky než debug/unpackaged | střední | střední | ověřit publish scénář zvlášť mimo debug workflow |
| Struktura manifestu nebude odpovídat finálním release assetům | střední | střední | předem sjednotit schema a názvy souborů |

## Ověření

> Ověřit veřejné URL manifestu, stažení release assetu, packaged build a chování update kontroly po spuštění aplikace.

## Rollback

> Ponechat aktuální placeholder URL, vrátit poslední změny v `AppConfiguration` a `UpdateService` a pokračovat bez update kanálu do další iterace.

## Poznámky po dokončení

> Doplnit po realizaci: URL repozitáře, finální manifest URL, schema manifestu, ověřený release postup a případné zbývající kroky.
