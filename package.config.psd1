@{
    PackageName  = "Dipvvf Authentication"
    Description  = "A DotNetNuke Authentication provider to integrate the dipvvf Active Directory domain authenticati."
    Owner        = "Roberto Corradetti"
    Organization = "Direzione Regionale VVF Marche"
    Url          = "http://www.mar.dipvvf.it"
    Email        = "roberto.corradetti@vigilfuoco.it"
    CoreVersion  = "07.04.02"
    Dependencies = @(
        # Esempio: dipendenza da un modulo aggiuntivo
        # @{ Type = "ManagedPackage/Package"; Name = "AltroModulo"; Version = "02.00.00" }
    )
    Controls = @(
        # Esempio: un controllo custom aggiuntivo
        # @{ Key = "Print"; Src = "Print.ascx"; Title = "Print View"; Type = "View"; Partial = $true; Order = 1 }
    )
}
