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

namespace TwitchPlanner.Services
{
    public class BrowserChromeService : IHostedService, IDisposable
    {
        /// <summary>
        /// TODO NLOG USELESS
        /// </summary>
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        private readonly IConfiguration _configuration;
        private readonly ILogger<BrowserChromeService> _logger;
        private readonly ILogger<PointCollectorTwitchJob> collectorLogger;
        private ChromeDriver _webDriver;
        private bool disposedValue;
        private Uri _followingUri;

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
            Task.Run(() =>
            {
                _logger.LogInformation("PerformerPlanner starting..");
                _log.Info($"Monitoring starting");
                StartInternal(cancellationToken);
            }, cancellationToken);
            return Task.CompletedTask;
        }

        private void StartInternal(CancellationToken cancellationToken)
        {
            OpenFollowingTabAndAuth("https://www.twitch.tv/directory/following/live");
            _logger.LogInformation($"Open followingUri: {_followingUri}");

            IsRunning = true;
            List<BrowserTab> currentBrowserTabs = new List<BrowserTab>();

            while (IsRunning)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    int tabIndex = 0;
                    _webDriver.SwitchTo().Window(_webDriver.WindowHandles[tabIndex]);
                    _webDriver.Navigate().Refresh();

                    UpdateBrowserTabsAndState(currentBrowserTabs);

                    for (tabIndex = 1; tabIndex <= currentBrowserTabs.Count; tabIndex++)
                    {
                        BrowserTab tab = currentBrowserTabs[tabIndex - 1];

                        try
                        {
                            if (!TrySwitchNewBrowserTab(tab, tabIndex))
                            {
                                currentBrowserTabs.Remove(tab);
                                tabIndex--;
                                continue;
                            }

                            foreach (IJob job in tab.Jobs)
                            {
                                try
                                {
                                    if (tab.State == ChannelState.New)
                                    {
                                        job.OnStartUp();
                                    }

                                    job.Execute(cancellationToken);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, ex.Message);
                                    _log.Error(ex, $"Error on Job: {job.Name}, Message: {ex.Message}");
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, ex.Message);
                            _log.Error(ex, $"BrowserTab: {tab}, Message: {ex.Message}");
                        }
                    }

                    string timeoutSecondsString = _configuration["TimeoutSecondsAfterJobs"];
                    int timeoutSeconds = 5;

                    if (timeoutSecondsString != null && int.TryParse(timeoutSecondsString, out int newtimeoutSeconds))
                    {
                        timeoutSeconds = newtimeoutSeconds;
                    }

                    Thread.Sleep(TimeSpan.FromSeconds(timeoutSeconds));
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogError(ex, ex.Message);
                    _log.Error(ex, ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    _log.Error(ex, $"Body error, Message: {ex.Message}");
                }
            }
        }

        private bool TrySwitchNewBrowserTab(BrowserTab tab, int tabIndex)
        {
            switch (tab.State)
            {
                case ChannelState.New:
                    _webDriver.SwitchTo().NewWindow(WindowType.Tab);
                    _webDriver.Navigate().GoToUrl(tab.Uri);
                    break;
                case ChannelState.Live:
                    _webDriver.SwitchTo().Window(_webDriver.WindowHandles[tabIndex]);
                    break;
                case ChannelState.Ofline:
                    IWebDriver deadTab = _webDriver.SwitchTo().Window(_webDriver.WindowHandles[tabIndex]);
                    deadTab.Close();
                    return false;
            }

            return true;
        }

        private void UpdateBrowserTabsAndState(List<BrowserTab> currentBrowserTabs)
        {
            Uri[] actualLinkChannels = TwitchHelper.GetLinkChannels(_webDriver).OrderBy(a => a.ToString()).ToArray();

            // Change all new state in Live
            currentBrowserTabs.Where(item => item.State == ChannelState.New).ForEach(item =>
            {
                item.State = ChannelState.Live;
            });

            // Stream is not alive, ofline
            currentBrowserTabs.Where(t => !actualLinkChannels.Contains(t.Uri)).ForEach(item =>
            {
                item.State = ChannelState.Ofline;
            });

            // new streams
            actualLinkChannels.ForEach(item =>
            {
                if (currentBrowserTabs.Count == 0 || !currentBrowserTabs.Any(tab => tab.Uri == item))
                {
                    IJob[] jobs = new IJob[]
                    {
                        new PointCollectorTwitchJob(_webDriver, _configuration, collectorLogger, item.AbsolutePath.TrimStart('/')),
                    };

                    currentBrowserTabs.Add(new BrowserTab(item, jobs));
                }
            });
        }

        private void OpenFollowingTabAndAuth(string followingUrl)
        {
            _followingUri = new Uri(followingUrl);
            InitChromDriver();

            _webDriver.Navigate().GoToUrl(followingUrl);

            string twitchLogin = _configuration["TwitchLogin"];
            string twitchPassword = _configuration["TwitchPassword"];

            TwitchHelper.TwitchAuth(new TwitchIdentity(twitchLogin, twitchPassword), _webDriver);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            IsRunning = false;

            string MESSAGE = $"Service {nameof(BrowserChromeService)} is shutting down.";
            _logger.LogInformation(MESSAGE);
            _log.Info(MESSAGE);

            return Task.CompletedTask;
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
