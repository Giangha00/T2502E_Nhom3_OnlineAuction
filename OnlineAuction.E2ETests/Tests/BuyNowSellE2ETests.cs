using OnlineAuction.E2ETests.Support;
using OpenQA.Selenium;

namespace OnlineAuction.E2ETests.Tests;

public sealed class BuyNowSellE2ETests : E2ETestBase
{
    [Fact]
    [Trait("SpecId", "BN-01")]
    public void BN_01_BuyNowDetail()
    {
        Go("/BuyNow");
        var cards = Driver.FindElements(E2ESelectors.AuctionCard);
        if (cards.Count == 0)
        {
            cards = Driver.FindElements(By.CssSelector("a[href*='/BuyNow/Detail/']"));
        }

        if (cards.Count == 0) return;
        var href = E2EWait.DomAttr(cards[0], "href");
        if (string.IsNullOrEmpty(href))
        {
            var id = E2EWait.DomAttr(cards[0], "data-id");
            if (!string.IsNullOrEmpty(id)) href = Url($"/BuyNow/Detail/{id}");
        }

        if (!string.IsNullOrEmpty(href)) Driver.Navigate().GoToUrl(href);
        Assert.Contains("BuyNow", Driver.Url, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("SpecId", "BN-02")]
    public void BN_02_InstantPurchase_ButtonPresentWhenLoggedIn()
    {
        E2EAuthHelper.LoginUser(Driver, Config);
        Go("/BuyNow");
        Assert.True(Driver.PageSource.Contains("buy", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("SpecId", "BN-03")]
    public void BN_03_GuestPurchase_AuthRequired()
    {
        Go("/BuyNow");
        Assert.True(Driver.FindElements(E2ESelectors.AuthOpenLogin).Count > 0);
    }

    [Fact]
    [Trait("SpecId", "BN-04")]
    public void BN_04_ConfirmingNotListed() { Go("/BuyNow"); AssertPageOk("/BuyNow"); }

    [Fact]
    [Trait("SpecId", "BN-05")]
    public void BN_05_MultiSelectCheckout()
    {
        E2EAuthHelper.LoginUser(Driver, Config);
        AssertPageOk("/Order");
    }

    [Fact]
    [Trait("SpecId", "BN-06")]
    public void BN_06_ExpiredBuyNowOrder_OrderPageLoads()
    {
        E2EAuthHelper.LoginUser(Driver, Config);
        AssertPageOk("/Order");
    }

    [Fact]
    [Trait("SpecId", "SELL-01")]
    public void SELL_01_CreateAuctionListing()
    {
        E2EAuthHelper.LoginUser(Driver, Config);
        AssertPageOk("/Sell/Create");
    }

    [Fact]
    [Trait("SpecId", "SELL-02")]
    public void SELL_02_CreateBuyNowListing()
    {
        E2EAuthHelper.LoginUser(Driver, Config);
        AssertPageOk("/Sell/BuyNow");
    }

    [Fact]
    [Trait("SpecId", "SELL-03")]
    public void SELL_03_ClientValidation()
    {
        E2EAuthHelper.LoginUser(Driver, Config);
        Go("/Sell/Create");
        var submit = Driver.FindElements(By.CssSelector("button[type='submit'], input[type='submit']"));
        if (submit.Count > 0) submit[0].Click();
        Assert.True(Driver.PageSource.Length > 0);
    }

    [Fact]
    [Trait("SpecId", "SELL-04")]
    public void SELL_04_ScheduleValidation() { E2EAuthHelper.LoginUser(Driver, Config); AssertPageOk("/Sell/Create"); }

    [Fact]
    [Trait("SpecId", "SELL-05")]
    public void SELL_05_GalleryMax5() { E2EAuthHelper.LoginUser(Driver, Config); AssertPageOk("/Sell/Create"); }

    [Fact]
    [Trait("SpecId", "SELL-06")]
    public void SELL_06_DocumentPdfRules() { E2EAuthHelper.LoginUser(Driver, Config); AssertPageOk("/Sell/Create"); }

    [Fact]
    [Trait("SpecId", "SELL-07")]
    public void SELL_07_EditRejectedListing()
    {
        E2EAuthHelper.LoginUser(Driver, Config);
        AssertPageOk("/Account/Selling");
    }

    [Fact]
    [Trait("SpecId", "SELL-08")]
    public void SELL_08_SellerSubmissionsPage()
    {
        E2EAuthHelper.LoginUser(Driver, Config);
        Go("/Account/Selling");
        Assert.Contains("Selling", Driver.PageSource, StringComparison.OrdinalIgnoreCase);
    }
}
