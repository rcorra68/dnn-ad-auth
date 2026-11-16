using DotNetNuke.Entities.Portals;
using DotNetNuke.Security.Roles;
using DotNetNuke.Services.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Runtime.InteropServices;
using System.Text;
using Vvf.Auth.Dipvvf.Components;
using Vvf.Auth.Dipvvf.Components.Groups;
using Vvf.Auth.Dipvvf.Components.Users;
using ADConfiguration = Vvf.Auth.Dipvvf.Components.Config.Configuration;

namespace Vvf.Auth.Dipvvf.Providers.ADSIProvider
{
    public class ADSIProvider : AuthenticationProvider
    {
        private PortalSettings _portalSettings = PortalController.Instance.GetCurrentPortalSettings();
        private Configuration _adsiConfig = Configuration.GetConfig();
        private ADConfiguration _config = ADConfiguration.GetConfig();

        #region Private Methods

        private ADUserInfo GetSimplyUser(string UserName)
        {
            ADUserInfo objAuthUser = new ADUserInfo();

            objAuthUser.PortalID = _portalSettings.PortalId;
            objAuthUser.IsNotSimplyUser = false;
            objAuthUser.Username = UserName;
            objAuthUser.FirstName = Utilities.TrimUserDomainName(UserName);
            objAuthUser.LastName = Utilities.GetUserDomainName(UserName);
            objAuthUser.IsSuperUser = false;
            objAuthUser.DistinguishedName = Utilities.ConvertToDistinguished(UserName);

            string strEmail = _adsiConfig.DefaultEmailDomain;
            if (!(strEmail.Length == 0))
            {
                if (strEmail.IndexOf("@") == -1)
                {
                    strEmail = "@" + strEmail;
                }
                strEmail = objAuthUser.FirstName + strEmail;
            }
            else
            {
                strEmail = objAuthUser.FirstName + "@" + objAuthUser.LastName + ".com";
                // confusing?
            }
            // Membership properties
            objAuthUser.Username = UserName;
            objAuthUser.Email = strEmail;
            objAuthUser.Membership.Approved = true;
            objAuthUser.Membership.LastLoginDate = DateTime.Now;
            objAuthUser.Membership.Password = Utilities.GetRandomPassword();
            objAuthUser.AuthenticationExists = false;

            return objAuthUser;
        }

        private bool IsAuthenticated(string Path, string UserName, string Password)
        {
            try
            {
                // Moved to private global for access from other functions - sawest
                // Dim _config As ActiveDirectory.Configuration = ActiveDirectory.Configuration.GetConfig()
                if (_config.StripDomainName)
                {
                    foreach (CrossReferenceCollection.CrossReference crossRef in Configuration.GetConfig().RefCollection)
                    {
                        UserName = crossRef.NetBIOSName + "\\" + UserName;
                    }
                }
                DirectoryEntry userEntry = new DirectoryEntry(Path, UserName, Password, AuthenticationTypes.Signing);
                // Bind to the native AdsObject to force authentication.
                object obj = userEntry.NativeObject;
            }
            catch (COMException)
            {
                return false;
            }

            return true;
        }
        private void FillUserInfo(DirectoryEntry UserEntry, ref ADUserInfo UserInfo)
        {
            UserInfo.IsSuperUser = false;
            UserInfo.Username = UserInfo.Username;
            UserInfo.Membership.Approved = true;
            UserInfo.Membership.LastLoginDate = DateTime.Now;
            if (UserEntry != null)
            {
                if (!(Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_EMAIL].Value) == ""))
                {
                    UserInfo.Email = Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_EMAIL].Value);
                }
                if (!(Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_CNAME].Value.ToString()) == ""))
                {
                    UserInfo.CName = Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_CNAME].Value.ToString());
                }
                if (!(Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_DISPLAYNAME].Value) == ""))
                {
                    UserInfo.DisplayName = Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_DISPLAYNAME].Value);
                }
                if (!(Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_DISTINGUISHEDNAME].Value.ToString()) == ""))
                {
                    UserInfo.DistinguishedName = Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_DISTINGUISHEDNAME].Value.ToString());
                }
                if (!(Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_ACCOUNTNAME].Value.ToString()) == ""))
                {
                    UserInfo.SAMAccountName = Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_ACCOUNTNAME].Value.ToString());
                }
                if (!(Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_CNAME].Value) == ""))
                {
                    UserInfo.Profile.FirstName = Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_FIRSTNAME].Value);
                }
                if (!(Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_LASTNAME].Value) == ""))
                {
                    UserInfo.Profile.LastName = Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_LASTNAME].Value);
                }
                if (!(Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_STREET].Value) == ""))
                {
                    UserInfo.Profile.Street = Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_STREET].Value);
                }
                if (!(Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_CITY].Value) == ""))
                {
                    UserInfo.Profile.City = Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_CITY].Value);
                }
                if (!(Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_REGION].Value) == ""))
                {
                    UserInfo.Profile.Region = Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_REGION].Value);
                }
                if (!(Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_POSTALCODE].Value) == ""))
                {
                    UserInfo.Profile.PostalCode = Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_POSTALCODE].Value);
                }
                if (!(Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_COUNTRY].Value) == ""))
                {
                    UserInfo.Profile.Country = Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_COUNTRY].Value);
                }
                if (!(Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_TELEPHONE].Value) == ""))
                {
                    UserInfo.Profile.Telephone = Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_TELEPHONE].Value);
                }
                if (!(Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_FAX].Value) == ""))
                {
                    UserInfo.Profile.Fax = Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_FAX].Value);
                }
                if (!(Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_CELL].Value) == ""))
                {
                    UserInfo.Profile.Cell = Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_CELL].Value);
                }
                if (!(Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_WEBSITE].Value) == ""))
                {
                    UserInfo.Profile.Website = Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_WEBSITE].Value);
                }
                if (_config.Photo)
                {
                    // sync photo from AD if checked in settings
                    if (!(Utilities.CheckNullString(UserEntry.Properties[Configuration.ADSI_PHOTO].Value) == ""))
                    {
                        UserInfo.Profile.Photo = Utilities.WritePhoto(UserInfo, (byte[])UserEntry.Properties[Configuration.ADSI_PHOTO].Value);
                    }
                }
            }

            if (UserInfo.Email == "")
            {
                UserInfo.Email = Utilities.TrimUserDomainName(UserInfo.Username) + _adsiConfig.DefaultEmailDomain;
            }
            if (UserInfo.DisplayName == "")
            {
                UserInfo.DisplayName = UserInfo.CName;
            }

            UserInfo.AuthenticationExists = true;
            // obtain firstname from username if admin has not enter enough user info
            if (UserInfo.Profile.FirstName.Length == 0)
            {
                UserInfo.Profile.FirstName = Utilities.TrimUserDomainName(UserInfo.Username);
            }
        }

        #endregion

        public override ADUserInfo GetUser(string LoggedOnUserName, string LoggedOnPassword)
        {
            ADUserInfo objAuthUser;

            if (!_adsiConfig.ADSINetwork)
            {
                return null;
            }

            try
            {
                DirectoryEntry entry = Utilities.GetUserEntryByName(LoggedOnUserName);
#if DEBUG
                foreach (string key in entry.Properties.PropertyNames)
                {
                    string sPropertyValues = "";
                    foreach (object value in entry.Properties[key])
                    {
                        sPropertyValues += Convert.ToString(value) + ";";
                    }
                    sPropertyValues = sPropertyValues.Substring(0, sPropertyValues.Length - 1);
                    System.Diagnostics.Debug.Print(key + "=" + sPropertyValues);
                }
#endif
                // Check authenticated
                string path;
                if (entry != null)
                {
                    path = entry.Path;
                }
                else
                {
                    path = _adsiConfig.RootDomainPath;
                }
                if (!IsAuthenticated(path, LoggedOnUserName, LoggedOnPassword))
                {
                    return null;
                }

                // Return authenticated if no error 
                objAuthUser = new ADUserInfo();
                // ACD-6760
                InitializeUser(objAuthUser);
                string location = Utilities.GetEntryLocation(entry);
                if (location.Length == 0)
                {
                    location = _adsiConfig.ConfigDomainPath;
                }

                objAuthUser.PortalID = _portalSettings.PortalId;
                objAuthUser.IsNotSimplyUser = true;
                objAuthUser.Username = LoggedOnUserName;
                objAuthUser.Membership.Password = LoggedOnPassword;

                FillUserInfo(entry, ref objAuthUser);

                return objAuthUser;
            }
            catch (Exception exc)
            {
                Exceptions.LogException(exc);
                return null;
            }
        }

        public override ADUserInfo GetUser(string LoggedOnUserName)
        {
            ADUserInfo objAuthUser;
            try
            {
                if (_adsiConfig.ADSINetwork)
                {
                    DirectoryEntry entry;

                    entry = Utilities.GetUserEntryByName(LoggedOnUserName);
#if DEBUG
                    foreach (string key in entry.Properties.PropertyNames)
                    {
                        string sPropertyValues = "";
                        foreach (object value in entry.Properties[key])
                        {
                            sPropertyValues += Convert.ToString(value) + ";";
                        }
                        sPropertyValues = sPropertyValues.Substring(0, sPropertyValues.Length - 1);
                        System.Diagnostics.Debug.Print(key + "=" + sPropertyValues);
                    }
#endif

                    if (entry != null)
                    {
                        objAuthUser = new ADUserInfo();
                        // ACD-6760
                        InitializeUser(objAuthUser);
                        string location = Utilities.GetEntryLocation(entry);
                        if (location.Length == 0)
                        {
                            location = _adsiConfig.ConfigDomainPath;
                        }

                        objAuthUser.PortalID = _portalSettings.PortalId;
                        objAuthUser.IsNotSimplyUser = true;
                        objAuthUser.Username = LoggedOnUserName;
                        objAuthUser.Membership.Password = Utilities.GetRandomPassword();

                        FillUserInfo(entry, ref objAuthUser);
                    }
                    else
                    {
                        objAuthUser = GetSimplyUser(LoggedOnUserName);
                    }
                }
                else // could not find it in AD, so populate user object with minumum info
                {
                    objAuthUser = GetSimplyUser(LoggedOnUserName);
                }

                return objAuthUser;
            }
            catch (COMException exc)
            {
                Exceptions.LogException(exc);
                return null;
            }
        }

        public override ArrayList GetGroups()
        {
            // Normally number of roles in DNN less than groups in Authentication,
            // so start from DNN roles to get better performance
            try
            {
                ArrayList colGroup = new ArrayList();
                RoleController roleController = new RoleController();
                IList<RoleInfo> listRoles = roleController.GetRoles(_portalSettings.PortalId);
                ArrayList AllAdGroupNames = Utilities.GetAllGroupnames();

                foreach (RoleInfo objRole in listRoles)
                {
                    // Auto assignment roles have been added by DNN, so don't need to get them
                    if (!objRole.AutoAssignment)
                    {
                        // It's possible in multiple domains network that search result return more than one group with the same name (i.e Administrators)
                        // We better check them all
                        if (AllAdGroupNames.Contains(objRole.RoleName))
                        {
                            GroupInfo group = new GroupInfo();

                            group.PortalID = objRole.PortalID;
                            group.RoleID = objRole.RoleID;
                            group.RoleName = objRole.RoleName;
                            group.Description = objRole.Description;
                            group.ServiceFee = objRole.ServiceFee;
                            group.BillingFrequency = objRole.BillingFrequency;
                            group.TrialPeriod = objRole.TrialPeriod;
                            group.TrialFrequency = objRole.TrialFrequency;
                            group.BillingPeriod = objRole.BillingPeriod;
                            group.TrialFee = objRole.TrialFee;
                            group.IsPublic = objRole.IsPublic;
                            group.AutoAssignment = objRole.AutoAssignment;

                            colGroup.Add(group);
                        }
                    }
                }

                return colGroup;
            }
            catch (COMException exc)
            {
                Exceptions.LogException(exc);
                return null;
            }
        }

        public override ArrayList GetGroups(ArrayList arrUserPortalRoles)
        {
            // Normally number of roles in DNN less than groups in Authentication,
            // so start from DNN roles to get better performance
            try
            {
                ArrayList colGroup = new ArrayList();
                //Dim objRoleController As New RoleController
                //Dim lstRoles As ArrayList = objRoleController.GetPortalRoles(_portalSettings.PortalId)
                //Dim AllAdGroupNames As ArrayList = Utilities.GetAllGroupnames

                foreach (RoleInfo objRole in arrUserPortalRoles)
                {
                    // Auto assignment roles have been added by DNN, so don't need to get them
                    if (!objRole.AutoAssignment)
                    {
                        // It's possible in multiple domains network that search result return more than one group with the same name (i.e Administrators)
                        // We better check them all
                        foreach (DirectoryEntry entry in Utilities.GetGroupEntriesByName(objRole.RoleName))
                        {
                            GroupInfo group = new GroupInfo();

                            group.PortalID = objRole.PortalID;
                            group.RoleID = objRole.RoleID;
                            group.RoleName = objRole.RoleName;
                            group.Description = objRole.Description;
                            group.ServiceFee = objRole.ServiceFee;
                            group.BillingFrequency = objRole.BillingFrequency;
                            group.TrialPeriod = objRole.TrialPeriod;
                            group.TrialFrequency = objRole.TrialFrequency;
                            group.BillingPeriod = objRole.BillingPeriod;
                            group.TrialFee = objRole.TrialFee;
                            group.IsPublic = objRole.IsPublic;
                            group.AutoAssignment = objRole.AutoAssignment;

                            colGroup.Add(group);
                        }
                    }
                }

                return colGroup;
            }
            catch (COMException exc)
            {
                Exceptions.LogException(exc);
                return null;
            }
        }

        public override Array GetAuthenticationTypes()
        {
            return Enum.GetValues(typeof(AuthenticationTypes));
        }

        public override string GetNetworkStatus()
        {
            StringBuilder sb = new StringBuilder();
            // Refresh settings cache first
            Configuration.ResetConfig();
            _adsiConfig = Configuration.GetConfig();

            sb.Append("<b>[Global Catalog Status]</b>" + "<br>");
            try
            {
                if (_adsiConfig.ADSINetwork)
                {
                    sb.Append("OK<br>");
                }
                else
                {
                    sb.Append("FAIL<br>");
                }
            }
            catch (COMException ex)
            {
                sb.Append("FAIL<br>");
                sb.Append(ex.Message + "<br>");
            }

            sb.Append("<b>[Root Domain Status]</b><br>");
            try
            {
                if (Utilities.GetRootEntry() != null)
                {
                    sb.Append("OK<br>");
                }
                else
                {
                    sb.Append("FAIL<br>");
                }
            }
            catch (COMException ex)
            {
                sb.Append("FAIL<br>");
                sb.Append(ex.Message + "<br>");
            }

            sb.Append("<b>[LDAP Status]</b><br>");
            try
            {
                if (_adsiConfig.LDAPAccesible)
                {
                    sb.Append("OK<br>");
                }
                else
                {
                    sb.Append("FAIL<br>");
                }
            }
            catch (COMException ex)
            {
                sb.Append("FAIL<br>");
                sb.Append(ex.Message + "<br>");
            }

            sb.Append("<b>[Network Domains Status]</b><br>");
            try
            {
                if (_adsiConfig.RefCollection != null && _adsiConfig.RefCollection.Count > 0)
                {
                    sb.Append(_adsiConfig.RefCollection.Count.ToString());
                    sb.Append(" Domain(s):<br>");
                    foreach (CrossReferenceCollection.CrossReference crossRef in _adsiConfig.RefCollection)
                    {
                        sb.Append(crossRef.CanonicalName);
                        sb.Append(" (");
                        sb.Append(crossRef.NetBIOSName);
                        sb.Append(")<br>");
                    }

                    if (_adsiConfig.RefCollection.ProcesssLog.Length > 0)
                    {
                        sb.Append(_adsiConfig.RefCollection.ProcesssLog + "<br>");
                    }
                }
                else
                {
                    sb.Append("[LDAP Error Message]<br>");
                }
            }
            catch (COMException ex)
            {
                sb.Append("[LDAP Error Message]<br>");
                sb.Append(ex.Message + "<br>");
            }

            if (_adsiConfig.ProcessLog.Length > 0)
            {
                sb.Append(_adsiConfig.ProcessLog + "<br>");
            }

            return sb.ToString();
        }

        /// -------------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <history>
        ///     [tamttt]	08/01/2004	Created
        ///     [mhorton]   27/04/2004  Initially the preferred local was set to the 
        ///                             CurrentCulture. Occasionaly this is reset to English and it 
        ///                             overwrites the user's Preferredlocale. I set it here to always
        ///                             use the portal's language setting.
        ///     [mhorton]   27/04/2009 Initialize the TimeZone.
        /// </history>
        /// -------------------------------------------------------------------
        private void InitializeUser(ADUserInfo objUser)
        {
            objUser.Profile.InitialiseProfile(_portalSettings.PortalId);

            // ACD-9442
            objUser.Profile.PreferredLocale = _portalSettings.DefaultLanguage;
            objUser.Profile.PreferredTimeZone = _portalSettings.TimeZone;
        }
    }
}