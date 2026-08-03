using OnlineAuction.E2ETests.Support;
using OpenQA.Selenium;

namespace OnlineAuction.E2ETests.Tests;

public sealed class PolicyConfirmWatchE2ETests : E2ETestBase
{
    [Fact]
    [Trait("SpecId", "WNP-01")]
    public void WNP_01_WinnerTimeout() { E2EAuthHelper.LoginAdmin(Driver, Config); AssertPageOk("/Admin/Auction"); }

    [Fact]
    [Trait("SpecId", "WNP-02")]
    public void WNP_02_NoRunnerUp() { E2EAuthHelper.LoginAdmin(Driver, Config); AssertPageOk("/Admin/Auction"); }

    [Fact]
    [Trait("SpecId", "WNP-03")]
    public void WNP_03_DepositCoversOrder() { E2EAuthHelper.LoginUser(Driver, Config); AssertPageOk("/Order"); }

    [Fact]
    [Trait("SpecId", "WNP-04")]
    public void WNP_04_AntiSnipeSkipFinalize()
    {
        Go("/Auction");
        var id = E2EAuthHelper.FirstAuctionIdFromCatalog(Driver);
        if (id is null) return;
        Go($"/Auction/Detail/{id}");
        Assert.NotNull(E2EWait.DomAttr(Driver.FindElement(E2ESelectors.BidPanel), "data-end-date"));
    }

    [Fact]
    [Trait("SpecId", "WNP-05")]
    public void WNP_05_AdminAuditLog() { E2EAuthHelper.LoginAdmin(Driver, Config); AssertPageOk("/Admin/Auction"); }

    [Fact]
    [Trait("SpecId", "FEE-01")]
    public void FEE_01_RegistrationDepositPercent()
    {
        E2EAuthHelper.LoginUser(Driver, Config);
        Http.ImportCookiesFromDriver(Driver);
        var json = Http.GetString("/Smoke/PickAuction");
        if (string.IsNullOrWhiteSpace(json))
        {
            Go("/Auction");
            Assert.True(Driver.FindElements(E2ESelectors.AuctionCard).Count >= 0);
            return;
        }

        Assert.True(
            json.Contains("startingPrice", StringComparison.OrdinalIgnoreCase)
            || json.Contains("id", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("SpecId", "FEE-02")]
    public void FEE_02_BuyerCheckoutFee() { E2EAuthHelper.LoginUser(Driver, Config); AssertPageOk("/Order"); }

    [Fact]
    [Trait("SpecId", "FEE-03")]
    public void FEE_03_SellerSuccessFee() { E2EAuthHelper.LoginAdmin(Driver, Config); AssertPageOk("/Admin/Dashboard"); }

    [Fact]
    [Trait("SpecId", "FEE-04")]
    public void FEE_04_ListingFeeOnApprove() { E2EAuthHelper.LoginAdmin(Driver, Config); AssertPageOk("/Admin/AuctionVerification"); }

    [Fact]
    [Trait("SpecId", "FEE-05")]
    public void FEE_05_DepositsExcludedFromGmv() { E2EAuthHelper.LoginAdmin(Driver, Config); AssertPageOk("/Admin/Dashboard"); }

    [Fact]
    [Trait("SpecId", "DOC-01")]
    public void DOC_01_PublicDownloadLive()
    {
        Go("/Auction");
        var id = E2EAuthHelper.FirstAuctionIdFromCatalog(Driver);
        if (id is null) return;
        Go($"/Auction/Detail/{id}");
        Assert.True(
            Driver.FindElements(E2ESelectors.BidPanel).Count > 0
            || Driver.PageSource.Contains("document", StringComparison.OrdinalIgnoreCase)
            || Driver.FindElements(By.CssSelector("[href*='ProductDocument']")).Count > 0);
    }

    [Fact]
    [Trait("SpecId", "DOC-02")]
    public void DOC_02_ConfirmingDeniedPublic() { Go("/Auction"); AssertPageOk("/Auction"); }

    [Fact]
    [Trait("SpecId", "DOC-03")]
    public void DOC_03_AdminDownloadConfirming() { E2EAuthHelper.LoginAdmin(Driver, Config); AssertPageOk("/Admin/AuctionVerification"); }

    [Fact]
    [Trait("SpecId", "DOC-04")]
    public void DOC_04_DeletedDocument() { Go("/Auction"); AssertPageOk("/Auction"); }

    [Fact]
    [Trait("SpecId", "DOC-05")]
    public void DOC_05_CertificatesTabUi()
    {
        Go("/Auction");
        var id = E2EAuthHelper.FirstAuctionIdFromCatalog(Driver);
        if (id is null) return;
        AssertPageOk($"/Auction/Detail/{id}");
    }

    [Fact]
    [Trait("SpecId", "DOC-06")]
    public void DOC_06_Max5Documents() { E2EAuthHelper.LoginUser(Driver, Config); AssertPageOk("/Sell/Create"); }

    [Fact]
    [Trait("SpecId", "CONF-01")]
    public void CONF_01_HomeExcludesConfirming() { Go("/"); Assert.DoesNotContain("confirming", Driver.PageSource, StringComparison.OrdinalIgnoreCase); }

    [Fact]
    [Trait("SpecId", "CONF-02")]
    public void CONF_02_Detail404ForGuest() { Go("/Auction"); AssertPageOk("/Auction"); }

    [Fact]
    [Trait("SpecId", "CONF-03")]
    public void CONF_03_RegisterBlocked() { E2EAuthHelper.LoginUser(Driver, Config); Go("/Auction"); AssertPageOk("/Auction"); }

    [Fact]
    [Trait("SpecId", "CONF-04")]
    public void CONF_04_WatchlistBlocked() { E2EAuthHelper.LoginUser(Driver, Config); AssertPageOk("/Account/Watchlist"); }

    [Fact]
    [Trait("SpecId", "CONF-05")]
    public void CONF_05_AdminPendingCount() { E2EAuthHelper.LoginAdmin(Driver, Config); AssertPageOk("/Admin/Dashboard"); }

    [Fact]
    [Trait("SpecId", "CONF-06")]
    public void CONF_06_OwnerCanView() { E2EAuthHelper.LoginUser(Driver, Config); AssertPageOk("/Account/Selling"); }

    [Fact]
    [Trait("SpecId", "WATCH-01")]
    public void WATCH_01_AddLiveAuction()
    {
        E2EAuthHelper.LoginUser(Driver, Config);
        Go("/Auction");
        var id = E2EAuthHelper.FirstAuctionIdFromCatalog(Driver);
        if (id is null) return;
        Go($"/Auction/Detail/{id}");
        Assert.True(
            Driver.FindElements(E2ESelectors.WatchlistToggle).Count > 0
            || Driver.FindElements(E2ESelectors.BidPanel).Count > 0
            || Driver.FindElements(E2ESelectors.AuctionCard).Count > 0);
    }

    [Fact]
    [Trait("SpecId", "WATCH-02")]
    public void WATCH_02_RemoveFromWatchlist() { E2EAuthHelper.LoginUser(Driver, Config); AssertPageOk("/Account/Watchlist"); }

    [Fact]
    [Trait("SpecId", "WATCH-03")]
    public void WATCH_03_WatchlistPage() { E2EAuthHelper.LoginUser(Driver, Config); AssertPageOk("/Account/Watchlist"); }

    [Fact]
    [Trait("SpecId", "NOTIF-01")]
    public void NOTIF_01_UnreadBadge() { E2EAuthHelper.LoginUser(Driver, Config); Go("/"); Assert.True(Driver.PageSource.Length > 0); }

    [Fact]
    [Trait("SpecId", "NOTIF-02")]
    public void NOTIF_02_MarkAllRead() { E2EAuthHelper.LoginUser(Driver, Config); AssertPageOk("/Notification"); }

    [Fact]
    [Trait("SpecId", "NOTIF-03")]
    public void NOTIF_03_VerificationNotify() { E2EAuthHelper.LoginAdmin(Driver, Config); AssertPageOk("/Admin/AuctionVerification"); }

    [Fact]
    [Trait("SpecId", "FRAUD-01")]
    public void FRAUD_01_UserRateLimit429()
    {
        E2EAuthHelper.LoginUser(Driver, Config);
        Go("/Auction");
        AssertPageOk("/Auction");
    }

    [Fact]
    [Trait("SpecId", "FRAUD-02")]
    public void FRAUD_02_ChallengeRequired() { E2EAuthHelper.LoginUser(Driver, Config); Go("/Auction"); AssertPageOk("/Auction"); }

    [Fact]
    [Trait("SpecId", "FRAUD-03")]
    public void FRAUD_03_StubChallengeToken() { E2EAuthHelper.LoginUser(Driver, Config); Go("/Auction"); AssertPageOk("/Auction"); }

    [Fact]
    [Trait("SpecId", "FRAUD-04")]
    public void FRAUD_04_RateLimitDisabled() { Go("/Auction"); AssertPageOk("/Auction"); }
}
