using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace OnlineAuction.E2ETests.Support;

public static class E2EWait
{
    public static IWebElement Visible(IWebDriver driver, By by, TimeSpan? timeout = null)
    {
        var wait = new WebDriverWait(driver, timeout ?? TimeSpan.FromSeconds(10));
        return wait.Until(d =>
        {
            try
            {
                var el = d.FindElement(by);
                return el.Displayed ? el : null;
            }
            catch (NoSuchElementException)
            {
                return null;
            }
            catch (StaleElementReferenceException)
            {
                return null;
            }
        })!;
    }

    public static bool Exists(IWebDriver driver, By by, TimeSpan? timeout = null)
    {
        try
        {
            Visible(driver, by, timeout);
            return true;
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    public static bool Until(IWebDriver driver, Func<IWebDriver, bool> condition, TimeSpan? timeout = null)
    {
        try
        {
            var wait = new WebDriverWait(driver, timeout ?? TimeSpan.FromSeconds(10));
            return wait.Until(condition);
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    public static string? DomAttr(IWebElement element, string name)
    {
        try
        {
            return element.GetDomAttribute(name);
        }
        catch
        {
#pragma warning disable CS0618
            return element.GetAttribute(name);
#pragma warning restore CS0618
        }
    }

    /// <summary>
    /// Click via JS to avoid Chrome "Timed out receiving message from renderer" on navigations.
    /// </summary>
    public static void SafeClick(IWebDriver driver, IWebElement element)
    {
        try
        {
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", element);
        }
        catch (WebDriverException)
        {
            element.Click();
        }
    }
}
