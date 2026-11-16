function New-ZipPackage {
    param($Paths)

    Write-Step "Creating ZIP..."

    Compress-Archive -Path "$($Paths.OutFolder)\*" `
        -DestinationPath $Paths.ZipPath `
        -Force
}
