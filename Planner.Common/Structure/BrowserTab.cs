using OpenQA.Selenium;

using System;

namespace Planner.Common.Structure
{
    public class BrowserTab
    {
        public Uri Uri { get; }
        public IWebDriver WebDriver { get; }
        public string SessionWindowName { get; set; }

        public IJob Job { get; set; }

        public bool JobIsPresent => Job != null;

        public BrowserTab(Uri uri, IWebDriver webDriver)
        {
            Uri = uri;
            WebDriver = webDriver;
        }
    }
}
