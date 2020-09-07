using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System.Threading;
using System.Threading.Tasks;

using TwitchPlanner;
using TwitchPlanner.Jobs;

namespace PerformerPlanner
{
    internal class BrowserChromeService : IHostedService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BrowserChromeService> _logger;
        private readonly ChromeManager _chromeManager;
        private readonly string[] urlStrings = new string[]
           {
                "https://www.twitch.tv/honeymad",
                "https://www.twitch.tv/alohadancetv",
                "https://www.twitch.tv/dreadztv",
                "https://www.twitch.tv/recrent",
                "https://www.twitch.tv/zanuda",
                "https://www.twitch.tv/mistafaker",
           };

        public BrowserChromeService(
            IConfiguration configuration,
            ILogger<BrowserChromeService> logger,
            ILogger<ChromeManager> chromeLogger)
        {
            _configuration = configuration;
            _logger = logger;
            _chromeManager = new ChromeManager(chromeLogger, configuration);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(StartAsyncInternal);

            return Task.CompletedTask;
        }

        private void StartAsyncInternal()
        {
            _chromeManager.OpenTabs(urlStrings);
            _logger.LogInformation($"OpenTabs Complete. Tabs count: {urlStrings.Length}");

            string twitchLogin = _configuration["TwitchLogin"];
            string twitchPassword = _configuration["TwitchPassword"];

            _logger.LogInformation($"AddJob..");
            _chromeManager.AddJob(
                url => PointCollectorTwitchJob.UrlValid(url.ToString()),
                webDriver => new PointCollectorTwitchJob(webDriver, new TwitchIdentity(twitchLogin, twitchPassword))
            );

            _logger.LogInformation("PerformerPlanner starting..");
            _chromeManager.StartAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _chromeManager.Stop();
            return Task.CompletedTask;
        }
    }
}
