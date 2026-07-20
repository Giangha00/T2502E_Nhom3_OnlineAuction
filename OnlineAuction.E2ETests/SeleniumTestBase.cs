using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace OnlineAuction.E2ETests;

public abstract class SeleniumTestBase : IDisposable
{
    protected SeleniumTestBase()
    {
        Settings = new E2ETestSettings();

        var options = new ChromeOptions();
        options.AddArgument("--window-size=1366,900");

        if (Settings.Headless)
        {
            options.AddArgument("--headless=new");
        }

        Driver = new ChromeDriver(options);
        Wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
    }

    protected E2ETestSettings Settings { get; }

    protected IWebDriver Driver { get; }

    protected WebDriverWait Wait { get; }

    public void Dispose()
    {
        Driver.Quit();
        Driver.Dispose();
    }

    protected IWebElement WaitUntilDisplayed(By locator)
    {
        return Wait.Until(driver =>
        {
            var element = driver.FindElements(locator).FirstOrDefault(candidate => candidate.Displayed);
            return element;
        });
    }

    protected void ClickDisplayed(By locator)
    {
        WaitUntilDisplayed(locator).Click();
    }
}
