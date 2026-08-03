using OpenQA.Selenium;

namespace OnlineAuction.E2ETests.Support;

public static class E2EAuthHelper
{
    public const string UserCookie = ".AuctionHouse.User";
    public const string AdminCookie = ".AuctionHouse.Admin";

    public static void LoginUser(IWebDriver driver, E2EConfig config)
    {
        driver.Navigate().GoToUrl($"{config.BaseUrl}/");
        new PageObjects.AuthModalPage(driver).Login(config.UserEmail, config.UserPassword);
    }

    public static void LoginAdmin(IWebDriver driver, E2EConfig config, string? returnUrl = null)
    {
        var url = $"{config.BaseUrl}/Admin/Account/Login";
        if (!string.IsNullOrEmpty(returnUrl))
        {
            url += $"?returnUrl={Uri.EscapeDataString(returnUrl)}";
        }

        driver.Navigate().GoToUrl(url);
        new PageObjects.AdminLoginPage(driver).Login(config.AdminEmail, config.AdminPassword);
    }

    public static void LogoutUser(IWebDriver driver, E2EConfig config)
    {
        driver.Navigate().GoToUrl($"{config.BaseUrl}/");
        if (!IsLoggedInHeader(driver))
        {
            return;
        }

        var js = (OpenQA.Selenium.IJavaScriptExecutor)driver;
        js.ExecuteScript("""
            var panel = document.getElementById('userMenuPanel');
            if (panel) { panel.classList.remove('hidden'); }
            var btn = document.querySelector("[data-e2e='logout-submit']")
                || document.querySelector("form[action*='Logout'] button[type='submit']");
            if (btn) { btn.click(); }
            """);
        E2EWait.Until(driver, d => !IsLoggedInHeader(d), TimeSpan.FromSeconds(10));
    }

    public static bool HasUserCookie(IWebDriver driver) =>
        driver.Manage().Cookies.AllCookies.Any(c => c.Name == UserCookie);

    public static bool HasAdminCookie(IWebDriver driver) =>
        driver.Manage().Cookies.AllCookies.Any(c => c.Name == AdminCookie);

    public static int? FirstAuctionIdFromCatalog(IWebDriver driver)
    {
        var cards = driver.FindElements(E2ESelectors.AuctionCard);
        foreach (var card in cards)
        {
            var id = E2EWait.DomAttr(card, "data-id");
            if (int.TryParse(id, out var auctionId))
            {
                return auctionId;
            }
        }

        return null;
    }

    public static bool IsLoggedInHeader(IWebDriver driver) =>
        driver.FindElements(E2ESelectors.UserMenu).Any(e => e.Displayed);
}
