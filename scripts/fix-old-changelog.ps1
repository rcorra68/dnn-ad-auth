# fix-old-changelog.ps1
$file = "CHANGELOG.md"
$content = Get-Content $file -Raw -Encoding UTF8

$replacements = @{
    # Sezioni git-cliff con byte corrotti → label nuove
    '### [^\S\r\n]*â[^\S\r\n]*š[^\n]*Miscellaneous Tasks' = '### 🔧 Chore'
    '### [^\S\r\n]*ð[^\n]*Other'                           = '### 💼 Altro'
    '### [^\S\r\n]*ð[^\n]*Refactor'                        = '### ♻️  Refactor'
    '### [^\S\r\n]*ð[^\n]*Features'                        = '### ✨ Nuove funzionalità'
    '### [^\S\r\n]*ð[^\n]*Documentation'                   = '### 📝 Documentazione'
    # Sezione [unreleased] → minuscolo coerente
    '\[unreleased\]'                                        = '[Unreleased]'
}

foreach ($pattern in $replacements.Keys) {
    $content = $content -replace $pattern, $replacements[$pattern]
}

# Rimuovi blocchi duplicati (stesso header ## [x.y.z] - data)
$content = $content -replace '(?s)(## \[\d+\.\d+\.\d+\][^\n]*\n(?:(?!## \[).)*)\1+', '$1'

Set-Content $file -Value $content -Encoding UTF8
Write-Host "CHANGELOG normalizzato." -ForegroundColor Green
