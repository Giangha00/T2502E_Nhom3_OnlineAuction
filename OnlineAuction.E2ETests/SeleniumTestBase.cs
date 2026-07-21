using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace OnlineAuction.E2ETests;

// Lop nen dung chung cho moi Selenium test.
// No tao ChromeDriver, tao WebDriverWait va dam bao browser duoc tat sau test.
public abstract class SeleniumTestBase : IDisposable
{
    protected SeleniumTestBase()
    {
        // Doc cau hinh tu environment variables hoac gia tri mac dinh.
        Settings = new E2ETestSettings();

        // Cau hinh Chrome voi kich thuoc on dinh de UI render nhat quan.
        var options = new ChromeOptions();
        options.AddArgument("--window-size=1366,900");

        // Headless dung cho CI hoac khi khong muon hien cua so browser.
        if (Settings.Headless)
        {
            options.AddArgument("--headless=new");
        }

        // Tao browser session. Selenium Manager se tu tim/tai ChromeDriver neu can.
        Driver = new ChromeDriver(options);

        // Explicit wait giup doi UI dong nhu modal, redirect, ajax render.
        Wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
    }

    protected E2ETestSettings Settings { get; }

    protected IWebDriver Driver { get; }

    protected WebDriverWait Wait { get; }

    public void Dispose()
    {
        // Luon dong browser sau test de khong de lai process Chrome/ChromeDriver.
        Driver.Quit();
        Driver.Dispose();
    }

    protected IWebElement WaitUntilDisplayed(By locator)
    {
        // Tim element theo locator va chi tra ve khi element da hien thi.
        // Dung FindElements de tranh NoSuchElementException trong luc dang doi.
        return Wait.Until(driver =>
        {
            var element = driver.FindElements(locator).FirstOrDefault(candidate => candidate.Displayed);
            return element;
        });
    }

    protected void ClickDisplayed(By locator)
    {
        // Helper rut gon thao tac pho bien: doi element hien thi roi click.
        WaitUntilDisplayed(locator).Click();
    }
}
