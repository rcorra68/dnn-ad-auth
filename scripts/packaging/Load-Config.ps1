function Load-Config {

    param(
        [string]$ConfigPath
    )

    if (!(Test-Path $ConfigPath)) {
        throw "Config file non trovato: $ConfigPath"
    }

    return Import-PowerShellDataFile $ConfigPath
}
