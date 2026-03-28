param(
    [Parameter(Mandatory = $true)]
    [string]$Version
)

$ErrorActionPreference = 'Stop'

$parsedVersion = $null
if (-not [Version]::TryParse($Version, [ref]$parsedVersion)) {
    throw "Neplatný formát verze '$Version'. Použij například 1.0.0.0."
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot '..'))
$projectPath = Join-Path $repoRoot 'Diablo4.WinUI\Diablo4.WinUI.csproj'
$publishProfile = 'installer-win-x64'
$publishDir = Join-Path $repoRoot 'artifacts\publish\win-x64'
$installerScript = Join-Path $scriptRoot 'Diablo4.WinUI.iss'

$isccCommand = Get-Command 'iscc' -ErrorAction SilentlyContinue
if ($null -eq $isccCommand) {
    $candidatePaths = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')
    )

    $resolvedPath = $candidatePaths | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
    if ($null -eq $resolvedPath) {
        throw 'Inno Setup Compiler (ISCC.exe) nebyl nalezen. Nainstaluj Inno Setup 6 a přidej ISCC do PATH, nebo použij standardní instalační cestu.'
    }

    $isccPath = $resolvedPath
}
else {
    $isccPath = $isccCommand.Source
}

dotnet publish $projectPath -c Release -p:PublishProfile=$publishProfile -p:Version=$Version -p:AssemblyVersion=$Version -p:FileVersion=$Version -p:InformationalVersion=$Version

$mainExecutable = Join-Path $publishDir 'Diablo4.WinUI.exe'
if (-not (Test-Path $mainExecutable)) {
    throw "Publish výstup neobsahuje '$mainExecutable'."
}

if (-not $repoRoot.EndsWith('\')) { $repoRoot += '\' }
if (-not $publishDir.EndsWith('\')) { $publishDir += '\' }

& $isccPath "/DAppVersion=$Version" "/DRepoRoot=$repoRoot" "/DPublishDir=$publishDir" $installerScript
if ($LASTEXITCODE -ne 0) {
    throw "Vytvoření installeru selhalo s návratovým kódem $LASTEXITCODE."
}
