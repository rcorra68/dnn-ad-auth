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

using DotNetNuke.Common.Utilities;
using DotNetNuke.Data;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Users;
using DotNetNuke.Security;
using DotNetNuke.Security.Membership;
using DotNetNuke.Security.Roles;
using DotNetNuke.Services.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using Vvf.Auth.Dipvvf.Components.Config;
using Vvf.Auth.Dipvvf.Components.Groups;
using Vvf.Auth.Dipvvf.Providers.ADSIProvider;
using DnnUserController = DotNetNuke.Entities.Users.UserController;
using ADConfiguration = Vvf.Auth.Dipvvf.Components.Config.Configuration;

namespace Vvf.Auth.Dipvvf.Components.Users
{
    public class UserController
    {
        private readonly string _providerTypeName = string.Empty;
        private static readonly DataProvider _dataProviderInstance = DataProvider.Instance();
        private static string _roleName = string.Empty;

        public UserController()
        {
            ADConfiguration config = ADConfiguration.GetConfig();
            _providerTypeName = config.ProviderTypeName;
        }

        public ADUserInfo GetUser(string loggedOnUserName)
        {
            return AuthenticationProvider.Instance(_providerTypeName).GetUser(loggedOnUserName);
        }

        public ADUserInfo GetUser(string loggedOnUserName, string loggedOnPassword)
        {
            return AuthenticationProvider.Instance(_providerTypeName).GetUser(loggedOnUserName, loggedOnPassword);
        }

        [Obsolete("No longer used")]
        private UserCreateStatus CreateDNNUser(ref ADUserInfo user)
        {
            var objSecurity = new PortalSecurity();
            string userName = objSecurity.InputFilter(
                user.Username,
                PortalSecurity.FilterFlag.NoScripting | PortalSecurity.FilterFlag.NoAngleBrackets | PortalSecurity.FilterFlag.NoMarkup);

            string email = objSecurity.InputFilter(
                user.Email,
                PortalSecurity.FilterFlag.NoScripting | PortalSecurity.FilterFlag.NoAngleBrackets | PortalSecurity.FilterFlag.NoMarkup);

            string lastName = objSecurity.InputFilter(
                user.LastName,
                PortalSecurity.FilterFlag.NoScripting | PortalSecurity.FilterFlag.NoAngleBrackets | PortalSecurity.FilterFlag.NoMarkup);

            string firstName = objSecurity.InputFilter(
                user.FirstName,
                PortalSecurity.FilterFlag.NoScripting | PortalSecurity.FilterFlag.NoAngleBrackets | PortalSecurity.FilterFlag.NoMarkup);

            string displayName = objSecurity.InputFilter(
                user.DisplayName,
                PortalSecurity.FilterFlag.NoScripting | PortalSecurity.FilterFlag.NoAngleBrackets | PortalSecurity.FilterFlag.NoMarkup);

            bool updatePassword = user.Membership.UpdatePassword;
            bool isApproved = user.Membership.Approved;
            UserCreateStatus createStatus = UserCreateStatus.Success;

            try
            {
                user.UserID = Convert.ToInt32(
                    _dataProviderInstance.AddUser(
                        user.PortalID,
                        userName,
                        firstName,
                        lastName,
                        user.AffiliateID,
                        user.IsSuperUser,
                        email,
                        displayName,
                        updatePassword,
                        isApproved,
                        -1));

                DataCache.ClearPortalCache(user.PortalID, false);

                if (!user.IsSuperUser)
                {
                    var objRoles = new RoleController();
                    ArrayList arrRoles = objRoles.GetPortalRoles(user.PortalID);

                    foreach (object item in arrRoles)
                    {
                        var objRole = (RoleInfo)item;
                        if (objRole.AutoAssignment)
                        {
                            objRoles.AddUserRole(user.PortalID, user.UserID, objRole.RoleID, Null.NullDate, Null.NullDate);
                        }
                    }
                }
            }
            catch (Exception)
            {
                user = null;
                createStatus = UserCreateStatus.ProviderError;
            }

            return createStatus;
        }

        public static void AddUserRoles(int portalID, ADUserInfo authenticationUser)
        {
            try
            {
                var objPortals = new PortalController();
                PortalInfo objPortal = objPortals.GetPortal(portalID);
                var objPortalSettings = new PortalSettings(portalID);
                var objRoleController = new RoleController();

                ArrayList arrUserADGroups = Utilities.GetADGroups(authenticationUser.Username);
                IList<UserRoleInfo> strUserPortalRoles = objRoleController.GetUserRoles(authenticationUser, true);
                var arrUserPortalRoles = new ArrayList();

                foreach (UserRoleInfo strRole in strUserPortalRoles)
                {
                    RoleInfo objRoleInfo = objRoleController.GetRoleByName(portalID, strRole.RoleName);
                    if (!objRoleInfo.AutoAssignment)
                    {
                        arrUserPortalRoles.Add(objRoleInfo);
                    }
                }

                var arrADGroupOnly = new ArrayList();
                var arrRolesOnly = new ArrayList();

                foreach (string strGroup in arrUserADGroups)
                {
                    bool bMatch = false;
                    foreach (UserRoleInfo strRole in strUserPortalRoles)
                    {
                        if (strRole.RoleName.Equals(strGroup, StringComparison.OrdinalIgnoreCase))
                        {
                            bMatch = true;
                            break;
                        }
                    }

                    if (!bMatch)
                    {
                        arrADGroupOnly.Add(strGroup);
                    }
                }

                foreach (RoleInfo objRoleInfo in arrUserPortalRoles)
                {
                    bool bMatch = false;
                    foreach (string strGroup in arrUserADGroups)
                    {
                        if (strGroup.Equals(objRoleInfo.RoleName, StringComparison.OrdinalIgnoreCase))
                        {
                            bMatch = true;
                            break;
                        }
                    }

                    if (!bMatch)
                    {
                        arrRolesOnly.Add(objRoleInfo);
                    }
                }

                IList<RoleInfo> arrPortalRoles = objRoleController.GetRoles(portalID);
                foreach (RoleInfo objRoleInfo in arrPortalRoles)
                {
                    if (!objRoleInfo.AutoAssignment && objRoleInfo.RoleID != objPortal.AdministratorRoleId)
                    {
                        if (arrADGroupOnly.Contains(objRoleInfo.RoleName))
                        {
                            objRoleController.AddUserRole(portalID, authenticationUser.UserID, objRoleInfo.RoleID, DateTime.Today, Null.NullDate);
                        }
                    }
                }

                var objGroupController = new GroupController();
                ArrayList arrADGroups = objGroupController.GetGroups(arrRolesOnly);
                foreach (RoleInfo objRoleInfo in arrADGroups)
                {
                    if (objRoleInfo.RoleID != objPortal.AdministratorRoleId)
                    {
                        RoleController.DeleteUserRole(authenticationUser, objRoleInfo, objPortalSettings, false);
                    }
                }
            }
            catch (Exception exc)
            {
                Exceptions.LogException(exc);
            }
        }

        private static bool RolesExists(string s)
        {
            return string.Equals(s, _roleName, StringComparison.OrdinalIgnoreCase);
        }

        public bool UpdateDnnUser(UserInfo authenticationUser)
        {
            DnnUserController.UpdateUser(authenticationUser.PortalID, authenticationUser);
            return true;
        }
    }
}