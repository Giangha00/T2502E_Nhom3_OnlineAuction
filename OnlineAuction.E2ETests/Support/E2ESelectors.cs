using OpenQA.Selenium;

namespace OnlineAuction.E2ETests.Support;

/// <summary>
/// Central CSS/Id selectors for Selenium E2E (prefer data-e2e hooks, keep id fallbacks).
/// </summary>
public static class E2ESelectors
{
    // Auth modal / header
    public static readonly By AuthModal = By.CssSelector("[data-e2e='auth-modal'], #authModalOverlay");
    public static readonly By AuthOpenLogin = By.CssSelector("[data-e2e='auth-open-login'], button[data-auth-tab='login']");
    public static readonly By AuthOpenSignup = By.CssSelector("[data-e2e='auth-open-signup'], button[data-auth-tab='signup']");
    public static readonly By AuthPanelLogin = By.Id("authPanelLogin");
    public static readonly By AuthPanelSignup = By.Id("authPanelSignup");
    public static readonly By AuthTabLogin = By.CssSelector("#authTabLogin, button[data-tab='login']");
    public static readonly By AuthTabSignup = By.CssSelector("#authTabSignup, button[data-tab='signup']");
    public static readonly By ModalEmail = By.Id("modalEmail");
    public static readonly By ModalPassword = By.Id("modalPassword");
    public static readonly By ModalFullName = By.Id("modalFullName");
    public static readonly By ModalSignupEmail = By.Id("modalSignupEmail");
    public static readonly By ModalPhone = By.Id("modalPhone");
    public static readonly By ModalSignupPassword = By.Id("modalSignupPassword");
    public static readonly By ModalConfirmPassword = By.Id("modalConfirmPassword");
    public static readonly By AuthLoginSubmit = By.CssSelector("[data-e2e='auth-login-submit'], #authPanelLogin button[type='submit']");
    public static readonly By AuthSignupSubmit = By.CssSelector("[data-e2e='auth-signup-submit'], #authPanelSignup button[type='submit']");
    public static readonly By AuthError = By.CssSelector("[data-e2e='auth-error'], #authModalAlerts");
    public static readonly By AlertModal = By.CssSelector("#confirmModalOverlay:not([hidden]), #confirmModalDialog");
    public static readonly By AlertModalMessage = By.Id("confirmModalMessage");

    // User menu / logout
    public static readonly By UserMenu = By.CssSelector("[data-e2e='user-menu'], #userMenuBtn");
    public static readonly By LogoutSubmit = By.CssSelector("[data-e2e='logout-submit'], form[data-e2e='logout-form'] button[type='submit'], form[action*='Logout'] button[type='submit']");

    // Admin login
    public static readonly By AdminLoginRoot = By.CssSelector("[data-e2e='admin-login'], [data-e2e-page='admin-login']");
    public static readonly By AdminEmail = By.CssSelector("[data-e2e='admin-email'], #Email");
    public static readonly By AdminPassword = By.CssSelector("[data-e2e='admin-password'], #Password");
    public static readonly By AdminLoginSubmit = By.CssSelector("[data-e2e='admin-login-submit'], form[data-e2e='admin-login-form'] button[type='submit'], form button[type='submit']");
    public static readonly By AdminValidationErrors = By.CssSelector(".validation-summary-errors, .text-red-600, .text-red-700");

    // Catalog / bid
    public static readonly By AuctionCard = By.CssSelector("[data-e2e='auction-card'][data-id], [data-e2e='auction-card'], [data-id]");
    public static readonly By BidPanel = By.CssSelector("[data-e2e='bid-panel'], .product-bid-panel");
    public static readonly By BidAmount = By.CssSelector("[data-e2e='bid-amount'], #bidAmount");
    public static readonly By PlaceBid = By.CssSelector("[data-e2e='place-bid'], #placeBidBtn");
    public static readonly By BidCountLabel = By.Id("bidCountLabel");
    public static readonly By RegisterBtn = By.CssSelector("[data-e2e='register-btn'], #registerAuctionBtn, [data-can-register='true']");
    public static readonly By WatchlistToggle = By.CssSelector("[data-e2e='watchlist-toggle'], [data-watchlist-toggle]");

    // Static / misc
    public static readonly By SiteMain = By.CssSelector("[data-e2e='site-main'], main");
    public static readonly By ContactPage = By.CssSelector("[data-e2e-page='contact'], .contact-page");
    public static readonly By LanguageSwitcher = By.CssSelector("[data-e2e='language-switcher'], #languageSwitcher");
    public static readonly By LanguageForm = By.CssSelector("[data-e2e='language-form'], form[action*='Language']");

    public static By Page(string pageKey) => By.CssSelector($"[data-e2e-page='{pageKey}']");
}
