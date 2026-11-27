namespace Vvf.Authentication.Dipvvf
{
    using System;
    using DotNetNuke.Services.Authentication;
    using Vvf.Authentication.Dipvvf.Components;

    /// <summary>
    /// Settings control shown in Admin > Extensions > Authentication Systems.
    /// </summary>
    public partial class Settings : AuthenticationSettingsBase
    {
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (!IsPostBack)
            {
                DipvvfAuthConfig config = DipvvfAuthConfig.GetConfig(PortalId);
                chkEnabled.Checked = config.Enabled;
                txtDomain.Text = config.Domain;
            }
        }

        public override void UpdateSettings()
        {
            DipvvfAuthConfig config = DipvvfAuthConfig.GetConfig(PortalId);

            string domain = txtDomain.Text.Trim().ToLowerInvariant();

            config.Enabled = chkEnabled.Checked;
            config.Domain = domain.Length > 0 ? domain : DipvvfAuthConfig.DefaultDomain;

            DipvvfAuthConfig.UpdateConfig(config);
        }
    }
}
