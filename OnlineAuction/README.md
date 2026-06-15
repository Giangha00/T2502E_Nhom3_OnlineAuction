# OnlineAuction — Dev Notes

## Public user authentication (ASP.NET Core Identity)

Public login/signup uses **Identity cookie auth** (`SignInManager` / `UserManager`), not session flags.

| Route | Method | Description |
|-------|--------|-------------|
| `/Auth/Login` | GET/POST | Sign in with email + password |
| `/Auth/SignUp` | GET/POST | Register new user (`UserRole.User`) |
| `/Auth/Logout` | POST | Sign out + clear legacy session |

Header modal (`_AuthModal`) posts to the same actions. Protected pages (e.g. `/Order`) require `User.Identity.IsAuthenticated`.

### Test accounts (after `UserSeeder` runs on empty DB)

| Email | Password | Notes |
|-------|----------|-------|
| `user1@auctionhouse.local` | `User@123` | Active regular user |
| `user3@auctionhouse.local` | `User@123` | Active regular user |
| `user4@auctionhouse.local` | `User@123` | **Inactive** — login rejected |
| `user12@auctionhouse.local` | `User@123` | Admin role (can still sign in on public site) |

Seeder creates `user1` … `user150@auctionhouse.local`, all with password **`User@123`**.

### Password policy

- Minimum 6 characters
- At least one uppercase, one lowercase, one digit
- Unique email required

### Username rule on sign-up

Username is generated from the email local-part (e.g. `john@gmail.com` → `john`). If taken, a numeric suffix is appended (`john1`, `john2`, …).
