// DotNetNuke® - http://www.dotnetnuke.com
// Copyright (c) 2002-2013 by DotNetNuke Corporation
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.

namespace Vvf.Auth.Dipvvf.Components.Config
{
    using System;
    using System.Collections.Generic;
    using DotNetNuke.Common.Utilities;
    using DotNetNuke.Entities.Portals;
    using DotNetNuke.Framework.Providers;
    using DotNetNuke.Security;
    using Vvf.Auth.Dipvvf.Providers.ADSIProvider;

    public class Configuration
    {
        public const string AUTHENTICATION_PATH = "/DesktopModules/AuthenticationServices/ActiveDirectory/";
        public const string AUTHENTICATION_LOGON_PAGE = "WindowsSignin.aspx";
        public const string AUTHENTICATION_LOGOFF_PAGE = "Logoff.aspx";
        public const string AUTHENTICATION_KEY = "authentication";
        public const string AUTHENTICATION_STATUS_KEY = "authentication.status";
        public const string LOGON_USER_VARIABLE = "LOGON_USER";
        private const string AUTHENTICATION_CONFIG_CACHE_PREFIX = "Authentication.Configuration";

        // Setting Name Constants
        public const string AD_WINDOWSAUTHENTICATION = "AD_WindowsAuthentication";
        public const string AD_HIDEWINDOWSLOGIN = "AD_HideWindowsLogin";
        public const string AD_SYNCHRONIZEROLE = "AD_SynchronizeRole";
        public const string AD_SYNCHRONIZEPASSWORD = "AD_SynchronizePassword";
        public const string AD_STRIPDOMAINNAME = "AD_StripDomainName";
        public const string AD_ROOTDOMAIN = "AD_RootDomain";
        public const string AD_EMAILDOMAIN = "AD_EmailDomain";
        public const string AD_USERNAME = "AD_UserName";
        public const string AD_PROVIDERTYPENAME = "AD_ProviderTypeName";
        public const string AD_AUTHENTICATIONTYPE = "AD_AuthenticationType";
        public const string AD_AUTHENTICATIONPASSWORD = "AD_AuthenticationPassword";
        public const string AD_SUBNET = "AD_SubNet";
        public const string AD_AUTOCREATEUSERS = "AD_AutoCreateUsers";
        public const string AD_DEFAULTDOMAIN = "AD_DefaultDomain";
        public const string AD_SEARCHBOTS = "AD_SearchBots";
        public const string AD_SYNCPHOTO = "AD_SyncPhoto";

        private readonly int _portalId;
        private readonly bool _windowsAuthentication;
        private readonly bool _hideWindowsLogin;
        private readonly string _rootDomain = string.Empty;
        private readonly string _userName = string.Empty;
        private readonly string _password = string.Empty;
        private readonly bool _synchronizeRole;
        private readonly bool _synchronizePassword;
        private readonly bool _stripDomainName;
        private readonly string _providerTypeName = DefaultProviderTypeName;
        private readonly string _authenticationType = DefaultAuthenticationType;
        private readonly string _emailDomain = DefaultEmailDomain;
        private readonly string _autoIP = string.Empty;
        private readonly bool _autoCreateUsers;
        private readonly string _defaultDomain = string.Empty;
        private readonly string _bots = string.Empty;
        private readonly bool _photo;

        public Configuration()
        {
            PortalSettings portalSettings = PortalController.Instance.GetCurrentPortalSettings();
            ProviderConfiguration providerConfiguration = ProviderConfiguration.GetProviderConfiguration(AUTHENTICATION_KEY);
            var objSecurity = new PortalSecurity();

            try
            {
                if (providerConfiguration.DefaultProvider == null)
                {
                    return;
                }

                _portalId = portalSettings.PortalId;
                Dictionary<string, string> cambrianSettings = PortalController.Instance.GetPortalSettings(_portalId);

                if (cambrianSettings.ContainsKey(AD_WINDOWSAUTHENTICATION))
                {
                    _windowsAuthentication = Convert.ToBoolean(Null.GetNull(cambrianSettings[AD_WINDOWSAUTHENTICATION], _windowsAuthentication));
                }

                if (cambrianSettings.ContainsKey(AD_HIDEWINDOWSLOGIN))
                {
                    _hideWindowsLogin = Convert.ToBoolean(Null.GetNull(cambrianSettings[AD_HIDEWINDOWSLOGIN], _hideWindowsLogin));
                }

                if (cambrianSettings.ContainsKey(AD_SYNCHRONIZEROLE))
                {
                    _synchronizeRole = Convert.ToBoolean(Null.GetNull(cambrianSettings[AD_SYNCHRONIZEROLE], _synchronizeRole));
                }

                if (cambrianSettings.ContainsKey(AD_SYNCHRONIZEPASSWORD))
                {
                    _synchronizePassword = Convert.ToBoolean(Null.GetNull(cambrianSettings[AD_SYNCHRONIZEPASSWORD], _synchronizePassword));
                }

                if (cambrianSettings.ContainsKey(AD_STRIPDOMAINNAME))
                {
                    _stripDomainName = Convert.ToBoolean(Null.GetNull(cambrianSettings[AD_STRIPDOMAINNAME], _stripDomainName));
                }

                if (cambrianSettings.ContainsKey(AD_ROOTDOMAIN))
                {
                    _rootDomain = Convert.ToString(Null.GetNull(cambrianSettings[AD_ROOTDOMAIN], _rootDomain));
                }

                if (cambrianSettings.ContainsKey(AD_EMAILDOMAIN))
                {
                    _emailDomain = Convert.ToString(Null.GetNull(cambrianSettings[AD_EMAILDOMAIN], _emailDomain));
                }

                if (cambrianSettings.ContainsKey(AD_USERNAME))
                {
                    _userName = Convert.ToString(Null.GetNull(cambrianSettings[AD_USERNAME], _userName));
                }

                if (cambrianSettings.ContainsKey(AD_PROVIDERTYPENAME))
                {
                    _providerTypeName = Convert.ToString(Null.GetNull(cambrianSettings[AD_PROVIDERTYPENAME], _providerTypeName));
                }

                if (cambrianSettings.ContainsKey(AD_AUTHENTICATIONTYPE))
                {
                    _authenticationType = Convert.ToString(Null.GetNull(cambrianSettings[AD_AUTHENTICATIONTYPE], _authenticationType));
                }

                if (cambrianSettings.ContainsKey(AD_AUTHENTICATIONPASSWORD))
                {
                    _password = objSecurity.Decrypt(AUTHENTICATION_KEY, Convert.ToString(Null.GetNull(cambrianSettings[AD_AUTHENTICATIONPASSWORD], _password)));
                }

                if (cambrianSettings.ContainsKey(AD_SUBNET))
                {
                    _autoIP = Convert.ToString(Null.GetNull(cambrianSettings[AD_SUBNET], _autoIP));
                }

                if (cambrianSettings.ContainsKey(AD_AUTOCREATEUSERS))
                {
                    _autoCreateUsers = Convert.ToBoolean(Null.GetNull(cambrianSettings[AD_AUTOCREATEUSERS], _autoCreateUsers));
                }

                if (cambrianSettings.ContainsKey(AD_DEFAULTDOMAIN))
                {
                    _defaultDomain = Convert.ToString(Null.GetNull(cambrianSettings[AD_DEFAULTDOMAIN], _defaultDomain));
                }

                if (cambrianSettings.ContainsKey(AD_SEARCHBOTS))
                {
                    _bots = Convert.ToString(Null.GetNull(cambrianSettings[AD_SEARCHBOTS], _bots));
                }

                if (cambrianSettings.ContainsKey(AD_SYNCPHOTO))
                {
                    _photo = Convert.ToBoolean(Null.GetNull(cambrianSettings[AD_SYNCPHOTO], _photo));
                }
            }
            catch (Exception ex)
            {
                Utilities.AddEventLog(portalSettings, "There was a problem loading the settings for the AD Authentication Provider. Error: " + ex.Message);
            }
        }

        public static Configuration GetConfig()
        {
            Configuration config = null;
            try
            {
                PortalSettings portalSettings = PortalController.Instance.GetCurrentPortalSettings();
                string strKey = $"{AUTHENTICATION_CONFIG_CACHE_PREFIX}.{portalSettings.PortalId}";

                config = (Configuration)DataCache.GetCache(strKey);

                if (config == null)
                {
                    config = new Configuration();
                    DataCache.SetCache(strKey, config);
                }
            }
            catch (Exception)
            {
                // Problems reading AD config, return null
            }

            return config;
        }

        public static void ResetConfig()
        {
            PortalSettings portalSettings = PortalController.Instance.GetCurrentPortalSettings();
            string strKey = $"{AUTHENTICATION_CONFIG_CACHE_PREFIX}.{portalSettings.PortalId}";
            DataCache.RemoveCache(strKey);

            strKey = $"AuthenticationProvider{portalSettings.PortalId}";
            DataCache.RemoveCache(strKey);
        }

        public static void UpdateConfig(
            int portalID,
            bool windowsAuthentication,
            bool hidden,
            string rootDomain,
            string emailDomain,
            string authenticationUserName,
            string authenticationPassword,
            bool synchronizeRole,
            bool synchronizePassword,
            bool stripDomainName,
            string providerTypeName,
            string authenticationType,
            string subNet,
            string defaultDomain,
            bool autoCreateUsers,
            string bots,
            bool photo)
        {
            var objSecurity = new PortalSecurity();

            PortalController.UpdatePortalSetting(portalID, AD_WINDOWSAUTHENTICATION, windowsAuthentication.ToString());
            PortalController.UpdatePortalSetting(portalID, AD_HIDEWINDOWSLOGIN, hidden.ToString());
            PortalController.UpdatePortalSetting(portalID, AD_SYNCHRONIZEROLE, synchronizeRole.ToString());
            PortalController.UpdatePortalSetting(portalID, AD_SYNCHRONIZEPASSWORD, synchronizePassword.ToString());
            PortalController.UpdatePortalSetting(portalID, AD_STRIPDOMAINNAME, stripDomainName.ToString());
            PortalController.UpdatePortalSetting(portalID, AD_ROOTDOMAIN, string.IsNullOrEmpty(rootDomain) ? "" : rootDomain);
            PortalController.UpdatePortalSetting(portalID, AD_EMAILDOMAIN, string.IsNullOrEmpty(emailDomain) ? "" : emailDomain);
            PortalController.UpdatePortalSetting(portalID, AD_USERNAME, string.IsNullOrEmpty(authenticationUserName) ? "" : authenticationUserName);
            PortalController.UpdatePortalSetting(portalID, AD_PROVIDERTYPENAME, string.IsNullOrEmpty(providerTypeName) ? "" : providerTypeName);
            PortalController.UpdatePortalSetting(portalID, AD_AUTHENTICATIONTYPE, string.IsNullOrEmpty(authenticationType) ? "" : authenticationType);
            PortalController.UpdatePortalSetting(portalID, AD_SUBNET, string.IsNullOrEmpty(subNet) ? "127.0.0.1" : subNet);
            PortalController.UpdatePortalSetting(portalID, AD_DEFAULTDOMAIN, string.IsNullOrEmpty(defaultDomain) ? "" : defaultDomain);
            PortalController.UpdatePortalSetting(portalID, AD_AUTOCREATEUSERS, autoCreateUsers.ToString());
            PortalController.UpdatePortalSetting(portalID, AD_SEARCHBOTS, string.IsNullOrEmpty(bots) ? "" : bots);
            PortalController.UpdatePortalSetting(portalID, AD_SYNCPHOTO, photo.ToString());

            if (!string.IsNullOrEmpty(authenticationPassword))
            {
                PortalController.UpdatePortalSetting(portalID, AD_AUTHENTICATIONPASSWORD, Convert.ToString(objSecurity.Encrypt(AUTHENTICATION_KEY, authenticationPassword)));
            }
        }

        public static string DefaultProviderTypeName => "DotNetNuke.Authentication.ActiveDirectory.ADSI.ADSIProvider, DotNetNuke.Authentication.ActiveDirectory";

        public static string DefaultAuthenticationType => "Delegation";

        public static string DefaultEmailDomain
        {
            get
            {
                PortalSettings portalSettings = PortalController.Instance.GetCurrentPortalSettings();
                string portalEmail = portalSettings.Email;
                string sRet = string.Empty;

                if (!string.IsNullOrEmpty(portalEmail))
                {
                    int nPos = portalEmail.IndexOf('@');
                    if (nPos > 0)
                    {
                        sRet = portalEmail.Substring(nPos);
                    }
                }

                return sRet;
            }
        }

        public bool WindowsAuthentication => _windowsAuthentication;

        public bool HideWindowsLogin => _hideWindowsLogin;

        public string RootDomain => _rootDomain;

        public string UserName => _userName;

        public string Password => _password;

        public bool SynchronizeRole => _synchronizeRole;

        public bool SynchronizePassword => _synchronizePassword;

        public bool StripDomainName => _stripDomainName;

        public int PortalId => _portalId;

        public string ProviderTypeName => _providerTypeName;

        public string AuthenticationType => _authenticationType;

        public string EmailDomain => _emailDomain;

        public string Bots => _bots;

        public string AutoIP => _autoIP;

        public bool AutoCreateUsers => _autoCreateUsers;

        public string DefaultDomain => _defaultDomain;

        public bool Photo => _photo;
    }
}