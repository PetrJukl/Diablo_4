# Release checklist pro `Diablo4.WinUI`

## Před releasem
- ověř lokálně `dotnet publish` pro profil `installer-win-x64`
- pokud buildíš lokálně, měj nainstalovaný `Inno Setup 6`
- použij tag ve formátu `vX.Y.Z.W`, například `v1.0.0.0`

## Publikace releasu
- vytvoř a pushni tag, například `git tag v1.0.0.0` a `git push origin v1.0.0.0`
- workflow `Build release installer` vytvoří publish výstup, sestaví `Inno Setup` installer `.exe`, spočítá jeho `SHA-256`, založí GitHub release a automaticky aktualizuje `update-manifest.json`
- pokud release notes nebo asset upravíš ručně po vydání, workflow `Update release manifest` znovu sesynchronizuje `update-manifest.json` včetně `Sha256`

## Ověření po publikaci
- zkontroluj workflow `Build release installer` v GitHub Actions
- zkontroluj workflow `Update release manifest` v GitHub Actions
- ověř veřejnou URL `https://raw.githubusercontent.com/PetrJukl/Diablo_4/main/update-manifest.json`
- ověř, že `DownloadUrl` v manifestu ukazuje na `.exe` asset z posledního releasu
- ověř, že `Sha256` v manifestu odpovídá publikovanému installeru
- ověř stažení balíčku mimo Visual Studio

## Poznámka
- lokální fallback je `installer/build-installer.ps1 -Version X.Y.Z.W`
- lokální hash lze dopočítat příkazem `Get-FileHash <cesta-k-installeru> -Algorithm SHA256`
- nepodepsaný installer může zobrazit SmartScreen varování, ale instalační a odinstalační flow zůstává funkční
