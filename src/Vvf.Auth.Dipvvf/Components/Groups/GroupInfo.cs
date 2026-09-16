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
    using DotNetNuke.Security.Roles;

    public class GroupInfo : RoleInfo, IAuthenticationObjectBase
    {
        private readonly ArrayList _authenticationMember = new ArrayList();

        public GroupInfo()
        {
        }

        public string Name => RoleName;

        public ObjectClass ObjectClass => ObjectClass.Group;

        public ArrayList AuthenticationMember => _authenticationMember;

        public bool IsPopulated { get; set; }
    }
}