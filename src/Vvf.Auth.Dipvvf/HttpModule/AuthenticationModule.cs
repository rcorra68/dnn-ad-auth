using System;
using System.Web;
using DotNetNuke.Common;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Users;
using Vvf.Auth.Dipvvf.Components;
using Vvf.Auth.Dipvvf.Components.Common;
using Vvf.Auth.Dipvvf.Providers.ADSIProvider;
using ADConfiguration = Vvf.Auth.Dipvvf.Components.Config.Configuration;

namespace Vvf.Auth.Dipvvf.HttpModule
{
    public class AuthenticationModule : IHttpModule
    {
        public string ModuleName
        {
            get
            {
                return "AuthenticationModule";
            }
        }

        public void Init(HttpApplication application)
        {
            application.AuthenticateRequest += OnAuthenticateRequest;
        }

        public void OnAuthenticateRequest(object s, EventArgs e)
        {
            HttpRequest request = HttpContext.Current.Request;
            HttpResponse response = HttpContext.Current.Response;

            // check if we are upgrading/installing/using a web service/rss feeds (ACD-7748)
            // Abort if NOT Default.aspx
            if (!request.Url.LocalPath.ToLower().EndsWith("default.aspx") || request.RawUrl.ToLower().Contains("rssid"))
            {
                return;
            }

            // Check that Host/Admin user is not already logged into the site. 
            // If so then bypass authentication (ACD-2592)
            if (!(UserController.Instance.GetCurrentUserInfo().Username == string.Empty))
            {
                bool host = UserController.Instance.GetCurrentUserInfo().IsSuperUser;
                bool admin = UserController.Instance.GetCurrentUserInfo().IsInRole("Administrators");
                if (admin || host) return;
            }

            // Moved the following statement from the top to correct ACD-9746
            PortalSettings portalSettings = Globals.GetPortalSettings();

            ADConfiguration config = ADConfiguration.GetConfig();

            if (config == null)
            {
                return;
            }

            // ACD-8846, WorkItems 6416,4766, 4077, 7805
            string rawUserAgent = request.ServerVariables["HTTP_USER_AGENT"];
            if (rawUserAgent == null)
            {
                return;
            }
            string strRequest = rawUserAgent.ToUpper();

            string[] arrBots = config.Bots.Split(';');
            for (int intCount = 0; intCount < arrBots.Length; intCount++)
            {
                string strBot = arrBots[intCount].ToUpper();
                if (strBot != "")
                {
                    if (strRequest.Contains(strBot))
                    {
                        return;
                    }
                }
            }

            if (strRequest.Contains("gsa-crawler"))
            {
                return;
            }

            AspNetHostingPermissionLevel permission = Utilities.GetCurrentTrustLevel();

            if (permission != AspNetHostingPermissionLevel.Unrestricted)
            {
                response.Redirect("~/DesktopModules/AuthenticationServices/ActiveDirectory/trusterror.htm", true);
            }

            // ACD-8589
            if (config.WindowsAuthentication || config.HideWindowsLogin)
            {
                AuthenticationStatus authStatus = AuthenticationController.GetStatus(portalSettings.PortalId);
                bool blnWinLogon = request.RawUrl.ToLower().IndexOf(ADConfiguration.AUTHENTICATION_LOGON_PAGE.ToLower()) > -1;
                bool blnWinLogoff = authStatus == AuthenticationStatus.WinLogoff &&
                                   request.RawUrl.ToLower().IndexOf(ADConfiguration.AUTHENTICATION_LOGOFF_PAGE.ToLower()) > -1;
                bool blnWinProcess = authStatus == AuthenticationStatus.WinProcess && !(blnWinLogon || blnWinLogoff);

                SetDnnReturnToCookie(request, response, portalSettings);

                if (authStatus == AuthenticationStatus.Undefined || blnWinProcess)
                {
                    AuthenticationController.SetStatus(portalSettings.PortalId, AuthenticationStatus.WinProcess);
                    string url = request.RawUrl;
                    string[] arrAutoIp = config.AutoIP.Split(';');
                    // ACD-7664
                    string strClientIp = Utilities.GetIP4Address(request.UserHostAddress);

                    for (int intCount = 0; intCount < arrAutoIp.Length; intCount++)
                    {
                        string strAutoIp = arrAutoIp[intCount];
                        if (strAutoIp.Contains("-"))
                        {
                            string[] arrIpRange = strAutoIp.Split('-');
                            uint lClientIp = IpAddressToLong(strClientIp);
                            if (lClientIp >= IpAddressToLong(Utilities.GetIP4Address(arrIpRange[0].Trim())) &&
                                lClientIp <= IpAddressToLong(Utilities.GetIP4Address(arrIpRange[1].Trim())))
                            {
                                url = GetRedirectUrl(request);
                                break;
                            }
                        }
                        else if (strClientIp.ToString().StartsWith(strAutoIp) || strAutoIp == "")
                        {
                            url = GetRedirectUrl(request);
                            break;
                        }
                    }

                    // WorkItem: 8571 
                    response.Redirect(url + "?portalid=" + portalSettings.PortalId);
                }
                else if (!(authStatus == AuthenticationStatus.WinLogoff) && blnWinLogoff)
                {
                    AuthenticationController objAuthentication = new AuthenticationController();
                    objAuthentication.AuthenticationLogoff();
                }
                else if (authStatus == AuthenticationStatus.WinLogoff && blnWinLogon) // has been logoff before
                {
                    AuthenticationController.SetStatus(portalSettings.PortalId, AuthenticationStatus.Undefined);
                    response.Redirect(request.RawUrl);
                }
            }
        }

        public void Dispose()
        {
            // Should check to see why this routine is never called
        }

        private static string GetRedirectUrl(HttpRequest request)
        {
            if (request.ApplicationPath == "/")
            {
                return ADConfiguration.AUTHENTICATION_PATH + ADConfiguration.AUTHENTICATION_LOGON_PAGE;
            }
            else
            {
                return request.ApplicationPath + ADConfiguration.AUTHENTICATION_PATH + ADConfiguration.AUTHENTICATION_LOGON_PAGE;
            }
        }

        private static void SetDnnReturnToCookie(HttpRequest request, HttpResponse response, PortalSettings portalSettings)
        {
            try
            {
                string refUrl = request.RawUrl;
                response.Clear();
                response.Cookies["DNNReturnTo"].Value = refUrl;
                response.Cookies["DNNReturnTo"].Path = "/";
                response.Cookies["DNNReturnTo"].Expires = DateTime.Now.AddMinutes(5);
            }
            catch
            {
            }
        }

        private static uint IpAddressToLong(string strPassedIp)
        {
            int x;
            int pos = 0;
            int prevPos = 0;
            int num;
            long lConvertToLong = 0;

            if (strPassedIp.Split('.').Length == 4)
            {
                for (x = 1; x <= 4; x++)
                {
                    pos = strPassedIp.IndexOf('.', prevPos);
                    if (pos == -1) pos = strPassedIp.Length;

                    num = int.Parse(strPassedIp.Substring(prevPos, pos - prevPos));

                    if (num > 255)
                    {
                        return 0;
                    }

                    prevPos = pos + 1;
                    lConvertToLong = (long)(num % 256 * Math.Pow(256, 4 - x)) + lConvertToLong;
                }
            }

            return (uint)lConvertToLong;
        }
    }
}