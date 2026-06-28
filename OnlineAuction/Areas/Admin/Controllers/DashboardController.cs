using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Areas.Admin.Services;
using OnlineAuction.Authorization;
using OnlineAuction.Configurations;

namespace OnlineAuction.Areas.Admin.Controllers;

public class DashboardController : BaseAdminController
{
    private readonly IAdminDashboardService _dashboardService;

    public DashboardController(IAdminDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [RequirePermission(PermissionCodes.DashboardView)]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await _dashboardService.GetDashboardAsync(cancellationToken);
        return View(model);
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.DashboardView)]
    public async Task<IActionResult> Export(int period = 30, CancellationToken cancellationToken = default)
    {
        var csvBytes = await _dashboardService.ExportSummaryCsvAsync(period, cancellationToken);
        var fileName = $"dashboard-report-{DateTime.UtcNow:yyyyMMdd}.csv";
        return File(csvBytes, "text/csv", fileName);
    }
}
