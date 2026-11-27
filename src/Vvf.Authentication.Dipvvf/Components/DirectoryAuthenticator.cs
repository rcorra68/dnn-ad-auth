namespace Vvf.Authentication.Dipvvf.Components
{
    using System;
    using System.DirectoryServices;
    using System.DirectoryServices.AccountManagement;
    using System.Runtime.InteropServices;
    using System.Text;
    using DotNetNuke.Instrumentation;

    public enum DirectoryAuthenticationStatus
    {
        Success,
        InvalidCredentials,
        DirectoryUnavailable
    }

    public class DirectoryAuthenticationResult
    {
        public DirectoryAuthenticationResult(DirectoryAuthenticationStatus status, string samAccountName)
        {
            Status = status;
            SamAccountName = samAccountName;
        }

        public DirectoryAuthenticationStatus Status { get; private set; }

        /// <summary>
        /// sAMAccountName (codice fiscale) of the authenticated user; null unless Status is Success.
        /// </summary>
        public string SamAccountName { get; private set; }

        /// <summary>Directory attributes, filled only when Status is Success.</summary>
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string DisplayName { get; set; }

        public string Email { get; set; }

        /// <summary>userPrincipalName as stored in AD (e.g. nome1.cognome@dipvvf.it).</summary>
        public string UserPrincipalName { get; set; }

        public static DirectoryAuthenticationResult Failed(DirectoryAuthenticationStatus status)
        {
            return new DirectoryAuthenticationResult(status, null);
        }
    }

    public interface IDirectoryAuthenticator
    {
        DirectoryAuthenticationResult Authenticate(string accountName, string password);
    }

    /// <summary>
    /// Validates credentials against Active Directory using the user principal
    /// name (accountName@domain), then reads the sAMAccountName and profile of
    /// that user with the application pool identity.
    /// </summary>
    public class DirectoryAuthenticator : IDirectoryAuthenticator
    {
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(DirectoryAuthenticator));

        // Explicit options: Kerberos/NTLM with signing and sealing, and no
        // automatic fallback to a clear-text simple bind.
        private const ContextOptions BindOptions =
            ContextOptions.Negotiate | ContextOptions.Signing | ContextOptions.Sealing;

        // Same guarantees for the DirectoryEntry used by the lookup.
        private const AuthenticationTypes SearchAuthentication =
            AuthenticationTypes.Secure | AuthenticationTypes.Signing | AuthenticationTypes.Sealing;

        private readonly string _domain;

        public DirectoryAuthenticator(string domain)
        {
            if (string.IsNullOrEmpty(domain) || domain.Trim().Length == 0)
            {
                throw new ArgumentException("Domain is required.", "domain");
            }

            _domain = domain.Trim();
        }

        public DirectoryAuthenticationResult Authenticate(string accountName, string password)
        {
            // An empty password would turn into an unauthenticated LDAP bind,
            // which the server accepts: never let it reach the directory.
            if (string.IsNullOrEmpty(accountName) || string.IsNullOrEmpty(password))
            {
                return DirectoryAuthenticationResult.Failed(DirectoryAuthenticationStatus.InvalidCredentials);
            }

            string userPrincipalName = accountName + "@" + _domain;

            try
            {
                // ValidateCredentials only binds as the user: it never runs the
                // PrincipalContext initialization that fails here with 0x8007200A.
                using (PrincipalContext context = new PrincipalContext(ContextType.Domain, _domain))
                {
                    if (!context.ValidateCredentials(userPrincipalName, password, BindOptions))
                    {
                        return DirectoryAuthenticationResult.Failed(DirectoryAuthenticationStatus.InvalidCredentials);
                    }
                }

                DirectoryAuthenticationResult result = FindUser(userPrincipalName);

                if (result == null)
                {
                    Logger.Error("Credentials valid for '" + userPrincipalName + "' but the user could not be read from the directory.");
                    return DirectoryAuthenticationResult.Failed(DirectoryAuthenticationStatus.InvalidCredentials);
                }

                return result;
            }
            catch (PrincipalServerDownException ex)
            {
                Logger.Error("Active Directory domain '" + _domain + "' is unreachable.", ex);
                return DirectoryAuthenticationResult.Failed(DirectoryAuthenticationStatus.DirectoryUnavailable);
            }
            catch (PrincipalException ex)
            {
                Logger.Error("Active Directory error while authenticating '" + userPrincipalName + "'.", ex);
                return DirectoryAuthenticationResult.Failed(DirectoryAuthenticationStatus.DirectoryUnavailable);
            }
            catch (COMException ex)
            {
                // Also covers DirectoryServicesCOMException
                Logger.Error("Active Directory error while authenticating '" + userPrincipalName + "' (0x" + ex.ErrorCode.ToString("X8") + ").", ex);
                return DirectoryAuthenticationResult.Failed(DirectoryAuthenticationStatus.DirectoryUnavailable);
            }
        }

        /// <summary>
        /// Reads sAMAccountName and profile of the user with the given UPN,
        /// or returns null if the user is not found. Plain LDAP search with the
        /// application pool identity (on a domain-joined server: the computer
        /// account): unlike PrincipalContext it does not read the schema.
        /// </summary>
        private DirectoryAuthenticationResult FindUser(string userPrincipalName)
        {
            using (DirectoryEntry root = new DirectoryEntry("LDAP://" + _domain, null, null, SearchAuthentication))
            using (DirectorySearcher searcher = new DirectorySearcher(root))
            {
                searcher.Filter = "(&(objectCategory=person)(objectClass=user)(userPrincipalName="
                    + EscapeLdapFilterValue(userPrincipalName) + "))";
                searcher.SearchScope = SearchScope.Subtree;
                searcher.PropertiesToLoad.Add("sAMAccountName");
                searcher.PropertiesToLoad.Add("givenName");
                searcher.PropertiesToLoad.Add("sn");
                searcher.PropertiesToLoad.Add("displayName");
                searcher.PropertiesToLoad.Add("mail");
                searcher.PropertiesToLoad.Add("userPrincipalName");

                SearchResult found = searcher.FindOne();

                string samAccountName = GetProperty(found, "sAMAccountName");

                if (string.IsNullOrEmpty(samAccountName))
                {
                    return null;
                }

                DirectoryAuthenticationResult result = new DirectoryAuthenticationResult(DirectoryAuthenticationStatus.Success, samAccountName);
                result.FirstName = GetProperty(found, "givenName");
                result.LastName = GetProperty(found, "sn");
                result.DisplayName = GetProperty(found, "displayName");
                result.Email = GetProperty(found, "mail");
                result.UserPrincipalName = GetProperty(found, "userPrincipalName");
                return result;
            }
        }

        private static string GetProperty(SearchResult result, string name)
        {
            if (result == null || !result.Properties.Contains(name) || result.Properties[name].Count == 0)
            {
                return string.Empty;
            }

            return Convert.ToString(result.Properties[name][0]);
        }

        /// <summary>
        /// Escapes the characters with a special meaning in an LDAP filter (RFC 4515).
        /// </summary>
        private static string EscapeLdapFilterValue(string value)
        {
            StringBuilder builder = new StringBuilder(value.Length);

            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\': builder.Append(@"\5c"); break;
                    case '*': builder.Append(@"\2a"); break;
                    case '(': builder.Append(@"\28"); break;
                    case ')': builder.Append(@"\29"); break;
                    case '\0': builder.Append(@"\00"); break;
                    default: builder.Append(c); break;
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Reduces what the user typed to the bare account name:
        /// "Nome.Cognome", "nome.cognome@dipvvf.it" and "DIPVVF\nome.cognome"
        /// all become "nome.cognome".
        /// </summary>
        public static string NormalizeAccountName(string input)
        {
            if (input == null)
            {
                return string.Empty;
            }

            string name = input.Trim();

            int slashIndex = name.LastIndexOf('\\');
            if (slashIndex >= 0)
            {
                name = name.Substring(slashIndex + 1);
            }

            int atIndex = name.IndexOf('@');
            if (atIndex >= 0)
            {
                name = name.Substring(0, atIndex);
            }

            return name.ToLowerInvariant();
        }
    }
}
