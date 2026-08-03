using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace OnlineAuction.E2ETests.Support;

public static class E2EDriverFactory
{
    public static IWebDriver Create()
    {
        var options = new ChromeOptions();
        options.AddArgument("--disable-gpu");
        options.AddArgument("--window-size=1400,900");
        if (string.Equals(Environment.GetEnvironmentVariable("E2E_HEADLESS"), "1", StringComparison.Ordinal))
        {
            options.AddArgument("--headless=new");
        }

        var driver = new ChromeDriver(options);
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(8);
        driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
        return driver;
    }
}
