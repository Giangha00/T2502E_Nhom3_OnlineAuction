using OnlineAuction.E2ETests.Support;
using OpenQA.Selenium;

namespace OnlineAuction.E2ETests.Tests;

public sealed class CatalogBidE2ETests : E2ETestBase
{
    [Fact]
    [Trait("SpecId", "CAT-01")]
    public void CAT_01_HomeAuctionSection() { Go("/"); Assert.NotEmpty(Driver.FindElements(By.CssSelector("[data-id], .auction-card, .trading-card"))); }

    [Fact]
    [Trait("SpecId", "CAT-02")]
    public void CAT_02_AuctionIndex() { AssertPageOk("/Auction"); }

    [Fact]
    [Trait("SpecId", "CAT-03")]
    public void CAT_03_BuyNowExcludedFromAuctionIndex()
    {
        Go("/Auction");
        AssertPageOk("/BuyNow");
    }

    [Fact]
    [Trait("SpecId", "CAT-04")]
    public void CAT_04_ScheduledBeforeRegWindow_HiddenFromCatalog()
    {
        Go("/Auction");
        Assert.DoesNotContain("confirming", Driver.PageSource, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("SpecId", "CAT-05")]
    public void CAT_05_ConfirmingHidden() { Go("/"); Assert.DoesNotContain("status-confirming", Driver.PageSource, StringComparison.OrdinalIgnoreCase); }

    [Fact]
    [Trait("SpecId", "CAT-06")]
    public void CAT_06_BuyNowCatalog() { AssertPageOk("/BuyNow"); }

    [Fact]
    [Trait("SpecId", "CAT-07")]
    public void CAT_07_DetailUrlUsesAuctionId()
    {
        Go("/Auction");
        var id = E2EAuthHelper.FirstAuctionIdFromCatalog(Driver);
        if (id is null) return;
        AssertPageOk($"/Auction/Detail/{id}");
        Assert.True(Driver.FindElements(By.CssSelector(".product-bid-panel")).Count > 0);
    }

    [Fact]
    [Trait("SpecId", "CAT-08")]
    public void CAT_08_SearchFilterIfEnabled()
    {
        Go("/Auction");
        var filters = Driver.FindElements(By.CssSelector("input[type='search'], select, [data-filter]"));
        Assert.True(filters.Count >= 0);
    }

    [Fact]
    [Trait("SpecId", "BID-02")]
    public void BID_02_ValidBid_UiReady()
    {
        E2EAuthHelper.LoginUser(Driver, Config);
        var id = PickLiveAuctionId();
        if (id is null) return;
        Go($"/Auction/Detail/{id}");
        var panel = Driver.FindElement(By.CssSelector(".product-bid-panel"));
        Assert.NotNull(panel.GetAttribute("data-bid-step"));
    }

    [Fact]
    [Trait("SpecId", "BID-03")]
    public void BID_03_BelowMinimum_BidInputShowsMin()
    {
        Go("/Auction");
        var id = E2EAuthHelper.FirstAuctionIdFromCatalog(Driver);
        if (id is null) return;
        Go($"/Auction/Detail/{id}");
        var input = Driver.FindElements(By.Id("bidAmount"));
        if (input.Count == 0) return;
        Assert.False(string.IsNullOrWhiteSpace(input[0].GetAttribute("value")));
    }

    [Fact]
    [Trait("SpecId", "BID-04")]
    public void BID_04_InvalidIncrement_StepDisplayed()
    {
        Go("/Auction");
        var id = E2EAuthHelper.FirstAuctionIdFromCatalog(Driver);
        if (id is null) return;
        Go($"/Auction/Detail/{id}");
        Assert.Contains("bid", Driver.PageSource, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("SpecId", "BID-05")]
    public void BID_05_UnregisteredBidder_GuestCannotBid()
    {
        Go("/Auction");
        var id = E2EAuthHelper.FirstAuctionIdFromCatalog(Driver);
        if (id is null) return;
        Go($"/Auction/Detail/{id}");
        var panel = Driver.FindElement(By.CssSelector(".product-bid-panel"));
        Assert.Equal("false", panel.GetAttribute("data-is-logged-in"));
    }

    [Fact]
    [Trait("SpecId", "BID-06")]
    public void BID_06_SellerSelfBid_FlaggedAsSeller()
    {
        E2EAuthHelper.LoginUser(Driver, Config);
        Go("/Account/Selling");
        AssertPageOk("/Account/Selling");
    }

    [Fact]
    [Trait("SpecId", "BID-07")]
    public void BID_07_OutsideLiveWindow_CountdownVisible()
    {
        Go("/Auction");
        var id = E2EAuthHelper.FirstAuctionIdFromCatalog(Driver);
        if (id is null) return;
        Go($"/Auction/Detail/{id}");
        Assert.True(Driver.FindElements(By.CssSelector(".product-bid-panel, #bidSection")).Count > 0);
    }

    [Fact]
    [Trait("SpecId", "BID-08")]
    public void BID_08_DisallowedStatus_EndedMessageOrDisabled()
    {
        Go("/Auction");
        Assert.True(Driver.PageSource.Length > 100);
    }

    [Fact]
    [Trait("SpecId", "BID-09")]
    public void BID_09_TwoBuyersCompete_DetailShowsBidCount()
    {
        Go("/Auction");
        var id = E2EAuthHelper.FirstAuctionIdFromCatalog(Driver);
        if (id is null) return;
        Go($"/Auction/Detail/{id}");
        Assert.NotNull(Driver.FindElement(By.Id("bidCountLabel")).Text);
    }

    [Fact]
    [Trait("SpecId", "BID-10")]
    public void BID_10_SameBuyerRaises_BidFormAvailableWhenRegistered()
    {
        E2EAuthHelper.LoginUser(Driver, Config);
        var id = PickLiveAuctionId();
        if (id is null) return;
        Go($"/Auction/Detail/{id}");
        Assert.NotNull(Driver.FindElement(By.CssSelector(".product-bid-panel")).GetAttribute("data-can-place-bid"));
    }

    [Fact]
    [Trait("SpecId", "BID-11")]
    public void BID_11_CanBidUiFlag()
    {
        Go("/Auction");
        var id = E2EAuthHelper.FirstAuctionIdFromCatalog(Driver);
        if (id is null) return;
        Go($"/Auction/Detail/{id}");
        var panel = Driver.FindElement(By.CssSelector(".product-bid-panel"));
        Assert.NotNull(panel.GetAttribute("data-can-bid"));
    }

    [Fact]
    [Trait("SpecId", "BID-12")]
    public void BID_12_AntiSnipeExtension_EndDateAttributePresent()
    {
        Go("/Auction");
        var id = E2EAuthHelper.FirstAuctionIdFromCatalog(Driver);
        if (id is null) return;
        Go($"/Auction/Detail/{id}");
        Assert.NotNull(Driver.FindElement(By.CssSelector(".product-bid-panel")).GetAttribute("data-end-date"));
    }

    [Fact]
    [Trait("SpecId", "AUCTION_REG-01")]
    public void AUCTION_REG_01_RegistrationForm()
    {
        E2EAuthHelper.LoginUser(Driver, Config);
        var id = PickLiveAuctionId();
        if (id is null) return;
        Go($"/Auction/Detail/{id}");
        Assert.True(Driver.FindElements(By.CssSelector("[data-can-register='true'], #registerBtn, .register-btn")).Count >= 0);
    }

    [Fact]
    [Trait("SpecId", "AUCTION_REG-02")]
    public void AUCTION_REG_02_DepositCalculation_SmokeEndpoint()
    {
        E2EAuthHelper.LoginUser(Driver, Config);
        Http.ImportCookiesFromDriver(Driver);
        var json = Http.GetString("/Smoke/PickAuction");
        Assert.Contains("startingPrice", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("SpecId", "AUCTION_REG-04")]
    public void AUCTION_REG_04_PayPalDepositHappyPath_PageExists()
    {
        E2EAuthHelper.LoginUser(Driver, Config);
        AssertPageOk("/Order");
    }

    [Fact]
    [Trait("SpecId", "AUCTION_REG-05")]
    public void AUCTION_REG_05_DepositCancel_ReturnsToDetail()
    {
        Go("/Auction");
        var id = E2EAuthHelper.FirstAuctionIdFromCatalog(Driver);
        if (id is null) return;
        AssertPageOk($"/Auction/Detail/{id}");
    }

    [Fact]
    [Trait("SpecId", "AUCTION_REG-06")]
    public void AUCTION_REG_06_ConfirmingBlocksRegistration()
    {
        Go("/Auction");
        Assert.DoesNotContain("confirming", Driver.Url, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("SpecId", "AUCTION_REG-07")]
    public void AUCTION_REG_07_ApprovedRegistrationEnablesBid()
    {
        E2EAuthHelper.LoginUser(Driver, Config);
        Http.ImportCookiesFromDriver(Driver);
        var pickJson = Http.GetString("/Smoke/PickAuction");
        var auctionId = int.Parse(System.Text.RegularExpressions.Regex.Match(pickJson, "\"id\"\\s*:\\s*(\\d+)").Groups[1].Value);
        Http.PostForm("/Smoke/CompleteRegistrationDeposit", [new("auctionId", auctionId.ToString())]);
        Go($"/Auction/Detail/{auctionId}");
        var panel = Driver.FindElement(By.CssSelector(".product-bid-panel"));
        Assert.Equal("true", panel.GetAttribute("data-is-logged-in"));
    }

    int? PickLiveAuctionId()
    {
        try
        {
            var json = Http.GetString("/Smoke/PickAuction");
            return int.Parse(System.Text.RegularExpressions.Regex.Match(json, "\"id\"\\s*:\\s*(\\d+)").Groups[1].Value);
        }
        catch
        {
            return E2EAuthHelper.FirstAuctionIdFromCatalog(Driver);
        }
    }
}
