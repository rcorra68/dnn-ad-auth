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

namespace Vvf.Auth.Dipvvf.Components.Users
{
    using DotNetNuke.Entities.Users;
    using Vvf.Auth.Dipvvf.Components;
    using Vvf.Auth.Dipvvf.Components.Common;

    public class ADUserInfo : UserInfo, IAuthenticationObjectBase
    {
        public ADUserInfo()
        {
        }

        public bool IsNotSimplyUser { get; set; }

        public string Name => SAMAccountName;

        public ObjectClass ObjectClass => ObjectClass.Person;

        public bool AuthenticationExists { get; set; }

        public string CName { get; set; } = string.Empty;

        public string DistinguishedName { get; set; } = string.Empty;

        public string SAMAccountName { get; set; } = string.Empty;
    }
}