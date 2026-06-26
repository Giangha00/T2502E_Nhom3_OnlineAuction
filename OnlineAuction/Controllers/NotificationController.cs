using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Controllers;

[Authorize]
[Route("[controller]")]
public class NotificationController : Controller
{
    private readonly INotificationService _notificationService;
    private readonly FirebaseSettings _firebaseSettings;

    public NotificationController(
        INotificationService notificationService,
        IOptions<FirebaseSettings> firebaseSettings)
    {
        _notificationService = notificationService;
        _firebaseSettings = firebaseSettings.Value;
    }

    [HttpGet("List")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var notifications = await _notificationService.GetRecentForUserAsync(userId.Value, cancellationToken: cancellationToken);
        var unreadCount = await _notificationService.GetUnreadCountAsync(userId.Value, cancellationToken);

        return Json(new
        {
            unreadCount,
            notifications
        });
    }

    [HttpPost("RegisterDevice")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue || string.IsNullOrWhiteSpace(request.FcmToken))
        {
            return BadRequest(new { message = "Invalid request." });
        }

        await _notificationService.RegisterDeviceTokenAsync(
            userId.Value,
            request.FcmToken,
            request.DeviceInfo,
            cancellationToken);

        return Ok(new { success = true });
    }

    [HttpPost("UnregisterDevice")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnregisterDevice([FromBody] RegisterDeviceRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue || string.IsNullOrWhiteSpace(request.FcmToken))
        {
            return BadRequest(new { message = "Invalid request." });
        }

        await _notificationService.UnregisterDeviceTokenAsync(userId.Value, request.FcmToken, cancellationToken);
        return Ok(new { success = true });
    }

    [HttpPost("MarkRead/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var updated = await _notificationService.MarkAsReadAsync(userId.Value, id, cancellationToken);
        if (!updated)
        {
            return NotFound();
        }

        var unreadCount = await _notificationService.GetUnreadCountAsync(userId.Value, cancellationToken);
        return Ok(new { success = true, unreadCount });
    }

    [HttpPost("MarkAllRead")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        await _notificationService.MarkAllAsReadAsync(userId.Value, cancellationToken);
        return Ok(new { success = true, unreadCount = 0 });
    }

    [AllowAnonymous]
    [HttpGet("/notification/firebase-config.js")]
    [ResponseCache(Duration = 300)]
    public ContentResult FirebaseConfig()
    {
        if (!_firebaseSettings.IsClientConfigured)
        {
            return Content("self.FIREBASE_CONFIG = null;", "application/javascript");
        }

        var config = new
        {
            apiKey = _firebaseSettings.WebApiKey,
            authDomain = $"{_firebaseSettings.ProjectId}.firebaseapp.com",
            projectId = _firebaseSettings.ProjectId,
            messagingSenderId = _firebaseSettings.MessagingSenderId,
            appId = _firebaseSettings.AppId
        };

        var json = JsonSerializer.Serialize(config);
        return Content($"self.FIREBASE_CONFIG = {json};", "application/javascript");
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }

    public sealed class RegisterDeviceRequest
    {
        public string FcmToken { get; set; } = string.Empty;

        public string? DeviceInfo { get; set; }
    }
}
