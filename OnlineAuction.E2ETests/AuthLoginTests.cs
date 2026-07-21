using OpenQA.Selenium;

namespace OnlineAuction.E2ETests;

// Test nay kiem tra luong dang nhap tren UI that cua OnlineAuction.
// Browser se mo home page, thao tac voi modal login, roi xac nhan user da dang nhap.
public sealed class AuthLoginTests : SeleniumTestBase
{
    [E2EFact]
    public void User_Can_Login_From_Home_Page_Modal()
    {
        // Mo trang chinh cua app. URL lay tu E2E_BASE_URL, mac dinh la http://localhost:5006.
        Driver.Navigate().GoToUrl(Settings.BaseUrl);

        // Tim nut Login trong layout bang data-auth-tab va click de mo modal dang nhap.
        ClickDisplayed(By.CssSelector("button[data-auth-tab='login']"));

        // Doi input email hien thi trong modal, sau do nhap email cua user seed.
        var emailInput = WaitUntilDisplayed(By.Id("modalEmail"));
        emailInput.Clear();
        emailInput.SendKeys(Settings.UserEmail);

        // Nhap password tu cau hinh test.
        var passwordInput = Driver.FindElement(By.Id("modalPassword"));
        passwordInput.Clear();
        passwordInput.SendKeys(Settings.UserPassword);

        // Submit form login trong panel dang nhap.
        Driver.FindElement(By.CssSelector("#authPanelLogin button[type='submit']")).Click();

        // Sau khi login thanh cong, layout se render form Logout.
        // Form co the nam trong dropdown nen chi can kiem tra ton tai trong DOM.
        Wait.Until(driver =>
            driver.FindElements(By.CssSelector("form[action*='Logout']")).Any());
    }
}
