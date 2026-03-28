# Release checklist pro `Diablo4.WinUI`

## Před releasem
- aktualizuj verzi aplikace konzistentně v `Package.appxmanifest` a případně dalších release metadatech
- připrav instalační artefakt v podporovaném formátu: `.msix`, `.msixbundle`, `.appinstaller`, `.exe` nebo `.zip`
- použij tag ve formátu `vX.Y.Z.W`, například `v1.0.0.0`

## Publikace releasu
- vytvoř GitHub release nad správným tagem
- nahraj instalační asset do releasu
- vyplň release notes, protože se propíší do `update-manifest.json`
- publikuj release

## Ověření po publikaci
- zkontroluj workflow `Update release manifest` v GitHub Actions
- ověř veřejnou URL `https://raw.githubusercontent.com/PetrJukl/Diablo_4/main/update-manifest.json`
- ověř, že `DownloadUrl` v manifestu ukazuje na asset z posledního releasu
- ověř stažení balíčku mimo Visual Studio

## Poznámka
- workflow negeneruje instalační balíček, pouze aktualizuje `update-manifest.json` podle publikovaného GitHub releasu
