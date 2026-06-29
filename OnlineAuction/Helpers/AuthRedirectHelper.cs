using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace OnlineAuction.Helpers;

public static class AuthRedirectHelper
{
  private static readonly HashSet<string> BlockedReturnPaths = new(StringComparer.OrdinalIgnoreCase)
  {
    "/Auth/Login",
    "/Auth/SignUp",
    "/Auth/ConfirmEmail",
    "/Auth/ForgotPassword"
  };

  public static string ResolveReturnUrl(IUrlHelper urlHelper, string? returnUrl)
  {
    var sanitized = SanitizeReturnUrl(urlHelper, returnUrl);
    return sanitized ?? urlHelper.Action("Index", "Home")!;
  }

  /// <summary>
  /// Removes auth-only routes and authTab query noise from return URLs.
  /// Prevents post-login redirects back to /Auth/ConfirmEmail or similar dead ends.
  /// </summary>
  public static string? SanitizeReturnUrl(IUrlHelper urlHelper, string? returnUrl)
  {
    if (string.IsNullOrWhiteSpace(returnUrl) || !urlHelper.IsLocalUrl(returnUrl))
    {
      return null;
    }

    var path = returnUrl;
    var query = string.Empty;
    var queryIndex = returnUrl.IndexOf('?', StringComparison.Ordinal);
    if (queryIndex >= 0)
    {
      path = returnUrl[..queryIndex];
      query = returnUrl[queryIndex..];
    }

    if (!path.StartsWith("/", StringComparison.Ordinal)
        || path.StartsWith("//", StringComparison.Ordinal)
        || path.Contains("://", StringComparison.Ordinal))
    {
      return null;
    }

    if (BlockedReturnPaths.Contains(path)
        || path.StartsWith("/Auth/", StringComparison.OrdinalIgnoreCase))
    {
      return null;
    }

    if (string.IsNullOrEmpty(query))
    {
      return path;
    }

    var parsed = QueryHelpers.ParseQuery(query);
    var rebuilt = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    foreach (var kvp in parsed)
    {
      if (string.Equals(kvp.Key, "authTab", StringComparison.OrdinalIgnoreCase))
      {
        continue;
      }

      rebuilt[kvp.Key] = kvp.Value.ToString();
    }

    return rebuilt.Count == 0
      ? path
      : path + QueryString.Create(rebuilt).ToString();
  }
}
