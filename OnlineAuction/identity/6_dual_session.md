# Dual Session — Admin vs Public User Login

OnlineAuction uses **two independent authentication cookies** so Admin and public User sessions do not overlap.

## Schemes & cookies

| Scheme | Cookie name | Path | Login | Expiration |
|--------|-------------|------|-------|------------|
| **User** (`Identity.Application`) | `.AuctionHouse.User` | `/` (site-wide) | `/Auth/Login` | 14 days (sliding) |
| **Admin** (`Admin`) | `.AuctionHouse.Admin` | `/Admin` | `/Admin/Account/Login` | 8 hours (sliding) |

Because the Admin cookie has `Path=/Admin`, the browser **does not send it** on public pages (`/`, `/Order`, `/Sell`, …).

## Login flows

### Public user
- URL: `/Auth/Login`
- Uses `SignInManager.PasswordSignInAsync` → **User scheme only**
- Admin accounts are rejected (`Please use the admin login page`)
- Logout: `/Auth/Logout` (POST) → clears **User cookie only**

### Admin
- URL: `/Admin/Account/Login`
- Validates password + `Admin` role, then `HttpContext.SignInAsync("Admin", …)`
- Does **not** sign in the User scheme (dual session allowed)
- Logout: `/Admin/Account/Logout` (POST) → clears **Admin cookie only**

## Authorization

```csharp
// Public controllers
[Authorize(AuthenticationSchemes = AuthSchemes.User)]

// Admin area
[Authorize(AuthenticationSchemes = AuthSchemes.Admin, Roles = "Admin")]
```

Unauthorized redirects:
- Public `[Authorize]` → `/Auth/Login?returnUrl=…`
- Admin area → `/Admin/Account/Login`
- `/api/**` → HTTP 401/403 (no redirect)

## UI rules

- **`_Layout.cshtml` (public):** logged-in state from **User scheme only**
- **`_AdminHeader.cshtml`:** profile from **Admin scheme only**

## Helper: `ICurrentUserContext`

```csharp
await _currentUserContext.GetUserIdAsync();   // public user id
await _currentUserContext.GetAdminIdAsync();  // admin id
```

## Manual test checklist

1. Login Admin → open `/` → header shows **Guest** (Login/Sign up)
2. Login User → open `/Admin/Dashboard` → redirect to Admin login
3. Login both (without logout) → public uses User, Admin area uses Admin
4. Logout Admin → User session remains; Logout User → Admin session remains

## Migration note

After deploying, **clear old `.AspNetCore.Identity.Application` cookies** in the browser (or log out once). Old single-cookie sessions may still appear logged in on both areas until expired.
