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

namespace DotNetNuke.Authentication.ActiveDirectory
{
    using System;
    using System.Web;
    using System.Web.Security;
    using System.Xml;
    using System.Xml.XPath;
    using DotNetNuke.Common;
    using DotNetNuke.Common.Utilities;
    using DotNetNuke.Entities.Modules;
    using DotNetNuke.Entities.Portals;
    using DotNetNuke.Entities.Users;
    using DotNetNuke.Security.Membership;
    using DotNetNuke.Services.Log.EventLog;
    using DNNUserController = DotNetNuke.Entities.Users.UserController;

    public class AuthenticationController : UserUserControlBase
    {
        private readonly string _mProviderTypeName = string.Empty;
        private readonly PortalSettings _portalSettings;

        public AuthenticationController()
        {
            Configuration config = Configuration.GetConfig();
            _portalSettings = PortalController.Instance.GetCurrentPortalSettings();
            _mProviderTypeName = config.ProviderTypeName;
        }

        public void AuthenticationLogon()
        {
            var objAuthUserController = new UserController();
            string loggedOnUserName = HttpContext.Current.Request.ServerVariables[Configuration.LOGON_USER_VARIABLE];
            UserLoginStatus loginStatus = UserLoginStatus.LOGIN_FAILURE;

            string ipAddress = HttpContext.Current.Request.UserHostAddress ?? string.Empty;

            if (!string.IsNullOrEmpty(loggedOnUserName))
            {
                ADUserInfo objAuthUser = objAuthUserController.GetUser(loggedOnUserName);
                UserInfo objUser = DNNUserController.GetUserByName(_portalSettings.PortalId, loggedOnUserName);

                UserInfo objReturnUser = AuthenticateUser(objUser, objAuthUser, ref loginStatus, ipAddress);

                if (objReturnUser != null)
                {
                    objAuthUser.LastIPAddress = ipAddress;
                    UpdateDNNUser(objReturnUser, objAuthUser);

                    FormsAuthentication.SetAuthCookie(loggedOnUserName, true);
                    SetStatus(_portalSettings.PortalId, AuthenticationStatus.WinLogon);

                    if (Config.GetSetting("PersistentCookieTimeout") != null)
                    {
                        if (int.TryParse(Config.GetSetting("PersistentCookieTimeout"), out int persistentCookieTimeout) && persistentCookieTimeout != 0)
                        {
                            string authCookie = FormsAuthentication.FormsCookieName;
                            foreach (string cookie in HttpContext.Current.Response.Cookies)
                            {
                                if (string.Equals(cookie, authCookie, StringComparison.OrdinalIgnoreCase))
                                {
                                    HttpContext.Current.Response.Cookies[cookie].Expires = DateTime.Now.AddMinutes(persistentCookieTimeout);
                                }
                            }
                        }
                    }

                    var objEventLog = new EventLogController();
                    var objEventLogInfo = new LogInfo();
                    objEventLogInfo.AddProperty("IP", ipAddress);
                    objEventLogInfo.LogPortalID = _portalSettings.PortalId;
                    objEventLogInfo.LogPortalName = _portalSettings.PortalName;
                    objEventLogInfo.LogUserID = objReturnUser.UserID;
                    objEventLogInfo.LogUserName = loggedOnUserName;
                    objEventLogInfo.AddProperty("WindowsAuthentication", "True");
                    objEventLogInfo.LogTypeKey = "LOGIN_SUCCESS";

                    objEventLog.AddLog(objEventLogInfo);
                }
            }

            string querystringparams = $"logon={DateTime.Now.Ticks}";
            string strUrl = Globals.NavigateURL(_portalSettings.ActiveTab.TabID, string.Empty, querystringparams);

            HttpCookie dnnReturnToCookie = HttpContext.Current.Request.Cookies["DNNReturnTo"];
            if (dnnReturnToCookie != null)
            {
                querystringparams = dnnReturnToCookie.Value;
                if (!string.IsNullOrEmpty(querystringparams))
                {
                    querystringparams = querystringparams.ToLowerInvariant();
                    if (querystringparams.IndexOf("windowssignin.aspx", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        strUrl = querystringparams;
                    }
                }
            }

            HttpContext.Current.Response.Redirect(strUrl, true);
        }

        public UserInfo ManualLogon(string userName, string strPassword, ref UserLoginStatus loginStatus, string ipAddress)
        {
            ADUserInfo objAuthUser = ProcessFormAuthentication(userName, strPassword);
            Configuration config = Configuration.GetConfig();
            UserInfo objReturnUser = null;

            if (!string.IsNullOrEmpty(userName) && objAuthUser != null)
            {
                if (config.StripDomainName)
                {
                    userName = Utilities.TrimUserDomainName(userName);
                }

                objAuthUser.Username = userName;
                UserInfo objUser = DNNUserController.GetUserByName(_portalSettings.PortalId, userName);

                objReturnUser = AuthenticateUser(objUser, objAuthUser, ref loginStatus, ipAddress);
                if (objReturnUser != null)
                {
                    objAuthUser.LastIPAddress = ipAddress;
                    UpdateDNNUser(objReturnUser, objAuthUser);
                }
            }

            return objReturnUser;
        }

        public UserInfo AuthenticateUser(UserInfo objUser, ADUserInfo objAuthUser, ref UserLoginStatus loginStatus, string ipAddress)
        {
            Configuration config = Configuration.GetConfig();
            UserInfo objReturnUser = null;

            if (objUser != null)
            {
                MembershipUser aspNetUser = Membership.GetUser(objUser.Username);
                string strPassword = (Membership.Provider.EnablePasswordRetrieval && Membership.Provider.PasswordFormat != MembershipPasswordFormat.Hashed)
                    ? RandomizePassword(aspNetUser, objUser, aspNetUser?.GetPassword())
                    : RandomizePassword(aspNetUser, objUser, string.Empty);

                if (!objUser.IsDeleted)
                {
                    objReturnUser = DNNUserController.ValidateUser(
                        _portalSettings.PortalId,
                        objUser.Username,
                        strPassword,
                        "Active Directory",
                        _portalSettings.PortalName,
                        ipAddress,
                        ref loginStatus);

                    if (config.SynchronizeRole)
                    {
                        SynchronizeRoles(objReturnUser);
                    }
                }
                else if (!config.AutoCreateUsers)
                {
                    objUser.IsDeleted = false;
                    objUser.Membership.IsDeleted = false;
                    objUser.Membership.Password = strPassword;
                    DNNUserController.UpdateUser(_portalSettings.PortalId, objUser);
                    CreateUser(objUser, ref loginStatus);

                    if (loginStatus == UserLoginStatus.LOGIN_SUCCESS)
                    {
                        objReturnUser = DNNUserController.GetUserByName(_portalSettings.PortalId, objAuthUser.Username);
                        if (config.SynchronizeRole)
                        {
                            SynchronizeRoles(objReturnUser);
                        }
                    }
                }
            }
            else if (!config.AutoCreateUsers)
            {
                objUser = DNNUserController.GetUserByName(Null.NullInteger, objAuthUser.Username);

                if (objUser == null)
                {
                    objAuthUser.Membership.Password = Utilities.GetRandomPassword();
                    var objDnnUserInfo = new UserInfo
                    {
                        AffiliateID = objAuthUser.AffiliateID,
                        DisplayName = objAuthUser.DisplayName,
                        Email = objAuthUser.Email,
                        FirstName = objAuthUser.FirstName,
                        IsDeleted = objAuthUser.IsDeleted,
                        IsSuperUser = objAuthUser.IsSuperUser,
                        LastIPAddress = ipAddress,
                        LastName = objAuthUser.LastName,
                        Membership = objAuthUser.Membership,
                        PortalID = objAuthUser.PortalID,
                        Profile = objAuthUser.Profile,
                        Roles = objAuthUser.Roles,
                        Username = objAuthUser.Username
                    };
                    CreateUser(objDnnUserInfo, ref loginStatus);
                }
                else
                {
                    objAuthUser.Membership.Password = RandomizePassword(objUser, string.Empty);
                    objAuthUser.UserID = objUser.UserID;
                    CreateUser(objAuthUser, ref loginStatus);
                }

                if (loginStatus == UserLoginStatus.LOGIN_SUCCESS)
                {
                    objReturnUser = DNNUserController.GetUserByName(_portalSettings.PortalId, objAuthUser.Username);
                    if (config.SynchronizeRole)
                    {
                        SynchronizeRoles(objReturnUser);
                    }
                }
            }

            return objReturnUser;
        }

        private void UpdateDNNUser(UserInfo objReturnUser, ADUserInfo objAuthUser)
        {
            if (!string.IsNullOrEmpty(objAuthUser.DisplayName)) objReturnUser.DisplayName = objAuthUser.DisplayName;
            if (!string.IsNullOrEmpty(objAuthUser.Email)) objReturnUser.Email = objAuthUser.Email;
            if (!string.IsNullOrEmpty(objAuthUser.FirstName)) objReturnUser.FirstName = objAuthUser.FirstName;
            if (!string.IsNullOrEmpty(objAuthUser.LastIPAddress)) objReturnUser.LastIPAddress = objAuthUser.LastIPAddress;
            if (!string.IsNullOrEmpty(objAuthUser.LastName)) objReturnUser.LastName = objAuthUser.LastName;

            if (!string.IsNullOrEmpty(objAuthUser.Profile.FirstName)) objReturnUser.Profile.FirstName = objAuthUser.Profile.FirstName;
            if (objAuthUser.Profile.LastName != null) objReturnUser.Profile.LastName = objAuthUser.Profile.LastName;
            if (!string.IsNullOrEmpty(objAuthUser.Profile.Street)) objReturnUser.Profile.Street = objAuthUser.Profile.Street;
            if (!string.IsNullOrEmpty(objAuthUser.Profile.City)) objReturnUser.Profile.City = objAuthUser.Profile.City;
            if (!string.IsNullOrEmpty(objAuthUser.Profile.Region)) objReturnUser.Profile.Region = objAuthUser.Profile.Region;
            if (!string.IsNullOrEmpty(objAuthUser.Profile.PostalCode)) objReturnUser.Profile.PostalCode = objAuthUser.Profile.PostalCode;
            if (!string.IsNullOrEmpty(objAuthUser.Profile.Country)) objReturnUser.Profile.Country = objAuthUser.Profile.Country;
            if (!string.IsNullOrEmpty(objAuthUser.Profile.Telephone)) objReturnUser.Profile.Telephone = objAuthUser.Profile.Telephone;
            if (!string.IsNullOrEmpty(objAuthUser.Profile.Fax)) objReturnUser.Profile.Fax = objAuthUser.Profile.Fax;
            if (!string.IsNullOrEmpty(objAuthUser.Profile.Cell)) objReturnUser.Profile.Cell = objAuthUser.Profile.Cell;
            if (!string.IsNullOrEmpty(objAuthUser.Profile.Website)) objReturnUser.Profile.Website = objAuthUser.Profile.Website;
            if (!string.IsNullOrEmpty(objAuthUser.Profile.Photo)) objReturnUser.Profile.Photo = objAuthUser.Profile.Photo;

            var objAuthUserController = new UserController();
            objAuthUserController.UpdateDnnUser(objReturnUser);
        }

        private void CreateUser(UserInfo objUser, ref UserLoginStatus loginStatus)
        {
            UpdateDisplayName(objUser);
            objUser.Membership.Approved = true;

            UserCreateStatus createStatus = DNNUserController.CreateUser(ref objUser);

            var args = new UserCreatedEventArgs(createStatus == UserCreateStatus.Success ? objUser : null)
            {
                CreateStatus = createStatus
            };

            OnUserCreated(args);
            OnUserCreateCompleted(args);

            loginStatus = (createStatus == UserCreateStatus.Success || createStatus == UserCreateStatus.UserAlreadyRegistered)
                ? UserLoginStatus.LOGIN_SUCCESS
                : UserLoginStatus.LOGIN_FAILURE;
        }

        private string RandomizePassword(UserInfo objUser, string strPassword)
        {
            MembershipUser aspNetUser = Membership.GetUser(objUser.Username);
            return RandomizePassword(aspNetUser, objUser, strPassword);
        }

        private string RandomizePassword(MembershipUser aspNetUser, UserInfo objUser, string strPassword)
        {
            string strStoredPassword = string.Empty;

            if (Membership.Provider.EnablePasswordRetrieval && Membership.Provider.PasswordFormat != MembershipPasswordFormat.Hashed)
            {
                strStoredPassword = aspNetUser?.GetPassword();
            }

            if (strStoredPassword == strPassword || string.IsNullOrEmpty(strStoredPassword))
            {
                string strRandomPassword = Utilities.GetRandomPassword();
                DNNUserController.ResetPasswordToken(objUser, 2);
                DNNUserController.ChangePasswordByToken(PortalSettings.PortalId, objUser.Username, strRandomPassword, objUser.PasswordResetToken.ToString());
                return strRandomPassword;
            }

            return strStoredPassword;
        }

        public void AuthenticationLogoff()
        {
            PortalSettings currentPortalSettings = PortalController.Instance.GetCurrentPortalSettings();

            FormsAuthentication.SignOut();
            if (GetStatus(currentPortalSettings.PortalId) == AuthenticationStatus.WinLogon)
            {
                SetStatus(currentPortalSettings.PortalId, AuthenticationStatus.WinLogoff);
            }

            ExpireCookie("portalaliasid");
            ExpireCookie("portalroles");

            string redirectUrl = currentPortalSettings.HomeTabId != -1
                ? Globals.NavigateURL(currentPortalSettings.HomeTabId)
                : Globals.NavigateURL();

            HttpContext.Current.Response.Redirect(redirectUrl, true);
        }

        private static void ExpireCookie(string name)
        {
            HttpContext.Current.Response.Cookies[name].Value = null;
            HttpContext.Current.Response.Cookies[name].Path = "/";
            HttpContext.Current.Response.Cookies[name].Expires = DateTime.Now.AddYears(-30);
        }

        public ADUserInfo ProcessFormAuthentication(string loggedOnUserName, string loggedOnPassword)
        {
            Configuration config = Configuration.GetConfig();
            if (config.WindowsAuthentication)
            {
                string userName = config.StripDomainName
                    ? Utilities.TrimUserDomainName(loggedOnUserName)
                    : loggedOnUserName;

                var objAuthUserController = new UserController();
                return objAuthUserController.GetUser(userName, loggedOnPassword);
            }
            return null;
        }

        public UserInfo GetDnnUser(int portalId, string loggedOnUserName)
        {
            Configuration config = Configuration.GetConfig();
            string userName = config.StripDomainName
                ? Utilities.TrimUserDomainName(loggedOnUserName)
                : loggedOnUserName;

            UserInfo objUser = DNNUserController.GetUserByName(Null.NullInteger, userName);
            if (objUser != null)
            {
                if (DNNUserController.GetUserByName(portalId, userName) == null)
                {
                    objUser.PortalID = portalId;
                    DNNUserController.CreateUser(ref objUser);
                }
                return objUser;
            }
            return null;
        }

        public Array AuthenticationTypes()
        {
            return AuthenticationProvider.Instance(_mProviderTypeName).GetAuthenticationTypes();
        }

        public string NetworkStatus()
        {
            return AuthenticationProvider.Instance(_mProviderTypeName).GetNetworkStatus();
        }

        public static AuthenticationStatus GetStatus(int portalId)
        {
            string authCookies = $"{Configuration.AUTHENTICATION_STATUS_KEY}.{portalId}";
            try
            {
                HttpCookie cookie = HttpContext.Current.Request.Cookies[authCookies];
                if (cookie != null)
                {
                    FormsAuthenticationTicket authenticationTicket = FormsAuthentication.Decrypt(cookie.Value);
                    return (AuthenticationStatus)Enum.Parse(typeof(AuthenticationStatus), authenticationTicket.UserData);
                }
            }
            catch
            {
                // Fallback safe return
            }
            return AuthenticationStatus.Undefined;
        }

        public static void SetStatus(int portalId, AuthenticationStatus status)
        {
            string authCookies = $"{Configuration.AUTHENTICATION_STATUS_KEY}.{portalId}";
            HttpRequest request = HttpContext.Current.Request;
            HttpResponse response = HttpContext.Current.Response;
            int nTimeOut = GetAuthCookieTimeout();

            if (nTimeOut == 0)
            {
                nTimeOut = 60;
            }

            var authenticationTicket = new FormsAuthenticationTicket(
                1,
                authCookies,
                DateTime.Now,
                DateTime.Now.AddMinutes(nTimeOut),
                false,
                status.ToString());

            string strAuthentication = FormsAuthentication.Encrypt(authenticationTicket);

            if (request.Cookies[authCookies] != null)
            {
                request.Cookies[authCookies].Value = null;
                request.Cookies[authCookies].Path = "/";
                request.Cookies[authCookies].Expires = DateTime.Now.AddYears(-1);
            }

            response.Cookies[authCookies].Value = strAuthentication;
            response.Cookies[authCookies].Path = "/";
            response.Cookies[authCookies].Expires = DateTime.Now.AddMinutes(nTimeOut);
        }

        [Obsolete("procedure obsoleted in 5.0.3 - use SynchronizeRoles(UserInfo objUser) instead")]
        public void SynchronizeRoles(string loggedOnUserName, int intUserId)
        {
            var objAuthUserController = new UserController();
            ADUserInfo objAuthUser = objAuthUserController.GetUser(loggedOnUserName);

            if (objAuthUser.IsNotSimplyUser)
            {
                objAuthUser.UserID = intUserId;
                UserController.AddUserRoles(_portalSettings.PortalId, objAuthUser);
                objAuthUserController.UpdateDnnUser(objAuthUser);
            }
        }

        public void SynchronizeRoles(UserInfo objUser)
        {
            var objAuthUserController = new UserController();
            ADUserInfo objAuthUser = objAuthUserController.GetUser(objUser.Username);
            objAuthUser.IsSuperUser = objUser.IsSuperUser;

            if (objAuthUser.IsNotSimplyUser)
            {
                objAuthUser.UserID = objUser.UserID;
                UserController.AddUserRoles(_portalSettings.PortalId, objAuthUser);
            }
        }

        private void UpdateDisplayName(UserInfo objDnnUser)
        {
            PortalSettings currentPortalSettings = PortalController.Instance.GetCurrentPortalSettings();
            object setting = GetSetting(currentPortalSettings.PortalId, "Security_DisplayNameFormat");
            if (setting != null && !string.IsNullOrEmpty(Convert.ToString(setting)))
            {
                objDnnUser.UpdateDisplayName(Convert.ToString(setting));
            }
        }

        public static int GetAuthCookieTimeout()
        {
            XmlDocument configDoc = Config.Load();
            XPathNavigator formsNav = configDoc.CreateNavigator().SelectSingleNode("configuration/system.web/authentication/forms")
                                      ?? configDoc.CreateNavigator().SelectSingleNode("configuration/location/system.web/authentication/forms");

            return formsNav != null ? XmlUtils.GetAttributeValueAsInteger(formsNav, "timeout", 30) : 0;
        }
    }
}