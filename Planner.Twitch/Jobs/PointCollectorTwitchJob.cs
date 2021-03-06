﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using NLog;

using OpenQA.Selenium;

using Planner.Common;

using System;

namespace Planner.Twitch.Jobs
{
    public class PointCollectorTwitchJob : TwitchJobBase
    {
        private const string CSS_SELECTOR_BUTTON_ON_GET_POINT = "button[class='tw-button tw-button--success tw-interactive']";
        public static readonly string TwitchAddressPrefix = "https://www.twitch.tv/";
        private readonly TwitchIdentity _twitchIdentity;
        private readonly ILogger<PointCollectorTwitchJob> _logger;
        private string _lastPoints;

        public override string Name { get; }

        public PointCollectorTwitchJob(IWebDriver webDriver, IConfiguration _configuration, ILogger<PointCollectorTwitchJob> logger, string channelName)
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

            Name = $"Channel: {channelName}";

            string twitchLogin = _configuration["TwitchLogin"];
            string twitchPassword = _configuration["TwitchPassword"];

            _twitchIdentity = new TwitchIdentity(twitchLogin, twitchPassword);
            _logger = logger;
        }

        public static bool UrlValid(string url) => url.StartsWith(TwitchAddressPrefix);

        public override void OnStartUp()
        {
            StopPlayer();

            TwitchAuth(_twitchIdentity);
        }

        private void StopPlayer()
        {
            By cssSelectorButtonPlayer = By.CssSelector("button[data-a-target=\"player-play-pause-button\"]");

            try
            {
                WebDriver.WaitUntilElementIsVisible(cssSelectorButtonPlayer);
                IWebElement buttonElement = WebDriver.FindElement(cssSelectorButtonPlayer);
                buttonElement.Click();
            }
            catch
            {
                // Не имеет значения даже для логирования
            }
        }

        public override void Execute(System.Threading.CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CollectPoint();
        }

        private void CollectPoint()
        {
            // span with value points
            By elementLocator = By.CssSelector("div.tw-c-text-alt-2.tw-flex>span.tw-animated-number");
            WebDriver.WaitUntilElementIsVisible(elementLocator);
            IWebElement valuePointsElements = WebDriver.FindElement(elementLocator);
            string valuePoints = valuePointsElements.Text;

            if (valuePoints != _lastPoints)
            {
                _log.Debug($"Uri: {WebDriver.Url}");
                _log.Info($"Value points: {valuePoints}");
                _logger.LogInformation($"{Name}, Value points: {valuePoints}");
            }

            if (WebDriver.IsElementExist(By.CssSelector(CSS_SELECTOR_BUTTON_ON_GET_POINT)))
            {
                IWebElement buttonAuth = WebDriver.FindElement(By.CssSelector(CSS_SELECTOR_BUTTON_ON_GET_POINT));
                buttonAuth.Click();
                _log.Info($"Get new points clicked");

                _logger.LogInformation($"JobName: {Name}, Get new points clicked.");
            }

            _lastPoints = valuePoints;
        }
    }
}
