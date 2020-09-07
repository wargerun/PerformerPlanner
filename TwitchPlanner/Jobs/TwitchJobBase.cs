using NLog;
using OpenQA.Selenium;
using Planner.Common;
using System;

namespace TwitchPlanner.Jobs
{
    public abstract class TwitchJobBase : IJob
    {
        protected IWebDriver WebDriver { get; }
        protected static readonly Logger _log = LogManager.GetCurrentClassLogger();
        protected static readonly By BodyElementAsLogged = By.CssSelector("body[class*='logged-in']");
        protected static readonly By BodyElement = By.CssSelector("body");
        private static readonly string CssSelectorOnLoginPageButton = "tw-align-items-center tw-align-middle tw-border-bottom-left-radius-medium tw-border-bottom-right-radius-medium tw-border-top-left-radius-medium tw-border-top-right-radius-medium tw-core-button tw-core-button--secondary tw-inline-flex tw-interactive tw-justify-content-center tw-overflow-hidden tw-relative";

        public bool LoginIsPresent() => WebDriver.IsElementExist(BodyElementAsLogged);

        public abstract string Name { get; }

        protected TwitchJobBase(IWebDriver webDriver)
        {
            if (webDriver is null)
            {
                throw new ArgumentNullException(nameof(webDriver));
            }

            WebDriver = webDriver;
        }

        public abstract void Execute();

        protected void TwitchAuth(TwitchIdentity identity)
        {
            if (!LoginIsPresent())
            {
                // Indetify login
                IWebElement loginButton = WebDriver.FindElement(By.CssSelector($"button[class='{CssSelectorOnLoginPageButton}']"));
                loginButton.Click();

                By byLoginUsername = By.Id("login-username");

                WebDriver.WaitUntilElementIsVisible(byLoginUsername, TimeSpan.FromSeconds(1));
                IWebElement txtLoginMailInout = WebDriver.FindElement(byLoginUsername);
                txtLoginMailInout.SendKeys(identity.Login);

                IWebElement txtPasswordInput = WebDriver.FindElement(By.Id("password-input"));
                txtPasswordInput.SendKeys(identity.Password);

                IWebElement buttonAuth = WebDriver.FindElement(By.CssSelector($"button[data-a-target='passport-login-button']"));
                buttonAuth.Click();

                // operation
                if (WebDriver.IsElementExist(By.CssSelector("div[class='auth-modal tw-relative']")))
                {
                    // Взять с почты код
                    Console.Write("Введите код с почты: ");
                    Console.ReadLine();
                }
            }
        }
    }
}
