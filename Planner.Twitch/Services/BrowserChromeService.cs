using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NLog;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

using Planner.Common;
using Planner.Common.Structure;
using Planner.Twitch.Jobs;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planner.Twitch.Services
{
    public class BrowserChromeService : IHostedService, IDisposable
    {
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
            var thread = new Thread(StartInternal)
            {
                Name = nameof(BrowserChromeService),
            };

            thread.Start(cancellationToken);

            return Task.CompletedTask;
        }

        private void StartInternal(object cancellationTokenObject)
        {
            CancellationToken cancellationToken = (CancellationToken) cancellationTokenObject;
            _logger.LogInformation("PerformerPlanner starting..");
            _log.Info($"Monitoring starting");

            OpenFollowingTabAndAuth("https://www.twitch.tv/directory/following/live");
            _logger.LogInformation($"Open followingUri: {_followingUri}");

            IsRunning = true;
            List<BrowserTab> currentBrowserTabs = new List<BrowserTab>();

            while (IsRunning)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _webDriver.SwitchTo().Window(_webDriver.WindowHandles[0]);
                    _webDriver.Navigate().Refresh();

                    UpdateBrowserTabsAndState(currentBrowserTabs);

                    _log.Trace($"After update tabs: \r\n[{string.Join(", ", currentBrowserTabs)}].");

                    for (int tabIndex = 1; tabIndex < _webDriver.WindowHandles.Count; tabIndex++)
                    {
                        try
                        {
                            _webDriver.SwitchTo().Window(_webDriver.WindowHandles[tabIndex]);

                            string url = _webDriver.Url;
                            BrowserTab browserTab = currentBrowserTabs.SingleOrDefault(t => t.Uri.ToString() == url);

                            // someone opened other tab
                            if (browserTab is null)
                            {
                                _log.Warn($"Tab has been closing {url}.");
                                _webDriver.Close();
                                continue;
                            }

                            _log.Trace($"SwitchTo on: {browserTab}.");

                            if (browserTab.State == TabState.Closed)
                            {
                                _webDriver.Close();
                                currentBrowserTabs.Remove(browserTab);

                                traceHandle($"Tab has been closing {url}.");
                            }

                            handleJobs(browserTab, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, ex.Message);
                            _log.Error(ex, $"BrowserTab: {_webDriver.Url}, Message: {ex.Message}");
                        }
                    }

                    WaitOnFinnalyTabs();
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogError(ex.Message);
                    _log.Error(ex.Message);
                }
                catch (Exception ex)
                {
                    errorHandle(ex, $"Body error url={_webDriver.Url}, message={ex.Message}.");
                }
            }
        }

        private void WaitOnFinnalyTabs()
        {
            string timeoutSecondsString = _configuration["TimeoutSecondsAfterJobs"];
            int timeoutSeconds = 5;

            if (timeoutSecondsString != null && int.TryParse(timeoutSecondsString, out int newtimeoutSeconds))
            {
                timeoutSeconds = newtimeoutSeconds;
            }

            Thread.Sleep(TimeSpan.FromSeconds(timeoutSeconds));
        }

        private void handleJobs(BrowserTab browserTab, CancellationToken cancellationToken)
        {
            foreach (ITabJob job in browserTab.Jobs)
            {
                try
                {
                    switch (browserTab.State)
                    {
                        case TabState.New:
                            job.OnStartUp();
                            break;
                        case TabState.Closed:
                            job.OnClosed();
                            continue;
                    }

                    if (job.CanExecute(browserTab))
                    {
                        job.Execute(cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    errorHandle(ex, $"Error on Job: {job.Name}, Message: {ex.Message}");
                    browserTab.CountErrorChain++;
                    break;
                }
            }
        }

        private void traceHandle(string message)
        {
            _logger.LogTrace(message);
            _log.Trace(message);
        }

        private void errorHandle(Exception ex, string message)
        {
            _logger.LogError(message);
            _log.Error(ex, message);
        }

        private void UpdateBrowserTabsAndState(List<BrowserTab> currentBrowserTabs)
        {
            Uri[] actualLinkChannels = TwitchHelper.GetLinkChannels(_webDriver);

            // Change all new state in Live
            currentBrowserTabs.Where(item => item.State == TabState.New).ForEach(item =>
            {
                item.State = TabState.Active;
            });

            // Stream is not alive, ofline
            currentBrowserTabs.Where(t => !actualLinkChannels.Contains(t.Uri)).ForEach(item =>
            {
                item.State = TabState.Closed;
            });

            // new streams
            actualLinkChannels.ForEach(item =>
            {
                if (currentBrowserTabs.Count == 0 || !currentBrowserTabs.Any(tab => tab.Uri == item))
                {
                    ITabJob[] jobs = new ITabJob[]
                    {
                        new PointCollectorTwitchJob(_webDriver, _configuration, collectorLogger, item.AbsolutePath.TrimStart('/')),
                    };

                    _webDriver.SwitchTo().NewWindow(WindowType.Tab);
                    _webDriver.Navigate().GoToUrl(item);
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

            _webDriver = new ChromeDriver(options); // 85.0.4183.87
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _followingUri = null;
                }

                _webDriver?.Dispose();
                disposedValue = true;
            }
        }

        ~BrowserChromeService()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
