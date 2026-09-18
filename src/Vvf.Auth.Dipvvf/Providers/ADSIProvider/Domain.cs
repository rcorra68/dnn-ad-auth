using System.Collections;
using System.DirectoryServices;
using DotNetNuke.Common.Utilities;
using Vvf.Auth.Dipvvf.Components.Common;

namespace Vvf.Auth.Dipvvf.Providers.ADSIProvider
{
    public class Domain : DirectoryEntry
    {
        private ArrayList _childDomains = new ArrayList();
        private ArrayList _allChildDomains = new ArrayList();
        private Domain _parentDomain;
        private string _distinguishedName = "";
        private string _netBIOSName = "";
        private string _canonicalName = "";
        private int _level;
        private bool _childPopulate = false;

        public Domain() : base()
        {
        }

        public Domain(string Path, string UserName, string Password, AuthenticationTypes AuthenticationType)
            : base(Path, UserName, Password, AuthenticationType)
        {
            PopulateInfo();
            PopulateChild(this);
            _childPopulate = true;
        }

        public Domain(string Path)
            : base(Path)
        {
            PopulateInfo();
            PopulateChild(this);
            _childPopulate = true;
        }

        private void PopulateInfo()
        {
            Configuration config = Configuration.GetConfig();

            _distinguishedName = (string)Properties[Configuration.ADSI_DISTINGUISHEDNAME].Value;
            _canonicalName = Utilities.ConvertToCanonical(_distinguishedName, false);

            // Note that this property will be null string if LDAP is unaccessible
            _netBIOSName = Utilities.CanonicalToNetBIOS(_canonicalName);
        }

        private void PopulateChild(Domain domain)
        {
            Search objSearch = new Search(domain);

            objSearch.SearchScope = SearchScope.OneLevel;
            objSearch.AddFilter(Configuration.ADSI_CLASS, CompareOperator.Is, ObjectClass.DomainDNS.ToString());

            ArrayList resDomains = objSearch.GetEntries();

            foreach (DirectoryEntry entry in resDomains)
            {
                Domain child = GetDomain(entry.Path);

                if (child != null)
                {
                    child.ParentDomain = domain;
                    child.Level = domain.Level + 1;
                    // Add this child into childDomains collection
                    domain.ChildDomains.Add(child);
                    // add this child and all it's child into allchilddomains collection
                    domain.AllChildDomains.Add(child);
                    domain.AllChildDomains.AddRange(child.AllChildDomains);
                }
            }
        }

        public static Domain GetDomain(string Path)
        {
            return GetDomain(Path, "", "", AuthenticationTypes.Delegation);
        }

        public static Domain GetDomain(string Path, string UserName, string Password, AuthenticationTypes AuthenticationType)
        {
            Domain Domain = (Domain)DataCache.GetCache(Path);
            if (Domain == null)
            {
                if (UserName.Length > 0 && Password.Length > 0)
                {
                    Domain = new Domain(Path, UserName, Password, AuthenticationType);
                }
                else
                {
                    Domain = new Domain(Path);
                }

                DataCache.SetCache(Path, Domain);
            }

            return Domain;
        }

        public static void ResetDomain(string Path)
        {
            DataCache.RemoveCache(Path);
        }

        public ArrayList ChildDomains
        {
            get { return _childDomains; }
        }

        public ArrayList AllChildDomains
        {
            get { return _allChildDomains; }
        }

        public Domain ParentDomain
        {
            get { return _parentDomain; }
            set { _parentDomain = value; }
        }

        public int Level
        {
            get { return _level; }
            set { _level = value; }
        }

        public string DistinguishedName
        {
            get { return _distinguishedName; }
            set { _distinguishedName = value; }
        }

        public bool ChildPopulate
        {
            get { return _childPopulate; }
            set { _childPopulate = value; }
        }
    }
}