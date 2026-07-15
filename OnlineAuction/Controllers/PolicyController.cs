using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;

namespace OnlineAuction.Controllers;

public class PolicyController : Controller
{
    private readonly PlatformFeeSettings _feeSettings;

    public PolicyController(IOptions<PlatformFeeSettings> feeSettings)
    {
        _feeSettings = feeSettings.Value;
    }

    public IActionResult Index()
    {
        return View("Policy", _feeSettings);
    }
}
