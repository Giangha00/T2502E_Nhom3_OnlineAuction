using Microsoft.AspNetCore.Mvc;

namespace OnlineAuction.Helpers;

public static class AuthRedirectHelper
{
    public static string ResolveReturnUrl(IUrlHelper urlHelper, string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && urlHelper.IsLocalUrl(returnUrl)
            ? returnUrl
            : urlHelper.Action("Index", "Home")!;
}
