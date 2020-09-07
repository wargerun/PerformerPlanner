using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using NLog;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

using Planner.Common;
using Planner.Common.Structure;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PerformerPlanner
{
    internal class ChromeManager
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private ChromeDriver _webDriver;
        private readonly ILogger<ChromeManager> _chromeLogger;
        private readonly IConfiguration _configuration;
        private HashSet<BrowserTab> _tabWithJobs;

        public bool IsRunning { get; private set; }

        public ChromeManager(ILogger<ChromeManager> chromeLogger, IConfiguration configuration)
        {    
            _chromeLogger = chromeLogger;
            _configuration = configuration;
        }

        internal Task StartAsync() => Task.Run(() =>
        {
            Start();
        });

        private void Start()
        {
            if (_tabWithJobs is null || _tabWithJobs.Count == 0)
            {
                return;
            }

            _log.Info($"Monitoring starting");
            IsRunning = true;

            while (IsRunning)
            {
                foreach (BrowserTab tab in _tabWithJobs.Where(t => t.JobIsPresent))
                {
                    try
                    {
                        tab.WebDriver.SwitchTo().Window(tab.SessionWindowName);
                        tab.Job.Execute();
                    }
                    catch (Exception ex)
                    {
                        _chromeLogger.LogError(ex, ex.Message);
                        _log.Error(ex, ex.Message);
                    }
                }

                string timeoutSecondsString = _configuration["TimeoutSecondsAfterJobs"];
                int timeoutSeconds = 5;

                if (timeoutSecondsString != null && int.TryParse(timeoutSecondsString, out int newtimeoutSeconds))
                {
                    timeoutSeconds = newtimeoutSeconds;
                }

                _chromeLogger.LogDebug($"Timeout seconds after jobs: {timeoutSeconds}");
                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(timeoutSeconds));
            }
        }

        internal void Stop()
        {
            IsRunning = false;
            _webDriver.Close();
        }

        public void AddJob(Predicate<Uri> predicate, Func<IWebDriver, IJob> action)
        {
            int tabIndex = 0;

            foreach (BrowserTab browserTab in _tabWithJobs)
            {
                if (predicate(browserTab.Uri))
                {
                    browserTab.SessionWindowName = browserTab.WebDriver.WindowHandles[tabIndex];
                    browserTab.WebDriver.SwitchTo().Window(browserTab.SessionWindowName);
                    browserTab.Job = action(browserTab.WebDriver);
                    
                    _log.Trace($"Added job: {browserTab.Job.Name}");
                }

                tabIndex++;
            }
        }

        public void OpenTabs(string[] urlStrings)
        {
            InitChromDriver();

            _tabWithJobs = new HashSet<BrowserTab>(urlStrings.Length);
            bool openNewTab = false;

            foreach (Uri uri in urlStrings.Where(urlString => urlString.IsNotNullOrEmpty()).Select(u => new Uri(u.Trim())))
            {
                if (openNewTab)
                {
                    IWebDriver newWindow = _webDriver.SwitchTo().NewWindow(WindowType.Tab);
                    newWindow.Navigate().GoToUrl(uri);

                    _tabWithJobs.Add(new BrowserTab(uri, newWindow));
                }
                else
                {
                    _webDriver.Navigate().GoToUrl(uri);
                    openNewTab = true;

                    _tabWithJobs.Add(new BrowserTab(uri, _webDriver));
                }

                _log.Trace($"Openning tab: {uri}");
            }
        }

        private void InitChromDriver()
        {
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("mute-audio");

            _webDriver = new ChromeDriver(@"D:\Dowloands\YandexBrowser\chromedriver_win32", options); // 85.0.4183.87
        }
    }
}