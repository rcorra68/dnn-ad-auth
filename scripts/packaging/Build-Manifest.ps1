function Build-Manifest {
    param($Paths, $Config, $Version, $Dlls, $Providers)

    Write-Step "Generating manifest..."

    # =========================
    # COMPOSE MANIFEST MODEL
    # =========================

    $manifest = @{
        Version       = $Version
        Module        = $Config.PackageName
        Description   = $Config.Description
        Owner         = $Config.Owner
        Organization  = $Config.Organization
        Url           = $Config.Url
        Email         = $Config.Email
        CoreVersion   = $Config.CoreVersion

        AssembliesXml = Get-DllSection $Dlls
        ScriptsXml    = Get-ScriptsSection $Config $Providers
        ModulesXml    = Get-ModuleSection $Config
        OtherDepsXml  = Get-DependenciesSection $Config
    }

    # =========================
    # TEMPLATE VALIDATION
    # =========================

    if (!(Test-Path $Paths.ManifestTemplatePath)) {
        Write-Error "Template manifest non trovato: $($Paths.ManifestTemplatePath)"
        return
    }

    $template = Get-Content $Paths.ManifestTemplatePath -Raw

    # =========================
    # RENDER TEMPLATE
    # =========================
    $output = $template `
        -replace "__VERSION__",      $manifest.Version `
        -replace "__MODULE__",       $manifest.Module `
        -replace "__DESCRIPTION__",  $manifest.Description `
        -replace "__OWNER__",        $manifest.Owner `
        -replace "__ORG__",          $manifest.Organization `
        -replace "__URL__",          $manifest.Url `
        -replace "__EMAIL__",        $manifest.Email `
        -replace "__ASSEMBLIES__",   $manifest.AssembliesXml `
        -replace "__SCRIPTS__",      $manifest.ScriptsXml `
        -replace "__MODULES__",      $manifest.ModulesXml `
        -replace "__CORE__",         $manifest.CoreVersion `
        -replace "__OTHER_DEPS__",   $manifest.OtherDepsXml


    # =========================
    # XML FORMAT & OUTPUT
    # =========================

    [xml]$xmlDoc = $output

    $manifestPath = Join-Path $Paths.DnnFolder "$($Config.PackageName).dnn"

    $settings = New-Object System.Xml.XmlWriterSettings
    $settings.Indent = $true
    $settings.IndentChars = "  "
    $settings.NewLineChars = "`n"
    $settings.NewLineHandling = "Replace"
    $settings.OmitXmlDeclaration = $false

    $writer = [System.Xml.XmlWriter]::Create($manifestPath, $settings)
    $xmlDoc.Save($writer)
    $writer.Close()
}

function Get-DllSection($Dlls) {

    Write-Deploy "Building assembly section..."

    return ($Dlls | ForEach-Object {

        "<assembly>
            <name>$_</name>
            <path>bin</path>
        </assembly>"

    }) -join "`n"
}

function Get-ScriptsSection($Config, $Providers) {

    Write-Deploy "Building scripts section..."

    $installScripts = $Providers | Where-Object { $_ -notlike "Uninstall*" }
    $uninstallScript = $Providers | Where-Object { $_ -like "Uninstall*" }

    # version-aware sort
    $installScripts = $installScripts | Sort-Object {
        [version]($_ -replace "\.SqlDataProvider$")
    }

    $installXml = ($installScripts | ForEach-Object {

        $version = $_ -replace "\.SqlDataProvider$", ""

        @"
<script type="Install">
  <path>Providers\DataProviders\SqlDataProvider</path>
  <name>$_</name>
  <version>$version</version>
</script>
"@

    }) -join "`n"

    $uninstallXml = ""

    if ($uninstallScript) {

        $file = ($uninstallScript | Select-Object -First 1)

        $uninstallXml = @"
<script type="Uninstall">
  <path>Providers\DataProviders\SqlDataProvider</path>
  <name>$file</name>
  <version>00.00.01</version>
</script>
"@
    }

    return @"
<scripts>
  <basePath>DesktopModules\$($Config.PackageName)</basePath>
$installXml
$uninstallXml
</scripts>
"@
}

function Get-ModuleSection($Config) {

    Write-Deploy "Building module section..."

    $basePath = "DesktopModules\$($Config.PackageName)"

    # ─── Controlli standard DNN (sempre presenti) ───────────────────
    $standardControls = @(
        @{ Key = "";        Src = "View.ascx";     Title = "View";     Type = "View";     Partial = $true;  Order = 0 }
        @{ Key = "Edit";    Src = "Edit.ascx";     Title = "Edit";     Type = "Edit";     Partial = $false; Order = 0 }
        @{ Key = "Settings"; Src = "Settings.ascx"; Title = "Settings"; Type = "Edit";    Partial = $false; Order = 0 }
    )

    # ─── Controlli extra da package.config.psd1 ─────────────────────
    $extraControls = @($Config.Controls)

    # Merge: standard + extra, evitando duplicati per Key
    $allControls = $standardControls | ForEach-Object { [PSCustomObject]$_ }

    foreach ($extra in $extraControls) {
        $key = $extra.Key
        $alreadyPresent = $allControls | Where-Object { $_.Key -eq $key }
        if ($alreadyPresent) {
            Write-Warning "Controllo con Key '$key' già presente negli standard — ignorato dal config."
        } else {
            $allControls += [PSCustomObject]$extra
        }
    }
    # ────────────────────────────────────────────────────────────────

    $controlsXml = ($allControls | ForEach-Object {

        $controlSrc = "$basePath/$($_.Src)" -replace "\\", "/"
        $partial    = if ($_.Partial) { "true" } else { "false" }

        $supportsPopups = if ($_.PSObject.Properties.Name -contains "PopUps") {
            $_.PopUps.ToString().ToLower()
        } else {
            "false"
        }

        @"
<moduleControl>
  <controlKey>$($_.Key)</controlKey>
  <controlSrc>$controlSrc</controlSrc>
  <supportsPartialRendering>$partial</supportsPartialRendering>
  <controlTitle>$($_.Title)</controlTitle>
  <controlType>$($_.Type)</controlType>
  <iconFile />
  <helpUrl />
  <viewOrder>$($_.Order)</viewOrder>
  <supportsPopUps>$supportsPopups</supportsPopUps>
</moduleControl>
"@
    }) -join "`n"

    return @"
<moduleDefinition>
  <friendlyName>$($Config.PackageName)</friendlyName>
  <defaultCacheTime>-1</defaultCacheTime>
  <moduleControls>
$controlsXml
  </moduleControls>
</moduleDefinition>
"@
}

function Get-DependenciesSection($Config) {

    Write-Deploy "Building dependencies section..."

    if (-not $Config.Dependencies -or $Config.Dependencies.Count -eq 0) {
        return ""
    }

    return ($Config.Dependencies | ForEach-Object {

        $type    = $_.Type
        $name    = $_.Name
        $version = $_.Version

        switch ($type.ToLower()) {

            "package" {
                "<dependency type=`"Package`">$($name):$($version)</dependency>"
            }

            "managedpackage" {
                "<dependency type=`"ManagedPackage`" version=`"$version`">$name</dependency>"
            }

            default {
                Write-Warning "Tipo dipendenza non riconosciuto: '$type' — ignorato."
            }
        }

    }) -join "`n  "
}