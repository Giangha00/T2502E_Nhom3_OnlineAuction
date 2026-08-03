using OnlineAuction.E2ETests.Support;
using OpenQA.Selenium;

namespace OnlineAuction.E2ETests.PageObjects;

public sealed class AdminLoginPage
{
    readonly IWebDriver _driver;

    public AdminLoginPage(IWebDriver driver) => _driver = driver;

    public void Login(string email, string password)
    {
        E2EWait.Visible(_driver, E2ESelectors.AdminLoginRoot, TimeSpan.FromSeconds(10));
        var emailEl = E2EWait.Visible(_driver, E2ESelectors.AdminEmail);
        emailEl.Clear();
        emailEl.SendKeys(email);
        var passwordEl = E2EWait.Visible(_driver, E2ESelectors.AdminPassword);
        passwordEl.Clear();
        passwordEl.SendKeys(password);
        E2EWait.SafeClick(_driver, E2EWait.Visible(_driver, E2ESelectors.AdminLoginSubmit));
        E2EWait.Until(
            _driver,
            d => (d.Url.Contains("/Admin", StringComparison.OrdinalIgnoreCase)
                  && !d.Url.Contains("Login", StringComparison.OrdinalIgnoreCase))
                 || HasValidationErrors()
                 || E2EAuthHelper.HasAdminCookie(d),
            TimeSpan.FromSeconds(20));
    }

    public bool HasValidationErrors() =>
        _driver.FindElements(E2ESelectors.AdminValidationErrors).Any(e => e.Displayed && !string.IsNullOrWhiteSpace(e.Text));
}
