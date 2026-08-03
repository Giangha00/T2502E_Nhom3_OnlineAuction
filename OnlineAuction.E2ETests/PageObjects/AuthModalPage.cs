using OnlineAuction.E2ETests.Support;
using OpenQA.Selenium;

namespace OnlineAuction.E2ETests.PageObjects;

public sealed class AuthModalPage
{
    readonly IWebDriver _driver;

    public AuthModalPage(IWebDriver driver) => _driver = driver;

    public void OpenLoginTab()
    {
        ClickHeaderAuthTab("login");
        E2EWait.Visible(_driver, E2ESelectors.AuthPanelLogin);
    }

    public void OpenSignupTab()
    {
        ClickHeaderAuthTab("signup");
        E2EWait.Visible(_driver, E2ESelectors.AuthPanelSignup);
    }

    void ClickHeaderAuthTab(string tab)
    {
        var openBy = tab == "signup" ? E2ESelectors.AuthOpenSignup : E2ESelectors.AuthOpenLogin;
        var btn = E2EWait.Visible(_driver, openBy);
        E2EWait.SafeClick(_driver, btn);
        E2EWait.Visible(_driver, E2ESelectors.AuthModal);

        // Ensure the correct inner tab is active after overlay opens.
        var innerTab = tab == "signup" ? E2ESelectors.AuthTabSignup : E2ESelectors.AuthTabLogin;
        if (E2EWait.Exists(_driver, innerTab, TimeSpan.FromSeconds(2)))
        {
            var inner = _driver.FindElement(innerTab);
            if (inner.Displayed)
            {
                E2EWait.SafeClick(_driver, inner);
            }
        }
    }

    public void Login(string email, string password)
    {
        OpenLoginTab();
        var emailEl = E2EWait.Visible(_driver, E2ESelectors.ModalEmail);
        emailEl.Clear();
        emailEl.SendKeys(email);
        var passwordEl = E2EWait.Visible(_driver, E2ESelectors.ModalPassword);
        passwordEl.Clear();
        passwordEl.SendKeys(password);
        E2EWait.SafeClick(_driver, E2EWait.Visible(_driver, E2ESelectors.AuthLoginSubmit));
        E2EWait.Until(
            _driver,
            d => IsLoggedInHeader() || IsErrorVisible() || E2EAuthHelper.HasUserCookie(d),
            TimeSpan.FromSeconds(20));
    }

    public void SignUp(string fullName, string email, string phone, string password, string confirmPassword)
    {
        OpenSignupTab();
        E2EWait.Visible(_driver, E2ESelectors.ModalFullName).SendKeys(fullName);
        E2EWait.Visible(_driver, E2ESelectors.ModalSignupEmail).SendKeys(email);
        E2EWait.Visible(_driver, E2ESelectors.ModalPhone).SendKeys(phone);
        E2EWait.Visible(_driver, E2ESelectors.ModalSignupPassword).SendKeys(password);
        E2EWait.Visible(_driver, E2ESelectors.ModalConfirmPassword).SendKeys(confirmPassword);
        E2EWait.SafeClick(_driver, E2EWait.Visible(_driver, E2ESelectors.AuthSignupSubmit));
        E2EWait.Until(
            _driver,
            d => IsLoggedInHeader() || IsErrorVisible() || E2EAuthHelper.HasUserCookie(d),
            TimeSpan.FromSeconds(20));
    }

    public bool IsErrorVisible()
    {
        if (TryFind(E2ESelectors.AuthError, out var alerts))
        {
            var authError = E2EWait.DomAttr(alerts, "data-auth-error");
            if (!string.IsNullOrWhiteSpace(authError))
            {
                return true;
            }

            if (alerts.Displayed && !string.IsNullOrWhiteSpace(alerts.Text))
            {
                return true;
            }
        }

        if (TryFind(E2ESelectors.AlertModalMessage, out var msg)
            && msg.Displayed
            && !string.IsNullOrWhiteSpace(msg.Text))
        {
            return true;
        }

        // Server validation toast / fixed toast fallback.
        var toasts = _driver.FindElements(By.CssSelector(".border-red-200, .bg-red-50, [role='alert']"));
        if (toasts.Any(t => t.Displayed && !string.IsNullOrWhiteSpace(t.Text)))
        {
            return true;
        }

        return false;
    }

    public string? ErrorText
    {
        get
        {
            if (TryFind(E2ESelectors.AuthError, out var alerts))
            {
                var authError = E2EWait.DomAttr(alerts, "data-auth-error");
                if (!string.IsNullOrWhiteSpace(authError))
                {
                    return authError;
                }

                if (!string.IsNullOrWhiteSpace(alerts.Text))
                {
                    return alerts.Text;
                }
            }

            if (TryFind(E2ESelectors.AlertModalMessage, out var msg) && !string.IsNullOrWhiteSpace(msg.Text))
            {
                return msg.Text;
            }

            return null;
        }
    }

    public bool IsLoggedInHeader() =>
        TryFind(E2ESelectors.UserMenu, out var el) && el.Displayed;

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
