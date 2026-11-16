using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.FileSystem;
using DotNetNuke.Services.Log.EventLog;
using System;
using System.Collections;
using System.DirectoryServices;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Web;
using Vvf.Auth.Dipvvf.Components.Common;
using Vvf.Auth.Dipvvf.Components.Users;
using SecurityException = System.Security.SecurityException;

namespace Vvf.Auth.Dipvvf.Providers.ADSIProvider
{
    public class Utilities
    {
        public static EventLogController objEventLog = new EventLogController();
        public const string AD_IMAGE_FOLDER_PATH = "Images/AD Photos";

        public Utilities()
        {
        }

        public static Domain GetRootDomain(Path ADSIPath)
        {
            try
            {
                Configuration adsiConfig = Configuration.GetConfig();

                string rootDomainFullPath = AddADSIPath(adsiConfig.RootDomainPath, ADSIPath);
                Domain rootDomainEntry = Domain.GetDomain(rootDomainFullPath, adsiConfig.UserName, adsiConfig.Password, adsiConfig.AuthenticationType);
                return rootDomainEntry;
            }
            catch (COMException exc)
            {
                Exceptions.LogException(exc);
                return null;
            }
        }

        public static Domain GetRootDomain()
        {
            try
            {
                Configuration adsiConfig = Configuration.GetConfig();

                string rootDomainFullPath = AddADSIPath(adsiConfig.RootDomainPath);
                Domain rootDomainEntry = Domain.GetDomain(rootDomainFullPath, adsiConfig.UserName, adsiConfig.Password, adsiConfig.AuthenticationType);
                return rootDomainEntry;
            }
            catch (COMException exc)
            {
                Exceptions.LogException(exc);
                return null;
            }
        }

        public static Domain GetDomainByBIOSName(string Name)
        {
            Configuration adsiConfig = Configuration.GetConfig();

            // Only access CrossRefCollection if LDAP is accessible
            if (adsiConfig.RefCollection != null && adsiConfig.RefCollection.Count > 0)
            {
                CrossReferenceCollection.CrossReference refObject = adsiConfig.RefCollection.ItemByNetBIOS(Name);
                string path = AddADSIPath(refObject.DomainPath);
                Domain domain = Domain.GetDomain(path, adsiConfig.UserName, adsiConfig.Password, adsiConfig.AuthenticationType);

                return domain;
            }
            else
            {
                return null;
            }
        }

        public static DirectoryEntry GetRootEntry()
        {
            return GetRootEntry(Path.GC);
        }

        public static DirectoryEntry GetRootEntry(Path ADSIPath)
        {
            try
            {
                Configuration adsiConfig = Configuration.GetConfig();
                DirectoryEntry entry = null;
                if (adsiConfig != null)
                {
                    string rootDomainFullPath = AddADSIPath(adsiConfig.RootDomainPath, ADSIPath);
                    if (rootDomainFullPath != null)
                    {
                        entry = GetDirectoryEntry(rootDomainFullPath);
                    }
                }
                if (entry != null && entry.Name.Length > 0)
                {
                    return entry;
                }
                else
                {
                    return null;
                }
            }
            catch (COMException exc)
            {
                Exceptions.LogException(exc);
                return null;
            }
        }

        public static DirectoryEntry GetDirectoryEntry(string Path)
        {
            Configuration adsiConfig = Configuration.GetConfig();
            DirectoryEntry returnEntry;

            if ((adsiConfig.UserName.Length > 0) && (adsiConfig.Password.Length > 0))
            {
                returnEntry = new DirectoryEntry(Path, adsiConfig.UserName, adsiConfig.Password, AuthenticationTypes.Delegation);
            }
            else
            {
                returnEntry = new DirectoryEntry(Path);
            }

            return returnEntry;
        }

        public static string GetRootForestPath(Path ADSIPath = Path.GC)
        {
            try
            {
                string strADSIPath = ADSIPath.ToString() + "://";
                DirectoryEntry ADsRoot = new DirectoryEntry(strADSIPath + "rootDSE");
                string strRootDomain = strADSIPath + (string)ADsRoot.Properties[Configuration.ADSI_ROOTDOMAINNAMIMGCONTEXT].Value;

                return strRootDomain;
            }
            catch (COMException ex)
            {
                Exceptions.LogException(ex);
                return null;
            }
        }

        public static string GetEntryLocation(DirectoryEntry Entry)
        {
            string strReturn = "";
            if (Entry != null)
            {
                string entryPath = CheckNullString(Entry.Path);

                if (entryPath.Length > 0)
                {
                    strReturn = entryPath.Substring(entryPath.IndexOf("DC="));
                    strReturn = ConvertToCanonical(strReturn, false);
                }
            }

            return strReturn;
        }

        public static ArrayList GetAllGroupnames()
        {
            Domain RootDomain = GetRootDomain();
            Search objSearch = new Search(RootDomain);

            objSearch.AddFilter(Configuration.ADSI_CLASS, CompareOperator.Is, ObjectClass.Group.ToString());
            objSearch.PropertiesToLoad.Add(Configuration.ADSI_CNAME);

            return objSearch.GetPropertyEntries(Configuration.ADSI_CNAME);
        }

        public static DirectoryEntry GetUserEntryByName(string Name)
        {
            // Create search object then assign required params to get user entry in Active Directory
            Search objSearch = new Search(GetRootDomain());
            ArrayList userEntries;
            Domain userDomain;

            objSearch.AddFilter(Configuration.ADSI_CLASS, CompareOperator.Is, ObjectClass.Person.ToString());
            objSearch.AddFilter(Configuration.ADSI_ACCOUNTNAME, CompareOperator.Is, TrimUserDomainName(Name));

            userEntries = objSearch.GetEntries();
            switch (userEntries.Count)
            {
                case 0:
                    // Found no entry, return nothing
                    return null;
                case 1:
                    // Find only one entry, return it
                    return (DirectoryEntry)userEntries[0];
                default:
                    // Find more than one entry, so we have to check to obtain correct user
                    // Get user domain
                    userDomain = GetDomainByBIOSName(GetUserDomainName(Name));
                    if (userDomain != null)
                    {
                        foreach (DirectoryEntry userEntry in userEntries)
                        {
                            string entryPath = userEntry.Path;
                            string entryLocation = entryPath.Substring(entryPath.IndexOf("DC="));
                            if (entryLocation.ToLower() == userDomain.DistinguishedName.ToLower())
                            {
                                return userEntry;
                            }
                        }
                    }
                    else
                    {
                        // If an error occurs while accessing LDAP (i.e double-hop issue), we return the first entry
                        // This method not very accurately, however it would be OK for ALMOST network
                        return (DirectoryEntry)userEntries[0];
                    }

                    break;
            }

            return null;
        }

        public static string CanonicalToNetBIOS(string CanonicalName)
        {
            Configuration config = Configuration.GetConfig();

            // Only access CrossRefCollection if LDAP is accessible
            if (config.RefCollection != null && config.RefCollection.Count > 0)
            {
                CrossReferenceCollection.CrossReference refObject = config.RefCollection.Item(CanonicalName);
                if (refObject != null)
                {
                    return refObject.mNetBIOSName;
                }
                else
                {
                    return string.Empty;
                }
            }
            else
            {
                return string.Empty;
            }
        }

        public static string UPNToLogonName0(string UserPrincipalName)
        {
            Configuration config = Configuration.GetConfig();
            string userName = UserPrincipalName;

            if (config.LDAPAccesible)
            {
                string userDomain = UserPrincipalName.Substring(UserPrincipalName.IndexOf("@") + 1);
                string userNetBIOS = CanonicalToNetBIOS(userDomain);
                if (!(userNetBIOS.Length == 0))
                {
                    userName = userNetBIOS + "\\" + TrimUserDomainName(UserPrincipalName);
                }
            }

            return userName;
        }

        public static string GetUserDomainName(string UserName)
        {
            string strReturn = "";
            if (UserName.IndexOf("\\") > 0)
            {
                strReturn = UserName.Substring(0, UserName.IndexOf("\\"));
            }
            return strReturn;
        }

        public static string TrimUserDomainName(string UserName)
        {
            string strReturn;
            if (UserName.IndexOf("\\") > -1)
            {
                strReturn = UserName.Substring(UserName.IndexOf("\\") + 1);
            }
            else if (UserName.IndexOf("@") > -1)
            {
                strReturn = UserName.Substring(0, UserName.IndexOf("@"));
            }
            else
            {
                strReturn = UserName;
            }

            return strReturn;
        }

        public static string AddADSIPath(string Path, Path ADSIPath = Path.GC)
        {
            if (Path.IndexOf("LDAP://") != -1)
            {
                return Path;
            }
            else if (Path.IndexOf("://") != -1)
            {
                // Clean existing ADs path first
                Path = Path.Substring(Path.IndexOf("://") + 3);
            }
            return ADSIPath.ToString() + "://" + Path;
        }

        public static string ValidateDomainPath(string Path, Path ADSIPath = Path.GC)
        {
            // If root domain is not specified in site settings, we start from top root forest
            if (Path.Length == 0)
            {
                return GetRootForestPath();
            }
            else if ((Path.IndexOf("DC=") != -1) && (Path.IndexOf("://") != -1))
            {
                return Path;
            }
            else if ((Path.IndexOf("LDAP://") != -1) && (Path.IndexOf("://") != -1))
            {
                return Path;
            }
            else if (Path.IndexOf(".") != -1)
            {
                // "ttt.com.vn" format,  it's possible for "LDAP://ttt.com.vn" format to access Authentication, however GC:// gives better performance
                return ConvertToDistinguished(Path);
            }
            else
            {
                // Invalid path, so we get root path from Active Directory
                return GetRootForestPath();
            }
        }

        public static string ConvertToDistinguished(string Canonical, Path ADSIPath = Path.GC)
        {
            string strDistinguished;

            // Clean up ADSI.Path to make sure we get a proper path
            if (Canonical.IndexOf("://") != -1)
            {
                strDistinguished = Canonical.Substring(Canonical.IndexOf("://") + 3);
            }
            else
            {
                strDistinguished = Canonical;
            }

            strDistinguished = strDistinguished.Replace(".", ",DC=");
            strDistinguished = "DC=" + strDistinguished;

            if (Canonical.IndexOf("://") != -1)
            {
                strDistinguished = AddADSIPath(strDistinguished, ADSIPath);
            }

            return strDistinguished;
        }

        public static string ConvertToCanonical(string Distinguished, bool IncludeADSIPath)
        {
            string strCanonical = Distinguished;

            if (!IncludeADSIPath && Distinguished.IndexOf("://") != -1)
            {
                strCanonical = Distinguished.Substring(Distinguished.IndexOf("://") + 3);
            }

            strCanonical = strCanonical.Replace("DC=", "");
            strCanonical = strCanonical.Replace("dc=", "");
            strCanonical = strCanonical.Replace("CN=", "");
            strCanonical = strCanonical.Replace("cn=", "");
            strCanonical = strCanonical.Replace(",", ".");

            return strCanonical;
        }

        public static string CheckNullString(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }
            else
            {
                return value.ToString();
            }
        }

        public static string GetRandomPassword()
        {
            Random rd = new Random();
            return Convert.ToString(rd.Next());
        }

        // See http://www.aspalliance.com/bbilbro/viewarticle.aspx?paged_article_id=4
        public static string ReplaceCaseInsensitive(string text, string oldValue, string newValue)
        {
            oldValue = GetCaseInsensitiveSearch(oldValue);

            return Regex.Replace(text, oldValue, newValue);
        }
        // ReplaceCaseInsensitive

        public static string GetCaseInsensitiveSearch(string search)
        {
            string result = string.Empty;

            for (int index = 0; index <= search.Length - 1; index++)
            {
                char character = search[index];
                char characterLower = char.ToLower(character);
                char characterUpper = char.ToUpper(character);

                if (characterUpper == characterLower)
                {
                    result = result + character;
                }
                else
                {
                    result = result + "[" + characterLower + characterUpper + "]";
                }
            }
            return result;
        }
        // GetCaseInsensitiveSearch

        // ACD-7422 - Role Synchronization Not Working On W2K Domain Controllers
        // By using TokenGroups it should work with W2K.
        public static ArrayList GetADGroups(string Name)
        {
            DirectoryEntry user = GetUserEntryByName(Name);
            IdentityReferenceCollection irc = ExpandTokenGroups(user).Translate(typeof(NTAccount));
            ArrayList arrAccounts = new ArrayList();

            foreach (IdentityReference account in irc)
            {
                if (account is NTAccount)
                {
#if DEBUG
                    System.Diagnostics.Debug.Print("Account=" + account.Value);
#endif
                    //arrAccounts.Add(account.Value)
                    // Trim the leading Group Name off the group (i.e. Remove DOMAIN\ from DOMAIN\Group)
                    if (account.Value.IndexOf("\\") >= 0)
                    {
                        if (!(arrAccounts.Contains(account.Value.Substring(account.Value.IndexOf("\\") + 1))))
                        {
                            arrAccounts.Add(account.Value.Substring(account.Value.IndexOf("\\") + 1));
                        }
                    }
                    else
                    {
                        arrAccounts.Add(account.Value);
                    }
                }
            }

            return arrAccounts;
        }

        private static IdentityReferenceCollection ExpandTokenGroups(DirectoryEntry user)
        {
            user.RefreshCache(new string[] { "tokenGroups" });

            IdentityReferenceCollection irc = new IdentityReferenceCollection();

            foreach (byte[] sidBytes in user.Properties["tokenGroups"])
            {
                irc.Add(new SecurityIdentifier(sidBytes, 0));
            }
            return irc;
        }

        public static string GetIP4Address(string strPassedIP)
        {
            string IP4Address = string.Empty;

            foreach (IPAddress IPA in Dns.GetHostAddresses(strPassedIP))
            {
                if (IPA.AddressFamily.ToString() == "InterNetwork")
                {
                    IP4Address = IPA.ToString();
                    break;
                }
            }

            if (IP4Address != string.Empty)
            {
                return IP4Address;
            }

            foreach (IPAddress IPA in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (IPA.AddressFamily.ToString() == "InterNetwork")
                {
                    IP4Address = IPA.ToString();
                    break;
                }
            }

            return IP4Address;
        }

        public static AspNetHostingPermissionLevel GetCurrentTrustLevel()
        {
            foreach (AspNetHostingPermissionLevel trustLevel in new AspNetHostingPermissionLevel[]
                     {
                         AspNetHostingPermissionLevel.Unrestricted, AspNetHostingPermissionLevel.High,
                         AspNetHostingPermissionLevel.Medium, AspNetHostingPermissionLevel.Low,
                         AspNetHostingPermissionLevel.Minimal
                     })
            {
                try
                {
                    AspNetHostingPermission perm = new AspNetHostingPermission(trustLevel);
                    perm.Demand();
                }
                catch (SecurityException)
                {
                    continue;
                }

                return trustLevel;
            }

            return AspNetHostingPermissionLevel.None;
        }

        public static ArrayList GetGroupEntriesByName(string GroupName)
        {
            Domain RootDomain = GetRootDomain();
            Search objSearch = new Search(RootDomain);

            objSearch.AddFilter(Configuration.ADSI_CLASS, CompareOperator.Is, ObjectClass.Group.ToString());
            objSearch.AddFilter(Configuration.ADSI_ACCOUNTNAME, CompareOperator.Is, GroupName);

            ArrayList groupEntries = objSearch.GetEntries();

            if (groupEntries != null)
            {
                return groupEntries;
            }
            else
            {
                return null;
            }
        }

        public static bool AddEventLog(DotNetNuke.Entities.Portals.PortalSettings portalsettings, string description)
        {
            // NOTE: the original VB function never assigns a return value (no "AddEventLog = ..." / "Return ..."
            // after the AddLog call), so it always returns the default Boolean value (False), regardless of the
            // outcome of AddLog. Preserved as-is for a faithful 1:1 port; worth revisiting in the refactor pass.
            objEventLog.AddLog("Description", description, portalsettings, -1, EventLogController.EventLogType.ADMIN_ALERT);
            return false;
        }

        public static string WritePhoto(ADUserInfo adUserInfo, byte[] photo)
        {
            IFolderInfo _folderinfo;
            IFileInfo _fileinfo;

            _folderinfo = FolderManager.Instance.GetUserFolder(adUserInfo);

            if (_folderinfo != null)
            {
                using (System.IO.MemoryStream stream = new System.IO.MemoryStream(photo))
                {
                    string fname = string.Format("{0}_profile_photo.jpg", adUserInfo.Username.Replace("\\", "_"));
                    _fileinfo = FileManager.Instance.AddFile(_folderinfo, fname, stream);
                    stream.Close();
                    _folderinfo = null;
                    if (_fileinfo != null)
                    {
                        return _fileinfo.FileId.ToString();
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
            }
            else
            {
                return string.Empty;
            }
        }
    }
}