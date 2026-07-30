# Mapa projektu

Tento dokument zatím popisuje release a aktualizační část architektury projektu.

## Nasazení aplikace

### Tok standardního releasu

```text
commit na main
  -> push main na GitHub
  -> vytvoření a push tagu vX.Y.Z.W
  -> GitHub Actions: Build release installer
  -> publish aplikace pro win-x64
  -> sestavení Inno Setup instalátoru
  -> GitHub Release s .exe souborem
  -> výpočet SHA-256 a aktualizace update-manifest.json
  -> bot commit manifestu do main
  -> aplikace zjistí novou verzi z veřejného manifestu
```

Tag se vytváří až po pushnutí příslušného commitu do `main`. Workflow se při
zápisu manifestu přepíná na vzdálený `main`, proto by tag neměl předběhnout
push zdrojového commitu.

### Soubory zapojené do nasazení

| Soubor | Úloha |
| --- | --- |
| `.github/workflows/build-release-installer.yml` | Hlavní release pipeline spouštěná pushem tagu `v*`. |
| `installer/build-installer.ps1` | Spustí `dotnet publish`, ověří výstup a zavolá Inno Setup Compiler. |
| `Diablo4.WinUI/Properties/PublishProfiles/installer-win-x64.pubxml` | Release publish profil pro Windows x64. |
| `installer/Diablo4.WinUI.iss` | Definice instalátoru a názvu `KontrolaParbySetup-X.Y.Z.W.exe`. |
| `.github/scripts/Write-UpdateManifest.ps1` | Vytvoří manifest a spočítá SHA-256 instalátoru. |
| `update-manifest.json` | Veřejná informace o poslední verzi, URL instalátoru a jeho SHA-256. |
| `Diablo4.WinUI/Helpers/AppConfiguration.cs` | Obsahuje veřejnou URL manifestu na větvi `main`. |
| `Diablo4.WinUI/Services/UpdateService.cs` | Porovná verze, stáhne instalátor a před spuštěním ověří SHA-256. |
| `.github/workflows/update-release-manifest.yml` | Pojistka pro ručně publikovaný nebo následně upravený GitHub Release. |

## Postup vydání nové verze

Ve všech příkazech nahraď `X.Y.Z.W` skutečnou novou čtyřdílnou verzí,
například `1.0.0.29`. Stejný tag nesmí na GitHubu už existovat.

### 1. Připrav a ověř změny

Pracuj na čistém a synchronizovaném `main`. Zkontroluj, že commit obsahuje
jen soubory určené do releasu.

```powershell
git status -sb
git diff
dotnet test "Diablo4.WinUI.Tests\Diablo4.WinUI.Tests.csproj" --runtime win-x64
```

Release publish lze předem ověřit bez sestavení instalátoru:

```powershell
dotnet publish "Diablo4.WinUI\Diablo4.WinUI.csproj" -c Release `
  -p:PublishProfile=installer-win-x64 `
  -p:Version=X.Y.Z.W `
  -p:AssemblyVersion=X.Y.Z.W `
  -p:FileVersion=X.Y.Z.W `
  -p:InformationalVersion=X.Y.Z.W
```

Úplný lokální fallback vyžaduje nainstalovaný Inno Setup 6:

```powershell
.\installer\build-installer.ps1 -Version X.Y.Z.W
```

Výsledný instalátor bude v
`artifacts\installer\KontrolaParbySetup-X.Y.Z.W.exe`.

### 2. Commitni a nejdřív pushni `main`

```powershell
git add -- cesta\k\upravenému\souboru
git diff --cached
git commit -m "popis změny"
git push origin main
```

### 3. Vytvoř a pushni release tag

Podporovaný formát je `vX.Y.Z.W`.

```powershell
git tag vX.Y.Z.W
git show --no-patch vX.Y.Z.W
git push origin vX.Y.Z.W
```

Push tagu spustí workflow `Build release installer`. Ručně se při standardním
postupu nevytváří GitHub Release ani neupravuje `update-manifest.json`.

### 4. Co udělá hlavní workflow

1. Z tagu odstraní počáteční `v` a ověří formát verze.
2. Na `windows-latest` připraví .NET 8 a Inno Setup 6.
3. Spustí `installer/build-installer.ps1` s verzí z tagu.
4. Publikuje `KontrolaParbySetup-X.Y.Z.W.exe` do nového GitHub Release.
5. Vygeneruje release notes a spočítá SHA-256 instalátoru.
6. Přepne se na výchozí větev, zapíše `update-manifest.json` a pushne bot commit
   `chore: update release manifest for vX.Y.Z.W` do `main`.

### 5. Ověření po vydání

- Workflow `Build release installer` musí skončit jako úspěšné:
  `https://github.com/PetrJukl/Diablo_4/actions`.
- Release musí obsahovat právě vydaný `.exe`:
  `https://github.com/PetrJukl/Diablo_4/releases`.
- Ve veřejném manifestu musí souhlasit `LatestVersion`, `DownloadUrl` a
  `Sha256`:
  `https://raw.githubusercontent.com/PetrJukl/Diablo_4/main/update-manifest.json`.
- SHA-256 v manifestu musí odpovídat digestu publikovaného `.exe`.
- Po bot commitu bude lokální `main` o jeden commit pozadu. Pokud je pracovní
  strom čistý, synchronizuj jej:

```powershell
git pull --ff-only
git status -sb
```

## Druhý manifest workflow

Workflow `Update release manifest` reaguje na ruční publikování nebo úpravu
GitHub Release a znovu dopočítá manifest z instalačního assetu.

U standardního releasu vytvořeného hlavním workflow se tento druhý workflow
nemusí samostatně spustit. Release vytvořený pomocí `GITHUB_TOKEN` běžně
nevyvolá další workflow. Není to chyba: hlavní workflow aktualizuje manifest
samo ještě před svým dokončením.

## Důležité zásady

- Nejdřív pushni release commit do `main`, teprve potom tag.
- Čtyřdílnou verzi drž stejnou v tagu, assembly i názvu instalátoru.
- Při běžném releasu nemanipuluj ručně s `update-manifest.json`.
- Nevydávej tag, dokud neprojdou testy a release publish kontrola.
- Commit bez tagu nevytvoří nový instalátor ani GitHub Release.
