namespace Vvf.Authentication.Dipvvf.Components
{
    using System;
    using System.DirectoryServices;
    using System.DirectoryServices.AccountManagement;
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
    /// name (accountName@domain), then reads the sAMAccountName of that user
    /// from the Global Catalog.
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
                using (PrincipalContext context = new PrincipalContext(ContextType.Domain, _domain))
                {
                    if (!context.ValidateCredentials(userPrincipalName, password, BindOptions))
                    {
                        return DirectoryAuthenticationResult.Failed(DirectoryAuthenticationStatus.InvalidCredentials);
                    }
                }

                // Lookup with the user's own credentials: it does not depend on
                // the read permissions of the application pool identity.
                string samAccountName = FindSamAccountName(userPrincipalName, password);

                if (string.IsNullOrEmpty(samAccountName))
                {
                    Logger.Error("Credentials valid for '" + userPrincipalName + "' but the user could not be read from the directory.");
                    return DirectoryAuthenticationResult.Failed(DirectoryAuthenticationStatus.InvalidCredentials);
                }

                return new DirectoryAuthenticationResult(DirectoryAuthenticationStatus.Success, samAccountName);
            }
            catch (PrincipalServerDownException ex)
            {
                Logger.Error("Active Directory domain '" + _domain + "' is unreachable.", ex);
                return DirectoryAuthenticationResult.Failed(DirectoryAuthenticationStatus.DirectoryUnavailable);
            }
            catch (PrincipalOperationException ex)
            {
                Logger.Error("Active Directory error while authenticating '" + userPrincipalName + "'.", ex);
                return DirectoryAuthenticationResult.Failed(DirectoryAuthenticationStatus.DirectoryUnavailable);
            }
            catch (DirectoryServicesCOMException ex)
            {
                Logger.Error("Active Directory error while reading '" + userPrincipalName + "' (0x" + ex.ErrorCode.ToString("X8") + ").", ex);
                return DirectoryAuthenticationResult.Failed(DirectoryAuthenticationStatus.DirectoryUnavailable);
            }
        }

        /// <summary>
        /// Searches the Global Catalog of the forest for the user with the given
        /// UPN and returns its sAMAccountName, or null if not found.
        /// A plain LDAP search avoids PrincipalContext initialization, which
        /// fails with 0x8007200A when created with user credentials.
        /// </summary>
        private string FindSamAccountName(string userPrincipalName, string password)
        {
            using (DirectoryEntry root = new DirectoryEntry("GC://" + _domain, userPrincipalName, password, SearchAuthentication))
            using (DirectorySearcher searcher = new DirectorySearcher(root))
            {
                searcher.Filter = "(&(objectCategory=person)(objectClass=user)(userPrincipalName="
                    + EscapeLdapFilterValue(userPrincipalName) + "))";
                searcher.PropertiesToLoad.Add("sAMAccountName");
                searcher.SearchScope = SearchScope.Subtree;

                SearchResult result = searcher.FindOne();

                if (result == null || result.Properties["sAMAccountName"].Count == 0)
                {
                    return null;
                }

                return Convert.ToString(result.Properties["sAMAccountName"][0]);
            }
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
