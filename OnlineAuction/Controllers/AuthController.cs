using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using OnlineAuction.Configurations;
using OnlineAuction.Entities;
using OnlineAuction.Enums;
using OnlineAuction.Helpers;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Controllers;

[AllowAnonymous]
public class AuthController : Controller
{
    private const string LegacySessionLoggedInKey = "IsLoggedIn";
    private const string LegacySessionUserNameKey = "UserName";

    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly IPasswordResetOtpService _passwordResetOtpService;
    private readonly IWebHostEnvironment _environment;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IEmailVerificationService emailVerificationService,
        IPasswordResetOtpService passwordResetOtpService,
        IWebHostEnvironment environment,
        IStringLocalizer<SharedResource> localizer)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _emailVerificationService = emailVerificationService;
        _passwordResetOtpService = passwordResetOtpService;
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
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, string? fromModal = null, string? returnUrl = null)
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
        TempData["ResetPasswordEmail"] = normalizedEmail;
        TempData["PasswordResetEmailMasked"] = MaskEmail(normalizedEmail);

        var user = await _userManager.FindByEmailAsync(normalizedEmail);
        if (user is not null && user.Status == UserStatus.Active && !await _userManager.IsInRoleAsync(user, UserRole.Admin.ToString()))
        {
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var otp = _passwordResetOtpService.CreateOtp(normalizedEmail, resetToken);

            if (_environment.IsDevelopment())
            {
                TempData["PasswordResetOtp"] = otp;
            }

            // Connect a Gmail API / Google Cloud Function mail sender here and send otp to the user.
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
    public IActionResult VerifyPasswordOtp(VerifyPasswordOtpViewModel model, string? fromModal = null, string? returnUrl = null)
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

        var normalizedEmail = model.Email.Trim();
        TempData["ResetPasswordEmail"] = normalizedEmail;
        TempData["PasswordResetEmailMasked"] = MaskEmail(normalizedEmail);

        if (!_passwordResetOtpService.VerifyOtp(normalizedEmail, model.Otp.Trim()))
        {
            return ForgotPasswordFailure(
                _localizer["Auth_Forgot_InvalidOtp"].Value,
                fromModal,
                returnUrl,
                "forgot-otp");
        }

        TempData["OpenAuthModal"] = "forgot-reset";

        if (IsFromModal(fromModal))
        {
            return RedirectToSafeReturnUrl(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model, string? fromModal = null, string? returnUrl = null)
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

        var normalizedEmail = model.Email.Trim();
        TempData["ResetPasswordEmail"] = normalizedEmail;

        if (!_passwordResetOtpService.TryConsumeVerifiedToken(normalizedEmail, out var resetToken) || string.IsNullOrWhiteSpace(resetToken))
        {
            return ForgotPasswordFailure(
                _localizer["Auth_Forgot_SessionExpired"].Value,
                fromModal,
                returnUrl,
                "forgot");
        }

        var user = await _userManager.FindByEmailAsync(normalizedEmail);
        if (user is null)
        {
            _passwordResetOtpService.Clear(normalizedEmail);
            return ForgotPasswordFailure(
                _localizer["Auth_Forgot_ResetFailed"].Value,
                fromModal,
                returnUrl,
                "forgot");
        }

        var result = await _userManager.ResetPasswordAsync(user, resetToken, model.Password);
        if (result.Succeeded)
        {
            _passwordResetOtpService.Clear(normalizedEmail);
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
        TempData["OpenAuthModal"] = step;

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

        return await _emailVerificationService.SendConfirmationAsync(
            user.Email,
            user.FullName,
            confirmUrl,
            locale,
            cancellationToken);
    }

    private IActionResult RedirectAfterAuthSuccess(string? returnUrl, string openAuthTab)
    {
        TempData["OpenAuthModal"] = openAuthTab;

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
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
