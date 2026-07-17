using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Entities;
using OnlineAuction.Enums;
using OnlineAuction.Helpers;
using OnlineAuction.Models;
using OnlineAuction.Services;
using OnlineAuction.Services.Interfaces;
using OnlineAuction.Services.Results;

namespace OnlineAuction.Controllers;

[AllowAnonymous]
public class AuthController : Controller
{
    private const string LegacySessionLoggedInKey = "IsLoggedIn";
    private const string LegacySessionUserNameKey = "UserName";
    private const string PasswordResetEmailSessionKey = "PasswordReset:Email";
    private const string PasswordResetUserIdSessionKey = "PasswordReset:UserId";
    private const string PasswordResetOtpIdSessionKey = "PasswordReset:OtpId";
    private const string PasswordResetVerifiedSessionKey = "PasswordReset:Verified";
    private const string PasswordResetVerifiedAtSessionKey = "PasswordReset:VerifiedAtUtc";

    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailQueueService _emailQueueService;
    private readonly IPasswordResetOtpService _passwordResetOtpService;
    private readonly PasswordResetOtpSettings _otpSettings;
    private readonly EmailVerificationSettings _emailVerificationSettings;
    private readonly IHostEnvironment _environment;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IEmailQueueService emailQueueService,
        IPasswordResetOtpService passwordResetOtpService,
        IOptions<PasswordResetOtpSettings> otpOptions,
        IOptions<EmailVerificationSettings> emailVerificationOptions,
        IHostEnvironment environment,
        IStringLocalizer<SharedResource> localizer)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _emailQueueService = emailQueueService;
        _passwordResetOtpService = passwordResetOtpService;
        _otpSettings = otpOptions.Value;
        _emailVerificationSettings = emailVerificationOptions.Value;
        _environment = environment;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        var userAuth = await HttpContext.AuthenticateAsync(AuthSchemes.User);
        if (userAuth.Succeeded)
        {
            return Redirect(AuthRedirectHelper.ResolveReturnUrl(Url, returnUrl));
        }

        return RedirectWithAuthTab("login", returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? fromModal = null)
    {
        if (!ModelState.IsValid)
        {
            return AuthFailureView(model, "login", fromModal);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return AuthFailureView(model, "login", fromModal);
        }

        if (user.Status != UserStatus.Active)
        {
            ModelState.AddModelError(string.Empty, "Your account has been deactivated.");
            return AuthFailureView(model, "login", fromModal);
        }

        if (user.Role == UserRole.Admin)
        {
            var adminReturnUrl = AuthRedirectHelper.SanitizeReturnUrl(Url, model.ReturnUrl);
            adminReturnUrl ??= Url.Action("Index", "Dashboard", new { area = "Admin" })!;
            return Redirect($"/Admin/Account/Login?returnUrl={Uri.EscapeDataString(adminReturnUrl)}");
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            ClearLegacySession();
            return Redirect(AuthRedirectHelper.ResolveReturnUrl(Url, model.ReturnUrl));
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Account locked due to multiple failed attempts. Try again later.");
        }
        else if (result.IsNotAllowed)
        {
            var message = await _userManager.IsEmailConfirmedAsync(user)
                ? "Sign-in is not allowed for this account."
                : "Tài khoản chưa được kích hoạt. Vui lòng kiểm tra email để hoàn tất đăng ký.";
            ModelState.AddModelError(string.Empty, message);
        }
        else if (result.RequiresTwoFactor)
        {
            ModelState.AddModelError(string.Empty, "Two-factor authentication is required.");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
        }

        return AuthFailureView(model, "login", fromModal);
    }

    [HttpGet]
    public async Task<IActionResult> SignUp(string? returnUrl = null)
    {
        var userAuth = await HttpContext.AuthenticateAsync(AuthSchemes.User);
        if (userAuth.Succeeded)
        {
            return Redirect(AuthRedirectHelper.ResolveReturnUrl(Url, returnUrl));
        }

        return RedirectWithAuthTab("signup", returnUrl);
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordViewModel model,
        string? fromModal = null,
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ForgotPasswordFailure(
                ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message))
                ?? _localizer["Auth_Forgot_InvalidEmail"].Value,
                fromModal,
                returnUrl);
        }

        var normalizedEmail = model.Email.Trim();
        var locale = HttpContext.Features.Get<Microsoft.AspNetCore.Localization.IRequestCultureFeature>()
            ?.RequestCulture.Culture.Name ?? System.Globalization.CultureInfo.CurrentUICulture.Name;
        var sendResult = await _passwordResetOtpService.GenerateAndSendAsync(
            normalizedEmail,
            locale,
            cancellationToken);

        if (sendResult.Status == PasswordResetOtpSendStatus.Cooldown)
        {
            return ForgotPasswordFailure(
                _localizer["Auth_Otp_ResendCooldown", sendResult.RetryAfterSeconds ?? _otpSettings.ResendCooldownSeconds].Value,
                fromModal,
                returnUrl);
        }

        if (sendResult.Status == PasswordResetOtpSendStatus.RateLimited)
        {
            return ForgotPasswordFailure(
                _localizer["Auth_Otp_TooManyResends"].Value,
                fromModal,
                returnUrl);
        }

        if (sendResult.Status == PasswordResetOtpSendStatus.Failed)
        {
            return ForgotPasswordFailure(
                _localizer["Auth_Forgot_ResetFailed"].Value,
                fromModal,
                returnUrl);
        }

        // From this point forward, the email used by Verify/Reset comes from server-side session.
        // Hidden email fields in the modal are only for UI convenience and are not trusted.
        SetPasswordResetEmailSession(normalizedEmail);
        TempData["ResetPasswordEmail"] = normalizedEmail;
        TempData["PasswordResetEmailMasked"] = sendResult.MaskedEmail;
        TempData["AuthSuccess"] = _localizer["Auth_Otp_Sent"].Value;
        if (!string.IsNullOrWhiteSpace(sendResult.DevelopmentOtp))
        {
            TempData["PasswordResetOtp"] = sendResult.DevelopmentOtp;
        }

        return IsFromModal(fromModal)
            ? RedirectWithAuthTab("forgot-otp", returnUrl)
            : RedirectWithAuthTab("forgot-otp", null);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyPasswordOtp(
        VerifyPasswordOtpViewModel model,
        string? fromModal = null,
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ForgotPasswordFailure(
                ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message))
                ?? _localizer["Auth_Forgot_InvalidOtp"].Value,
                fromModal,
                returnUrl,
                "forgot-otp");
        }

        var normalizedEmail = GetPasswordResetEmailFromSession();
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return ForgotPasswordFailure(
                _localizer["Auth_Forgot_SessionExpired"].Value,
                fromModal,
                returnUrl,
                "forgot");
        }

        TempData["ResetPasswordEmail"] = normalizedEmail;
        TempData["PasswordResetEmailMasked"] = MaskEmail(normalizedEmail);

        var verifyResult = await _passwordResetOtpService.VerifyAsync(
            normalizedEmail,
            model.Otp.Trim(),
            cancellationToken);

        if (verifyResult.Status == PasswordResetOtpVerifyStatus.Expired)
        {
            return ForgotPasswordFailure(
                _localizer["Auth_Otp_Expired"].Value,
                fromModal,
                returnUrl,
                "forgot-otp");
        }

        if (verifyResult.Status == PasswordResetOtpVerifyStatus.MaxAttemptsReached)
        {
            ClearPasswordResetSession();
            return ForgotPasswordFailure(
                _localizer["Auth_Otp_MaxAttempts"].Value,
                fromModal,
                returnUrl,
                "forgot");
        }

        if (verifyResult.Status != PasswordResetOtpVerifyStatus.Valid ||
            !verifyResult.UserId.HasValue ||
            !verifyResult.OtpId.HasValue)
        {
            return ForgotPasswordFailure(
                _localizer["Auth_Otp_Invalid"].Value,
                fromModal,
                returnUrl,
                "forgot-otp");
        }

        // Store both UserId and OtpId. OtpId prevents an old verified session from being reused
        // after the user requests a newer OTP in another tab/device.
        SetPasswordResetVerifiedSession(verifyResult.UserId.Value, verifyResult.OtpId.Value);

        return IsFromModal(fromModal)
            ? RedirectWithAuthTab("forgot-reset", returnUrl)
            : RedirectWithAuthTab("forgot-reset", null);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendPasswordOtp(
        string? fromModal = null,
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        var email = GetPasswordResetEmailFromSession();
        if (string.IsNullOrWhiteSpace(email))
        {
            return ForgotPasswordFailure(
                _localizer["Auth_Forgot_SessionExpired"].Value,
                fromModal,
                returnUrl,
                "forgot");
        }

        var locale = HttpContext.Features.Get<Microsoft.AspNetCore.Localization.IRequestCultureFeature>()
            ?.RequestCulture.Culture.Name ?? System.Globalization.CultureInfo.CurrentUICulture.Name;
        var sendResult = await _passwordResetOtpService.GenerateAndSendAsync(
            email,
            locale,
            cancellationToken);

        if (sendResult.Status == PasswordResetOtpSendStatus.Cooldown)
        {
            return ForgotPasswordFailure(
                _localizer["Auth_Otp_ResendCooldown", sendResult.RetryAfterSeconds ?? _otpSettings.ResendCooldownSeconds].Value,
                fromModal,
                returnUrl,
                "forgot-otp");
        }

        if (sendResult.Status == PasswordResetOtpSendStatus.RateLimited)
        {
            return ForgotPasswordFailure(
                _localizer["Auth_Otp_TooManyResends"].Value,
                fromModal,
                returnUrl,
                "forgot-otp");
        }

        if (sendResult.Status == PasswordResetOtpSendStatus.Failed)
        {
            return ForgotPasswordFailure(
                _localizer["Auth_Forgot_ResetFailed"].Value,
                fromModal,
                returnUrl,
                "forgot-otp");
        }

        SetPasswordResetEmailSession(email);
        TempData["ResetPasswordEmail"] = email;
        TempData["PasswordResetEmailMasked"] = sendResult.MaskedEmail;
        TempData["AuthSuccess"] = _localizer["Auth_Otp_Sent"].Value;
        if (!string.IsNullOrWhiteSpace(sendResult.DevelopmentOtp))
        {
            TempData["PasswordResetOtp"] = sendResult.DevelopmentOtp;
        }

        return IsFromModal(fromModal)
            ? RedirectWithAuthTab("forgot-otp", returnUrl)
            : RedirectWithAuthTab("forgot-otp", null);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordViewModel model,
        string? fromModal = null,
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ForgotPasswordFailure(
                ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message))
                ?? _localizer["Auth_Forgot_ResetFailed"].Value,
                fromModal,
                returnUrl,
                "forgot-reset");
        }

        var session = GetVerifiedPasswordResetSession();
        if (session is null)
        {
            return ForgotPasswordFailure(
                _localizer["Auth_Forgot_SessionExpired"].Value,
                fromModal,
                returnUrl,
                "forgot");
        }

        var isOtpStillUsable = await _passwordResetOtpService.IsVerifiedOtpStillUsableAsync(
            session.Value.UserId,
            session.Value.OtpId,
            cancellationToken);
        if (!isOtpStillUsable)
        {
            ClearPasswordResetSession();
            return ForgotPasswordFailure(
                _localizer["Auth_Forgot_SessionExpired"].Value,
                fromModal,
                returnUrl,
                "forgot");
        }

        var user = await _userManager.FindByIdAsync(session.Value.UserId.ToString());
        if (user is null)
        {
            ClearPasswordResetSession();
            return ForgotPasswordFailure(
                _localizer["Auth_Forgot_ResetFailed"].Value,
                fromModal,
                returnUrl,
                "forgot");
        }

        // Identity's reset token is generated only now, inside this request, after OTP session checks pass.
        // This keeps the long reset token out of email and out of session.
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, resetToken, model.Password);
        if (result.Succeeded)
        {
            await _passwordResetOtpService.InvalidateAsync(
                session.Value.UserId,
                session.Value.OtpId,
                cancellationToken);
            ClearPasswordResetSession();
            TempData["AuthSuccess"] = _localizer["Auth_Forgot_ResetSuccess"].Value;

            return IsFromModal(fromModal)
                ? RedirectWithAuthTab("login", returnUrl)
                : RedirectWithAuthTab("login", null);
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        TempData["ResetPasswordEmail"] = GetPasswordResetEmailFromSession() ?? string.Empty;
        return ForgotPasswordFailure(
            result.Errors.FirstOrDefault()?.Description ?? _localizer["Auth_Forgot_ResetFailed"].Value,
            fromModal,
            returnUrl,
            "forgot-reset");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SignUp(
        SignUpViewModel model,
        string? fromModal = null,
        CancellationToken cancellationToken = default)
    {
        model.PhoneNumber = new string(model.PhoneNumber.Where(char.IsDigit).ToArray());
        if (model.PhoneNumber.Length != 11)
        {
            ModelState.AddModelError(
                nameof(model.PhoneNumber),
                "Số điện thoại phải gồm đúng 11 chữ số.");
        }

        if (!ModelState.IsValid)
        {
            return AuthFailureView(model, "signup", fromModal);
        }

        var normalizedEmail = model.Email.Trim();
        var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);
        if (existingUser is not null)
        {
            if (!await _userManager.IsEmailConfirmedAsync(existingUser))
            {
                if (ShouldUseMockEmailConfirmation())
                {
                    existingUser.EmailConfirmed = true;
                    await _userManager.UpdateAsync(existingUser);
                    TempData["AuthSuccess"] =
                        "Email này đã đăng ký nhưng chưa kích hoạt. Tài khoản đã được kích hoạt tự động (chế độ dev).";
                    return RedirectAfterAuthSuccess(model.ReturnUrl, "login");
                }

                var resent = await SendEmailConfirmationAsync(existingUser, cancellationToken);
                if (resent)
                {
                    TempData["AuthSuccess"] =
                        "Email này đã đăng ký nhưng chưa kích hoạt. Chúng tôi đã gửi lại email kích hoạt.";
                    return RedirectAfterAuthSuccess(model.ReturnUrl, "login");
                }

                ModelState.AddModelError(
                    string.Empty,
                    "Không gửi được email kích hoạt. Vui lòng thử lại sau.");
                return AuthFailureView(model, "signup", fromModal);
            }

            ModelState.AddModelError(string.Empty, "Email already exists.");
            return AuthFailureView(model, "signup", fromModal);
        }

        var username = await GenerateUniqueUsernameAsync(normalizedEmail);
        var user = new ApplicationUser
        {
            UserName = username,
            Email = normalizedEmail,
            FullName = model.FullName.Trim(),
            PhoneNumber = model.PhoneNumber.Trim(),
            Role = UserRole.User,
            Status = UserStatus.Active,
            EmailConfirmed = false,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return AuthFailureView(model, "signup", fromModal);
        }

        var emailSent = await SendEmailConfirmationAsync(user, cancellationToken);
        if (!emailSent)
        {
            await _userManager.DeleteAsync(user);
            ModelState.AddModelError(
                string.Empty,
                "Không gửi được email kích hoạt. Vui lòng thử lại sau.");
            return AuthFailureView(model, "signup", fromModal);
        }

        if (ShouldUseMockEmailConfirmation())
        {
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
            TempData["AuthSuccess"] =
                "Đăng ký thành công. Tài khoản đã được kích hoạt tự động (chế độ dev). Bạn có thể đăng nhập ngay.";
            return RedirectAfterAuthSuccess(model.ReturnUrl, "login");
        }

        TempData["AuthSuccess"] =
            "Đăng ký thành công. Vui lòng kiểm tra email để kích hoạt tài khoản.";
        return RedirectAfterAuthSuccess(model.ReturnUrl, "login");
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(int userId, string? code)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(code))
        {
            TempData["AuthError"] = "Link kích hoạt không hợp lệ.";
            return RedirectAfterAuthSuccess(null, "login");
        }

        var user = await _userManager.FindByIdAsync(userId.ToString(CultureInfo.InvariantCulture));
        if (user is null)
        {
            TempData["AuthError"] = "Không tìm thấy tài khoản cần kích hoạt.";
            return RedirectAfterAuthSuccess(null, "login");
        }

        if (await _userManager.IsEmailConfirmedAsync(user))
        {
            TempData["AuthSuccess"] = "Tài khoản đã được kích hoạt trước đó. Bạn có thể đăng nhập.";
            return RedirectAfterAuthSuccess(null, "login");
        }

        string token;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        }
        catch (FormatException)
        {
            TempData["AuthError"] = "Link kích hoạt không hợp lệ hoặc đã bị thay đổi.";
            return RedirectAfterAuthSuccess(null, "login");
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            TempData["AuthError"] = "Không thể kích hoạt tài khoản. Link có thể đã hết hạn.";
            return RedirectAfterAuthSuccess(null, "login");
        }

        TempData["AuthSuccess"] = "Kích hoạt tài khoản thành công. Bạn có thể đăng nhập.";
        return RedirectAfterAuthSuccess(null, "login");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    private IActionResult ForgotPasswordFailure(string errorMessage, string? fromModal, string? returnUrl, string step = "forgot")
    {
        TempData["AuthError"] = errorMessage;
        var resetEmail = GetPasswordResetEmailFromSession();
        if (!string.IsNullOrWhiteSpace(resetEmail))
        {
            TempData["ResetPasswordEmail"] = resetEmail;
            TempData["PasswordResetEmailMasked"] = MaskEmail(resetEmail);
        }

        if (IsFromModal(fromModal))
        {
            return RedirectWithAuthTab(step, returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    private IActionResult AuthFailureView(object model, string tab, string? fromModal)
    {
        var errorMessage = ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message))
            ?? "Unable to complete the request.";

        var returnUrl = model switch
        {
            LoginViewModel login => login.ReturnUrl,
            SignUpViewModel signUp => signUp.ReturnUrl,
            _ => null
        };

        TempData["AuthError"] = errorMessage;
        return RedirectWithAuthTab(tab, returnUrl);
    }

    private IActionResult RedirectToSafeReturnUrl(string? returnUrl)
    {
        var sanitized = AuthRedirectHelper.SanitizeReturnUrl(Url, returnUrl);
        if (!string.IsNullOrWhiteSpace(sanitized))
        {
            return Redirect(sanitized);
        }

        return RedirectToAction("Index", "Home");
    }

    private static bool IsFromModal(string? fromModal) =>
        string.Equals(fromModal, "true", StringComparison.OrdinalIgnoreCase);

    private void ClearLegacySession()
    {
        HttpContext.Session.Remove(LegacySessionLoggedInKey);
        HttpContext.Session.Remove(LegacySessionUserNameKey);
    }

    private void SetPasswordResetEmailSession(string email)
    {
        HttpContext.Session.SetString(PasswordResetEmailSessionKey, email);
        HttpContext.Session.Remove(PasswordResetUserIdSessionKey);
        HttpContext.Session.Remove(PasswordResetOtpIdSessionKey);
        HttpContext.Session.Remove(PasswordResetVerifiedSessionKey);
        HttpContext.Session.Remove(PasswordResetVerifiedAtSessionKey);
    }

    private string? GetPasswordResetEmailFromSession() =>
        HttpContext.Session.GetString(PasswordResetEmailSessionKey);

    private void SetPasswordResetVerifiedSession(int userId, int otpId)
    {
        HttpContext.Session.SetInt32(PasswordResetUserIdSessionKey, userId);
        HttpContext.Session.SetInt32(PasswordResetOtpIdSessionKey, otpId);
        HttpContext.Session.SetString(PasswordResetVerifiedSessionKey, bool.TrueString);
        HttpContext.Session.SetString(PasswordResetVerifiedAtSessionKey, DateTime.UtcNow.ToString("O"));
    }

    private (int UserId, int OtpId)? GetVerifiedPasswordResetSession()
    {
        var userId = HttpContext.Session.GetInt32(PasswordResetUserIdSessionKey);
        var otpId = HttpContext.Session.GetInt32(PasswordResetOtpIdSessionKey);
        var isVerified = string.Equals(
            HttpContext.Session.GetString(PasswordResetVerifiedSessionKey),
            bool.TrueString,
            StringComparison.Ordinal);
        var verifiedAtRaw = HttpContext.Session.GetString(PasswordResetVerifiedAtSessionKey);

        if (!userId.HasValue ||
            !otpId.HasValue ||
            !isVerified ||
            !DateTime.TryParse(
                verifiedAtRaw,
                null,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out var verifiedAtUtc))
        {
            return null;
        }

        if (DateTime.UtcNow - verifiedAtUtc.ToUniversalTime() > TimeSpan.FromMinutes(_otpSettings.VerifiedSessionMinutes))
        {
            return null;
        }

        return (userId.Value, otpId.Value);
    }

    private void ClearPasswordResetSession()
    {
        HttpContext.Session.Remove(PasswordResetEmailSessionKey);
        HttpContext.Session.Remove(PasswordResetUserIdSessionKey);
        HttpContext.Session.Remove(PasswordResetOtpIdSessionKey);
        HttpContext.Session.Remove(PasswordResetVerifiedSessionKey);
        HttpContext.Session.Remove(PasswordResetVerifiedAtSessionKey);
    }

    private async Task<bool> SendEmailConfirmationAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return false;
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var confirmUrl = Url.Action(
            nameof(ConfirmEmail),
            "Auth",
            new { userId = user.Id, code = encodedToken },
            Request.Scheme);

        if (string.IsNullOrWhiteSpace(confirmUrl))
        {
            return false;
        }

        var locale = HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name
            ?? CultureInfo.CurrentUICulture.Name;

        return await _emailQueueService.QueueEmailConfirmationAsync(
            user.Email,
            user.FullName,
            confirmUrl,
            locale,
            cancellationToken);
    }

    private bool ShouldUseMockEmailConfirmation() =>
        _emailVerificationSettings.UseMockEmailConfirmation && _environment.IsDevelopment();

    private IActionResult RedirectAfterAuthSuccess(string? returnUrl, string openAuthTab) =>
        RedirectWithAuthTab(openAuthTab, returnUrl);

    private IActionResult RedirectWithAuthTab(string authTab, string? returnUrl = null)
    {
        var path = AuthRedirectHelper.SanitizeReturnUrl(Url, returnUrl)
            ?? Url.Action("Index", "Home")
            ?? "/";

        var separator = path.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return Redirect($"{path}{separator}authTab={Uri.EscapeDataString(authTab)}");
    }

    private async Task<string> GenerateUniqueUsernameAsync(string email)
    {
        var localPart = email.Split('@')[0];
        var baseUsername = new string(localPart
            .Where(ch => char.IsLetterOrDigit(ch) || ch is '.' or '_' or '-')
            .ToArray());

        if (string.IsNullOrWhiteSpace(baseUsername))
        {
            baseUsername = "user";
        }

        var candidate = baseUsername;
        var suffix = 1;

        while (await _userManager.FindByNameAsync(candidate) is not null)
        {
            candidate = $"{baseUsername}{suffix}";
            suffix++;
        }

        return candidate;
    }

    private static string MaskEmail(string email)
    {
        var parts = email.Split('@', 2);
        if (parts.Length != 2 || parts[0].Length <= 2)
        {
            return email;
        }

        return $"{parts[0][0]}***{parts[0][^1]}@{parts[1]}";
    }
}
