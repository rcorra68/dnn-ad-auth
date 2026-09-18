using DotNetNuke.Entities.Portals;
using DotNetNuke.Framework.Providers;
using DotNetNuke.Services.Authentication;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.Localization;
using DotNetNuke.UI.Skins;
using DotNetNuke.UI.Skins.Controls;
using System;
using System.Collections;
using System.Web;
using System.Web.UI.WebControls;
using Vvf.Auth.Dipvvf.Providers.ADSIProvider;
using ADConfiguration = Vvf.Auth.Dipvvf.Components.Config.Configuration;

namespace Vvf.Auth.Dipvvf
{
    public partial class Settings : AuthenticationSettingsBase
    {
        private string _strError = string.Empty;

        #region Private Methods

        private void DisplayIpError(string strInvalidIP)
        {
            string strError = $"{strInvalidIP} {Localization.GetString("InValidIPAddress", LocalResourceFile)}";
            tblSettings.Visible = true;
            pnlError.Visible = true;
            lblError.Text = strError;
        }

        private string GetUserDomainName(string userName)
        {
            int index = userName.IndexOf(@"\", StringComparison.Ordinal);
            return index > 0 ? userName.Substring(0, index) : string.Empty;
        }

        private string LocalizedStatus(string inputText)
        {
            string strReturn = inputText;
            strReturn = strReturn.Replace("[Global Catalog Status]", Localization.GetString("[Global Catalog Status]", LocalResourceFile));
            strReturn = strReturn.Replace("[Root Domain Status]", Localization.GetString("[Root Domain Status]", LocalResourceFile));
            strReturn = strReturn.Replace("[LDAP Status]", Localization.GetString("[LDAP Status]", LocalResourceFile));
            strReturn = strReturn.Replace("[Network Domains Status]", Localization.GetString("[Network Domains Status]", LocalResourceFile));
            strReturn = strReturn.Replace("[LDAP Error Message]", Localization.GetString("[LDAP Error Message]", LocalResourceFile));
            strReturn = strReturn.Replace("OK", Localization.GetString("OK", LocalResourceFile));
            strReturn = strReturn.Replace("FAIL", Localization.GetString("FAIL", LocalResourceFile));
            return strReturn;
        }

        private bool CheckEnteredIPAddr()
        {
            if (txtAutoIP.Text.EndsWith(";"))
            {
                txtAutoIP.Text = txtAutoIP.Text.Substring(0, txtAutoIP.Text.Length - 1);
            }

            var arrIPArray = new ArrayList();
            string[] arrAutoIP = txtAutoIP.Text.Split(';');

            foreach (string strAutoIP in arrAutoIP)
            {
                if (strAutoIP.Contains("-"))
                {
                    string[] arrIPRange = strAutoIP.Split('-');
                    foreach (string ipRangeItem in arrIPRange)
                    {
                        int intFullIPAddr = ipRangeItem.Split('.').GetUpperBound(0);
                        if (intFullIPAddr == 3)
                        {
                            arrIPArray.Add(ipRangeItem);
                        }
                        else
                        {
                            DisplayIpError(ipRangeItem);
                            return false;
                        }
                    }
                }
                else
                {
                    int intFullIPAddr = strAutoIP.Split('.').GetUpperBound(0);
                    if (intFullIPAddr == 3)
                    {
                        arrIPArray.Add(strAutoIP);
                    }
                    else
                    {
                        DisplayIpError(strAutoIP);
                        return false;
                    }
                }
            }

            foreach (string ipToCheck in arrIPArray)
            {
                try
                {
                    string strIPAddr = Utilities.GetIP4Address(ipToCheck);
                }
                catch (Exception)
                {
                    DisplayIpError(ipToCheck);
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Public Methods

        public override void UpdateSettings()
        {
            var portalSettings = (PortalSettings)HttpContext.Current.Items["PortalSettings"];
            try
            {
                if (!chkAuthentication.Checked)
                {
                    ADConfiguration.UpdateConfig(portalSettings.PortalId, false, false, "", "", "", "", false, false, false, "", "", "", "", false, "", false);
                    ADConfiguration.ResetConfig();
                }
                else
                {
                    string providerTypeName = cboProviders.SelectedItem.Value;
                    string authenticationType = cboAuthenticationType.SelectedItem.Value;

                    if (!string.IsNullOrEmpty(txtAutoIP.Text))
                    {
                        if (!CheckEnteredIPAddr())
                        {
                            return;
                        }
                    }

                    if (chkAuthentication.Checked && !chkHidden.Checked)
                    {
                        ADConfiguration.UpdateConfig(
                            portalSettings.PortalId,
                            chkAuthentication.Checked,
                            chkHidden.Checked,
                            txtRootDomain.Text,
                            txtEmailDomain.Text,
                            txtUserName.Text,
                            txtPassword.Text,
                            chkSynchronizeRole.Checked,
                            chkSynchronizePassword.Checked,
                            chkStripDomainName.Checked,
                            providerTypeName,
                            authenticationType,
                            txtAutoIP.Text,
                            txtDefaultDomain.Text,
                            chkAutoCreate.Checked,
                            txtBots.Text,
                            chkSynchronizePhoto.Checked);
                    }
                    else
                    {
                        ADConfiguration.UpdateConfig(
                            portalSettings.PortalId,
                            false,
                            chkHidden.Checked,
                            txtRootDomain.Text,
                            txtEmailDomain.Text,
                            txtUserName.Text,
                            txtPassword.Text,
                            chkSynchronizeRole.Checked,
                            chkSynchronizePassword.Checked,
                            chkStripDomainName.Checked,
                            providerTypeName,
                            authenticationType,
                            txtAutoIP.Text,
                            txtDefaultDomain.Text,
                            chkAutoCreate.Checked,
                            txtBots.Text,
                            chkSynchronizePhoto.Checked);
                    }

                    ADConfiguration.ResetConfig();
                    var authenticationController = new Vvf.Auth.Dipvvf.Components.AuthenticationController();
                    string statusMessage = authenticationController.NetworkStatus();

                    if (statusMessage.ToLower().Contains("fail"))
                    {
                        MessageCell.Controls.Add(Skin.GetModuleMessageControl("", LocalizedStatus(statusMessage), ModuleMessage.ModuleMessageType.RedError));
                    }
                    else
                    {
                        MessageCell.Controls.Add(Skin.GetModuleMessageControl("", LocalizedStatus(statusMessage), ModuleMessage.ModuleMessageType.GreenSuccess));
                    }
                }
            }
            catch (Exception exc)
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }

        #endregion

        #region Event Handlers

        protected void Page_Init(object sender, EventArgs e)
        {
            var authenticationController = new Components.AuthenticationController();
            ProviderConfiguration providerConfiguration = ProviderConfiguration.GetProviderConfiguration(ADConfiguration.AUTHENTICATION_KEY);

            foreach (DictionaryEntry provider in providerConfiguration.Providers)
            {
                string providerName = (string)provider.Key;
                string providerType = ((Provider)provider.Value).Type;
                cboProviders.Items.Add(new ListItem(providerName, providerType));
            }

            try
            {
                cboAuthenticationType.DataSource = authenticationController.AuthenticationTypes();
            }
            catch (TypeInitializationException)
            {
                _strError = Localization.GetString("AuthProviderError", LocalResourceFile);
            }

            cboAuthenticationType.DataBind();
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                AspNetHostingPermissionLevel permission = Utilities.GetCurrentTrustLevel();
                if (permission != AspNetHostingPermissionLevel.Unrestricted)
                {
                    Response.Redirect("~/DesktopModules/AuthenticationServices/ActiveDirectory/trusterror.htm", true);
                }
                else
                {
                    PortalSettings portalSettings = PortalController.Instance.GetCurrentPortalSettings();

                    ADConfiguration.ResetConfig();
                    ADConfiguration config = ADConfiguration.GetConfig();

                    if (UserInfo.Username.IndexOf(@"\", StringComparison.Ordinal) > 0)
                    {
                        string strDomain = GetUserDomainName(UserInfo.Username);
                        if (string.Equals(strDomain, Request.ServerVariables["SERVER_NAME"], StringComparison.OrdinalIgnoreCase))
                        {
                            _strError = string.Format(
                                Localization.GetString("SameDomainError", LocalResourceFile),
                                strDomain,
                                HttpUtility.HtmlEncode(Request.ServerVariables["SERVER_NAME"]));
                        }
                    }

                    if (!IsPostBack)
                    {
                        chkAuthentication.Checked = config.WindowsAuthentication;
                        chkHidden.Checked = config.HideWindowsLogin;
                        if (chkHidden.Checked)
                        {
                            chkAuthentication.Checked = true;
                        }

                        chkSynchronizeRole.Checked = config.SynchronizeRole;
                        chkSynchronizePhoto.Checked = config.Photo;
                        chkSynchronizePassword.Checked = config.SynchronizePassword;
                        chkStripDomainName.Checked = config.StripDomainName;
                        txtRootDomain.Text = config.RootDomain;
                        txtUserName.Text = config.UserName;
                        txtEmailDomain.Text = config.EmailDomain;
                        txtAutoIP.Text = config.AutoIP;
                        txtDefaultDomain.Text = config.DefaultDomain;
                        chkAutoCreate.Checked = config.AutoCreateUsers;
                        txtBots.Text = config.Bots;

                        if (string.IsNullOrEmpty(txtBots.Text))
                        {
                            txtBots.Text = "gsa-crawler;MS Search 5.0 Robot";
                        }

                        ListItem selectedType = cboAuthenticationType.Items.FindByText(config.AuthenticationType);
                        if (selectedType != null)
                        {
                            selectedType.Selected = true;
                        }
                    }

                    valConfirm.ErrorMessage = Localization.GetString("PasswordMatchFailure", LocalResourceFile);

                    if (string.IsNullOrEmpty(_strError))
                    {
                        tblSettings.Visible = true;
                        pnlError.Visible = false;
                    }
                    else
                    {
                        tblSettings.Visible = false;
                        pnlError.Visible = true;
                        lblError.Text = _strError;
                    }
                }
            }
            catch (Exception exc)
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }

        #endregion
    }
}