using System;
using System.Security.Permissions;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Users;
using DotNetNuke.Security;
using DotNetNuke.Security.Membership;
using DotNetNuke.Services.Authentication;
using DotNetNuke.Services.Localization;
using DotNetNuke.Services.Log.EventLog;
using DNNUserInfo = DotNetNuke.Entities.Users.UserInfo;

namespace Vvf.Auth.Dipvvf
{
    public partial class Login : AuthenticationLoginBase
    {
        private readonly MembershipProvider _memberProvider = MembershipProvider.Instance();

        #region Protected Properties

        /// <summary>
        /// Gets whether the Captcha control is used to validate the login.
        /// </summary>
        Protected bool UseCaptcha
        {
            Get
            {
                Object setting = GetSetting(PortalId, "Security_CaptchaLogin");
                Return setting != null && Convert.ToBoolean(setting);
            }
        }

        /// <summary>
        /// Returns the username entered formatted to DOMAIN\User structure.
        /// </summary>
        Protected string UserName
        {
            Get { return ResolveFormattedUserName(txtUsername.Text); }
            Set { txtUsername.Text = value; }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Check if the Auth System is Enabled for the Portal.
        /// </summary>
        Public override bool Enabled
        {
            Get
            {
                Try
                {
                    Var hostingPermissions = new AspNetHostingPermission(PermissionState.Unrestricted);
                    HostingPermissions.Demand();

                    Return Configuration.GetConfig().WindowsAuthentication;
                }
                Catch
                {
                    Return false;
                }
            }
        }

        #endregion

        #region Event Handlers

        Protected void Page_Load(object sender, EventArgs e)
        {
            If (!Request.IsAuthenticated)
            {
                If (!IsPostBack)
                {
                    If (!string.IsNullOrEmpty(Request.QueryString["username"]))
                    {
                        TxtUsername.Text = Request.QueryString["username"];
                    }
                }

                TxtPassword.Attributes.Add("value", txtPassword.Text);

                Try
                {
                    If (string.IsNullOrEmpty(txtUsername.Text))
                    {
                        SetFormFocus(txtUsername);
                    }
                    Else
                    {
                        SetFormFocus(txtPassword);
                    }
                }
                Catch
                {
                    // Ignore focus setting failure when control rendering is delayed
                }
            }

            DivCaptcha1.Visible = UseCaptcha;
            DivCaptcha2.Visible = UseCaptcha;

            If (UseCaptcha)
            {
                CtlCaptcha.ErrorMessage = Localization.GetString("InvalidCaptcha", Localization.SharedResourceFile);
                CtlCaptcha.Text = Localization.GetString("CaptchaText", Localization.SharedResourceFile);
            }
        }

        Protected void cmdLogin_Click(object sender, EventArgs e)
        {
            If (UseCaptcha && !ctlCaptcha.IsValid)
            {
                Return;
            }

            Var loginStatus = UserLoginStatus.LOGIN_FAILURE;
            Var objAuthentication = new AuthenticationController();
            DNNUserInfo objUser = null;

            Var formattedUsername = UserName;

            If (formattedUsername.Contains(@"\"))
            {
                ObjUser = objAuthentication.ManualLogon(formattedUsername, txtPassword.Text, ref loginStatus, IPAddress);
            }

            Bool authenticated = loginStatus != UserLoginStatus.LOGIN_FAILURE;
            String message = Null.NullString;

            If (objUser == null)
            {
                AddEventLog(PortalId, formattedUsername, Null.NullInteger, PortalSettings.PortalName, IPAddress, loginStatus);
            }

            Var eventArgs = new UserAuthenticatedEventArgs(objUser, formattedUsername, loginStatus, "Active Directory")
            {
                Authenticated = authenticated,
                Message = message
            };

            OnUserAuthenticated(eventArgs);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Normalizes raw input username (UPN or SAM) into standard DOMAIN\User format.
        /// </summary>
        Private string ResolveFormattedUserName(string rawInput)
        {
            If (string.IsNullOrWhiteSpace(rawInput))
            {
                Return string.Empty;
            }

            Var config = Configuration.GetConfig();
            String defaultDomain = config.DefaultDomain ?? String.Empty;

            // Handle UPN format (user@domain.com)
            If (rawInput.Contains("@"))
            {
                Return ADSI.Utilities.UPNToLogonName0(rawInput.ToLowerInvariant());
            }

            // Handle DOMAIN\User format
            If (rawInput.Contains(@"\"))
            {
                String[] parts = rawInput.Split('\\');
                String domain = parts[0].ToUpperInvariant();
                String user = parts[1].ToUpperInvariant();

                If (domain.Contains("."))
                {
                    Domain = ADSI.Utilities.CanonicalToNetBIOS(domain.ToLowerInvariant());
                }

                Return !string.IsNullOrEmpty(domain) ? string.Format(@"{0}\{1}", domain, user) : user;
            }

            // Fallback: Append default domain if specified
            If (!string.IsNullOrEmpty(defaultDomain))
            {
                String cleanDomain = defaultDomain.Trim().Replace(@"\", string.Empty);
                Return string.Format(@"{0}\{1}", cleanDomain, rawInput);
            }

            Return rawInput;
        }

        Private static void AddEventLog(int portalId, string username, int userId, string portalName, string ip, UserLoginStatus loginStatus)
        {
            Var objEventLog = new EventLogController();
            Var objEventLogInfo = new LogInfo();
            Var objSecurity = new PortalSecurity();

            ObjEventLogInfo.AddProperty("IP", ip);
            ObjEventLogInfo.LogPortalID = portalId;
            ObjEventLogInfo.LogPortalName = portalName;
            ObjEventLogInfo.LogUserName = objSecurity.InputFilter(
                Username,
                PortalSecurity.FilterFlag.NoScripting |
                PortalSecurity.FilterFlag.NoAngleBrackets |
                PortalSecurity.FilterFlag.NoMarkup
            );
            ObjEventLogInfo.LogUserID = userId;
            ObjEventLogInfo.LogTypeKey = loginStatus.ToString();

            ObjEventLog.AddLog(objEventLogInfo);
        }

        #endregion
    }
}
