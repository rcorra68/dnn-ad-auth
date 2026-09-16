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
    using System.Collections;

    public class GroupController
    {
        private readonly string _providerTypeName = string.Empty;

        public GroupController()
        {
            Configuration config = Configuration.GetConfig();
            _providerTypeName = config.ProviderTypeName;
        }

        public ArrayList GetGroups()
        {
            return AuthenticationProvider.Instance(_providerTypeName).GetGroups();
        }

        public ArrayList GetGroups(ArrayList arrUserPortalRoles)
        {
            return AuthenticationProvider.Instance(_providerTypeName).GetGroups(arrUserPortalRoles);
        }
    }
}