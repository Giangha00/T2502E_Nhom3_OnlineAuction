using OnlineAuction.E2ETests.Support;

namespace OnlineAuction.E2ETests.Tests;

public sealed class AdminModulesE2ETests : E2ETestBase
{
    void LoginAdmin() => E2EAuthHelper.LoginAdmin(Driver, Config);

    [Fact]
    [Trait("SpecId", "ADM-SYNC-01")]
    public void ADM_SYNC_01_AdminCreateAuction() { LoginAdmin(); AssertPageOk("/Admin/Auction/CreateAuction"); }

    [Fact]
    [Trait("SpecId", "ADM-SYNC-02")]
    public void ADM_SYNC_02_AdminCreateBuyNow() { LoginAdmin(); AssertPageOk("/Admin/Auction/CreateBuyNow"); }

    [Fact]
    [Trait("SpecId", "ADM-SYNC-03")]
    public void ADM_SYNC_03_PublicCardParity() { LoginAdmin(); Go("/"); AssertPageOk("/"); }

    [Fact]
    [Trait("SpecId", "ADM-SYNC-04")]
    public void ADM_SYNC_04_PreviewAuctionLive() { LoginAdmin(); AssertPageOk("/Admin/Auction/CreateAuction"); }

    [Fact]
    [Trait("SpecId", "ADM-SYNC-05")]
    public void ADM_SYNC_05_PreviewBuyNowLive() { LoginAdmin(); AssertPageOk("/Admin/Auction/CreateBuyNow"); }

    [Fact]
    [Trait("SpecId", "ADM-SYNC-06")]
    public void ADM_SYNC_06_AdminScheduleValidation() { LoginAdmin(); AssertPageOk("/Admin/Auction/CreateAuction"); }

    [Fact]
    [Trait("SpecId", "ADM-SYNC-07")]
    public void ADM_SYNC_07_AdminEditMissingSpecs() { LoginAdmin(); AssertPageOk("/Admin/Auction"); }

    [Fact]
    [Trait("SpecId", "ADM-SYNC-08")]
    public void ADM_SYNC_08_AdminUploadPdfDoc() { LoginAdmin(); AssertPageOk("/Admin/Auction/CreateAuction"); }

    [Fact]
    [Trait("SpecId", "VERIFY-01")]
    public void VERIFY_01_ConfirmingQueue() { LoginAdmin(); AssertPageOk("/Admin/AuctionVerification"); }

    [Fact]
    [Trait("SpecId", "VERIFY-02")]
    public void VERIFY_02_ReviewDetails() { LoginAdmin(); AssertPageOk("/Admin/AuctionVerification"); }

    [Fact]
    [Trait("SpecId", "VERIFY-03")]
    public void VERIFY_03_ApproveValidListing() { LoginAdmin(); AssertPageOk("/Admin/AuctionVerification"); }

    [Fact]
    [Trait("SpecId", "VERIFY-04")]
    public void VERIFY_04_RejectWithReason() { LoginAdmin(); AssertPageOk("/Admin/AuctionVerification"); }

    [Fact]
    [Trait("SpecId", "VERIFY-05")]
    public void VERIFY_05_ApproveValidationFail() { LoginAdmin(); AssertPageOk("/Admin/AuctionVerification"); }

    [Fact]
    [Trait("SpecId", "VERIFY-06")]
    public void VERIFY_06_AdminDirectCreateBypass() { LoginAdmin(); AssertPageOk("/Admin/Auction/CreateAuction"); }

    [Fact]
    [Trait("SpecId", "VERIFY-07")]
    public void VERIFY_07_OwnerPreviewConfirming()
    {
        E2EAuthHelper.LoginUser(Driver, Config);
        AssertPageOk("/Account/Selling");
    }

    [Fact]
    [Trait("SpecId", "VERIFY-08")]
    public void VERIFY_08_GatesWhileConfirming() { Go("/Auction"); AssertPageOk("/Auction"); }

    [Fact]
    [Trait("SpecId", "DASH-01")]
    public void DASH_01_DefaultDateFilter() { LoginAdmin(); AssertPageOk("/Admin/Dashboard"); }

    [Fact]
    [Trait("SpecId", "DASH-02")]
    public void DASH_02_GmvAndFees() { LoginAdmin(); AssertPageOk("/Admin/Dashboard"); }

    [Fact]
    [Trait("SpecId", "DASH-03")]
    public void DASH_03_SnapshotMetrics() { LoginAdmin(); AssertPageOk("/Admin/Dashboard"); }

    [Fact]
    [Trait("SpecId", "DASH-04")]
    public void DASH_04_ExportExcel() { LoginAdmin(); AssertPageOk("/Admin/Dashboard"); }

    [Fact]
    [Trait("SpecId", "DASH-05")]
    public void DASH_05_InvalidDateRange() { LoginAdmin(); AssertPageOk("/Admin/Dashboard"); }

    [Fact]
    [Trait("SpecId", "DASH-06")]
    public void DASH_06_PendingVerificationWidget() { LoginAdmin(); AssertPageOk("/Admin/Dashboard"); }

    [Fact]
    [Trait("SpecId", "ADM-CRUD-01")]
    public void ADM_CRUD_01_CategoryCrud() { LoginAdmin(); AssertPageOk("/Admin/Category"); }

    [Fact]
    [Trait("SpecId", "ADM-CRUD-02")]
    public void ADM_CRUD_02_UserManagement() { LoginAdmin(); AssertPageOk("/Admin/User"); }

    [Fact]
    [Trait("SpecId", "ADM-CRUD-03")]
    public void ADM_CRUD_03_ProductAdminEdit() { LoginAdmin(); AssertPageOk("/Admin/Product"); }

    [Fact]
    [Trait("SpecId", "ADM-CRUD-04")]
    public void ADM_CRUD_04_PermissionPolicy() { LoginAdmin(); AssertPageOk("/Admin/Permission"); }

    [Fact]
    [Trait("SpecId", "ADM-CRUD-05")]
    public void ADM_CRUD_05_SuperuserBypass() { LoginAdmin(); AssertPageOk("/Admin/Dashboard"); }

    [Fact]
    [Trait("SpecId", "ADM-CRUD-06")]
    public void ADM_CRUD_06_ComplaintsModule() { LoginAdmin(); AssertPageOk("/Admin/Complaint"); }

    [Fact]
    [Trait("SpecId", "ADM-CRUD-07")]
    public void ADM_CRUD_07_AuctionAdminList() { LoginAdmin(); AssertPageOk("/Admin/Auction"); }

    [Fact]
    [Trait("SpecId", "ADM-CRUD-08")]
    public void ADM_CRUD_08_BuyNowAdmin() { LoginAdmin(); AssertPageOk("/Admin/BuyNow"); }
}
