# =========================
# DNN BUILD ENGINE
# =========================

$ErrorActionPreference = "Stop"

Write-Host "=== DNN BUILD ENGINE START ===" -ForegroundColor Cyan

# 1. Root del repository (indipendente da dove lo lanci)
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Resolve-Path "$scriptRoot\.."

Write-Host "Root detected: $root"

# 2. Trova automaticamente la solution
$sln = Get-ChildItem -Path $root -Filter *.sln | Select-Object -First 1

if (-not $sln) {
    throw "Nessuna solution (.sln) trovata nella root: $root"
}

Write-Host "Solution found: $($sln.FullName)"

# 3. MSBuild legacy (.NET Framework 4.0)
$msbuild = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"

if (-not (Test-Path $msbuild)) {
    throw "MSBuild .NET 4.0 non trovato"
}

# 4. Build with improved property handling
Write-Host "Building solution..." -ForegroundColor Yellow

$buildArgs = @(
    $sln.FullName,
    "/p:Configuration=Release",
    "/p:Platform=Any CPU",
    "/t:Rebuild",
    "/p:VisualStudioVersion=10.0" # Standard for DNN 7/VS 2013 targeting .NET 4.0
)

& $msbuild $buildArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "Build completed successfully" -ForegroundColor Green

# 5. LEGGI VERSIONE DA version.txt (scritto da bump-version.ps1)
$versionFile = Join-Path $root "version.txt"
if (-not (Test-Path $versionFile)) {
    throw "version.txt non trovato. Esegui prima bump-version.ps1"
}
$version = (Get-Content $versionFile -Raw).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+$') {
    throw "version.txt contiene un valore non valido: '$version'"
}
Write-Host "Version from version.txt: $version" -ForegroundColor Green

# 5b. Propaga la versione a tutti gli AssemblyInfo.cs
Get-ChildItem -Path $root -Recurse -Filter "AssemblyInfo.cs" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    $content = $content -replace 'AssemblyVersion\("[^"]+"\)',     "AssemblyVersion(`"$version`")"
    $content = $content -replace 'AssemblyFileVersion\("[^"]+"\)', "AssemblyFileVersion(`"$version`")"
    Set-Content $_.FullName -Value $content -Encoding UTF8
    Write-Host "  AssemblyInfo aggiornato: $($_.FullName)"
}

Write-Host "Computed version: $version" -ForegroundColor Green
