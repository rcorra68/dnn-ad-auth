function Build-Paths {
    param($config, $version)

    $repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

    # Progetto sorgente: ProjectName (se presente) → src\<PackageName> → convenzione moduli
    $projectName = if ($config.ProjectName) {
        $config.ProjectName
    } elseif (Test-Path (Join-Path $repoRoot "src/$($config.PackageName)")) {
        $config.PackageName
    } else {
        "Vvf.Modules.$($config.PackageName).Web"
    }
    $moduleSource = Join-Path $repoRoot "src/$projectName"

    # Tipo di pacchetto, template e cartella di installazione in DNN
    $packageType = if ($config.PackageType) { $config.PackageType } else { "Module" }

    switch ($packageType) {
        "Module" {
            $templateName = "module_dnn.template"
            $installBase  = "DesktopModules/$($config.PackageName)"
        }
        "Auth_System" {
            if (-not $config.Auth -or -not $config.Auth.BasePath) {
                throw "PackageType 'Auth_System' requires Auth.BasePath in package.config.psd1"
            }
            $templateName = "auth_dnn.template"
            $installBase  = $config.Auth.BasePath.TrimEnd('/', '\')
        }
        default {
            throw "Unsupported PackageType: '$packageType' (allowed: Module, Auth_System)"
        }
    }

    $paths = @{
        Root              = $repoRoot
        PackageName       = $config.PackageName
        PackageType       = $packageType
        InstallBasePath   = $installBase

        ManifestTemplatePath = Join-Path $repoRoot "scripts/$templateName"

        SrcDllPath        = Join-Path $moduleSource "bin/Release"
        SrcProviderPath   = Join-Path $moduleSource "Providers/DataProviders/SqlDataProvider"
        ModuleSource      = $moduleSource

        OutputRoot        = Join-Path $repoRoot "artifacts"
        ResourcesTemp     = Join-Path $repoRoot "artifacts/resources-temp"

        OutFolder         = Join-Path $repoRoot "artifacts/$($config.PackageName)_$version"
        DnnFolder         = Join-Path $repoRoot "artifacts/$($config.PackageName)_$version"
        ZipPath           = Join-Path $repoRoot "artifacts/$($config.PackageName)_$($version)_Install.zip"
    }

    if (-not (Test-Path $paths.ModuleSource)) {
        throw "Project folder not found: $($paths.ModuleSource) (check ProjectName in package.config.psd1)"
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