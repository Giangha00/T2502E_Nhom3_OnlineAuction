using OpenQA.Selenium;

namespace OnlineAuction.E2ETests.Support;

public abstract class E2ETestBase : IDisposable
{
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
    }

    public void Dispose()
    {
        Driver.Quit();
        Driver.Dispose();
        Http.Dispose();
    }
}
