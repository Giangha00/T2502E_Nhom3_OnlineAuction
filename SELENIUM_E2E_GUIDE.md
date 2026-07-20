# Руководство По Selenium E2E Для OnlineAuction

Этот документ объясняет с самых базовых шагов, как человеку без опыта Selenium понять общий поток, установить нужные библиотеки, создать отдельный test project, написать код и запустить automation test для проекта OnlineAuction.

## 1. Что такое Selenium?

Selenium - это набор инструментов для автоматизации браузера. В этом проекте используется Selenium WebDriver: он открывает Chrome, нажимает кнопки, вводит данные в формы, отправляет форму и проверяет результат в реальном UI.

Общий поток работы:

```text
xUnit test
  -> Selenium WebDriver C# library
  -> Selenium Manager
  -> ChromeDriver
  -> Chrome browser
  -> OnlineAuction local website
```

Что делает каждая часть:

- xUnit: test framework, который запускает тесты и показывает pass/fail.
- Selenium WebDriver: C# библиотека для управления браузером.
- Selenium Manager: инструмент, который идет вместе с Selenium и может автоматически найти или скачать browser driver.
- ChromeDriver: мост между Selenium и Chrome.
- OnlineAuction: основное веб-приложение проекта, которое локально работает на `http://localhost:5006`.

Официальная документация:

- First script: https://www.selenium.dev/documentation/webdriver/getting_started/first_script/
- Waits: https://www.selenium.dev/documentation/webdriver/waits/
- Selenium Manager: https://www.selenium.dev/documentation/selenium_manager/

## 2. Что должно быть установлено

Нужно иметь:

- .NET SDK 8.
- Google Chrome или Microsoft Edge.
- Рабочий локальный проект OnlineAuction.
- PowerShell terminal.

Проверить .NET:

```powershell
dotnet --info
```

Проверить текущие тесты проекта:

```powershell
dotnet test Nhom3.sln
```

Если эта команда проходит успешно, базовая test-инфраструктура проекта работает.

## 3. Почему нужен отдельный E2E project?

Сейчас в проекте есть:

```text
OnlineAuction
OnlineAuction.Tests
```

Значение:

- `OnlineAuction`: основное приложение.
- `OnlineAuction.Tests`: unit/integration tests для сервисов, helper-классов и внутренней логики.

Selenium - это E2E test. Ему нужен реальный браузер и запущенное приложение. Поэтому лучше создать отдельный project:

```text
OnlineAuction.E2ETests
```

Итоговая структура:

```text
OnlineAuction            = основное приложение
OnlineAuction.Tests      = unit/integration tests
OnlineAuction.E2ETests   = Selenium E2E tests
```

## 4. Создание Selenium E2E project

Открой terminal в корне repository:

```powershell
dotnet new xunit -n OnlineAuction.E2ETests
dotnet sln Nhom3.sln add OnlineAuction.E2ETests\OnlineAuction.E2ETests.csproj
```

Эти команды делают две вещи:

- Создают новый xUnit project `OnlineAuction.E2ETests`.
- Добавляют его в solution `Nhom3.sln`.

## 5. Установка Selenium libraries

Выполни:

```powershell
dotnet add OnlineAuction.E2ETests\OnlineAuction.E2ETests.csproj package Selenium.WebDriver
dotnet add OnlineAuction.E2ETests\OnlineAuction.E2ETests.csproj package Selenium.Support
```

Назначение библиотек:

- `Selenium.WebDriver`: основная библиотека для открытия браузера, поиска элементов, click, ввода текста.
- `Selenium.Support`: содержит `WebDriverWait`, который нужен для ожидания элементов.

В файле `OnlineAuction.E2ETests.csproj` должны появиться строки:

```xml
<PackageReference Include="Selenium.Support" Version="4.46.0" />
<PackageReference Include="Selenium.WebDriver" Version="4.46.0" />
```

На первом этапе не нужно вручную скачивать `chromedriver.exe`. Selenium Manager обычно сам управляет driver.

## 6. Структура файлов

В папке `OnlineAuction.E2ETests` создай файлы:

```text
OnlineAuction.E2ETests
  AuthLoginTests.cs
  AuthSignupTests.cs
  E2EFactAttribute.cs
  E2ETestSettings.cs
  SeleniumTestBase.cs
```

Если есть стандартный файл:

```text
UnitTest1.cs
```

его можно удалить.

## 7. Файл E2EFactAttribute.cs

Этот файл нужен, чтобы Selenium tests не запускались автоматически при обычном запуске всех тестов solution.

Причина: E2E tests требуют, чтобы приложение уже было запущено на `localhost:5006`. Если приложение не запущено, Selenium test упадет.

Код:

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

Как это понимать:

- Обычно xUnit использует `[Fact]`.
- Здесь создается свой атрибут `[E2EFact]`.
- Если переменная `E2E_RUN=true` не задана, test будет skipped.
- Когда нужно реально запустить Selenium, задается environment variable `E2E_RUN=true`.

## 8. Файл E2ETestSettings.cs

Этот файл хранит настройки Selenium tests.

Код:

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

Что означает каждая настройка:

- `BaseUrl`: URL, где работает OnlineAuction.
- `UserEmail`: email для login test.
- `UserPassword`: password для login test.
- `SignupPassword`: password для создания нового account.
- `Headless`: если `true`, Chrome работает скрыто; если `false`, окно Chrome будет видно.

Seed account, который уже есть в проекте:

```text
Email: user1@auctionhouse.local
Password: User@123
```

## 9. Файл SeleniumTestBase.cs

Это базовый class для всех Selenium tests.

Код:

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

Объяснение:

- `new ChromeOptions()`: настройки Chrome.
- `--window-size=1366,900`: стабильный размер окна браузера.
- `--headless=new`: запуск Chrome без видимого окна.
- `new ChromeDriver(options)`: создание browser session.
- `new WebDriverWait(..., 10 секунд)`: ожидание элемента до 10 секунд.
- `Dispose()`: после теста закрывает browser через `Driver.Quit()`.
- `WaitUntilDisplayed`: ждет, пока element появится и станет visible.
- `ClickDisplayed`: ждет visible element и нажимает на него.

Почему важен wait?

В OnlineAuction есть login/signup modal, JavaScript, redirect и динамический UI. Если Selenium начнет искать или нажимать element слишком рано, test будет нестабильным. Wait делает test надежнее.

## 10. Что такое selector в Selenium?

Selector - это способ найти element на странице.

Пример:

```csharp
By.Id("modalEmail")
```

Ищет HTML element:

```html
id="modalEmail"
```

Пример:

```csharp
By.CssSelector("button[data-auth-tab='login']")
```

Ищет button:

```html
data-auth-tab="login"
```

Selectors, которые используются в OnlineAuction:

```text
Login button: button[data-auth-tab='login']
Sign Up button: button[data-auth-tab='signup']
Login email: #modalEmail
Login password: #modalPassword
Signup full name: #modalFullName
Signup email: #modalSignupEmail
Signup phone: #modalPhone
Signup password: #modalSignupPassword
Signup confirm password: #modalConfirmPassword
Success message: #authModalSuccess
Logout form after login: form[action*='Logout']
```

### Где используются эти selectors?

Эти selectors используются в Selenium test files, то есть в файлах:

```text
OnlineAuction.E2ETests/AuthLoginTests.cs
OnlineAuction.E2ETests/AuthSignupTests.cs
```

Пример из `AuthLoginTests.cs`:

```csharp
ClickDisplayed(By.CssSelector("button[data-auth-tab='login']"));
```

Эта строка говорит Selenium:

```text
Найди кнопку с data-auth-tab='login', дождись, пока она будет видимой, и нажми на нее.
```

Этот selector взят из view file:

```text
OnlineAuction/Views/Shared/_Layout.cshtml
```

Внутри view есть HTML:

```html
<button type="button" data-auth-tab="login">
```

Поэтому test использует:

```csharp
By.CssSelector("button[data-auth-tab='login']")
```

Другой пример из `AuthLoginTests.cs`:

```csharp
var emailInput = WaitUntilDisplayed(By.Id("modalEmail"));
emailInput.SendKeys(Settings.UserEmail);
```

Этот selector взят из:

```text
OnlineAuction/Views/Shared/Partials/_AuthModal.cshtml
```

Внутри view есть:

```html
<input name="email" id="modalEmail" type="email" />
```

Поэтому Selenium использует:

```csharp
By.Id("modalEmail")
```

Коротко:

```text
.cshtml file = место, где описан HTML element
.cs test file = место, где Selenium использует selector, чтобы найти/click/input/assert element
```

### Если нужно написать test для bid, где искать selector?

Для bid flow сначала нужно найти view, который отвечает за auction detail screen. В этом проекте bid elements находятся в основном в:

```text
OnlineAuction/Views/Auction/Partials/_ProductBidPanel.cshtml
OnlineAuction/Views/Auction/Partials/_BidHistorySection.cshtml
```

В `_ProductBidPanel.cshtml` есть важные elements:

```html
<input id="bidAmount" name="bidAmount" />
<button id="placeBidBtn" type="button">
<div id="bidFeedback"></div>
<p id="currentPriceDisplay"></p>
```

В `_BidHistorySection.cshtml` есть:

```html
<tbody id="bidHistoryBody">
```

Значит в Selenium bid test можно использовать:

```csharp
By.Id("bidAmount")
By.Id("placeBidBtn")
By.Id("bidFeedback")
By.Id("currentPriceDisplay")
By.Id("bidHistoryBody")
```

Пример базового bid test:

```csharp
var bidInput = WaitUntilDisplayed(By.Id("bidAmount"));
bidInput.Clear();
bidInput.SendKeys("150");

Driver.FindElement(By.Id("placeBidBtn")).Click();

Wait.Until(driver =>
    driver.FindElement(By.Id("bidFeedback")).Text.Length > 0);
```

Пример проверки, что current price изменился после bid:

```csharp
var oldPrice = Driver.FindElement(By.Id("currentPriceDisplay")).Text;

var bidInput = WaitUntilDisplayed(By.Id("bidAmount"));
bidInput.Clear();
bidInput.SendKeys("200");

Driver.FindElement(By.Id("placeBidBtn")).Click();

Wait.Until(driver =>
    driver.FindElement(By.Id("currentPriceDisplay")).Text != oldPrice);
```

### Как самостоятельно найти selector для нового flow

Шаги:

1. Запусти OnlineAuction local.
2. Открой нужную страницу в Chrome.
3. Нажми `F12`, чтобы открыть DevTools.
4. Нажми icon выбора element в DevTools.
5. Кликни по button/input/message, который нужно автоматизировать.
6. Посмотри HTML этого element.
7. Выбирай selector в таком порядке:

```text
data-testid
id
name
short CSS selector
short XPath, only if there is no better option
```

Например, если HTML такой:

```html
<button id="placeBidBtn" type="button">
```

то используй:

```csharp
By.Id("placeBidBtn")
```

Если HTML такой:

```html
<button data-testid="place-bid-button">
```

то используй:

```csharp
By.CssSelector("[data-testid='place-bid-button']")
```

В дальнейшем лучше добавить `data-testid` для важных elements:

```html
<input id="bidAmount" data-testid="bid-amount" />
<button id="placeBidBtn" data-testid="place-bid-button">
<div id="bidFeedback" data-testid="bid-feedback"></div>
<p id="currentPriceDisplay" data-testid="current-price"></p>
```

Тогда Selenium test будет понятнее:

```csharp
By.CssSelector("[data-testid='bid-amount']")
By.CssSelector("[data-testid='place-bid-button']")
By.CssSelector("[data-testid='bid-feedback']")
By.CssSelector("[data-testid='current-price']")
```

## 11. Login test: AuthLoginTests.cs

Код:

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

Поток login test:

```text
Открыть home page
-> нажать Login
-> дождаться email input
-> ввести email
-> ввести password
-> нажать submit
-> дождаться появления Logout form в DOM
```

Почему проверяется logout form?

После успешного login layout приложения рендерит logout form. Этот form может находиться внутри закрытого dropdown, поэтому не обязательно проверять `Displayed`; достаточно проверить, что он появился в DOM.

## 12. Signup test: AuthSignupTests.cs

Код:

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

Поток signup test:

```text
Открыть home page
-> нажать Sign Up
-> ввести full name
-> ввести новый email
-> ввести phone из 11 цифр
-> ввести password
-> ввести confirm password
-> нажать submit
-> дождаться success message
-> проверить, что modal вернулся на Login tab
```

Почему email должен быть random?

Если использовать один и тот же email, первый запуск может пройти, а второй получит ошибку "Email already exists". Поэтому test каждый раз генерирует новый email:

```csharp
var uniqueSuffix = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
var email = $"selenium-{uniqueSuffix}@auctionhouse.local";
```

## 13. Запуск OnlineAuction

Открой Terminal 1 в корне repo:

```powershell
dotnet run --project OnlineAuction --launch-profile http
```

Если команда выше дает ошибку, используй путь к `.csproj`:

```powershell
dotnet run --project OnlineAuction\OnlineAuction.csproj --launch-profile http
```

Когда app запущен правильно, он будет listening на:

```text
http://localhost:5006
```

Terminal 1 должен оставаться открытым.

## 14. Запуск Selenium login

Открой Terminal 2 в корне repo:

```powershell
$env:E2E_RUN="true"
$env:E2E_BASE_URL="http://localhost:5006"
$env:E2E_HEADLESS="false"
dotnet test OnlineAuction.E2ETests\OnlineAuction.E2ETests.csproj --filter "FullyQualifiedName~AuthLoginTests"
```

Если test прошел, результат будет примерно такой:

```text
Passed!  - Failed: 0, Passed: 1, Skipped: 0, Total: 1
```

## 15. Запуск Selenium signup

Signup зависит от email confirmation. В local environment лучше включить smoke mode, чтобы приложение приняло signup flow без реальной отправки email.

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

Если test прошел:

```text
Passed!  - Failed: 0, Passed: 1, Skipped: 0, Total: 1
```

## 16. Запуск login и signup вместе

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

Правильный результат:

```text
Passed!  - Failed: 0, Passed: 2, Skipped: 0, Total: 2
```

## 17. Запуск без видимого browser window

Если не хочешь видеть окно Chrome:

```powershell
$env:E2E_HEADLESS="true"
```

После этого запускай test как обычно.

## 18. Запуск всех tests solution

```powershell
dotnet test Nhom3.sln
```

Если `E2E_RUN=true` не задан, Selenium tests будут skipped:

```text
OnlineAuction.Tests: pass
OnlineAuction.E2ETests: skipped
```

Это правильное поведение, потому что E2E tests требуют отдельно запущенное приложение.

## 19. Частые ошибки

### Ошибка: Required argument missing for option: '--launch-profile'

Причина: не указан profile name.

Неправильно:

```powershell
dotnet run --project OnlineAuction --launch-profile
```

Правильно:

```powershell
dotnet run --project OnlineAuction --launch-profile http
```

### Ошибка: connection refused или localhost не открывается

Причина: app не запущен, указан неправильный port или terminal с app был закрыт.

Проверь:

```text
http://localhost:5006
```

Если browser не открывает этот URL, Selenium тоже не сможет тестировать app.

### Ошибка: test skipped

Причина: не задано `E2E_RUN=true`.

Выполни:

```powershell
$env:E2E_RUN="true"
```

### Ошибка: signup не показывает success

Частая причина: не включен smoke mode, и app пытается выполнить реальную email confirmation flow.

Запусти app так:

```powershell
$env:SmokeTesting__Enabled="true"
dotnet run --project OnlineAuction --launch-profile http
```

### Ошибка: element not found

Примеры:

```text
NoSuchElementException
WebDriverTimeoutException
```

Возможные причины:

- Selector неправильный.
- Modal не открылся.
- Element еще не успел render.
- App показывает другой UI, например user уже logged in или открыт другой URL.

Как исправлять:

- Использовать `WaitUntilDisplayed`.
- Проверить id/class в `.cshtml`.
- Запустить с `E2E_HEADLESS=false`, чтобы увидеть, где остановился browser.

## 20. Правила написания Selenium tests для OnlineAuction

Хорошая практика:

- Каждый test должен быть независимым.
- Для signup использовать random email.
- Использовать explicit wait вместо `Thread.Sleep`.
- Предпочитать стабильные selectors: `id`, `data-testid`.
- Хранить E2E tests в отдельном project.
- Закрывать browser через `Driver.Quit()`.

Плохая практика:

- Не hard-code UI text для assert, если app поддерживает несколько языков.
- Не использовать длинный XPath, который зависит от layout.
- Не запускать Selenium tests по умолчанию внутри `dotnet test Nhom3.sln`, если app не стартует автоматически.
- Не тестировать реальный PayPal в первом spike.

## 21. Что улучшить дальше

После того как login/signup работают стабильно, стоит добавить `data-testid` в view:

```html
<button data-testid="auth-login-trigger">
<input data-testid="login-email">
<input data-testid="login-password">
<button data-testid="login-submit">
```

Тогда selector в Selenium будет понятнее:

```csharp
By.CssSelector("[data-testid='login-email']")
```

Следующие flows, которые можно добавить:

- Login с неправильным password.
- Signup с уже существующим email.
- Auction registration.
- Place bid.
- Проверка bid feedback и current price.
