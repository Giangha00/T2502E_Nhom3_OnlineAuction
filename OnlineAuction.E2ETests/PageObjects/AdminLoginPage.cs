using OpenQA.Selenium;

namespace OnlineAuction.E2ETests.PageObjects;

public sealed class AdminLoginPage
{
    readonly IWebDriver _driver;

    public AdminLoginPage(IWebDriver driver) => _driver = driver;

    public void Login(string email, string password)
    {
        _driver.FindElement(By.Id("Email")).Clear();
        _driver.FindElement(By.Id("Email")).SendKeys(email);
        _driver.FindElement(By.Id("Password")).Clear();
        _driver.FindElement(By.Id("Password")).SendKeys(password);
        _driver.FindElement(By.CssSelector("form button[type='submit']")).Click();
    }

    public bool HasValidationErrors() =>
        _driver.FindElements(By.CssSelector(".validation-summary-errors, .text-red-600, .text-red-700")).Count > 0;
}
