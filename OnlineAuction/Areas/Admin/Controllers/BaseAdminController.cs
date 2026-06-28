using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Configurations;

namespace OnlineAuction.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(AuthenticationSchemes = AuthSchemes.Admin)]
public abstract class BaseAdminController : Controller
{
    protected bool IsAjaxListRequest()
        => string.Equals(Request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

    protected IActionResult ListOrDefaultView(object model, string partialViewName)
        => IsAjaxListRequest() ? PartialView(partialViewName, model) : View(model);
}
