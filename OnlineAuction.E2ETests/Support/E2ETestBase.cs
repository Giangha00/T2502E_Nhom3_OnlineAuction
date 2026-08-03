using OpenQA.Selenium;

namespace OnlineAuction.E2ETests.Support;

public abstract class E2ETestBase : IDisposable
{
    static readonly Dictionary<string, string> PageMarkers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/"] = "home",
        ["/Auction"] = "auction-index",
        ["/BuyNow"] = "buy-now",
        ["/Order"] = "order",
        ["/Sell/Create"] = "sell-create",
        ["/Sell/BuyNow"] = "sell-buynow",
        ["/Contact"] = "contact",
        ["/Account/Selling"] = "account-selling",
        ["/Account/Watchlist"] = "account-listings",
        ["/Watchlist"] = "account-listings",
        ["/Admin/Dashboard"] = "admin-dashboard",
        ["/Admin/Auction"] = "admin-auction",
        ["/Admin/BuyNow"] = "admin-buynow",
        ["/Admin/Category"] = "admin-category",
        ["/Admin/User"] = "admin-user",
        ["/Admin/Product"] = "admin-product",
        ["/Admin/Complaint"] = "admin-complaint",
        ["/Admin/Permission"] = "admin-permission",
        ["/Admin/AuctionVerification"] = "admin-verification",
        ["/Admin/Account/Login"] = "admin-login",
    };

    protected E2ETestBase()
    {
        Config = E2EConfig.Load();
        Driver = E2EDriverFactory.Create();
        Http = new E2EHttpHelper(Config.BaseUrl);
        E2ERuntime.RequireApp(Http, Config);
    }

    protected E2EConfig Config { get; }
    protected IWebDriver Driver { get; }
    protected E2EHttpHelper Http { get; }

    protected string Url(string path)
    {
        if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        var normalized = path.StartsWith('/') ? path : "/" + path;
        return $"{Config.BaseUrl}{normalized}";
    }

    protected void Go(string path) => Driver.Navigate().GoToUrl(Url(path));

    protected bool HasCookie(string name) =>
        Driver.Manage().Cookies.AllCookies.Any(c => c.Name == name);

    protected void AssertPageOk(string path)
    {
        Go(path);
        Assert.DoesNotContain("404", Driver.Title, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(Driver.PageSource));

        var marker = ResolvePageMarker(path);
        if (marker is not null)
        {
            Assert.True(
                Driver.FindElements(E2ESelectors.Page(marker)).Count > 0
                || Driver.FindElements(E2ESelectors.SiteMain).Count > 0,
                $"Expected data-e2e-page='{marker}' (or site main) on {path}");
        }
        else if (path.StartsWith("/Auction/Detail/", StringComparison.OrdinalIgnoreCase))
        {
            Assert.True(
                Driver.FindElements(E2ESelectors.Page("auction-detail")).Count > 0
                || Driver.FindElements(E2ESelectors.BidPanel).Count > 0,
                $"Expected auction detail markers on {path}");
        }
    }

    static string? ResolvePageMarker(string path)
    {
        var normalized = path.StartsWith('/') ? path : "/" + path;
        var q = normalized.IndexOf('?', StringComparison.Ordinal);
        if (q >= 0)
        {
            normalized = normalized[..q];
        }

        if (PageMarkers.TryGetValue(normalized, out var marker))
        {
            return marker;
        }

        return null;
    }

    public void Dispose()
    {
        Driver.Quit();
        Driver.Dispose();
        Http.Dispose();
    }
}
