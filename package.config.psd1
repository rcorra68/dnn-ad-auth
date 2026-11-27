@{
    PackageType  = "Auth_System"

    PackageName  = "Vvf.Authentication.Dipvvf"
    FriendlyName = "Dipvvf Authentication"
    Description  = "A DotNetNuke Authentication provider to integrate the dipvvf.it Active Directory domain authentication."
    Owner        = "Roberto Corradetti"
    Organization = "Direzione Regionale VVF Marche"
    Url          = "http://www.mar.dipvvf.it"
    Email        = "roberto.corradetti@vigilfuoco.it"
    CoreVersion  = "07.04.02"

    Dependencies = @()

    # ─── Auth_System only ───────────────────────────────────────────
    Auth = @{
        Type            = "Dipvvf"
        BasePath        = "DesktopModules/AuthenticationServices/Dipvvf"
        SettingsControl = "Settings.ascx"
        LoginControl    = "Login.ascx"
        LogoffControl   = ""
    }

    # Single assembly in bin: no per-DLL overrides needed.
    # No web.config changes and nothing legacy to clean up: no fragments.
}
