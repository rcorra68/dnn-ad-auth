$ColorStep = "Cyan"
$ColorOk = "Green"
$ColorError = "Red"
$ColorWarning = "Yellow"
$ColorDeployLabel = "Gray"
$ColorFile = "Green"

function Write-Step($msg) {
    Write-Host "▶ $msg" -ForegroundColor $ColorStep
}

function Write-Ok($msg) {
    Write-Host "[OK ]✅   $msg" -ForegroundColor $ColorOk
}

function Write-Error($msg) {
    Write-Host "[ERR] ❌   $msg" -ForegroundColor $ColorError
}

function Write-Warning($msg) {
    Write-Host "[WARN]⚠️   $msg" -ForegroundColor $ColorWarning
}

function Write-Deploy($file) {
    Write-Host "   ├─ " -NoNewline -ForegroundColor $ColorDeployLabel
    Write-Host $file -ForegroundColor $ColorFile
}