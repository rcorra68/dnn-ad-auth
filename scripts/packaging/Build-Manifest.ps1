function Build-Manifest {
    param($Paths, $Config, $Version, $Dlls, $Providers)

    Write-Step "Generating manifest..."

    # =========================
    # COMPOSE MANIFEST MODEL
    # =========================

    $manifest = @{
        Version       = $Version
        Module        = $Config.PackageName
        FriendlyName  = if ($Config.FriendlyName) { $Config.FriendlyName } else { $Config.PackageName }
        Description   = $Config.Description
        Owner         = $Config.Owner
        Organization  = $Config.Organization
        Url           = $Config.Url
        Email         = $Config.Email
        CoreVersion   = $Config.CoreVersion

        BasePath      = $Paths.InstallBasePath -replace "/", "\"

        AssembliesXml = Get-DllSection $Dlls
        ScriptsXml    = Get-ScriptsSection $Paths $Providers
        ModulesXml    = ""
        Auth          = @{}
        OtherDepsXml  = Get-DependenciesSection $Config
    }

    switch ($Paths.PackageType) {
        "Module"      { $manifest.ModulesXml = Get-ModuleSection $Config }
        "Auth_System" { $manifest.Auth       = Get-AuthenticationValues $Config $Paths }
    }

    # =========================
    # TEMPLATE VALIDATION
    # =========================

    if (!(Test-Path $Paths.ManifestTemplatePath)) {
        throw "Template manifest non trovato: $($Paths.ManifestTemplatePath)"
    }

    $template = Get-Content $Paths.ManifestTemplatePath -Raw

    # =========================
    # RENDER TEMPLATE
    # =========================
    # Plain string replacement (no regex): values may contain '$' or '\'
    $tokens = [ordered]@{
        "__VERSION__"           = $manifest.Version
        "__MODULE__"            = $manifest.Module
        "__FRIENDLYNAME__"      = $manifest.FriendlyName
        "__DESCRIPTION__"       = $manifest.Description
        "__OWNER__"             = $manifest.Owner
        "__ORG__"               = $manifest.Organization
        "__URL__"               = $manifest.Url
        "__EMAIL__"             = $manifest.Email
        "__BASEPATH__"          = $manifest.BasePath
        "__ASSEMBLIES__"        = $manifest.AssembliesXml
        "__SCRIPTS_COMPONENT__" = $manifest.ScriptsXml
        "__MODULES__"           = $manifest.ModulesXml
        "__FRIENDLY_NAME__"     = $manifest.FriendlyName
        "__AUTH_TYPE__"         = $manifest.Auth.Type
        "__AUTH_BASEPATH__"     = $manifest.Auth.BasePath
        "__SETTINGS_SRC__"      = $manifest.Auth.SettingsSrc
        "__LOGIN_SRC__"         = $manifest.Auth.LoginSrc
        "__LOGOFF_SRC__"        = $manifest.Auth.LogoffSrc
        # Optional fragments, not used yet
        "__CLEANUP__"           = ""
        "__CONFIG__"            = ""
        "__CORE__"              = $manifest.CoreVersion
        "__OTHER_DEPS__"        = $manifest.OtherDepsXml
    }

    $output = $template
    foreach ($token in $tokens.Keys) {
        $output = $output.Replace($token, [string]$tokens[$token])
    }

    if ($output -match "__[A-Z_]+__") {
        throw "Unreplaced placeholder in manifest: $($Matches[0])"
    }

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

function Get-ScriptsSection($Paths, $Providers) {

    Write-Deploy "Building scripts section..."

    $Providers = @($Providers | Where-Object { $_ })

    # No SQL scripts → no Script component at all
    if ($Providers.Count -eq 0) {
        Write-Deploy "No SQL scripts: Script component omitted"
        return ""
    }

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

    $basePath = $Paths.InstallBasePath -replace "/", "\"

    return @"
<component type="Script">
<scripts>
  <basePath>$basePath</basePath>
$installXml
$uninstallXml
</scripts>
</component>
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

function Get-AuthenticationValues($Config, $Paths) {

    Write-Deploy "Building authentication section..."

    $auth = $Config.Auth

    if (-not $auth.Type) {
        throw "Auth.Type missing in package.config.psd1"
    }
    if (-not $auth.LoginControl) {
        throw "Auth.LoginControl missing in package.config.psd1"
    }

    $base = $Paths.InstallBasePath

    return @{
        Type        = $auth.Type
        BasePath    = $base -replace "/", "\"
        LoginSrc    = "$base/$($auth.LoginControl)"
        SettingsSrc = if ($auth.SettingsControl) { "$base/$($auth.SettingsControl)" } else { "" }
        LogoffSrc   = if ($auth.LogoffControl)   { "$base/$($auth.LogoffControl)" }   else { "" }
    }
}
