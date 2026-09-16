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
    using System;
    using System.Web.UI;
    using DotNetNuke.Entities.Portals;

    public partial class WindowsSignin : Page
    {
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            string logonUser = Request.ServerVariables["LOGON_USER"];
            if (!string.IsNullOrEmpty(logonUser))
            {
                var objAuthentication = new AuthenticationController();

                Configuration.ResetConfig();
                Configuration config = Configuration.GetConfig();

                if (config.WindowsAuthentication || config.HideWindowsLogin)
                {
                    objAuthentication.AuthenticationLogon();
                }
                else
                {
                    plNoAuthentication.Visible = true;
                    plSetIIS.Visible = false;
                }
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // Page initialization logic if needed
        }
    }
}
