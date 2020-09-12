using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

using System;

namespace Planner.Common
{
    public static class SeleniumExtensions
    {
        public static bool IsElementExist(this ISearchContext webElement, By by)
        {
            try
            {
                webElement.FindElement(by);
                return true;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        public static void WaitUntilElementIsVisible(this IWebDriver webElement, By elementLocator, int timeout = 10) => webElement.WaitUntilElementIsVisible(elementLocator, TimeSpan.FromSeconds(timeout));

        public static void WaitUntilElementIsVisible(this IWebDriver webElement, By elementLocator, TimeSpan timeout)
        {
            WebDriverWait wait = new WebDriverWait(webElement, timeout);

            Func<IWebDriver, IWebElement> condition = SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(elementLocator);

            wait.Until(condition);
        }

        public static void WaitUntilElementExists(this IWebDriver webElement, By elementLocator, int timeout = 10) => webElement.WaitUntilElementExists(elementLocator, TimeSpan.FromSeconds(timeout));

        public static void WaitUntilElementExists(this IWebDriver webElement, By elementLocator, TimeSpan timeout)
        {
            WebDriverWait wait = new WebDriverWait(webElement, timeout);

            Func<IWebDriver, IWebElement> condition = SeleniumExtras.WaitHelpers.ExpectedConditions.ElementExists(elementLocator);

            wait.Until(condition);
        }
    }
}
