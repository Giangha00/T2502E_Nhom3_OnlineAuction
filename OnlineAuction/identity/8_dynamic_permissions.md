# Dynamic permission-based authorization

OnlineAuction uses **permission codes in the database**, loaded into the Admin cookie as claims at login, and enforced via a custom `IAuthorizationPolicyProvider` + `PermissionAuthorizationHandler`.

Only two application roles exist: **User** and **Admin**.

## Architecture (video-style dynamic policies)

| Layer | Implementation |
|-------|----------------|
| Storage | `permissions`, `user_permissions` |
| Constants | `PermissionCodes` |
| Service | `IPermissionService` / `PermissionService` |
| Policy provider | `PermissionAuthorizationPolicyProvider` (creates `Permission:{code}` policies at runtime) |
| Handler | `PermissionAuthorizationHandler` |
| Attribute | `[RequirePermission("auctions.verify")]` |
| Superuser | `UserRole.Admin` bypasses all permission checks |

## Two-role model

| Application role | Admin panel | Permissions |
|------------------|-------------|-------------|
| **Admin** | Full access via `/Admin/Account/Login` | All permissions (bypass) |
| **User** | Access only if assigned permissions | From `user_permissions` table |

Delegated staff: assign permissions on **Permissions** page or **Users → Edit**.

Demo admin (`User@123`): `admin@auctionhouse.com`

## Admin login flow

1. Sign in at `/Admin/Account/Login` (Admin role, or User role with at least one permission)
2. `SignInAdminAsync` loads `app_role` + `permission` claims (or `super_admin` for Admin role)
3. Controllers use `[RequirePermission(...)]` — policy resolved dynamically
4. Missing permission → `/Admin/Account/AccessDenied`

## Permission management UI

- **Permissions** (`/Admin/Permission`): select a User account, tick modules, save
- **Users → Edit**: same permission checkboxes for User role accounts

After changes, affected users must sign out and sign in again.

## Seller resource policy

Public seller routes use policy **`ListingOwner`** — only the product owner may edit/cancel listings.

See also [6_dual_session.md](6_dual_session.md) for cookie schemes.
