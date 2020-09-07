using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NLog;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

using Planner.Common;
using Planner.Common.Structure;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using TwitchPlanner.Jobs;

namespace PerformerPlanner
{
    internal class BrowserChromeService : IHostedService, IDisposable
    {
        /// <summary>
        /// TODO NLOG USELESS
        /// </summary>
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        private readonly IConfiguration _configuration;
        private readonly ILogger<BrowserChromeService> _logger;
        private readonly ILogger<PointCollectorTwitchJob> collectorLogger;
        private ChromeDriver _webDriver;
        private HashSet<BrowserTab> _tabWithJobs;
        private bool disposedValue;

        public bool IsRunning { get; private set; }

        public BrowserChromeService(
            IConfiguration configuration,
            ILogger<BrowserChromeService> logger,
            ILogger<PointCollectorTwitchJob> collectorLogger)
        {
            _configuration = configuration;
            _logger = logger;
            this.collectorLogger = collectorLogger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(() => StartAsyncInternal(cancellationToken), cancellationToken);

            return Task.CompletedTask;
        }

        private void StartAsyncInternal(CancellationToken cancellationToken)
        {
            string[] urlStrings = _configuration.GetSection("TwitchUrl").Get<string[]>();

            OpenTabs(urlStrings);
            _logger.LogInformation($"OpenTabs Complete. Tabs count: {urlStrings.Length}");


            _logger.LogInformation($"AddJob..");
            AddJob(
                url => PointCollectorTwitchJob.UrlValid(url.ToString()),
                webDriver => new PointCollectorTwitchJob(webDriver, _configuration, collectorLogger)
            );

            _logger.LogInformation("PerformerPlanner starting..");
            Start(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            IsRunning = false;
            return Task.CompletedTask;
        }

        private void Start(CancellationToken cancellationToken)
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
                        tab.Job.Execute(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, ex.Message);
                        _log.Error(ex, $"Error on Job: {tab.Job.Name}, Message: {ex.Message}");
                    }
                }

                string timeoutSecondsString = _configuration["TimeoutSecondsAfterJobs"];
                int timeoutSeconds = 5;

                if (timeoutSecondsString != null && int.TryParse(timeoutSecondsString, out int newtimeoutSeconds))
                {
                    timeoutSeconds = newtimeoutSeconds;
                }

         //       _logger.LogDebug($"Timeout seconds after jobs: {timeoutSeconds}");
                Thread.Sleep(TimeSpan.FromSeconds(timeoutSeconds));
            }
        }

        private void AddJob(Predicate<Uri> predicate, Func<IWebDriver, IJob> action)
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

        private void OpenTabs(string[] urlStrings)
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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _tabWithJobs.Clear();
                }

                _webDriver?.Dispose();
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
