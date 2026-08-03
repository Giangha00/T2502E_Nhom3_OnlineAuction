using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace OnlineAuction.E2ETests.Support;

public static class E2EDriverFactory
{
    public static IWebDriver Create()
    {
        var options = new ChromeOptions();
        options.PageLoadStrategy = PageLoadStrategy.Eager;
        options.AddArgument("--disable-gpu");
        options.AddArgument("--window-size=1400,900");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-background-networking");
        options.AddArgument("--disable-extensions");
        options.AddArgument("--disable-popup-blocking");
        options.AddArgument("--disable-renderer-backgrounding");
        options.AddArgument("--disable-ipc-flooding-protection");
        options.AddArgument("--hide-scrollbars");
        options.AddArgument("--mute-audio");

        // Default headless for stability under CI/local suites; set E2E_HEADLESS=0 for headed debug.
        var headless = Environment.GetEnvironmentVariable("E2E_HEADLESS");
        if (!string.Equals(headless, "0", StringComparison.Ordinal))
        {
            options.AddArgument("--headless=new");
        }

        var driver = new ChromeDriver(options);
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2);
        driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(45);
        driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromSeconds(30);
        return driver;
    }
}
