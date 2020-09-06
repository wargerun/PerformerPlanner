using NLog;

using System.Threading;
using System.Threading.Tasks;

using TwitchPlanner;
using TwitchPlanner.Jobs;

namespace PerformerPlanner
{
    class Program
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        static async Task Main(string[] args)
        {
            string[] urlStrings = new string[]
            {
                "https://www.twitch.tv/honeymad",
                "https://www.twitch.tv/alohadancetv",
                "https://www.twitch.tv/dreadztv",
                "https://www.twitch.tv/recrent",
                "https://www.twitch.tv/zanuda",
                "https://www.twitch.tv/mistafaker",
            };

            ChromeManager chromeManager = new ChromeManager();
            chromeManager.OpenTabs(urlStrings);

            chromeManager.AddJob(
                url => PointCollectorTwitchJob.UrlValid(url.ToString()),
                webDriver => new PointCollectorTwitchJob(webDriver, new TwitchIdentity(args[0], args[1]))
            );

            _log.Info("PerformerPlanner starting..");
            await chromeManager.StartAsync();

            Thread.Sleep(Timeout.Infinite);
        }
    }
}
