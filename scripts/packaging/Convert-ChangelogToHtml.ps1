function Convert-ChangelogToHtml {
    param(
        [string]$SourcePath,
        [string]$TargetPath,
        [string]$Title = "Release Notes"
    )

    if (!(Test-Path $SourcePath)) {
        Write-Warn "CHANGELOG.md not found: $SourcePath"
        return
    }

    $md = Get-Content $SourcePath -Raw

    # =========================
    # BASIC MARKDOWN → HTML
    # =========================

    $html = $md

    # Headers
    $html = $html -replace "### (.*)", "<h3>$1</h3>"
    $html = $html -replace "## (.*)", "<h2>$1</h2>"
    $html = $html -replace "# (.*)", "<h1>$1</h1>"

    # Bold
    $html = $html -replace "\*\*(.*?)\*\*", "<strong>$1</strong>"

    # Bullet lists
    $html = $html -replace "^\- (.*)$", "<li>$1</li>"

    # Wrap lists (best effort)
    $html = "<html>
<head>
  <meta charset='utf-8'>
  <title>$Title</title>
</head>
<body>
$html
</body>
</html>"

    # Ensure folder exists
    $dir = Split-Path $TargetPath -Parent
    if (!(Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }

    Set-Content -Path $TargetPath -Value $html -Encoding UTF8

    Write-Deploy "ReleaseNotes.html generated"
}