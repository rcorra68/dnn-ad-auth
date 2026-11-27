namespace Vvf.Authentication.Dipvvf
{
    using System;
    using DotNetNuke.Entities.Users;
    using DotNetNuke.Instrumentation;
    using DotNetNuke.Security.Membership;
    using DotNetNuke.Services.Authentication;
    using DotNetNuke.Services.Localization;
    using Vvf.Authentication.Dipvvf.Components;

    /// <summary>
    /// Login control: validates nome.cognome + password against Active Directory,
    /// then signs in the DNN user DIPVVF\&lt;sAMAccountName&gt;. A missing DNN user
    /// is created on first login when AutoCreateUsers is enabled.
    /// </summary>
    public partial class Login : AuthenticationLoginBase
    {
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(Login));

        private DipvvfAuthConfig _config;

        public override bool Enabled
        {
            get { return Config.Enabled; }
        }

        private DipvvfAuthConfig Config
        {
            get
            {
                if (_config == null)
                {
                    _config = DipvvfAuthConfig.GetConfig(PortalId);
                }

                return _config;
            }
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            cmdLogin.Click += cmdLogin_Click;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (!IsPostBack)
            {
                lblDomain.Text = "@" + Config.Domain;
            }
        }

        private void cmdLogin_Click(object sender, EventArgs e)
        {
            lblMessage.Visible = false;

            string accountName = DirectoryAuthenticator.NormalizeAccountName(txtUsername.Text);
            IDirectoryAuthenticator authenticator = new DirectoryAuthenticator(Config.Domain);

            DirectoryAuthenticationResult result = authenticator.Authenticate(accountName, txtPassword.Text);

            if (result.Status == DirectoryAuthenticationStatus.DirectoryUnavailable)
            {
                ShowMessage("DirectoryUnavailable");
                return;
            }

            if (result.Status != DirectoryAuthenticationStatus.Success)
            {
                RaiseLoginFailure(accountName);
                return;
            }

            // DNN users are stored as DIPVVF\<codice fiscale>
            string dnnUsername = DipvvfAuthConfig.DnnUsernamePrefix + result.SamAccountName.ToUpperInvariant();

            UserInfo user = UserController.GetUserByName(PortalId, dnnUsername);

            if (user != null && user.IsDeleted)
            {
                // Soft-deleted account: never recreate or revive it here.
                Logger.Warn("AD login succeeded for '" + accountName + "' but DNN user '" + dnnUsername + "' is deleted in portal " + PortalId + ".");
                RaiseLoginFailure(accountName);
                return;
            }

            if (user == null)
            {
                if (!Config.AutoCreateUsers)
                {
                    // Valid AD credentials but no DNN account: same generic failure
                    // for the user, detailed reason only in the log.
                    Logger.Warn("AD login succeeded for '" + accountName + "' but no DNN user '" + dnnUsername + "' exists in portal " + PortalId + " and AutoCreateUsers is off.");
                    RaiseLoginFailure(accountName);
                    return;
                }

                user = CreateDnnUser(dnnUsername, accountName, result);

                if (user == null)
                {
                    RaiseLoginFailure(accountName);
                    return;
                }
            }

            UserLoginStatus status = UserLoginStatus.LOGIN_SUCCESS;

            if (user.Membership.LockedOut)
            {
                status = UserLoginStatus.LOGIN_USERLOCKEDOUT;
            }
            else if (!user.Membership.Approved)
            {
                status = UserLoginStatus.LOGIN_USERNOTAPPROVED;
            }

            UserAuthenticatedEventArgs args = new UserAuthenticatedEventArgs(user, accountName, status, AuthenticationType);
            args.RememberMe = chkRememberMe.Checked;

            OnUserAuthenticated(args);
        }

        /// <summary>
        /// Creates the DNN user from the Active Directory attributes. The DNN
        /// password is random and never used: authentication always goes to AD.
        /// </summary>
        private UserInfo CreateDnnUser(string dnnUsername, string accountName, DirectoryAuthenticationResult directoryUser)
        {
            UserInfo user = new UserInfo();
            user.PortalID = PortalId;
            user.Username = dnnUsername;
            user.Profile.InitialiseProfile(PortalId);

            user.FirstName = directoryUser.FirstName;
            user.LastName = directoryUser.LastName;
            user.Profile.FirstName = directoryUser.FirstName;
            user.Profile.LastName = directoryUser.LastName;

            // Display name: the UPN prefix from AD (nome.cognome, or nome1.cognome
            // for homonyms), which is what users know each other by.
            user.DisplayName = GetUpnPrefix(directoryUser.UserPrincipalName);
            if (string.IsNullOrEmpty(user.DisplayName))
            {
                user.DisplayName = accountName;
            }

            user.Email = !string.IsNullOrEmpty(directoryUser.Email)
                ? directoryUser.Email
                : accountName + "@" + Config.Domain;

            user.Profile.PreferredLocale = DipvvfAuthConfig.NewUserLocale;
            user.Profile.PreferredTimeZone = GetNewUserTimeZone();

            user.Membership.Password = Guid.NewGuid().ToString("N") + "Aa1!";
            user.Membership.Approved = true;

            UserCreateStatus createStatus = UserController.CreateUser(ref user);

            if (createStatus != UserCreateStatus.Success)
            {
                Logger.Error("Could not create DNN user '" + dnnUsername + "' for '" + accountName + "' in portal " + PortalId + ": " + createStatus + ".");
                return null;
            }

            Logger.Info("Created DNN user '" + dnnUsername + "' for '" + accountName + "' in portal " + PortalId + ".");
            return user;
        }

        private static string GetUpnPrefix(string userPrincipalName)
        {
            if (string.IsNullOrEmpty(userPrincipalName))
            {
                return string.Empty;
            }

            int atIndex = userPrincipalName.IndexOf('@');
            string prefix = atIndex >= 0 ? userPrincipalName.Substring(0, atIndex) : userPrincipalName;
            return prefix.Trim().ToLowerInvariant();
        }

        private static TimeZoneInfo GetNewUserTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(DipvvfAuthConfig.NewUserTimeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                Logger.Warn("Time zone '" + DipvvfAuthConfig.NewUserTimeZoneId + "' not found: server time zone used for new users.");
                return TimeZoneInfo.Local;
            }
        }

        private void RaiseLoginFailure(string accountName)
        {
            UserAuthenticatedEventArgs args = new UserAuthenticatedEventArgs(null, accountName, UserLoginStatus.LOGIN_FAILURE, AuthenticationType);
            args.Authenticated = false;

            OnUserAuthenticated(args);
        }

        private void ShowMessage(string resourceKey)
        {
            lblMessage.Text = Localization.GetString(resourceKey, LocalResourceFile);
            lblMessage.Visible = true;
        }
    }
}
