using System.Collections;
using System.DirectoryServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Vvf.Auth.Dipvvf.Providers.ADSIProvider
{
    public class Search : DirectorySearcher
    {
        private ArrayList mSearchFilters = new ArrayList();

        public Search()
            : base()
        {
        }

        public Search(DirectoryEntry rearchRoot)
            : base(rearchRoot)
        {
            PopulateDefaultProperties();
        }

        public Search(DirectoryEntry rearchRoot, string Filter, string SortProperty = Configuration.ADSI_CNAME)
            : base(rearchRoot, Filter)
        {
            PopulateDefaultProperties();

            Sort.PropertyName = SortProperty;
        }

        private void PopulateDefaultProperties()
        {
            CacheResults = true;
            // default is True
            ReferralChasing = ReferralChasingOption.All;
            // default is External
            SearchScope = SearchScope.Subtree;
            // default is Subtree
            PropertyNamesOnly = false;
            PageSize = 1000;
        }

        public DirectoryEntry GetEntry()
        {
            SearchResult result;

            try
            {
                Filter = FilterString;
                result = FindOne();

                if (result != null)
                {
                    return result.GetDirectoryEntry();
                }
                else
                {
                    return null;
                }
            }
            catch (COMException)
            {
                return null;
            }
        }

        public ArrayList GetEntries()
        {
            SearchResultCollection resultCollection;
            ArrayList entries = new ArrayList();
            try
            {
                Filter = FilterString;
                resultCollection = FindAll();
                foreach (SearchResult result in resultCollection)
                {
                    entries.Add(result.GetDirectoryEntry());
                }

                // Item 4230 - Explicit call of Dispose() is required, according to 
                // http://msdn.microsoft.com/library/system.directoryservices.directorysearcher.findall.aspx
                resultCollection.Dispose();
            }
            catch (COMException)
            {
            }

            return entries;
        }

        public ArrayList GetPropertyEntries(string Propertyname)
        {
            SearchResultCollection resultCollection;
            ArrayList entries = new ArrayList();
            try
            {
                Filter = FilterString;
                resultCollection = FindAll();
                foreach (SearchResult result in resultCollection)
                {
                    entries.Add(result.GetDirectoryEntry().Properties[Propertyname][0]);
                }

                // Explicit call of Dispose() is required, according to 
                // http://msdn.microsoft.com/library/system.directoryservices.directorysearcher.findall.aspx
                resultCollection.Dispose();
            }
            catch (COMException)
            {
            }

            return entries;
        }

        public void AddFilter(string Name, CompareOperator Operator, string Value = "*")
        {
            SearchFilter filter = new SearchFilter();

            filter.SetFilter(Name, Operator, Value);
            mSearchFilters.Add(filter);
        }

        public ArrayList SearchFilters
        {
            get { return mSearchFilters; }
            set { mSearchFilters = value; }
        }

        public string FilterString
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("(&");
                foreach (SearchFilter filter in this.SearchFilters)
                {
                    sb.Append(AppendFilter(filter));
                }
                sb.Append(")");
                return sb.ToString();
            }
        }

        private string AppendFilter(SearchFilter Filter)
        {
            StringBuilder sb = new StringBuilder();
            switch (Filter.ADSICompareOperator)
            {
                case CompareOperator.Is:
                    sb.Append("(");
                    sb.Append(Filter.Name);
                    sb.Append("=");
                    sb.Append(Filter.Value);
                    sb.Append(")");
                    break;
                case CompareOperator.IsNot:
                    sb.Append("(!");
                    sb.Append(Filter.Name);
                    sb.Append("=");
                    sb.Append(Filter.Value);
                    sb.Append(")");
                    break;
                case CompareOperator.StartsWith:
                    sb.Append("(");
                    sb.Append(Filter.Name);
                    sb.Append("=");
                    sb.Append(Filter.Value);
                    sb.Append("*)");
                    break;
                case CompareOperator.EndsWith:
                    sb.Append("(");
                    sb.Append(Filter.Name);
                    sb.Append("=*");
                    sb.Append(Filter.Value);
                    sb.Append(")");
                    break;
                case CompareOperator.Present:
                    sb.Append("(");
                    sb.Append(Filter.Name);
                    sb.Append("=");
                    sb.Append("*)");
                    break;
                case CompareOperator.NotPresent:
                    sb.Append("(!");
                    sb.Append(Filter.Name);
                    sb.Append("=");
                    sb.Append("*)");
                    break;
            }

            return sb.ToString();
        }

        public struct SearchFilter
        {
            internal string _name;
            internal string _value;
            internal CompareOperator _compareOperator;

            internal void SetFilter(string Name, CompareOperator Operator, string Value)
            {
                _name = Name;
                _value = Value;
                _compareOperator = Operator;
            }

            public string Name
            {
                get { return _name; }
            }

            public string Value
            {
                get { return _value; }
            }

            public CompareOperator ADSICompareOperator
            {
                get { return _compareOperator; }
            }
        }
    }
}