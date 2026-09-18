using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.Exceptions;
using System;
using System.DirectoryServices;
using System.Runtime.InteropServices;
using ADConfiguration = Vvf.Auth.Dipvvf.Components.Config.Configuration;

namespace Vvf.Auth.Dipvvf.Providers.ADSIProvider
{
    #region Enum

    public enum Path
    {
        GC,
        LDAP,
        ADs,
        WinNT
    }

    public enum CompareOperator
    {
        Is,
        IsNot,
        StartsWith,
        EndsWith,
        Present,
        NotPresent
    }

    public enum GroupType
    {
        UNIVERSAL_GROUP = -2147483640,
        GLOBAL_GROUP = -2147483646,
        DOMAIN_LOCAL_GROUP = -2147483644
    }

    public enum UserFlag
    {
        ADS_UF_SCRIPTADS_UF_SCRIPT = 1,
        // 0x1 The logon script is executed. This flag does not work for the ADSI LDAP provider on either read or write operations. For the  ADSI WinNT provider, this flag is  read-only data, and it cannot be set for user objects. = 1    
        ADS_UF_ACCOUNTDISABLE = 2,
        // 0x2 user account is disabled.
        ADS_UF_HOMEDIR_REQUIRED = 8,
        // 0x8 The home directory is required.  
        ADS_UF_LOCKOUT = 16,
        // 0x10 The account is currently locked out.  
        ADS_UF_PASSWD_NOTREQD = 32,
        // 0x20 No password is required.
        ADS_UF_PASSWD_CANT_CHANGE = 64,
        // 0x40 The user cannot change the password. This flag can be read, but not set directly.  For more information and a code example that shows how to prevent a user from changing the password, see User Cannot Change Password. 
        ADS_UF_ENCRYPTED_TEXT_PASSWORD_ALLOWED = 128,
        // 0x80 The user can send an encrypted password. 
        ADS_UF_TEMP_DUPLICATE_ACCOUNT = 256,
        // 0x100 This is an account for users whose primary account is in another domain. This account provides user access to this domain, but not to any domain that trusts this domain. Also known as a  local user account. = 256,    
        ADS_UF_NORMAL_ACCOUNT = 512,
        // 0x200 This is a default account type that represents a typical user.
        ADS_UF_INTERDOMAIN_TRUST_ACCOUNT = 2048,
        // 0x800 This is a permit to trust account for a system domain that trusts other domains.
        ADS_UF_WORKSTATION_TRUST_ACCOUNT = 4096,
        // This is a computer account for a Microsoft Windows NT Workstation/Windows 2000 Professional or Windows NT Server/Windows 2000 Server that is a member of this domain.  0x1000
        ADS_UF_SERVER_TRUST_ACCOUNT = 8192,
        // This is a computer account for a system backup domain controller that is a member of this domain. 0x2000
        ADS_UF_DONT_EXPIRE_PASSWD = 65536,
        // 0x10000 When set, the password will not expire on this account.  
        ADS_UF_MNS_LOGON_ACCOUNT = 131072,
        // 0x20000 This is an MNS logon account. 
        ADS_UF_SMARTCARD_REQUIRED = 262144,
        // 0x40000 When set, this flag will force the user to log on using a smart card. 
        ADS_UF_TRUSTED_FOR_DELEGATION = 524288,
        // 0x80000 When set, the service account (user or computer account), under which a service runs, is trusted for Kerberos delegation. Any such service can impersonate a client requesting the service. To enable a service for Kerberos delegation, set this flag on the  userAccountControl property of the service account. 
        ADS_UF_NOT_DELEGATED = 1048576,
        // 0x100000 When set, the security context of the user will not be delegated to a service even if the service account is set as trusted for Kerberos delegation. 
        ADS_UF_USE_DES_KEY_ONLY = 2097152,
        // 0x200000 Restrict this principal to use only Data Encryption Standard (DES) encryption types for keys.Active Directory Client Extension:  Not supported.
        ADS_UF_DONT_REQUIRE_PREAUTH = 4194304,
        // 0x400000 This account does not require Kerberos preauthentication for logon.Active Directory Client Extension:  Not supported.
        ADS_UF_PASSWORD_EXPIRED = 8388608,
        // 0x800000 The user password has expired. This flag is created by the system using data from the  password last set attribute and the domain policy.  It is read-only and cannot be set. To manually set a user password as expired, use the NetUserSetInfo function with the USER_INFO_3 (usri3_password_expired member) or USER_INFO_4 (usri4_password_expired member) structure.Active Directory Client Extension:  Not supported.
        ADS_UF_TRUSTED_TO_AUTHENTICATE_FOR_DELEGATION = 16777216
        // The account is enabled for delegation. This is a security-sensitive setting; accounts with this option enabled should be strictly controlled. This setting enables a service running under the account to assume a client identity and authenticate as that user to other remote servers on the network.Active Directory Client Extension:  Not supported.
    }

    #endregion

    public class Configuration
    {
        public const string ADSI_CONFIGURATIONNAMIMGCONTEXT = "configurationNamingContext";
        public const string ADSI_DEFAULTNAMIMGCONTEXT = "defaultNamingContext";
        public const string ADSI_ROOTDOMAINNAMIMGCONTEXT = "rootDomainNamingContext";
        public const string ADSI_USERACCOUNTCONTROL = "userAccountControl";
        public const string ADSI_CLASS = "objectClass";
        public const string ADSI_CATEGORY = "objectCategory";
        public const string ADSI_DC = "dc";
        public const string ADSI_NCNAME = "nCName";
        public const string ADSI_DNSROOT = "dnsRoot";
        public const string ADSI_GROUPTYPE = "groupType";
        public const string ADSI_MEMBER = "member";
        public const string ADSI_CNAME = "cn";
        public const string ADSI_ACCOUNTNAME = "sAMAccountName";
        public const string ADSI_DISTINGUISHEDNAME = "distinguishedName";
        public const string ADSI_CANONICALNAME = "canonicalName";
        public const string ADSI_UPN = "userPrincipalName";
        public const string ADSI_DISPLAYNAME = "displayName";
        public const string ADSI_FIRSTNAME = "givenName";
        public const string ADSI_LASTNAME = "sn";
        public const string ADSI_STREET = "streetAddress";
        public const string ADSI_CITY = "l";
        public const string ADSI_POSTALCODE = "postalCode";
        public const string ADSI_REGION = "st";
        public const string ADSI_COUNTRY = "co";
        public const string ADSI_TELEPHONE = "telephoneNumber";
        public const string ADSI_FAX = "facsimileTelephoneNumber";
        public const string ADSI_CELL = "mobile";
        public const string ADSI_HOMEPHONE = "homePhone";
        public const string ADSI_EMAIL = "mail";
        public const string ADSI_WEBSITE = "url";
        public const string ADSI_DESCRIPTION = "description";
        public const string ADSI_EMPLOYEEID = "employeeID";
        public const string ADSI_COMPANY = "company";
        public const string ADSI_MANAGER = "manager";
        public const string ADSI_DEPARTMENT = "department";
        public const string ADSI_ASSISTANT = "assistant";
        public const string ADSI_PHOTO = "thumbnailPhoto";

        private const string ADSI_CONFIG_CACHE_PREFIX = "ADSI.Configuration";

        private int _portalId;
        private int _settingModuleId;

        // mRootDomainPath will be stored in DC=ttt,DC=com,DC=vn format (without ADSIPath)
        // ADSIPath to be added depends on Authentication accessing method
        private bool _adsiNetwork = false;
        private bool _ldapAccesible = false;
        private string _configDomainPath = "";
        // Row value user input in site settings
        private string _defaultEmailDomain = "";
        // Row value user input in site settings - without @
        private string _rootDomainPath = "";
        private string _configurationPath = "";
        private AuthenticationTypes _authenticationType = AuthenticationTypes.Delegation;
        private string _userName = "";
        private string _password = "";
        private int _searchPageSize = 1000;
        private Path _adsiPath = Path.GC;
        private string _processLog = "";

        // For Domain Reference Configuration
        private CrossReferenceCollection _refCollection;

        public Configuration()
        {
            ADConfiguration authConfig = ADConfiguration.GetConfig();
            _portalId = authConfig.PortalId;

            try
            {
                // Temporary fix this setting as TRUE for design, to be removed when release
                _configDomainPath = authConfig.RootDomain;
                _defaultEmailDomain = authConfig.EmailDomain;
                _userName = authConfig.UserName;
                _password = authConfig.Password;
                _authenticationType = (AuthenticationTypes)Enum.Parse(typeof(AuthenticationTypes), authConfig.AuthenticationType);
                // IMPORTANT: Remove ADSIPath, to be added later depends on accessing method

                _rootDomainPath = Utilities.ValidateDomainPath(_configDomainPath);
                _rootDomainPath = _rootDomainPath.Substring(_rootDomainPath.IndexOf("DC="));
            }
            catch (Exception exc)
            {
                _processLog += exc.Message + "<br>";
            }

            // Also check if Authentication implemented in this Windows Network
            DirectoryEntry gc = new DirectoryEntry();
            try
            {
                if (DirectoryEntry.Exists("GC://rootDSE"))
                {
                    DirectoryEntry rootGC;
                    //if ((mUserName.Length > 0) && (mPassword.Length > 0))
                    //    rootGC = new DirectoryEntry("GC://rootDSE", mUserName, mPassword, mAuthenticationType);
                    //else
                    rootGC = new DirectoryEntry("GC://rootDSE");
                    _configurationPath = rootGC.Properties[ADSI_CONFIGURATIONNAMIMGCONTEXT].Value.ToString();
                    _adsiNetwork = true;
                }
            }
            catch (COMException exc)
            {
                _adsiNetwork = false;
                _ldapAccesible = false;
                _processLog += exc.Message + "<br>";
                Exceptions.LogException(exc);
                // Nothing to do if we could not access Global Catalog, so return
                //return;
            }

            // Also check if LDAP fully accessible
            DirectoryEntry ldap = new DirectoryEntry();
            try
            {
                if (DirectoryEntry.Exists("LDAP://rootDSE"))
                {
                    _ldapAccesible = true;
                    _refCollection = new CrossReferenceCollection(_userName, _password, _authenticationType);
                }
            }
            catch (COMException exc)
            {
                _ldapAccesible = false;
                _processLog += exc.Message + "<br>";
                Exceptions.LogException(exc);
            }
        }

        public static Configuration GetConfig()
        {
            PortalSettings _portalSettings = PortalController.Instance.GetCurrentPortalSettings();
            string strKey = ADSI_CONFIG_CACHE_PREFIX + "." + _portalSettings.PortalId.ToString();

            Configuration config = (Configuration)DataCache.GetCache(strKey);
            if (config == null)
            {
                config = new Configuration();
                DataCache.SetCache(strKey, config);
            }

            return config;
        }

        public static void ResetConfig()
        {
            PortalSettings _portalSettings = PortalController.Instance.GetCurrentPortalSettings();
            string strKey = ADSI_CONFIG_CACHE_PREFIX + "." + _portalSettings.PortalId.ToString();
            DataCache.RemoveCache(strKey);
        }

        public void SetSecurity(DirectoryEntry entry)
        {
            try
            {
                entry.AuthenticationType = _authenticationType;
                if ((_userName.Length > 0) && (_password.Length > 0))
                {
                    entry.Username = _userName;
                    entry.Password = _password;
                }
            }
            catch (COMException ex)
            {
                Exceptions.LogException(ex);
            }
        }

        public int PortalId
        {
            get { return _portalId; }
        }

        public string UserName
        {
            get { return _userName; }
        }

        public string Password
        {
            get { return _password; }
        }

        public CrossReferenceCollection RefCollection
        {
            get { return _refCollection; }
        }

        public AuthenticationTypes AuthenticationType
        {
            get { return _authenticationType; }
        }

        public string RootDomainPath
        {
            get { return _rootDomainPath; }
        }

        public string ConfigDomainPath
        {
            get { return _configDomainPath; }
        }

        public string ConfigurationPath
        {
            get { return _configurationPath; }
        }

        public string DefaultEmailDomain
        {
            get { return _defaultEmailDomain; }
        }

        public bool ADSINetwork
        {
            get { return _adsiNetwork; }
        }

        public bool LDAPAccesible
        {
            get { return _ldapAccesible; }
        }

        public string ProcessLog
        {
            get { return _processLog; }
        }

        private string GetValue(object Input, string DefaultValue)
        {
            if (Input == null)
            {
                return DefaultValue;
            }
            else
            {
                return Convert.ToString(Input);
            }
        }
    }
}