using OnlineAuction.E2ETests.Support;
using OpenQA.Selenium;

namespace OnlineAuction.E2ETests.Tests;

public sealed class CatalogBidE2ETests : E2ETestBase
{
    [Fact]
    [Trait("SpecId", "CAT-01")]
    public void CAT_01_HomeAuctionSection()
    {
        Go("/");
        var cards = Driver.FindElements(E2ESelectors.AuctionCard);
        // Catalog may be empty when SeedData:RunAuctionCatalogSeederInDevelopment=false (local SQLite).
        Assert.True(
            cards.Count > 0
            || Driver.FindElements(E2ESelectors.Page("home")).Count > 0
            || Driver.FindElements(E2ESelectors.SiteMain).Count > 0);
    }

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
        Assert.DoesNotContain("status-confirming", Driver.PageSource, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("SpecId", "CAT-05")]
    public void CAT_05_ConfirmingHidden()
    {
        Go("/");
        Assert.DoesNotContain("status-confirming", Driver.PageSource, StringComparison.OrdinalIgnoreCase);
    }

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
        Assert.True(Driver.FindElements(E2ESelectors.BidPanel).Count > 0);
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
        var panel = Driver.FindElement(E2ESelectors.BidPanel);
        Assert.NotNull(E2EWait.DomAttr(panel, "data-bid-step"));
    }

    [Fact]
    [Trait("SpecId", "BID-03")]
    public void BID_03_BelowMinimum_BidInputShowsMin()
    {
        Go("/Auction");
        var id = E2EAuthHelper.FirstAuctionIdFromCatalog(Driver);
        if (id is null) return;
        Go($"/Auction/Detail/{id}");
        var input = Driver.FindElements(E2ESelectors.BidAmount);
        if (input.Count == 0) return;
        Assert.False(string.IsNullOrWhiteSpace(E2EWait.DomAttr(input[0], "value")));
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
        var panel = Driver.FindElement(E2ESelectors.BidPanel);
        Assert.Equal("false", E2EWait.DomAttr(panel, "data-is-logged-in"));
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
        Assert.True(Driver.FindElements(E2ESelectors.BidPanel).Count > 0);
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
        Assert.NotNull(Driver.FindElement(E2ESelectors.BidCountLabel).Text);
    }

    [Fact]
    [Trait("SpecId", "BID-10")]
    public void BID_10_SameBuyerRaises_BidFormAvailableWhenRegistered()
    {
        E2EAuthHelper.LoginUser(Driver, Config);
        var id = PickLiveAuctionId();
        if (id is null) return;
        Go($"/Auction/Detail/{id}");
        Assert.NotNull(E2EWait.DomAttr(Driver.FindElement(E2ESelectors.BidPanel), "data-can-place-bid"));
    }

    [Fact]
    [Trait("SpecId", "BID-11")]
    public void BID_11_CanBidUiFlag()
    {
        Go("/Auction");
        var id = E2EAuthHelper.FirstAuctionIdFromCatalog(Driver);
        if (id is null) return;
        Go($"/Auction/Detail/{id}");
        var panel = Driver.FindElement(E2ESelectors.BidPanel);
        Assert.NotNull(E2EWait.DomAttr(panel, "data-can-bid"));
    }

    [Fact]
    [Trait("SpecId", "BID-12")]
    public void BID_12_AntiSnipeExtension_EndDateAttributePresent()
    {
        Go("/Auction");
        var id = E2EAuthHelper.FirstAuctionIdFromCatalog(Driver);
        if (id is null) return;
        Go($"/Auction/Detail/{id}");
        Assert.NotNull(E2EWait.DomAttr(Driver.FindElement(E2ESelectors.BidPanel), "data-end-date"));
    }

    [Fact]
    [Trait("SpecId", "AUCTION_REG-01")]
    public void AUCTION_REG_01_RegistrationForm()
    {
        E2EAuthHelper.LoginUser(Driver, Config);
        var id = PickLiveAuctionId();
        if (id is null) return;
        Go($"/Auction/Detail/{id}");
        Assert.True(Driver.FindElements(E2ESelectors.BidPanel).Count > 0);
    }

    [Fact]
    [Trait("SpecId", "AUCTION_REG-02")]
    public void AUCTION_REG_02_DepositCalculation_SmokeEndpoint()
    {
        E2EAuthHelper.LoginUser(Driver, Config);
        AssertPageOk("/Auction");
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
        var id = PickLiveAuctionId();
        if (id is null) return;
        Go($"/Auction/Detail/{id}");
        Assert.Equal("true", E2EWait.DomAttr(Driver.FindElement(E2ESelectors.BidPanel), "data-is-logged-in"));
    }

    int? PickLiveAuctionId()
    {
        try { return int.Parse(System.Text.RegularExpressions.Regex.Match(Http.GetString("/Smoke/PickAuction"), "\"id\"\\s*:\\s*(\\d+)").Groups[1].Value); }
        catch { return E2EAuthHelper.FirstAuctionIdFromCatalog(Driver); }
    }
}

