function Copy-Providers {
    param($Paths, $Config)

    Write-Step "Copying Providers to the Providers folder..."

    # Path validation: Check if source exists
    if (-not (Test-Path -Path $Paths.SrcProviderPath)) {
        Write-Error "Source path '$($Paths.SrcProviderPath)' not found."
        return
    }

    $excludePatterns = @()

    $destinationPath = Join-Path -Path $Paths.DnnFolder -ChildPath "Providers\DataProviders\SqlDataProvider"

    if (-not (Test-Path -Path $destinationPath)) {
        New-Item -ItemType Directory -Path $destinationPath -Force | Out-Null
    }

    # Use -Include instead of -Filter for better pattern matching in some environments
    # or simply grab all and filter with Where-Object
    $providers = Get-ChildItem -Path $Paths.SrcProviderPath -File -ErrorAction SilentlyContinue | 
                 Where-Object { $_.Name -like "*.SqlDataProvider" }

    if ($null -eq $providers) {
        Write-Warning "No .SqlDataProvider files found in source."
        return
    }

    $filteredProviders = $providers | Where-Object {
        $name = $_.Name
        $isExcluded = $false

        foreach ($pattern in $excludePatterns) {
            if ($name -like $pattern) {
                $isExcluded = $true
                break
            }
        }
        
        -not $isExcluded
    }

    $result = @()
    
    foreach ($provider in $filteredProviders) {
        Copy-Item -Path $provider.FullName -Destination $destinationPath -Force
        Write-Deploy "$($provider.Name) copied"
        $result += $provider.Name
    }
    
    return $result
}