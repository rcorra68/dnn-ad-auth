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
    /// then signs in the DNN user DIPVVF\&lt;sAMAccountName&gt;. DNN users are never
    /// created here: an account must already exist in DNN.
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

            if (user == null || user.IsDeleted)
            {
                // Valid AD credentials but no DNN account: same generic failure
                // for the user, detailed reason only in the log.
                Logger.Warn("AD login succeeded for '" + accountName + "' but no active DNN user '" + dnnUsername + "' exists in portal " + PortalId + ".");
                RaiseLoginFailure(accountName);
                return;
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
