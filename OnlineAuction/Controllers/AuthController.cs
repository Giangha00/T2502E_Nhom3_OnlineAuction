using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Entities;
using OnlineAuction.Enums;
using OnlineAuction.Helpers;
using OnlineAuction.Models;
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
    private readonly IPasswordResetOtpService _passwordResetOtpService;
    private readonly PasswordResetOtpSettings _otpSettings;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IPasswordResetOtpService passwordResetOtpService,
        IOptions<PasswordResetOtpSettings> otpOptions,
        IStringLocalizer<SharedResource> localizer)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _passwordResetOtpService = passwordResetOtpService;
        _otpSettings = otpOptions.Value;
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

        TempData["OpenAuthModal"] = "login";
        return RedirectToHomeWithReturnUrl(returnUrl);
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

        if (await _userManager.IsInRoleAsync(user, UserRole.Admin.ToString()))
        {
            ModelState.AddModelError(string.Empty, "Please use the admin login page.");
            return AuthFailureView(model, "login", fromModal);
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
            ModelState.AddModelError(string.Empty, "Sign-in is not allowed for this account.");
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

        TempData["OpenAuthModal"] = "signup";
        return RedirectToHomeWithReturnUrl(returnUrl);
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        TempData["OpenAuthModal"] = "forgot";
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

        TempData["OpenAuthModal"] = "forgot-otp";

        if (IsFromModal(fromModal))
        {
            return RedirectToSafeReturnUrl(returnUrl);
        }

        return RedirectToAction("Index", "Home");
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
        TempData["OpenAuthModal"] = "forgot-reset";

        if (IsFromModal(fromModal))
        {
            return RedirectToSafeReturnUrl(returnUrl);
        }

        return RedirectToAction("Index", "Home");
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
        TempData["OpenAuthModal"] = "forgot-otp";
        if (!string.IsNullOrWhiteSpace(sendResult.DevelopmentOtp))
        {
            TempData["PasswordResetOtp"] = sendResult.DevelopmentOtp;
        }

        if (IsFromModal(fromModal))
        {
            return RedirectToSafeReturnUrl(returnUrl);
        }

        return RedirectToAction("Index", "Home");
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
            TempData["OpenAuthModal"] = "login";

            if (IsFromModal(fromModal))
            {
                return RedirectToSafeReturnUrl(returnUrl);
            }

            return RedirectToAction(nameof(Login));
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
    public async Task<IActionResult> SignUp(SignUpViewModel model, string? fromModal = null)
    {
        if (!ModelState.IsValid)
        {
            return AuthFailureView(model, "signup", fromModal);
        }

        var normalizedEmail = model.Email.Trim();
        if (await _userManager.FindByEmailAsync(normalizedEmail) is not null)
        {
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

        await _signInManager.SignInAsync(user, isPersistent: false);
        ClearLegacySession();

        TempData["AuthSuccess"] = "Account created successfully.";
        return Redirect(AuthRedirectHelper.ResolveReturnUrl(Url, model.ReturnUrl));
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
        TempData["OpenAuthModal"] = step;
        var resetEmail = GetPasswordResetEmailFromSession();
        if (!string.IsNullOrWhiteSpace(resetEmail))
        {
            TempData["ResetPasswordEmail"] = resetEmail;
            TempData["PasswordResetEmailMasked"] = MaskEmail(resetEmail);
        }

        if (IsFromModal(fromModal))
        {
            return RedirectToSafeReturnUrl(returnUrl);
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

        if (IsFromModal(fromModal))
        {
            TempData["AuthError"] = errorMessage;
            TempData["OpenAuthModal"] = tab;

            var returnUrl = model switch
            {
                LoginViewModel login => login.ReturnUrl,
                SignUpViewModel signUp => signUp.ReturnUrl,
                _ => null
            };

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        return model switch
        {
            LoginViewModel login => View("Login", login),
            SignUpViewModel signUp => View("SignUp", signUp),
            _ => RedirectToAction("Login")
        };
    }

    private IActionResult RedirectToSafeReturnUrl(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    private IActionResult RedirectToHomeWithReturnUrl(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect($"{returnUrl}{(returnUrl.Contains('?') ? "&" : "?")}auth=1");
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
