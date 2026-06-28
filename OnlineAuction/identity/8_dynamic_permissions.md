# Dynamic permission-based authorization

OnlineAuction uses **permission codes stored in the database**, loaded into the Admin cookie as claims at login, and enforced via `PermissionAuthorizationHandler`.

Only two application roles exist: **User** (public site) and **Admin** (admin panel).

## Architecture

| Layer | Implementation |
|-------|----------------|
| Storage | `permissions`, `role_permissions` (reserved for future granular admin roles) |
| Constants | `PermissionCodes` |
| Service | `IPermissionService` / `PermissionService` |
| Handler | `PermissionAuthorizationHandler` |
| Attribute | `[RequirePermission("auctions.verify")]` |
| Superuser | Identity role **Admin** bypasses all permission checks |

## Permission codes (seeded)

| Code | Module | Typical use |
|------|--------|-------------|
| `dashboard.view` | Dashboard | Dashboard index/export |
| `auctions.view` | Auctions | List/details |
| `auctions.manage` | Auctions | Create/edit/delete |
| `auctions.verify` | Auctions | Verify queue |
| `users.view` | Users | List/details |
| `users.manage` | Users | CRUD, bulk |
| `categories.manage` | Categories | Full CRUD |
| `products.manage` | Products | Backlog |
| `complaints.review` | Complaints | Backlog |

## Roles

| Role | Admin area | Identity role | Permissions |
|------|------------|---------------|-------------|
| **User** | No access | None | None; seller routes use `ListingOwner` policy |
| **Admin** | Full access via `/Admin/Account/Login` | `Admin` | All permissions (handler bypass) |

Demo admin account (password `User@123`): `admin@auctionhouse.com`

## Admin login flow

1. User signs in at `/Admin/Account/Login` (must have Identity role `Admin`)
2. `SignInAdminAsync` loads role claims + permission claims (`permission` claim type)
3. Controllers use `[RequirePermission(...)]` policies
4. Missing permission → `/Admin/Account/AccessDenied` (403)

## Role ↔ ApplicationUser.Role sync

`UserService` create/update/bulk role changes call `IdentityRoleSyncService.SyncUserRoleAsync`:

- `UserRole.User` → remove Admin Identity role
- `UserRole.Admin` → Identity role `Admin`

Startup seeder `PermissionSeeder` backfills Identity roles for existing users and migrates legacy Moderator/Support rows to User.

## Seller resource policy

Public seller routes use policy **`ListingOwner`** (`ListingOwnerAuthorizationHandler`) — only the product owner may edit/cancel listings via `UserAuctionController`.

## Manual test checklist

1. Login as Admin → all admin modules OK
2. Change user role to Admin in admin form → `IsInRole("Admin")` true
3. `user1@...` cannot access `/Admin`
4. Public `/Auth/Login` rejects Admin accounts

See also [6_dual_session.md](6_dual_session.md) for cookie schemes.
