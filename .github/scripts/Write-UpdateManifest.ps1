[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Version,

    [Parameter(Mandatory)]
    [string]$DownloadUrl,

    [Parameter()]
    [AllowEmptyString()]
    [string]$ReleaseNotes = '',

    [Parameter()]
    [AllowEmptyString()]
    [string]$MinimumVersion = '',

    [Parameter(Mandatory)]
    [string]$ReleaseDate,

    [Parameter(Mandatory)]
    [string]$OutputPath,

    [Parameter()]
    [string]$InstallerPath,

    [Parameter()]
    [AllowEmptyString()]
    [string]$Sha256 = ''
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($InstallerPath) -and [string]::IsNullOrWhiteSpace($Sha256)) {
    $Sha256 = ''
}
elseif (-not [string]::IsNullOrWhiteSpace($InstallerPath)) {
    if (-not (Test-Path -LiteralPath $InstallerPath -PathType Leaf)) {
        throw "Installer soubor '$InstallerPath' nebyl nalezen."
    }

    $Sha256 = (Get-FileHash -Path $InstallerPath -Algorithm SHA256).Hash.ToUpperInvariant()
}
else {
    $Sha256 = $Sha256.ToUpperInvariant()
}

$manifest = [ordered]@{
    LatestVersion = $Version
    DownloadUrl = $DownloadUrl
    ReleaseNotes = $ReleaseNotes
    MinimumVersion = $MinimumVersion
    ReleaseDate = $ReleaseDate
    Sha256 = $Sha256
}

$manifest | ConvertTo-Json | Set-Content -Path $OutputPath -Encoding utf8NoBOM
Write-Host "Manifest '$OutputPath' byl aktualizován."