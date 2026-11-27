namespace Vvf.Authentication.Dipvvf.Components
{
    using DotNetNuke.Entities.Portals;

    /// <summary>
    /// Portal-level settings of the Dipvvf authentication provider.
    /// Stored as portal settings with the "DipvvfAuth_" prefix.
    /// </summary>
    public class DipvvfAuthConfig
    {
        public const string DefaultDomain = "dipvvf.it";

        /// <summary>
        /// DNN usernames are stored as DIPVVF\&lt;sAMAccountName&gt; (codice fiscale).
        /// </summary>
        public const string DnnUsernamePrefix = @"DIPVVF\";

        private const string SettingPrefix = "DipvvfAuth_";
        private const string EnabledKey = SettingPrefix + "Enabled";
        private const string DomainKey = SettingPrefix + "Domain";
        private const string AutoCreateUsersKey = SettingPrefix + "AutoCreateUsers";

        public int PortalId { get; private set; }

        public bool Enabled { get; set; }

        /// <summary>
        /// DNS name of the Active Directory domain used both to locate the
        /// domain controllers and as UPN suffix (nome.cognome@Domain).
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// When true, a missing DNN user is created on the first successful
        /// Active Directory login. When false, the account must already exist.
        /// </summary>
        public bool AutoCreateUsers { get; set; }

        public static DipvvfAuthConfig GetConfig(int portalId)
        {
            DipvvfAuthConfig config = new DipvvfAuthConfig();
            config.PortalId = portalId;
            config.Enabled = PortalController.GetPortalSettingAsBoolean(EnabledKey, portalId, false);
            config.Domain = PortalController.GetPortalSetting(DomainKey, portalId, DefaultDomain);
            config.AutoCreateUsers = PortalController.GetPortalSettingAsBoolean(AutoCreateUsersKey, portalId, true);
            return config;
        }

        public static void UpdateConfig(DipvvfAuthConfig config)
        {
            PortalController.UpdatePortalSetting(config.PortalId, EnabledKey, config.Enabled.ToString());
            PortalController.UpdatePortalSetting(config.PortalId, DomainKey, config.Domain);
            PortalController.UpdatePortalSetting(config.PortalId, AutoCreateUsersKey, config.AutoCreateUsers.ToString());
        }
    }
}
