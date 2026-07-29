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
        driver.Navigate().GoToUrl($"{config.BaseUrl}/Auth/Logout");
    }

    public static bool HasUserCookie(IWebDriver driver) =>
        driver.Manage().Cookies.AllCookies.Any(c => c.Name == UserCookie);

    public static bool HasAdminCookie(IWebDriver driver) =>
        driver.Manage().Cookies.AllCookies.Any(c => c.Name == AdminCookie);

    public static int? FirstAuctionIdFromCatalog(IWebDriver driver)
    {
        var cards = driver.FindElements(By.CssSelector("[data-id]"));
        foreach (var card in cards)
        {
            var id = card.GetAttribute("data-id");
            if (int.TryParse(id, out var auctionId))
            {
                return auctionId;
            }
        }

        return null;
    }

    public static bool IsLoggedInHeader(IWebDriver driver) =>
        driver.FindElements(By.Id("userMenuBtn")).Any(e => e.Displayed);
}
