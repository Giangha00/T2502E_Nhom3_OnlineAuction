using OpenQA.Selenium;

namespace OnlineAuction.E2ETests;

// Test nay kiem tra luong dang ky tai khoan moi tren UI that cua OnlineAuction.
// Email duoc sinh moi moi lan chay de tranh loi trung email trong database.
public sealed class AuthSignupTests : SeleniumTestBase
{
    [E2EFact]
    public void User_Can_Sign_Up_From_Home_Page_Modal()
    {
        // Tao email duy nhat theo timestamp de test co the chay lai nhieu lan.
        var uniqueSuffix = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var email = $"selenium-{uniqueSuffix}@auctionhouse.local";

        // Mo home page cua app dang chay local.
        Driver.Navigate().GoToUrl(Settings.BaseUrl);

        // Click nut Sign Up trong layout de mo modal o tab dang ky.
        ClickDisplayed(By.CssSelector("button[data-auth-tab='signup']"));

        // Nhap cac field bat buoc cua form dang ky.
        WaitUntilDisplayed(By.Id("modalFullName")).SendKeys("Selenium Test User");
        Driver.FindElement(By.Id("modalSignupEmail")).SendKeys(email);
        Driver.FindElement(By.Id("modalPhone")).SendKeys("09012345678");
        Driver.FindElement(By.Id("modalSignupPassword")).SendKeys(Settings.SignupPassword);
        Driver.FindElement(By.Id("modalConfirmPassword")).SendKeys(Settings.SignupPassword);

        // Submit form signup trong panel dang ky.
        Driver.FindElement(By.CssSelector("#authPanelSignup button[type='submit']")).Click();

        // Signup thanh cong se hien alert success va dua modal ve panel Login.
        // Khi chay local nen bat SmokeTesting__Enabled=true de khong phu thuoc email that.
        Wait.Until(driver =>
            driver.FindElements(By.Id("authModalSuccess")).Any()
            && driver.FindElements(By.Id("authPanelLogin")).Any(element => element.Displayed));
    }
}
