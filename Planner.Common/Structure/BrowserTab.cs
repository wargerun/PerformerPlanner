using System;
using System.Collections.Generic;

namespace Planner.Common.Structure
{
    public class BrowserTab
    {          
        public Uri Uri { get; }

        public IEnumerable<ITabJob> Jobs { get; set; }

        public TabState State { get; set; }

        public int CountErrorChain { get; set; }

        public BrowserTab(Uri uri, IEnumerable<ITabJob> jobs)
        {
            Uri = uri;
            Jobs = jobs;
            State = TabState.New;
        }

        public override string ToString()
        {
            return $"{State} - {Uri.AbsolutePath.TrimStart('/')}";
        }
    }
}
