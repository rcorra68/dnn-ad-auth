# bump-version.ps1
# Conventional Commits → SemVer + CHANGELOG.md
# Nessuna dipendenza esterna. Richiede solo Git nel PATH.
#
# USO LOCALE:
#   .\bump-version.ps1                        # bump automatico, tutti i commit dall'ultimo tag
#   .\bump-version.ps1 -DryRun                # anteprima senza modificare nulla
#   .\bump-version.ps1 -Force patch           # forza tipo di bump
#
# USO DA CI (PR merge):
#   .\bump-version.ps1 -From <base_sha>       # solo i commit della PR
#   .\bump-version.ps1 -From <base_sha> -DryRun
#
# CONVENTIONAL COMMITS supportati:
#   feat:            → minor bump
#   fix:, perf:      → patch bump
#   feat!: / BREAKING CHANGE → major bump
#   docs, chore, ci, refactor, test, build, style → solo CHANGELOG, no bump

param(
    [switch]$DryRun,
    [ValidateSet("patch","minor","major","")]
    [string]$Force = "",
    [string]$From = ""      # SHA base da cui leggere i commit (usato dalla CI)
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ─────────────────────────────────────────────
# CONFIGURAZIONE
# ─────────────────────────────────────────────
$ChangelogFile = "CHANGELOG.md"
$VersionFile   = "version.txt"

# Tipi inclusi nel CHANGELOG
$VisibleTypes = @("feat","fix","perf","refactor","docs","ci","build","style","test","chore")

$SectionLabel = @{
    "feat"     = "✨ Nuove funzionalità"
    "fix"      = "🐛 Correzioni"
    "perf"     = "⚡ Performance"
    "refactor" = "♻️  Refactor"
    "docs"     = "📝 Documentazione"
    "ci"       = "👷 CI/Build"
    "build"    = "🏗️  Build"
    "style"    = "💄 Stile"
    "test"     = "✅ Test"
    "chore"    = "🔧 Chore"
    "BREAKING" = "💥 Breaking Changes"
}

# ─────────────────────────────────────────────
# HELPER
# ─────────────────────────────────────────────
function Invoke-Git {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    # esegui git SENZA mischiare stderr nello stream normale
    $output = & git @Arguments
    $exitCode = $LASTEXITCODE

    if ($exitCode -ne 0) {
        # recupera stderr solo in caso di errore vero
        $errorOutput = & git @Arguments 2>&1
        throw "git $($Arguments -join ' ') fallito:`n$errorOutput"
    }

    return $output
}

# ─────────────────────────────────────────────
# 1. VERSIONE CORRENTE
# ─────────────────────────────────────────────
function Get-CurrentVersion {
    $tags = git tag --list "v*" 2>$null

    $tag = $tags |
        ForEach-Object { $_ -replace '^v','' } |
        Sort-Object { [Version]$_ } |
        Select-Object -Last 1
    
    if ($LASTEXITCODE -eq 0 -and $tag -match '(\d+\.\d+\.\d+)') {
        return [Version]$matches[1]
    }
    if (Test-Path $VersionFile) {
        $raw = (Get-Content $VersionFile -Raw).Trim()
        if ($raw -match '(\d+\.\d+\.\d+)') { return [Version]$matches[1] }
    }
    Write-Warning "Nessun tag o version.txt trovato. Parto da 0.0.0"
    return [Version]"0.0.0"
}

# ─────────────────────────────────────────────
# 2. LEGGI COMMIT
#    Se -From è valorizzato usa quel range (CI),
#    altrimenti usa l'ultimo tag (locale)
# ─────────────────────────────────────────────
function Get-Commits {
    if ($From) {
        $range = "$From..HEAD"
        Write-Host "Range commit: $range (dalla PR)"
    } else {
        $tags = git tag --list "v*" 2>$null

        $lastTag = $tags |
            ForEach-Object { $_ -replace '^v','' } |
            Sort-Object { [Version]$_ } |
            Select-Object -Last 1

        if ($lastTag) {
            $lastTag = "v$lastTag"
        }
        
        if ($LASTEXITCODE -ne 0 -or -not $lastTag) {
            Write-Host "Nessun tag precedente — leggo tutta la history."
            $range = "HEAD"
        } else {
            $range = "$lastTag..HEAD"
            Write-Host "Range commit: $range (dall'ultimo tag)"
        }
    }

    $log = @(git log $range --pretty=format:"%H|||%s|||%b<END_BODY>" 2>&1)
    if ($LASTEXITCODE -ne 0 -or -not $log) { return @() }

    $commits = @()
    $raw = ($log -join "`n") -split '<END_BODY>'
    foreach ($entry in $raw) {
        $entry = $entry.Trim()
        if (-not $entry) { continue }
        $parts = $entry -split '\|\|\|', 3
        if ($parts.Count -lt 2) { continue }
        $commits += [PSCustomObject]@{
            Hash    = $parts[0].Trim()
            Subject = $parts[1].Trim()
            Body    = if ($parts.Count -eq 3) { $parts[2].Trim() } else { "" }
        }
    }
    return $commits
}

# ─────────────────────────────────────────────
# 3. PARSE CONVENTIONAL COMMIT
# ─────────────────────────────────────────────
function Parse-ConventionalCommit($commit) {
    $result = [PSCustomObject]@{
        Type        = ""
        Scope       = ""
        Description = $commit.Subject
        IsBreaking  = $false
    }

    if ($commit.Subject -match '^([a-zA-Z]+)(\([^)]+\))?(!)?\:\s*(.+)$') {
        $result.Type        = $matches[1].ToLower()
        $result.Scope       = if ($matches[2]) { $matches[2].Trim('(', ')') } else { "" }
        $result.IsBreaking  = ($matches[3] -eq "!")
        $result.Description = $matches[4].Trim()
    }

    if ($commit.Body -match 'BREAKING[\s-]CHANGE') {
        $result.IsBreaking = $true
    }

    return $result
}

# ─────────────────────────────────────────────
# 4. CALCOLA BUMP
# ─────────────────────────────────────────────
function Get-BumpType($parsedCommits) {
    if ($Force) { return $Force }

    $bump = "none"
    foreach ($c in $parsedCommits) {
        if ($c.IsBreaking)                                          { return "major" }
        if ($c.Type -eq "feat"   -and $bump -ne "major")           { $bump = "minor" }
        if ($c.Type -in @("fix","perf") -and $bump -eq "none")     { $bump = "patch" }
    }
    return $bump
}

# ─────────────────────────────────────────────
# 5. NUOVA VERSIONE
# ─────────────────────────────────────────────
function Get-NewVersion([Version]$current, [string]$bump) {
    switch ($bump) {
        "major" { return [Version]"$($current.Major + 1).0.0" }
        "minor" { return [Version]"$($current.Major).$($current.Minor + 1).0" }
        "patch" { return [Version]"$($current.Major).$($current.Minor).$($current.Build + 1)" }
        default { return $current }
    }
}

# ─────────────────────────────────────────────
# 6. BLOCCO CHANGELOG
# ─────────────────────────────────────────────
function Build-ChangelogBlock([string]$version, [array]$parsedCommits) {
    $date  = Get-Date -Format "yyyy-MM-dd"
    $lines = @("## [$version] - $date", "")

    $breaking = $parsedCommits | Where-Object { $_.IsBreaking }
    if ($breaking) {
        $lines += "### $($SectionLabel['BREAKING'])"
        foreach ($c in $breaking) {
            $scope  = if ($c.Scope) { "**$($c.Scope)**: " } else { "" }
            $lines += "- $scope$($c.Description)"
        }
        $lines += ""
    }

    foreach ($type in $VisibleTypes) {
        $group = $parsedCommits | Where-Object { $_.Type -eq $type -and -not $_.IsBreaking }
        if (-not $group) { continue }
        $label  = if ($SectionLabel.ContainsKey($type)) { $SectionLabel[$type] } else { $type }
        $lines += "### $label"
        foreach ($c in $group) {
            $scope  = if ($c.Scope) { "**$($c.Scope)**: " } else { "" }
            $lines += "- $scope$($c.Description)"
        }
        $lines += ""
    }

    return $lines -join "`n"
}

# ─────────────────────────────────────────────
# 7. AGGIORNA version.txt
#    Decommenta il blocco che ti serve per
#    .nuspec / AssemblyInfo.cs
# ─────────────────────────────────────────────
function Update-VersionFile([string]$newVersion) {
    Set-Content -Path $VersionFile -Value $newVersion -Encoding UTF8

    # Aggiorna tutti gli AssemblyInfo.cs del solution
    Get-ChildItem -Recurse -Filter "AssemblyInfo.cs" | ForEach-Object {
        $content = Get-Content $_.FullName -Raw
        $content = $content -replace 'AssemblyVersion\("[^"]+"\)',     "AssemblyVersion(`"$newVersion`")"
        $content = $content -replace 'AssemblyFileVersion\("[^"]+"\)', "AssemblyFileVersion(`"$newVersion`")"
        Set-Content $_.FullName -Value $content -Encoding UTF8
        Write-Host "  Aggiornato: $($_.FullName)"
    }
}

# ─────────────────────────────────────────────
# 8. AGGIORNA CHANGELOG.md
# ─────────────────────────────────────────────
function Update-Changelog([string]$newBlock) {
    if (Test-Path $ChangelogFile) {
        $existing = Get-Content $ChangelogFile -Raw

        # Rimuovi eventuali blocchi già presenti con la stessa versione+data
        $versionHeader = ($newBlock -split "`n")[0]   # es. "## [2.1.0] - 2026-05-20"
        # Rimuovi tutti i blocchi duplicati prima di inserire
        $escapedHeader = [regex]::Escape($versionHeader)
        $existing = $existing -replace "(?s)$escapedHeader.*?(?=\n## |\z)", ""

        if ($existing -match '^(#[^\n]*\n)(.*)$') {
            $content = "$($matches[1])`n$newBlock`n$($matches[2].TrimStart())"
        } else {
            $content = "$newBlock`n$existing"
        }
    } else {
        $content = "# Changelog`n`n$newBlock`n"
    }
    Set-Content -Path $ChangelogFile -Value $content -Encoding UTF8
}

# ─────────────────────────────────────────────
# MAIN
# ─────────────────────────────────────────────
Write-Host "`n=== bump-version.ps1 ===" -ForegroundColor Cyan

$currentVersion = Get-CurrentVersion
Write-Host "Versione corrente  : $currentVersion"

$commits = Get-Commits
if (-not $commits -or $commits.Count -eq 0) {
    Write-Host "Nessun commit nuovo. Nulla da fare." -ForegroundColor Yellow
    exit 0
}

$parsed = $commits | ForEach-Object { Parse-ConventionalCommit $_ }

Write-Host "Commit analizzati  : $($commits.Count)"
$parsed | Where-Object { $_.Type } | Group-Object Type | ForEach-Object {
    Write-Host "  $($_.Name.PadRight(12)): $($_.Count)"
}
$breakingCount = @($parsed | Where-Object { $_.IsBreaking }).Count
if ($breakingCount) { Write-Host "  BREAKING    : $breakingCount" -ForegroundColor Red }

$bumpType   = Get-BumpType $parsed
$newVersion = Get-NewVersion $currentVersion $bumpType

if ($bumpType -eq "none") {
    Write-Host "`nNessun feat/fix/perf — nessun bump di versione." -ForegroundColor Yellow
    Write-Host "Usa -Force patch|minor|major per forzare." -ForegroundColor Yellow
    exit 0
}

# all'inizio del MAIN, dopo aver calcolato $newVersion
if (Test-Path $ChangelogFile) {
    $existing = Get-Content $ChangelogFile -Raw
    if ($existing -match [regex]::Escape("## [$newVersion]")) {
        Write-Host "Versione $newVersion già nel CHANGELOG. Skip." -ForegroundColor Yellow
        exit 0
    }
}

Write-Host "`nBump tipo          : $bumpType"  -ForegroundColor Green
Write-Host "Nuova versione     : $newVersion" -ForegroundColor Green

$block = Build-ChangelogBlock $newVersion $parsed
Write-Host "`n─── Anteprima CHANGELOG ───────────────────"
Write-Host $block
Write-Host "───────────────────────────────────────────"

if ($DryRun) {
    Write-Host "`n[DRY RUN] Nessuna modifica applicata." -ForegroundColor Yellow
    exit 0
}

Update-VersionFile $newVersion
Update-Changelog $block

Invoke-Git @("add", $ChangelogFile, $VersionFile)
Invoke-Git @("commit", "-m", "chore(release): $newVersion [skip ci]")
Invoke-Git @("tag", "-a", "v$newVersion", "-m", "Release $newVersion")

Write-Host "`n✅ Versione $newVersion committata e taggata." -ForegroundColor Green
Write-Host "   Esegui 'git push --follow-tags' per pubblicare."
