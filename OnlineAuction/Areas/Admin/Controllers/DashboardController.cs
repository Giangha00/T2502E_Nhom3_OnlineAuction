using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Areas.Admin.Services;
using OnlineAuction.Areas.Admin.ViewModels.Dashboard;
using OnlineAuction.Authorization;
using OnlineAuction.Configurations;
using OnlineAuction.Enums;
using OnlineAuction.Helpers;

namespace OnlineAuction.Areas.Admin.Controllers;

public class DashboardController : BaseAdminController
{
    private readonly IAdminDashboardService _dashboardService;

    public DashboardController(IAdminDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [RequirePermission(PermissionCodes.DashboardView)]
    public async Task<IActionResult> Index(
        string? dateRange,
        DateTime? dateFrom,
        DateTime? dateTo,
        string? registrationGranularity,
        CancellationToken cancellationToken)
    {
        var filter = _dashboardService.NormalizeFilter(
            dateFrom,
            dateTo,
            dateRange,
            registrationGranularity);

        var validation = DashboardFilterValidator.Validate(filter.DateFrom, filter.DateTo);
        if (!validation.IsValid)
        {
            return View(new AdminDashboardViewModel
            {
                Filter = filter,
                HasValidFilter = false,
                FilterValidationErrorKey = validation.ErrorKey
            });
        }

        var model = await _dashboardService.GetDashboardAsync(filter, cancellationToken);
        ApplyKpiLinks(model, filter);
        return View(model);
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.DashboardView)]
    public async Task<IActionResult> Export(
        string? dateRange,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        var filter = _dashboardService.NormalizeFilter(dateFrom, dateTo, dateRange);
        var validation = DashboardFilterValidator.Validate(filter.DateFrom, filter.DateTo);

        if (!validation.IsValid)
        {
            return RedirectToAction(nameof(Index), new
            {
                dateFrom = filter.DateFrom.ToString("yyyy-MM-dd"),
                dateTo = filter.DateTo.ToString("yyyy-MM-dd")
            });
        }

        var fileBytes = await _dashboardService.ExportExcelAsync(filter, cancellationToken);
        var fileName = $"dashboard-report-{filter.DateFrom:yyyyMMdd}-{filter.DateTo:yyyyMMdd}.xlsx";
        return File(
            fileBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    private void ApplyKpiLinks(AdminDashboardViewModel model, DashboardFilterViewModel filter)
    {
        var dateRangeValue = AdminDateRangeHelper.Format(filter.DateFrom, filter.DateTo);
        var revenueSectionUrl = Url.Action(
            nameof(Index),
            "Dashboard",
            new
            {
                area = "Admin",
                dateFrom = filter.DateFrom.ToString("yyyy-MM-dd"),
                dateTo = filter.DateTo.ToString("yyyy-MM-dd")
            }) + "#dashboard-revenue-section";

        model.RevenueSection.GmvKpi.LinkUrl = revenueSectionUrl;
        model.RevenueSection.CommissionKpi.LinkUrl = revenueSectionUrl;
        model.RevenueSection.BuyerFeeKpi.LinkUrl = revenueSectionUrl;
        model.RevenueSection.SellerFeeKpi.LinkUrl = revenueSectionUrl;
        model.RevenueSection.SellerProceedsKpi.LinkUrl = revenueSectionUrl;

        model.UserSection.NewRegistrationsKpi.LinkUrl = Url.Action(
            "Index",
            "User",
            new
            {
                area = "Admin",
                Status = UserStatus.Active,
                Role = UserRole.User,
                DateRange = dateRangeValue,
                SortOrder = "date_desc"
            });

        model.UserSection.ActiveUsersKpi.LinkUrl = Url.Action(
            "Index",
            "User",
            new
            {
                area = "Admin",
                Status = UserStatus.Active,
                Role = UserRole.User,
                SortOrder = "date_desc"
            });

        model.AuctionSection.OngoingKpi.LinkUrl = BuildAuctionListUrl("live,ending_soon,scheduled");
        model.AuctionSection.EndedKpi.LinkUrl = BuildAuctionListUrl("ended,awaiting_payment,completed");
        model.AuctionSection.CancelledKpi.LinkUrl = BuildAuctionListUrl("cancelled,rejected");
        model.AuctionSection.PendingVerificationKpi.LinkUrl = Url.Action(
            "Index",
            "AuctionVerification",
            new { area = "Admin" });
        model.AuctionSection.SuccessRateKpi.LinkUrl = BuildAuctionListUrl("completed");
    }

    private string? BuildAuctionListUrl(string status)
    {
        var url = Url.Action(
            "Index",
            "Auction",
            new
            {
                area = "Admin",
                Status = status
            });

        if (string.IsNullOrEmpty(url))
        {
            return url;
        }

        // Force ListingType= so the list includes auction + buy-now (matches dashboard totals).
        return url + (url.Contains('?', StringComparison.Ordinal) ? "&" : "?") + "ListingType=";
    }
}
