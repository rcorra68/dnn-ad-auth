//
// DotNetNuke® - http://www.dotnetnuke.com
// Copyright (c) 2002-2013
// by DotNetNuke Corporation
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.DirectoryServices;
using System.Runtime.InteropServices;

namespace Vvf.Auth.Dipvvf.Providers.ADSIProvider
{
    public class CrossReferenceCollection : CollectionBase
    {
        /// -------------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <history>
        ///     [tamttt]	08/01/2004	Created
        /// </history>
        /// -------------------------------------------------------------------
        public class CrossReference
        {
            internal string mDomainPath;
            internal string mCanonicalName;
            internal string mNetBIOSName;

            internal CrossReference(string Path, string NetBIOS, string Canonical)
            {
                mDomainPath = Path;
                mCanonicalName = Canonical;
                mNetBIOSName = NetBIOS;
            }

            public string DomainPath
            {
                get { return mDomainPath; }
            }

            public string CanonicalName
            {
                get { return mCanonicalName; }
            }

            public string NetBIOSName
            {
                get { return mNetBIOSName; }
            }
        }

        // Allows access to items by both NetBiosName or CanonicalName
        private Hashtable mNetBIOSLookup = new Hashtable();
        private Hashtable mCanonicalLookup = new Hashtable();
        private string mProcessLog = "";

        /// -------------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <history>
        ///     [tamttt]	08/01/2004	Created
        /// </history>
        /// -------------------------------------------------------------------
        public CrossReferenceCollection(string UserName, string Password, AuthenticationTypes AuthType)
            : base()
        {
            try
            {
                // Obtain NETBIOS only if LDAP accessible to prevent error
                DirectoryEntry rootLDAP = new DirectoryEntry("LDAP://rootDSE", UserName, Password, AuthType);
                string crossRefPath = "LDAP://CN=Partitions," + rootLDAP.Properties["configurationNamingContext"].Value.ToString();
                DirectoryEntry objCrossRefContainer;

                if ((UserName.Length > 0) && (Password.Length > 0))
                {
                    objCrossRefContainer = new DirectoryEntry(crossRefPath, UserName, Password, AuthType);
                }
                else
                {
                    objCrossRefContainer = new DirectoryEntry(crossRefPath);
                }

                foreach (DirectoryEntry objCrossRef in objCrossRefContainer.Children)
                {
                    if (objCrossRef.Properties["nETBIOSName"].Value != null)
                    {
                        string netBIOSName = (string)objCrossRef.Properties["nETBIOSName"].Value;
                        string canonicalName = (string)objCrossRef.Properties["dnsRoot"].Value;
                        string domainPath = (string)objCrossRef.Properties["nCName"].Value;
                        CrossReference crossRef = new CrossReference(domainPath, netBIOSName, canonicalName);
                        this.Add(crossRef);
                    }
                }
            }
            catch (COMException ex)
            {
                mProcessLog += ex.Message + "<br>";
            }
        }

        /// -------------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <history>
        ///     [tamttt]	08/01/2004	Created
        /// </history>
        /// -------------------------------------------------------------------
        internal new void Clear()
        {
            mNetBIOSLookup.Clear();
            mCanonicalLookup.Clear();
            base.Clear();
        }

        /// -------------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <history>
        ///     [tamttt]	08/01/2004	Created
        /// </history>
        /// -------------------------------------------------------------------
        internal void Add(CrossReference RefObject)
        {
            int index;
            try
            {
                index = base.List.Add(RefObject);
                mCanonicalLookup.Add(RefObject.CanonicalName, index);
                mNetBIOSLookup.Add(RefObject.NetBIOSName, index);
            }
            catch (COMException ex)
            {
                mProcessLog += ex.Message;
            }
        }

        /// -------------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <history>
        ///     [tamttt]	08/01/2004	Created
        /// </history>
        /// -------------------------------------------------------------------
        public CrossReference Item(int index)
        {
            try
            {
                object obj = base.List[index];
                return (CrossReference)obj;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// -------------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <history>
        ///     [tamttt]	08/01/2004	Created
        /// </history>
        /// -------------------------------------------------------------------
        public CrossReference Item(string Name)
        {
            int index;
            object obj;

            // Do validation first
            try
            {
                if (mCanonicalLookup[Name] == null)
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }

            index = (int)mCanonicalLookup[Name];
            obj = base.List[index];

            return (CrossReference)obj;
        }

        /// -------------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <history>
        ///     [tamttt]	08/01/2004	Created
        /// </history>
        /// -------------------------------------------------------------------
        public CrossReference ItemByNetBIOS(string Name)
        {
            int index;
            object obj;

            // Do validation first
            try
            {
                if (mNetBIOSLookup[Name] == null)
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }

            index = (int)mNetBIOSLookup[Name];
            obj = base.List[index];

            return (CrossReference)obj;
        }

        /// -------------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <history>
        ///     [tamttt]	08/01/2004	Created
        /// </history>
        /// -------------------------------------------------------------------
        public string ProcesssLog
        {
            get { return mProcessLog; }
        }
    }
}