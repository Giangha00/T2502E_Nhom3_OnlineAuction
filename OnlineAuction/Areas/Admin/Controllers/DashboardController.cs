using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Areas.Admin.Services;
using OnlineAuction.Areas.Admin.ViewModels.Dashboard;
using OnlineAuction.Authorization;
using OnlineAuction.Configurations;
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
        string? status,
        int? categoryId,
        DateTime? registrationDate,
        string? registrationGranularity,
        string? section,
        string? revenueType,
        CancellationToken cancellationToken)
    {
        var filter = _dashboardService.NormalizeFilter(
            dateFrom,
            dateTo,
            dateRange,
            status,
            categoryId,
            registrationDate,
            registrationGranularity,
            section,
            revenueType);

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
}
