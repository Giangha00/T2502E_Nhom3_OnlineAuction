using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace OnlineAuction.E2ETests.PageObjects;

public sealed class AuthModalPage
{
    readonly IWebDriver _driver;

    public AuthModalPage(IWebDriver driver) => _driver = driver;

    public void OpenLoginTab()
    {
        ClickHeaderAuthTab("login");
        WaitForVisible(By.Id("authPanelLogin"));
    }

    public void OpenSignupTab()
    {
        ClickHeaderAuthTab("signup");
        WaitForVisible(By.Id("authPanelSignup"));
    }

    void ClickHeaderAuthTab(string tab)
    {
        var btn = _driver.FindElement(By.CssSelector($"button[data-auth-tab='{tab}']"));
        btn.Click();
        WaitForVisible(By.Id("authModalOverlay"));
    }

    public void Login(string email, string password)
    {
        OpenLoginTab();
        _driver.FindElement(By.Id("modalEmail")).Clear();
        _driver.FindElement(By.Id("modalEmail")).SendKeys(email);
        _driver.FindElement(By.Id("modalPassword")).Clear();
        _driver.FindElement(By.Id("modalPassword")).SendKeys(password);
        _driver.FindElement(By.CssSelector("#authPanelLogin button[type='submit']")).Click();
    }

    public void SignUp(string fullName, string email, string phone, string password, string confirmPassword)
    {
        OpenSignupTab();
        _driver.FindElement(By.Id("modalFullName")).SendKeys(fullName);
        _driver.FindElement(By.Id("modalSignupEmail")).SendKeys(email);
        _driver.FindElement(By.Id("modalPhone")).SendKeys(phone);
        _driver.FindElement(By.Id("modalSignupPassword")).SendKeys(password);
        _driver.FindElement(By.Id("modalConfirmPassword")).SendKeys(confirmPassword);
        _driver.FindElement(By.CssSelector("#authPanelSignup button[type='submit']")).Click();
    }

    public bool IsErrorVisible() =>
        TryFind(By.Id("authModalError"), out var el) && el.Displayed;

    public string? ErrorText =>
        TryFind(By.Id("authModalError"), out var el) && el.Displayed ? el.Text : null;

    public bool IsLoggedInHeader() =>
        TryFind(By.Id("userMenuBtn"), out var el) && el.Displayed;

    static void WaitForVisible(By by)
    {
        // Driver fixture sets implicit wait.
    }

    static bool TryFind(IWebDriver driver, By by, out IWebElement element)
    {
        try
        {
            element = driver.FindElement(by);
            return true;
        }
        catch (NoSuchElementException)
        {
            element = null!;
            return false;
        }
    }

    bool TryFind(By by, out IWebElement element) => TryFind(_driver, by, out element);
}
