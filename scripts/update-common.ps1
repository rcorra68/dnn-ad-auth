# update-common.ps1
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Resolve-Path "$scriptRoot\.."

# Read current dnn-common version
$commonVersionFile = Resolve-Path "$root\..\dnn-common\version.txt"
$version = (Get-Content $commonVersionFile -Raw).Trim()
Write-Host "Vvf.Modules.Common version: $version" -ForegroundColor Cyan

# Verify .nupkg is already present in _packages
$packagesDir = Resolve-Path "$root\..\_packages"
$nupkgPath   = Join-Path $packagesDir "Vvf.Modules.Common.$version.nupkg"
if (-not (Test-Path $nupkgPath)) {
    Write-Host "  .nupkg not found in _packages: run dnn-common build first." -ForegroundColor Red
    exit 1
}
Write-Host "  .nupkg found: Vvf.Modules.Common.$version.nupkg" -ForegroundColor DarkGray

# Remove old Vvf.Modules.Common versions from packages folder
Write-Host "Cleaning old Vvf.Modules.Common versions..." -ForegroundColor Cyan
$localPackages = Join-Path $root "packages"
Get-ChildItem -Path $localPackages -Filter "Vvf.Modules.Common.*" -Directory | Where-Object { $_.Name -ne "Vvf.Modules.Common.$version" } | ForEach-Object {
    Remove-Item $_.FullName -Recurse -Force
    Write-Host "  $($_.Name): removed" -ForegroundColor DarkGray
}

# Update packages.config references
Write-Host "Updating packages.config..." -ForegroundColor Cyan
Get-ChildItem -Path $root -Recurse -Filter "packages.config" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    $updated = $content -replace 'id="Vvf\.Modules\.Common" version="[^"]+"', "id=`"Vvf.Modules.Common`" version=`"$version`""
    if ($content -ne $updated) {
        [System.IO.File]::WriteAllText($_.FullName, $updated, [System.Text.Encoding]::UTF8)
        Write-Host "  $($_.Directory.Name): updated" -ForegroundColor Yellow
    } else {
        Write-Host "  $($_.Directory.Name): already up to date" -ForegroundColor DarkGray
    }
}

# Update .csproj references
Write-Host "Updating .csproj files..." -ForegroundColor Cyan
Get-ChildItem -Path $root -Recurse -Filter "*.csproj" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    $updated = $content -replace 'Vvf\.Modules\.Common\.\d+\.\d+\.\d+', "Vvf.Modules.Common.$version"
    if ($content -ne $updated) {
        [System.IO.File]::WriteAllText($_.FullName, $updated, [System.Text.Encoding]::UTF8)
        Write-Host "  $($_.BaseName): updated" -ForegroundColor Yellow
    } else {
        Write-Host "  $($_.BaseName): already up to date" -ForegroundColor DarkGray
    }
}

# NuGet restore (warnings suppressed)
Write-Host "Running NuGet restore..." -ForegroundColor Cyan
$nuget = Resolve-Path "$root\..\\_tools\\nuget.exe"
& "$nuget" restore -Verbosity quiet 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "NuGet restore failed." -ForegroundColor Red
    exit 1
}
Write-Host "Done. Vvf.Modules.Common updated to $version" -ForegroundColor Green