using OpenQA.Selenium;

namespace OnlineAuction.E2ETests;

public sealed class AuthSignupTests : SeleniumTestBase
{
    [E2EFact]
    public void User_Can_Sign_Up_From_Home_Page_Modal()
    {
        var uniqueSuffix = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var email = $"selenium-{uniqueSuffix}@auctionhouse.local";

        Driver.Navigate().GoToUrl(Settings.BaseUrl);

        ClickDisplayed(By.CssSelector("button[data-auth-tab='signup']"));

        WaitUntilDisplayed(By.Id("modalFullName")).SendKeys("Selenium Test User");
        Driver.FindElement(By.Id("modalSignupEmail")).SendKeys(email);
        Driver.FindElement(By.Id("modalPhone")).SendKeys("09012345678");
        Driver.FindElement(By.Id("modalSignupPassword")).SendKeys(Settings.SignupPassword);
        Driver.FindElement(By.Id("modalConfirmPassword")).SendKeys(Settings.SignupPassword);

        Driver.FindElement(By.CssSelector("#authPanelSignup button[type='submit']")).Click();

        Wait.Until(driver =>
            driver.FindElements(By.Id("authModalSuccess")).Any()
            && driver.FindElements(By.Id("authPanelLogin")).Any(element => element.Displayed));
    }
}
