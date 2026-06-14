using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Data;
using OnlineAuction.Models;

namespace OnlineAuction.Controllers;

[Route("notifications")]
public class NotificationController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        var model = new NotificationPageViewModel
        {
            Notifications = MockNotificationData.GetNotifications()
        };

        return View(model);
    }

    [HttpGet("{id:int}")]
    public IActionResult Detail(int id)
    {
        var notification = MockNotificationData.GetById(id);
        if (notification is null)
        {
            return NotFound();
        }

        return View(notification);
    }
}
