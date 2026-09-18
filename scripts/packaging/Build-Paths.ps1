function Build-Paths {
    param($config, $version)

    $repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

    $paths = @{
        Root              = $repoRoot
        PackageName       = $config.PackageName
        
        ManifestTemplatePath = Join-Path $repoRoot "scripts\module_dnn.template"

        SrcDllPath        = Join-Path $repoRoot "src\Vvf.Modules.$($config.PackageName).Web\bin\Release"
        SrcProviderPath   = Join-Path $repoRoot "src\Vvf.Modules.$($config.PackageName).Web\Providers\DataProviders\SqlDataProvider"
        ModuleSource      = Join-Path $repoRoot "src\Vvf.Modules.$($config.PackageName).Web"

        OutputRoot        = Join-Path $repoRoot "artifacts"
        ResourcesTemp     = Join-Path $repoRoot "artifacts\resources-temp"

        OutFolder         = Join-Path $repoRoot "artifacts\$($config.PackageName)_$version"
        DnnFolder         = Join-Path $repoRoot "artifacts\$($config.PackageName)_$version"
        ZipPath           = Join-Path $repoRoot "artifacts\$($config.PackageName)_$($version)_Install.zip"
    }

    Ensure-Clean $paths

    Ensure-Folder $paths.OutputRoot
    Ensure-Folder $paths.OutFolder
    Ensure-Folder $paths.DnnFolder
    Ensure-Folder $paths.ResourcesTemp

    return $paths
}

function Ensure-Clean {
    param($paths)

    Write-Step("Cleaning previous artifacts...")

    if (Test-Path $paths.OutFolder) {
        Remove-Item $paths.OutFolder -Recurse -Force -ErrorAction SilentlyContinue
    }

    if (Test-Path $paths.ZipPath) {
        Remove-Item $paths.ZipPath -Force -ErrorAction SilentlyContinue
    }
    
    if (Test-Path $paths.ResourcesTemp) {
        Remove-Item $paths.ResourcesTemp -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Ensure-Folder {
    param([string]$Path)

    if (!(Test-Path $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}