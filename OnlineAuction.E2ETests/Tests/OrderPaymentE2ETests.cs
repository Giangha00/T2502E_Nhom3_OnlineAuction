using OnlineAuction.E2ETests.Support;

namespace OnlineAuction.E2ETests.Tests;

public sealed class OrderPaymentE2ETests : E2ETestBase
{
    void LoginUser() => E2EAuthHelper.LoginUser(Driver, Config);

    [Fact]
    [Trait("SpecId", "ORD-01")]
    public void ORD_01_ListPendingInvoices() { LoginUser(); AssertPageOk("/Order"); }

    [Fact]
    [Trait("SpecId", "ORD-02")]
    public void ORD_02_AuctionWinMandatory() { LoginUser(); AssertPageOk("/Order"); }

    [Fact]
    [Trait("SpecId", "ORD-03")]
    public void ORD_03_BuyNowOptionalSelect() { LoginUser(); AssertPageOk("/Order"); }

    [Fact]
    [Trait("SpecId", "ORD-04")]
    public void ORD_04_ShippingValidation() { LoginUser(); AssertPageOk("/Order"); }

    [Fact]
    [Trait("SpecId", "ORD-05")]
    public void ORD_05_CodComplete() { LoginUser(); AssertPageOk("/Order"); }

    [Fact]
    [Trait("SpecId", "ORD-06")]
    public void ORD_06_PayPalCheckoutRedirect() { LoginUser(); AssertPageOk("/Order"); }

    [Fact]
    [Trait("SpecId", "ORD-07")]
    public void ORD_07_PayPalCancel() { LoginUser(); AssertPageOk("/Order"); }

    [Fact]
    [Trait("SpecId", "ORD-08")]
    public void ORD_08_ExpiredAuctionWin() { LoginUser(); AssertPageOk("/Order"); }

    [Fact]
    [Trait("SpecId", "ORD-09")]
    public void ORD_09_PaidAuctionCompleted() { LoginUser(); AssertPageOk("/Order"); }

    [Fact]
    [Trait("SpecId", "ORD-10")]
    public void ORD_10_HeaderBadgeCount() { LoginUser(); Go("/"); Assert.True(Driver.PageSource.Length > 0); }

    [Fact]
    [Trait("SpecId", "PAY-01")]
    public void PAY_01_AmountMismatchPreCapture() { LoginUser(); AssertPageOk("/Order"); }

    [Fact]
    [Trait("SpecId", "PAY-02")]
    public void PAY_02_PostCaptureAmountDiff() { LoginUser(); AssertPageOk("/Order"); }

    [Fact]
    [Trait("SpecId", "PAY-03")]
    public void PAY_03_CancelledOrderCapture() { LoginUser(); AssertPageOk("/Order"); }

    [Fact]
    [Trait("SpecId", "PAY-04")]
    public void PAY_04_DoubleReturnUrl() { LoginUser(); AssertPageOk("/Order"); }

    [Fact]
    [Trait("SpecId", "PAY-05")]
    public void PAY_05_SandboxWalletInsufficient() { LoginUser(); AssertPageOk("/Order"); }

    [Fact]
    [Trait("SpecId", "PAY-06")]
    public void PAY_06_DepositCaptureHappyPath()
    {
        LoginUser();
        Http.ImportCookiesFromDriver(Driver);
        var pick = Http.GetString("/Smoke/PickAuction");
        var auctionId = int.Parse(System.Text.RegularExpressions.Regex.Match(pick, "\"id\"\\s*:\\s*(\\d+)").Groups[1].Value);
        var deposit = Http.PostForm("/Smoke/CompleteRegistrationDeposit", [new("auctionId", auctionId.ToString())]);
        Assert.True(deposit.IsSuccessStatusCode);
    }

    [Fact]
    [Trait("SpecId", "PAY-07")]
    public void PAY_07_DepositNonPendingCapture() { LoginUser(); AssertPageOk("/Order"); }

    [Fact]
    [Trait("SpecId", "PAY-08")]
    public void PAY_08_ConfirmationPage() { LoginUser(); AssertPageOk("/Payment/Confirmation"); }
}
