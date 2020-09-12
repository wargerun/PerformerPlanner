using System;
using System.Collections.Generic;

namespace Planner.Common.Structure
{
    public class BrowserTab
    {          
        public Uri Uri { get; }

        public IEnumerable<IJob> Jobs { get; set; }

        public ChannelState State { get; set; }

        public BrowserTab(Uri uri, IEnumerable<IJob> jobs)
        {
            Uri = uri;
            Jobs = jobs;
            State = ChannelState.New;
        }

        public override string ToString()
        {
            return $"{State} - {Uri.AbsolutePath.TrimStart('/')}";
        }
    }
}
