using OpenQA.Selenium;

namespace OnlineAuction.E2ETests;

public sealed class AuthLoginTests : SeleniumTestBase
{
    [E2EFact]
    public void User_Can_Login_From_Home_Page_Modal()
    {
        Driver.Navigate().GoToUrl(Settings.BaseUrl);

        ClickDisplayed(By.CssSelector("button[data-auth-tab='login']"));

        var emailInput = WaitUntilDisplayed(By.Id("modalEmail"));
        emailInput.Clear();
        emailInput.SendKeys(Settings.UserEmail);

        var passwordInput = Driver.FindElement(By.Id("modalPassword"));
        passwordInput.Clear();
        passwordInput.SendKeys(Settings.UserPassword);

        Driver.FindElement(By.CssSelector("#authPanelLogin button[type='submit']")).Click();

        Wait.Until(driver =>
            driver.FindElements(By.CssSelector("form[action*='Logout']")).Any());
    }
}
