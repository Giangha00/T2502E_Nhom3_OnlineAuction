# Dynamic permission-based authorization

OnlineAuction uses **permission codes stored in the database**, loaded into the Admin cookie as claims at login, and enforced via `PermissionAuthorizationHandler`.

## Architecture

| Layer | Implementation |
|-------|----------------|
| Storage | `permissions`, `role_permissions` |
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
| `users.manage` | Users | CRUD, bulk, role matrix |
| `categories.manage` | Categories | Full CRUD |
| `products.manage` | Products | Backlog |
| `complaints.review` | Complaints | Backlog |

## Staff roles (Identity)

| Role | Default permissions |
|------|---------------------|
| **Admin** | All (bypass in handler) |
| **Moderator** | dashboard.view, auctions.view, auctions.verify |
| **Support** | dashboard.view, users.view, complaints.review |
| **User** | No admin permissions |

Demo accounts (password `User@123`):

- `moderator@auctionhouse.com` — Moderator
- `support@auctionhouse.com` — Support
- `admin@auctionhouse.com` — Admin

## Admin login flow

1. User signs in at `/Admin/Account/Login` (must have a staff Identity role)
2. `SignInAdminAsync` loads role claims + permission claims (`permission` claim type)
3. Controllers use `[RequirePermission(...)]` policies
4. Missing permission → `/Admin/Account/AccessDenied` (403)

**Important:** After changing role permissions in the UI, users must **re-login** to refresh claims (MVP).

## Role ↔ ApplicationUser.Role sync

`UserService` create/update/bulk role changes call `IdentityRoleSyncService.SyncUserRoleAsync`:

- `UserRole.User` → remove all staff Identity roles
- `UserRole.Admin` → Identity role `Admin`
- `UserRole.Moderator` → Identity role `Moderator`
- `UserRole.Support` → Identity role `Support`

Startup seeder `PermissionSeeder` backfills Identity roles for existing users.

## Admin UI

`/Admin/RolePermission` — matrix editor (requires `users.manage`).

## Seller resource policy

Public seller routes use policy **`ListingOwner`** (`ListingOwnerAuthorizationHandler`) — only the product owner may edit/cancel listings via `UserAuctionController`.

## Manual test checklist

1. Login as Moderator → Verify Auctions OK, User Manage → 403
2. Login as Admin → all modules OK
3. Change user role to Admin in admin form → `IsInRole("Admin")` true
4. `user1@...` cannot access `/Admin`
5. Public `/Auth/Login` rejects staff accounts

See also [6_dual_session.md](6_dual_session.md) for cookie schemes.
