using OpenQA.Selenium;

using Planner.Common;

using System;
using System.Collections.ObjectModel;

namespace Planner.Twitch
{
    public static class TwitchHelper
    {
        private static readonly By BodyElementAsLogged = By.CssSelector("body[class*='logged-in']");

        public static bool LoginIsPresent(IWebDriver webDriver) => webDriver.IsElementExist(BodyElementAsLogged);

        public static void TwitchAuth(TwitchIdentity identity, IWebDriver webDriver)
        {
            By byLoginUsername = By.Id("login-username");

            webDriver.WaitUntilElementIsVisible(byLoginUsername);
            IWebElement txtLoginMailInout = webDriver.FindElement(byLoginUsername);
            txtLoginMailInout.SendKeys(identity.Login);

            IWebElement txtPasswordInput = webDriver.FindElement(By.Id("password-input"));
            txtPasswordInput.SendKeys(identity.Password);

            IWebElement buttonAuth = webDriver.FindElement(By.CssSelector($"button[data-a-target='passport-login-button']"));
            buttonAuth.Click();

            // operation
            if (webDriver.IsElementExist(By.CssSelector("div[class='auth-modal tw-relative']")))
            {
                // Взять с почты код
                Console.Write("Введите код с почты: ");
                Console.ReadLine();
            }
        }

        public static Uri[] GetLinkChannels(IWebDriver webDriver)
        {
            const string SELECTOR = "a[class=\"tw-interactive tw-link\"][data-a-target=\"preview-card-image-link\"]";

            webDriver.WaitUntilElementIsVisible(By.CssSelector(SELECTOR));
            ReadOnlyCollection<IWebElement> elements = webDriver.FindElements(By.CssSelector(SELECTOR));
            Uri[] linkChannels = new Uri[elements.Count];

            elements.ForEach((item, index) =>
            {
                string hrefChannel = item.GetAttribute("href");

                linkChannels[index] = new Uri(hrefChannel);
            });

            return linkChannels;
        }
    }
}
