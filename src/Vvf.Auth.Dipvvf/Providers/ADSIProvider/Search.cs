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

using System.Collections;
using System.DirectoryServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Vvf.Auth.Dipvvf.Providers.ADSIProvider
{
    public class Search : DirectorySearcher
    {
        private ArrayList mSearchFilters = new ArrayList();
        private string mFilterString;

        /// -------------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <history>
        ///     [tamttt]	08/01/2004	Created
        /// </history>
        /// -------------------------------------------------------------------
        public Search()
            : base()
        {
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
        public Search(DirectoryEntry rearchRoot)
            : base(rearchRoot)
        {
            PopulateDefaultProperties();
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
        public Search(DirectoryEntry rearchRoot, string Filter, string SortProperty = Configuration.ADSI_CNAME)
            : base(rearchRoot, Filter)
        {
            PopulateDefaultProperties();

            Sort.PropertyName = SortProperty;
        }

        /// -------------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <history>
        ///     [tamttt]	08/01/2004	Created
        ///     [mhorton]   10/05/2009  Added PropertyNamesOnly - WorkItem:2943
        /// </history>
        /// -------------------------------------------------------------------
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

        /// -------------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <history>
        ///     [tamttt]	08/01/2004	Created
        /// </history>
        /// -------------------------------------------------------------------
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

        /// -------------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <history>
        ///     [tamttt]	08/01/2004	Created
        /// </history>
        /// -------------------------------------------------------------------
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

        /// -------------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <history>
        ///     [tamttt]	08/01/2004	Created
        ///     [mhorton]   10/05/2009  Added PropertyNamesOnly - WorkItem:2943
        /// </history>
        /// -------------------------------------------------------------------
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

        /// -------------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <history>
        ///     [tamttt]	08/01/2004	Created
        /// </history>
        /// -------------------------------------------------------------------
        public void AddFilter(string Name, CompareOperator Operator, string Value = "*")
        {
            SearchFilter filter = new SearchFilter();

            filter.SetFilter(Name, Operator, Value);
            mSearchFilters.Add(filter);
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
        public ArrayList SearchFilters
        {
            get { return mSearchFilters; }
            set { mSearchFilters = value; }
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

        /// -------------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <history>
        ///     [tamttt]	08/01/2004	Created
        /// </history>
        /// -------------------------------------------------------------------
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

        /// -------------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <history>
        ///     [tamttt]	08/01/2004	Created
        /// </history>
        /// -------------------------------------------------------------------
        public struct SearchFilter
        {
            internal string mName;
            internal string mValue;
            internal CompareOperator mCompareOperator;

            internal void SetFilter(string Name, CompareOperator Operator, string Value)
            {
                mName = Name;
                mValue = Value;
                mCompareOperator = Operator;
            }

            public string Name
            {
                get { return mName; }
            }

            public string Value
            {
                get { return mValue; }
            }

            public CompareOperator ADSICompareOperator
            {
                get { return mCompareOperator; }
            }
        }
    }
}