function Copy-Resources {
    param($Paths, $Config)

    if (-not (Test-Path $Paths.ModuleSource)) {
        throw "Module source not found: $($Paths.ModuleSource)"
    }

    $extensions = if ($Config -and $Config.ResourceExtensions) { @($Config.ResourceExtensions) } else { @(".ascx", ".resx") }

    Write-Step("Building resource files...")

    $resourceFolders = @(
        "Images",
        "Documentation",
        "App_LocalResources",
        "Scripts"
    )

    $copied = @{}

    #
    # 1. Copy known folders first
    #
    foreach ($folder in $resourceFolders) {

        $sourceFolder = Join-Path $Paths.ModuleSource $folder

        if (Test-Path $sourceFolder) {

            $targetFolder = Join-Path $Paths.ResourcesTemp $folder
            Copy-Item $sourceFolder -Destination $targetFolder -Recurse -Force

            Get-ChildItem $sourceFolder -Recurse -File | ForEach-Object {
                $copied[$_.FullName] = $true
            }
        }
    }

    #
    # 2. Copy files with the configured extensions outside known folders
    #    (bin/obj excluded)
    #
    Get-ChildItem $Paths.ModuleSource -Recurse -File |
    Where-Object {
        $_.Extension -in $extensions -and
        $_.FullName -notmatch "[\\/](bin|obj)[\\/]"
    } |
    ForEach-Object {

        if ($copied.ContainsKey($_.FullName)) {
            return
        }

        foreach ($folder in $resourceFolders) {
            if ($_.FullName -match "[\\/]$folder[\\/]") {
                return
            }
        }

        $relative = $_.FullName.Substring($Paths.ModuleSource.Length).TrimStart('\', '/')
        $target = Join-Path $Paths.ResourcesTemp $relative

        $dir = Split-Path $target -Parent
        if (!(Test-Path $dir)) {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
        }

        Copy-Item $_.FullName -Destination $target -Force
    }

    #
    # 3. Create Resources.zip
    #
    $zipPath = Join-Path $Paths.OutFolder "Resources.zip"

    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem

    [System.IO.Compression.ZipFile]::CreateFromDirectory(
        $Paths.ResourcesTemp,
        $zipPath
    )

    Write-Deploy("Resources.zip created")

    #
    # 4. Cleanup temp folder
    #
    if (Test-Path $Paths.ResourcesTemp) {
        Remove-Item $Paths.ResourcesTemp -Recurse -Force
    }
}