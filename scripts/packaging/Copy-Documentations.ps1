function Copy-Documentations {
    param($Paths)

    Write-Step "Copying LICENSE and CHANGELOG to Documentation folder..."

    # Cartella Documentation dentro artifacts/xx.xx.xx
    $destinationPath = Join-Path -Path $Paths.DnnFolder -ChildPath "Documentation"

    # Crea la cartella se non esiste
    if (-not (Test-Path -Path $destinationPath)) {
        New-Item -ItemType Directory -Path $destinationPath -Force | Out-Null
    }

    # Root della solution
    $repoRoot = $Paths.Root

    # === LICENSE -> License.txt ===
    $licenseSource = Join-Path -Path $repoRoot -ChildPath "LICENSE"
    $licenseDestination = Join-Path -Path $destinationPath -ChildPath "License.txt"

    if (Test-Path $licenseSource) {
        Copy-Item -Path $licenseSource -Destination $licenseDestination -Force
        Write-Deploy "License.txt copied"
    }
    else {
        Write-Warning "LICENSE file not found: $licenseSource"
    }

    # === CHANGELOG.md -> ReleaseNotes.html ===
    $changelogSource = Join-Path -Path $repoRoot -ChildPath "CHANGELOG.md"
    $releaseNotesHtml = Join-Path -Path $destinationPath -ChildPath "ReleaseNotes.html"

    if (Test-Path $changelogSource) {
        Convert-ChangelogToHtml -SourcePath $changelogSource -TargetPath $releaseNotesHtml
        Write-Deploy "ReleaseNotes.html copied"
    }
    else {
        Write-Warning "CHANGELOG.md file not found: $changelogSource"
    }
}