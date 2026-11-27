function Copy-Dlls {
    param($Paths, $Config)

    Write-Step "Copying DLLs to the bin folder..."

    # Define exclusion patterns to avoid copying standard or 3rd party libraries
    $excludePatterns = @(
        "DotNetNuke.*",
        "Telerik.*",
        "System.*",
        "Microsoft.*",
        "netstandard.dll"
    )

    # Ensure the destination path points to the 'bin' subdirectory
    $destinationPath = Join-Path -Path $Paths.DnnFolder -ChildPath "bin"

    # Create directory if it doesn't exist (safety measure)
    if (-not (Test-Path -Path $destinationPath)) {
        New-Item -ItemType Directory -Path $destinationPath -Force | Out-Null
    }

    if (-not (Test-Path -Path $Paths.SrcDllPath)) {
        throw "Build output not found: $($Paths.SrcDllPath). Run 'task build' first."
    }

    $dlls = Get-ChildItem $Paths.SrcDllPath -Filter "*.dll"

    $filteredDlls = $dlls | Where-Object {
        $name = $_.Name
        $isExcluded = $false

        foreach ($pattern in $excludePatterns) {
            if ($name -like $pattern) {
                $isExcluded = $true
                break
            }
        }
        
        # Return true only if it's NOT in the exclusion list
        -not $isExcluded
    }

    $result = @()

    foreach ($dll in $filteredDlls) {
        Copy-Item $dll.FullName -Destination $destinationPath -Force
        # Indented output with Green color for success status
        Write-Deploy "$($dll.Name) copied"
        $result += $dll.Name
    }
    
    if ($result.Count -eq 0) {
        throw "No DLLs to package in $($Paths.SrcDllPath)"
    }

    return $result
}