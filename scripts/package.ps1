param(
    [string]$Version
)

$repoRoot = Split-Path $PSScriptRoot -Parent
[string]$ConfigPath = Join-Path $repoRoot "package.config.psd1"

. "$PSScriptRoot\packaging\Write-Helper.ps1"
. "$PSScriptRoot\packaging\Load-Config.ps1"
. "$PSScriptRoot\packaging\Build-Paths.ps1"
. "$PSScriptRoot\packaging\Copy-Dlls.ps1"
. "$PSScriptRoot\packaging\Copy-Providers.ps1"
. "$PSScriptRoot\packaging\Copy-Documentations.ps1"
. "$PSScriptRoot\packaging\Copy-Resources.ps1"
. "$PSScriptRoot\packaging\Build-Manifest.ps1"
. "$PSScriptRoot\packaging\New-ZipPackage.ps1"
. "$PSScriptRoot\packaging\Get-Version.ps1"
. "$PSScriptRoot\packaging\Convert-ChangelogToHtml.ps1"

if (-not $Version) {
    $Version = Get-NextVersion
}

Write-Host "Building version: " -NoNewline -ForegroundColor Cyan
Write-Host "$Version" -ForegroundColor Green

$config = Load-Config $ConfigPath
$paths = Build-Paths $config $Version

$dlls = Copy-Dlls -Paths $paths -Config $config
$providers = Copy-Providers -Paths $paths
Copy-Documentations -Paths $paths
Copy-Resources -Paths $paths
Build-Manifest -Paths $paths -Config $config -Version $Version -Dlls $Dlls -Providers $Providers
New-ZipPackage -Paths $paths
