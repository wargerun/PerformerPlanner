using NLog;
using OpenQA.Selenium;
using Planner.Common;
using System;

namespace TwitchPlanner.Jobs
{
    public class PointCollectorTwitchJob : TwitchJobBase
    {
        private const string CSS_SELECTOR_BUTTON_ON_GET_POINT = "button[class='tw-button tw-button--success tw-interactive']";
        public static readonly string TwitchAddressPrefix = "https://www.twitch.tv/";
        private readonly TwitchIdentity _twitchIdentity;
        private string _lastPoints;

        public override string Name { get; }

        public PointCollectorTwitchJob(IWebDriver webDriver, TwitchIdentity twitchIdentity)
            : base(webDriver)
        {
            if (webDriver.Url is null)
            {
                throw new ArgumentNullException(nameof(webDriver));
            }

            if (!UrlValid(webDriver.Url))
            {
                throw new ArgumentException("Is not twitch uri", nameof(webDriver));
            }

            string url = webDriver.Url;
            Name = $"Channel: {url.Substring(url.LastIndexOf('/') + 1)}";
            _twitchIdentity = twitchIdentity;
        }

        public static bool UrlValid(string url) => url.StartsWith(TwitchAddressPrefix);

        public override void Execute()
        {
            TwitchAuth(_twitchIdentity);
            CollectPoint();
        }

        private void CollectPoint()
        {
            // span with value points
            IWebElement valuePointsElements = WebDriver.FindElement(By.CssSelector("div.tw-c-text-alt-2.tw-flex>span.tw-animated-number"));
            string valuePoints = valuePointsElements.Text;

            if (valuePoints != _lastPoints)
            {
                _log.Debug($"Uri: {WebDriver.Url}");
                _log.Info($"Value points: {valuePoints}");
                Console.WriteLine($"[{DateTime.Now}] - {Name}, Value points: {valuePoints}");
            }

            if (WebDriver.IsElementExist(By.CssSelector(CSS_SELECTOR_BUTTON_ON_GET_POINT)))
            {
                IWebElement buttonAuth = WebDriver.FindElement(By.CssSelector(CSS_SELECTOR_BUTTON_ON_GET_POINT));
                buttonAuth.Click();
                _log.Info($"Get new points clicked");

                Console.WriteLine($"[{DateTime.Now}] - JobName: {Name}, Get new points clicked.");
            }

            _lastPoints = valuePoints;
        }
    }
}
