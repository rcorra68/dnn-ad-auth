using DotNetNuke.Entities.Portals;
using DotNetNuke.Framework;
using System;
using System.Collections;
using Vvf.Auth.Dipvvf.Components.Config;
using Vvf.Auth.Dipvvf.Components.Users;

namespace Vvf.Auth.Dipvvf.Components
{

    public abstract class AuthenticationProvider
    {
        private static AuthenticationProvider objProvider;

        static AuthenticationProvider()
        {
            PortalSettings portalSettings = PortalController.Instance.GetCurrentPortalSettings();
            Configuration config = Configuration.GetConfig();
            string strKey = string.Format("AuthenticationProvider{0}", portalSettings.PortalId);

            objProvider = (AuthenticationProvider)Reflection.CreateObject(config.ProviderTypeName, strKey);
        }

        public static AuthenticationProvider Instance(string authenticationTypeName)
        {
            PortalSettings portalSettings = PortalController.Instance.GetCurrentPortalSettings();
            string strKey = string.Format("AuthenticationProvider{0}", portalSettings.PortalId);
            objProvider = (AuthenticationProvider)Reflection.CreateObject(authenticationTypeName, strKey);
            return objProvider;
        }

        public abstract ADUserInfo GetUser(string loggedOnUserName, string loggedOnPassword);

        public abstract ADUserInfo GetUser(string loggedOnUserName);

        public abstract ArrayList GetGroups();

        public abstract ArrayList GetGroups(ArrayList arrUserPortalRoles);

        public abstract Array GetAuthenticationTypes();

        public abstract string GetNetworkStatus();
    }
}