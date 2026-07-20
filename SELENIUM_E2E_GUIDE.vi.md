# Huong Dan Selenium E2E Cho OnlineAuction

Tai lieu nay huong dan tu muc co ban nhat de mot nguoi chua biet Selenium co the hieu luong, cai thu vien, tao project test rieng, viet code va chay automation test cho OnlineAuction.

## 1. Selenium la gi?

Selenium la bo cong cu dung de tu dong hoa trinh duyet. Trong du an nay minh dung Selenium WebDriver de mo Chrome, click nut, nhap form, submit va kiem tra ket qua tren UI that.

Luong hoat dong:

```text
xUnit test
  -> Selenium WebDriver C# library
  -> Selenium Manager
  -> ChromeDriver
  -> Chrome browser
  -> OnlineAuction local website
```

Giai thich tung phan:

- xUnit: framework chay test va bao pass/fail.
- Selenium WebDriver: thu vien C# giup code dieu khien browser.
- Selenium Manager: cong cu di kem Selenium tu dong tim/tai driver neu may chua co.
- ChromeDriver: cau noi de Selenium dieu khien Chrome.
- OnlineAuction: web app chinh cua du an, chay o `http://localhost:5006`.

## 2. Can cai gi?

Can co:

- .NET SDK 8.
- Google Chrome hoac Microsoft Edge.
- Project OnlineAuction chay duoc local.
- PowerShell terminal.

Kiem tra .NET:

```powershell
dotnet --info
```

Kiem tra test hien tai:

```powershell
dotnet test Nhom3.sln
```

## 3. Tao project E2E rieng

Trong thu muc goc repo, chay:

```powershell
dotnet new xunit -n OnlineAuction.E2ETests
dotnet sln Nhom3.sln add OnlineAuction.E2ETests\OnlineAuction.E2ETests.csproj
```

Sau do solution co:

```text
OnlineAuction            = app chinh
OnlineAuction.Tests      = unit/integration test
OnlineAuction.E2ETests   = Selenium E2E test
```

## 4. Cai thu vien Selenium

Chay:

```powershell
dotnet add OnlineAuction.E2ETests\OnlineAuction.E2ETests.csproj package Selenium.WebDriver
dotnet add OnlineAuction.E2ETests\OnlineAuction.E2ETests.csproj package Selenium.Support
```

Y nghia:

- `Selenium.WebDriver`: mo browser, tim element, click, nhap text.
- `Selenium.Support`: co `WebDriverWait` de doi element xuat hien.

Khong can tai `chromedriver.exe` thu cong trong buoc dau. Selenium Manager thuong se tu xu ly.

## 5. Tao cac file can thiet

Trong `OnlineAuction.E2ETests`, tao:

```text
AuthLoginTests.cs
AuthSignupTests.cs
E2EFactAttribute.cs
E2ETestSettings.cs
SeleniumTestBase.cs
```

Xoa file mac dinh neu co:

```text
UnitTest1.cs
```

## 6. E2EFactAttribute.cs

File nay giup Selenium test khong chay mac dinh khi chay `dotnet test Nhom3.sln`.

```csharp
using Xunit;

namespace OnlineAuction.E2ETests;

public sealed class E2EFactAttribute : FactAttribute
{
    public E2EFactAttribute()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("E2E_RUN"), "true", StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Set E2E_RUN=true and start OnlineAuction before running Selenium E2E tests.";
        }
    }
}
```

Neu chua set `E2E_RUN=true`, test se bi skip. Khi muon chay Selenium that, set bien nay trong terminal.

## 7. E2ETestSettings.cs

File nay gom cau hinh cho Selenium test:

```csharp
namespace OnlineAuction.E2ETests;

public sealed class E2ETestSettings
{
    public string BaseUrl { get; } =
        Environment.GetEnvironmentVariable("E2E_BASE_URL")?.TrimEnd('/') ?? "http://localhost:5006";

    public string UserEmail { get; } =
        Environment.GetEnvironmentVariable("E2E_USER_EMAIL") ?? "user1@auctionhouse.local";

    public string UserPassword { get; } =
        Environment.GetEnvironmentVariable("E2E_USER_PASSWORD") ?? "User@123";

    public string SignupPassword { get; } =
        Environment.GetEnvironmentVariable("E2E_SIGNUP_PASSWORD") ?? "User@123";

    public bool Headless { get; } =
        string.Equals(Environment.GetEnvironmentVariable("E2E_HEADLESS"), "true", StringComparison.OrdinalIgnoreCase);
}
```

Tai khoan seed san:

```text
Email: user1@auctionhouse.local
Password: User@123
```

## 8. SeleniumTestBase.cs

Day la lop nen dung chung cho cac Selenium test:

```csharp
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace OnlineAuction.E2ETests;

public abstract class SeleniumTestBase : IDisposable
{
    protected SeleniumTestBase()
    {
        Settings = new E2ETestSettings();

        var options = new ChromeOptions();
        options.AddArgument("--window-size=1366,900");

        if (Settings.Headless)
        {
            options.AddArgument("--headless=new");
        }

        Driver = new ChromeDriver(options);
        Wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
    }

    protected E2ETestSettings Settings { get; }

    protected IWebDriver Driver { get; }

    protected WebDriverWait Wait { get; }

    public void Dispose()
    {
        Driver.Quit();
        Driver.Dispose();
    }

    protected IWebElement WaitUntilDisplayed(By locator)
    {
        return Wait.Until(driver =>
        {
            var element = driver.FindElements(locator).FirstOrDefault(candidate => candidate.Displayed);
            return element;
        });
    }

    protected void ClickDisplayed(By locator)
    {
        WaitUntilDisplayed(locator).Click();
    }
}
```

Y nghia:

- `ChromeDriver`: mo va dieu khien Chrome.
- `WebDriverWait`: doi element toi da 10 giay.
- `WaitUntilDisplayed`: doi element hien thi.
- `ClickDisplayed`: doi element hien thi roi click.
- `Dispose`: tat browser sau khi test xong.

## 9. Selector trong du an

Selenium can selector de tim element tren HTML.

Vi du:

```csharp
By.Id("modalEmail")
```

Tim element co:

```html
id="modalEmail"
```

Selector dang dung trong OnlineAuction:

```text
Nut Login: button[data-auth-tab='login']
Nut Sign Up: button[data-auth-tab='signup']
Email login: #modalEmail
Password login: #modalPassword
Full name signup: #modalFullName
Email signup: #modalSignupEmail
Phone signup: #modalPhone
Password signup: #modalSignupPassword
Confirm password signup: #modalConfirmPassword
Thong bao success: #authModalSuccess
Form logout sau login: form[action*='Logout']
```

### Selector nay dung o dau?

Nhung selector tren duoc dung trong file Selenium test, tuc la trong cac file:

```text
OnlineAuction.E2ETests/AuthLoginTests.cs
OnlineAuction.E2ETests/AuthSignupTests.cs
```

Vi du trong `AuthLoginTests.cs`:

```csharp
ClickDisplayed(By.CssSelector("button[data-auth-tab='login']"));
```

Dong nay bao Selenium:

```text
Hay tim nut co data-auth-tab='login', doi no hien thi, roi click.
```

Selector nay lay tu file view:

```text
OnlineAuction/Views/Shared/_Layout.cshtml
```

Trong view co HTML dang:

```html
<button type="button" data-auth-tab="login">
```

Vi vay test dung:

```csharp
By.CssSelector("button[data-auth-tab='login']")
```

Vi du khac trong `AuthLoginTests.cs`:

```csharp
var emailInput = WaitUntilDisplayed(By.Id("modalEmail"));
emailInput.SendKeys(Settings.UserEmail);
```

Selector nay lay tu:

```text
OnlineAuction/Views/Shared/Partials/_AuthModal.cshtml
```

Trong view co:

```html
<input name="email" id="modalEmail" type="email" />
```

Nen Selenium dung:

```csharp
By.Id("modalEmail")
```

Tom lai:

```text
.cshtml file = noi dinh nghia HTML element
.cs test file = noi Selenium dung selector de tim/click/nhap/kiem tra element
```

### Neu lam test ve bid thi tim selector o dau?

Voi luong bid, truoc tien tim view lien quan den man hinh auction detail. Trong du an nay, cac element bid nam chu yeu o:

```text
OnlineAuction/Views/Auction/Partials/_ProductBidPanel.cshtml
OnlineAuction/Views/Auction/Partials/_BidHistorySection.cshtml
```

Trong `_ProductBidPanel.cshtml` co cac element quan trong:

```html
<input id="bidAmount" name="bidAmount" />
<button id="placeBidBtn" type="button">
<div id="bidFeedback"></div>
<p id="currentPriceDisplay"></p>
```

Trong `_BidHistorySection.cshtml` co:

```html
<tbody id="bidHistoryBody">
```

Vi vay khi viet Selenium test cho bid, co the dung:

```csharp
By.Id("bidAmount")
By.Id("placeBidBtn")
By.Id("bidFeedback")
By.Id("currentPriceDisplay")
By.Id("bidHistoryBody")
```

Vi du code test bid co ban:

```csharp
var bidInput = WaitUntilDisplayed(By.Id("bidAmount"));
bidInput.Clear();
bidInput.SendKeys("150");

Driver.FindElement(By.Id("placeBidBtn")).Click();

Wait.Until(driver =>
    driver.FindElement(By.Id("bidFeedback")).Text.Length > 0);
```

Vi du kiem tra gia hien tai thay doi sau khi bid:

```csharp
var oldPrice = Driver.FindElement(By.Id("currentPriceDisplay")).Text;

var bidInput = WaitUntilDisplayed(By.Id("bidAmount"));
bidInput.Clear();
bidInput.SendKeys("200");

Driver.FindElement(By.Id("placeBidBtn")).Click();

Wait.Until(driver =>
    driver.FindElement(By.Id("currentPriceDisplay")).Text != oldPrice);
```

### Cach tu tim selector cho luong moi

Lam theo cac buoc:

1. Chay app OnlineAuction local.
2. Mo trang can test tren Chrome.
3. Bam `F12` de mo DevTools.
4. Bam icon chon element trong DevTools.
5. Click vao nut/input/message can automate.
6. Xem HTML cua element do.
7. Uu tien chon selector theo thu tu:

```text
data-testid
id
name
CSS selector ngan
XPath ngan, chi dung khi khong con cach khac
```

Vi du thay HTML:

```html
<button id="placeBidBtn" type="button">
```

Thi dung:

```csharp
By.Id("placeBidBtn")
```

Vi du thay HTML:

```html
<button data-testid="place-bid-button">
```

Thi dung:

```csharp
By.CssSelector("[data-testid='place-bid-button']")
```

Khuyen nghi sau nay nen them `data-testid` vao cac element quan trong:

```html
<input id="bidAmount" data-testid="bid-amount" />
<button id="placeBidBtn" data-testid="place-bid-button">
<div id="bidFeedback" data-testid="bid-feedback"></div>
<p id="currentPriceDisplay" data-testid="current-price"></p>
```

Khi do Selenium test ro y nghia hon:

```csharp
By.CssSelector("[data-testid='bid-amount']")
By.CssSelector("[data-testid='place-bid-button']")
By.CssSelector("[data-testid='bid-feedback']")
By.CssSelector("[data-testid='current-price']")
```

## 10. AuthLoginTests.cs

```csharp
using OpenQA.Selenium;

namespace OnlineAuction.E2ETests;

public sealed class AuthLoginTests : SeleniumTestBase
{
    [E2EFact]
    public void User_Can_Login_From_Home_Page_Modal()
    {
        Driver.Navigate().GoToUrl(Settings.BaseUrl);

        ClickDisplayed(By.CssSelector("button[data-auth-tab='login']"));

        var emailInput = WaitUntilDisplayed(By.Id("modalEmail"));
        emailInput.Clear();
        emailInput.SendKeys(Settings.UserEmail);

        var passwordInput = Driver.FindElement(By.Id("modalPassword"));
        passwordInput.Clear();
        passwordInput.SendKeys(Settings.UserPassword);

        Driver.FindElement(By.CssSelector("#authPanelLogin button[type='submit']")).Click();

        Wait.Until(driver =>
            driver.FindElements(By.CssSelector("form[action*='Logout']")).Any());
    }
}
```

Luong login:

```text
Mo home page
-> click Login
-> doi input email hien thi
-> nhap email
-> nhap password
-> submit
-> kiem tra form Logout da co trong DOM
```

## 11. AuthSignupTests.cs

```csharp
using OpenQA.Selenium;

namespace OnlineAuction.E2ETests;

public sealed class AuthSignupTests : SeleniumTestBase
{
    [E2EFact]
    public void User_Can_Sign_Up_From_Home_Page_Modal()
    {
        var uniqueSuffix = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var email = $"selenium-{uniqueSuffix}@auctionhouse.local";

        Driver.Navigate().GoToUrl(Settings.BaseUrl);

        ClickDisplayed(By.CssSelector("button[data-auth-tab='signup']"));

        WaitUntilDisplayed(By.Id("modalFullName")).SendKeys("Selenium Test User");
        Driver.FindElement(By.Id("modalSignupEmail")).SendKeys(email);
        Driver.FindElement(By.Id("modalPhone")).SendKeys("09012345678");
        Driver.FindElement(By.Id("modalSignupPassword")).SendKeys(Settings.SignupPassword);
        Driver.FindElement(By.Id("modalConfirmPassword")).SendKeys(Settings.SignupPassword);

        Driver.FindElement(By.CssSelector("#authPanelSignup button[type='submit']")).Click();

        Wait.Until(driver =>
            driver.FindElements(By.Id("authModalSuccess")).Any()
            && driver.FindElements(By.Id("authPanelLogin")).Any(element => element.Displayed));
    }
}
```

Luong dang ky:

```text
Mo home page
-> click Sign Up
-> nhap full name
-> nhap email random
-> nhap phone 11 so
-> nhap password
-> nhap confirm password
-> submit
-> kiem tra co thong bao success
-> kiem tra modal quay ve tab Login
```

Email phai random de tranh loi `Email already exists`.

## 12. Chay app chinh

Mo Terminal 1:

```powershell
dotnet run --project OnlineAuction --launch-profile http
```

Neu loi, dung:

```powershell
dotnet run --project OnlineAuction\OnlineAuction.csproj --launch-profile http
```

App chay tai:

```text
http://localhost:5006
```

## 13. Chay test login

Mo Terminal 2:

```powershell
$env:E2E_RUN="true"
$env:E2E_BASE_URL="http://localhost:5006"
$env:E2E_HEADLESS="false"
dotnet test OnlineAuction.E2ETests\OnlineAuction.E2ETests.csproj --filter "FullyQualifiedName~AuthLoginTests"
```

Ket qua dung:

```text
Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1
```

## 14. Chay test dang ky

Dang ky can smoke mode de khong phu thuoc email that.

Terminal 1:

```powershell
$env:SmokeTesting__Enabled="true"
dotnet run --project OnlineAuction --launch-profile http
```

Terminal 2:

```powershell
$env:E2E_RUN="true"
$env:E2E_BASE_URL="http://localhost:5006"
$env:E2E_HEADLESS="false"
dotnet test OnlineAuction.E2ETests\OnlineAuction.E2ETests.csproj --filter "FullyQualifiedName~AuthSignupTests"
```

## 15. Chay ca login va signup

Terminal 1:

```powershell
$env:SmokeTesting__Enabled="true"
dotnet run --project OnlineAuction --launch-profile http
```

Terminal 2:

```powershell
$env:E2E_RUN="true"
$env:E2E_BASE_URL="http://localhost:5006"
$env:E2E_HEADLESS="false"
dotnet test OnlineAuction.E2ETests\OnlineAuction.E2ETests.csproj
```

Ket qua dung:

```text
Passed! - Failed: 0, Passed: 2, Skipped: 0, Total: 2
```

## 16. Chay an Chrome

Dung:

```powershell
$env:E2E_HEADLESS="true"
```

Sau do chay test nhu binh thuong.

## 17. Cac loi thuong gap

### Thieu launch profile

Sai:

```powershell
dotnet run --project OnlineAuction --launch-profile
```

Dung:

```powershell
dotnet run --project OnlineAuction --launch-profile http
```

### Test bi skipped

Can set:

```powershell
$env:E2E_RUN="true"
```

### Khong vao duoc localhost

Kiem tra app co dang chay tai:

```text
http://localhost:5006
```

Neu URL nay khong mo duoc thi Selenium cung khong chay duoc.

### Signup khong success

Can chay app voi:

```powershell
$env:SmokeTesting__Enabled="true"
dotnet run --project OnlineAuction --launch-profile http
```

### Khong tim thay element

Nguyen nhan:

- Modal chua mo.
- Selector sai.
- Element chua render kip.
- Dang o UI khac.

Cach xu ly:

- Chay `E2E_HEADLESS=false` de nhin browser.
- Kiem tra lai id trong `.cshtml`.
- Dung `WaitUntilDisplayed`.

## 18. Huong phat trien tiep

Nen them `data-testid` vao view:

```html
<button data-testid="auth-login-trigger">
<input data-testid="login-email">
<input data-testid="login-password">
<button data-testid="login-submit">
```

Sau do Selenium selector se ro hon:

```csharp
By.CssSelector("[data-testid='login-email']")
```

Cac luong co the them tiep:

- Login sai password.
- Signup email da ton tai.
- Dang ky auction.
- Dat gia bid.
- Kiem tra current price sau khi bid.
