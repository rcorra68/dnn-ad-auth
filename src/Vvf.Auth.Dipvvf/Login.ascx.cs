using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Security;
using DotNetNuke.Security.Membership;
using DotNetNuke.Services.Authentication;
using DotNetNuke.Services.Localization;
using DotNetNuke.Services.Log.EventLog;
using System;
using System.Security.Permissions;
using System.Web;
using Vvf.Auth.Dipvvf.Providers.ADSIProvider;
using ADConfiguration = Vvf.Auth.Dipvvf.Components.Config.Configuration;
using DNNUserInfo = DotNetNuke.Entities.Users.UserInfo;

namespace Vvf.Auth.Dipvvf
{
    public partial class Login : AuthenticationLoginBase
    {
        private readonly MembershipProvider _memberProvider = MembershipProvider.Instance();

        #region protected Properties

        /// <summary>
        /// Gets whether the Captcha control is used to validate the login.
        /// </summary>
        protected bool UseCaptcha
        {
            get
            {
                Object setting = GetSetting(PortalId, "Security_CaptchaLogin");
                return setting != null && Convert.ToBoolean(setting);
            }
        }

        /// <summary>
        /// Returns the username entered formatted to DOMAIN\User structure.
        /// </summary>
        protected string UserName
        {
            get { return ResolveFormattedUserName(txtUsername.Text); }
            set { txtUsername.Text = value; }
        }

        #endregion

        #region public Properties

        /// <summary>
        /// Check if the Auth System is Enabled for the Portal.
        /// </summary>
        public override bool Enabled
        {
            get
            {
                try
                {
                    var hostingPermissions = new AspNetHostingPermission(PermissionState.Unrestricted);
                    hostingPermissions.Demand();

                    return ADConfiguration.GetConfig().WindowsAuthentication;
                }
                catch
                {
                    return false;
                }
            }
        }

        #endregion

        #region Event Handlers

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Request.IsAuthenticated)
            {
                if (!IsPostBack)
                {
                    if (!string.IsNullOrEmpty(Request.QueryString["username"]))
                    {
                        txtUsername.Text = Request.QueryString["username"];
                    }
                }

                txtPassword.Attributes.Add("value", txtPassword.Text);

                try
                {
                    if (string.IsNullOrEmpty(txtUsername.Text))
                    {
                        Globals.SetFormFocus(txtUsername);
                    }
                    else
                    {
                        Globals.SetFormFocus(txtPassword);
                    }
                }
                catch
                {
                    // Ignore focus setting failure when control rendering is delayed
                }
            }

            divCaptcha1.Visible = UseCaptcha;
            divCaptcha2.Visible = UseCaptcha;

            if (UseCaptcha)
            {
                ctlCaptcha.ErrorMessage = Localization.GetString("InvalidCaptcha", Localization.SharedResourceFile);
                ctlCaptcha.Text = Localization.GetString("CaptchaText", Localization.SharedResourceFile);
            }
        }

        protected void cmdLogin_Click(object sender, EventArgs e)
        {
            if (UseCaptcha && !ctlCaptcha.IsValid)
            {
                return;
            }

            var loginStatus = UserLoginStatus.LOGIN_FAILURE;
            var objAuthentication = new Vvf.Auth.Dipvvf.Components.AuthenticationController();
            DNNUserInfo objUser = null;

            var formattedUsername = UserName;

            if (formattedUsername.Contains(@"\"))
            {
                objUser = objAuthentication.ManualLogon(formattedUsername, txtPassword.Text, ref loginStatus, IPAddress);
            }

            bool authenticated = loginStatus != UserLoginStatus.LOGIN_FAILURE;
            String message = Null.NullString;

            if (objUser == null)
            {
                AddEventLog(PortalId, formattedUsername, Null.NullInteger, PortalSettings.PortalName, IPAddress, loginStatus);
            }

            var eventArgs = new UserAuthenticatedEventArgs(objUser, formattedUsername, loginStatus, "Active Directory")
            {
                Authenticated = authenticated,
                Message = message
            };

            OnUserAuthenticated(eventArgs);
        }

        #endregion

        #region private Methods

        /// <summary>
        /// Normalizes raw input username (UPN or SAM) into standard DOMAIN\User format.
        /// </summary>
        private string ResolveFormattedUserName(string rawInput)
        {
            if (string.IsNullOrWhiteSpace(rawInput))
            {
                return string.Empty;
            }

            var config = ADConfiguration.GetConfig();
            String defaultDomain = config.DefaultDomain ?? String.Empty;

            // Handle UPN format (user@domain.com)
            if (rawInput.Contains("@"))
            {
                return Utilities.UPNToLogonName0(rawInput.ToLowerInvariant());
            }

            // Handle DOMAIN\User format
            if (rawInput.Contains(@"\"))
            {
                String[] parts = rawInput.Split('\\');
                String domain = parts[0].ToUpperInvariant();
                String user = parts[1].ToUpperInvariant();

                if (domain.Contains("."))
                {
                    domain = Utilities.CanonicalToNetBIOS(domain.ToLowerInvariant());
                }

                return !string.IsNullOrEmpty(domain) ? string.Format(@"{0}\{1}", domain, user) : user;
            }

            // Fallback: Append default domain if specified
            if (!string.IsNullOrEmpty(defaultDomain))
            {
                String cleanDomain = defaultDomain.Trim().Replace(@"\", string.Empty);
                return string.Format(@"{0}\{1}", cleanDomain, rawInput);
            }

            return rawInput;
        }

        private static void AddEventLog(int portalId, string username, int userId, string portalName, string ip, UserLoginStatus loginStatus)
        {
            var eventLogController = new EventLogController();
            var logInfo = new LogInfo();
            var portalSecurity = new PortalSecurity();

            logInfo.AddProperty("IP", ip);
            logInfo.LogPortalID = portalId;
            logInfo.LogPortalName = portalName;
            logInfo.LogUserName = portalSecurity.InputFilter(
                username,
                PortalSecurity.FilterFlag.NoScripting |
                PortalSecurity.FilterFlag.NoAngleBrackets |
                PortalSecurity.FilterFlag.NoMarkup
            );
            logInfo.LogUserID = userId;
            logInfo.LogTypeKey = loginStatus.ToString();

            eventLogController.AddLog(logInfo);
        }

        #endregion
    }
}
