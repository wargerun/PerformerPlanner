using NLog;

using OpenQA.Selenium;

using Planner.Common;
using Planner.Common.Structure;

using System;

namespace Planner.Twitch.Jobs
{
    public abstract class TwitchJobBase : ITabJob
    {
        protected IWebDriver WebDriver { get; }
        protected static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private static readonly string CssSelectorOnLoginPageButton = "tw-align-items-center tw-align-middle tw-border-bottom-left-radius-medium tw-border-bottom-right-radius-medium tw-border-top-left-radius-medium tw-border-top-right-radius-medium tw-core-button tw-core-button--secondary tw-inline-flex tw-interactive tw-justify-content-center tw-overflow-hidden tw-relative";
        protected static readonly By BodyElement = By.CssSelector("body");
        protected static readonly By VideoPlayerCssSelector = By.CssSelector("div[data-a-target=\"video-player\"]");
        public abstract string Name { get; }

        protected TwitchJobBase(IWebDriver webDriver)
        {
            if (webDriver is null)
            {
                throw new ArgumentNullException(nameof(webDriver));
            }

            WebDriver = webDriver;
        }

        public virtual bool CanExecute(BrowserTab browserTab)
        {
            if (browserTab.CountErrorChain > 10)
            {
                browserTab.State = TabState.Closed;
                return false;
            }

            return true;
        }

        public abstract void Execute(System.Threading.CancellationToken cancellationToken);

        public abstract void OnStartUp();

        protected void TwitchAuth(TwitchIdentity identity)
        {
            if (!TwitchHelper.LoginIsPresent(WebDriver))
            {
                // Indetify login
                IWebElement loginButton = WebDriver.FindElement(By.CssSelector($"button[class='{CssSelectorOnLoginPageButton}']"));
                loginButton.Click();

                TwitchHelper.TwitchAuth(identity, WebDriver);
            }
        }

        public virtual void OnClosed()
        {

        }
    }
}
