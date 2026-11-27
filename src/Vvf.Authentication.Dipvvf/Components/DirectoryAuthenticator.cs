namespace Vvf.Authentication.Dipvvf.Components
{
    using System;
    using System.DirectoryServices.AccountManagement;
    using System.Runtime.InteropServices;
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
                // The context binds with the application pool identity (on a
                // domain-joined server: the computer account, which can read AD).
                using (PrincipalContext context = new PrincipalContext(ContextType.Domain, _domain))
                {
                    if (!context.ValidateCredentials(userPrincipalName, password, BindOptions))
                    {
                        return DirectoryAuthenticationResult.Failed(DirectoryAuthenticationStatus.InvalidCredentials);
                    }

                    DirectoryAuthenticationResult result = FindUser(context, userPrincipalName);

                    if (result == null)
                    {
                        Logger.Error("Credentials valid for '" + userPrincipalName + "' but the user could not be read from the directory.");
                        return DirectoryAuthenticationResult.Failed(DirectoryAuthenticationStatus.InvalidCredentials);
                    }

                    return result;
                }
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
        /// or returns null if the user is not found.
        /// </summary>
        private static DirectoryAuthenticationResult FindUser(PrincipalContext context, string userPrincipalName)
        {
            using (UserPrincipal user = UserPrincipal.FindByIdentity(context, IdentityType.UserPrincipalName, userPrincipalName))
            {
                if (user == null || string.IsNullOrEmpty(user.SamAccountName))
                {
                    return null;
                }

                DirectoryAuthenticationResult result = new DirectoryAuthenticationResult(DirectoryAuthenticationStatus.Success, user.SamAccountName);
                result.FirstName = user.GivenName ?? string.Empty;
                result.LastName = user.Surname ?? string.Empty;
                result.DisplayName = user.DisplayName ?? string.Empty;
                result.Email = user.EmailAddress ?? string.Empty;
                return result;
            }
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
