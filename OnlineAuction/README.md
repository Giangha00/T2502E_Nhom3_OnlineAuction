# OnlineAuction — Dev Notes

## Quick start (JetBrains Rider / Mac / Windows)

### Prerequisites

| Tool | Version |
|------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0+ |
| MySQL | 8.0+ (XAMPP, Docker, or native install) |
| Node.js | 18+ (Tailwind build on `dotnet build`) |

### 1. Clone & database

```bash
# Create database in MySQL/phpMyAdmin
CREATE DATABASE online_auction CHARACTER SET utf8mb4;

cd OnlineAuction
dotnet ef database update
```

Default connection (empty MySQL root password, common on XAMPP Mac):

`server=localhost;port=3306;database=online_auction;user=root;password=;`

If your MySQL password differs (typical on Windows):

```bash
cp appsettings.Local.json.example appsettings.Local.json
# Edit appsettings.Local.json with your password
```

### 2. PayPal Sandbox (optional — for `/Order` checkout)

```bash
dotnet user-secrets set "PayPal:ClientId" "YOUR_SANDBOX_CLIENT_ID"
dotnet user-secrets set "PayPal:ClientSecret" "YOUR_SANDBOX_CLIENT_SECRET"
dotnet user-secrets set "PayPal:Mode" "sandbox"
```

User Secrets work when `ASPNETCORE_ENVIRONMENT=Development` (default in `launchSettings.json`).

### 3. Run from JetBrains Rider

1. Open `Nhom3.sln`
2. Select run configuration **OnlineAuction** (uses profile `http` → `http://localhost:5006`)
3. Press Run

Or from terminal:

```bash
cd OnlineAuction
dotnet run --launch-profile http
```

### Configuration files

| File | Committed? | Purpose |
|------|------------|---------|
| `appsettings.json` | Yes | Shared defaults (MySQL, PayPal placeholders) |
| `appsettings.Local.json` | No (gitignored) | Machine-specific DB password / PayPal keys |
| `appsettings.example.json` | Yes | Reference copy of base config |
| User Secrets | Per machine | PayPal credentials (recommended) |

**Do not** recreate `appsettings.Development.json` — it caused config overrides and confusion between machines.

### Troubleshooting

| Issue | Fix |
|-------|-----|
| Cannot connect to MySQL | Start XAMPP/MySQL; check `appsettings.Local.json` password |
| PayPal not configured | Set User Secrets (step 2); restart app; env must be `Development` |
| Port 5006 in use | Kill old process or change port in `launchSettings.json` |
| HTTPS redirect warning | Fixed — HTTPS redirect only runs in Production |
| Seeder crash on startup | Fixed — FK cleanup before delete; refresh is off by default |

In **Development**, sample auction listings (`RareCard Vault Test Auctions`) are seeded once and **updated in place** on later starts (stable product/auction IDs). Expired seeded listings are reactivated without recreating rows.

To wipe and reseed test auctions (IDs will jump), add to `appsettings.Local.json`:

```json
{ "SeedData": { "RefreshTestAuctionsInDevelopment": true } }
```

`SyncCatalogInDevelopment` (default `true`) keeps catalog fields in sync and removes orphaned seed products; it does **not** recreate products that already exist.
### Release smoke (pre-merge / demo)

Short pack (≤ 20 min): `AUTH-REG-01` → `AUTH-LOGIN-01` → `AUCTION_REG-03` → `BID-01`.  
Smoke fail → **block release** of the related feature. See [Documents/release_smoke.md](Documents/release_smoke.md).

```powershell
# appsettings.Local.json → "SmokeTesting": { "Enabled": true }
dotnet run --launch-profile http
.\scripts\smoke\Invoke-ReleaseSmoke.ps1
```

Report template: `scripts/smoke/SMOKE_REPORT_TEMPLATE.md`.

---

## Authentication (dual session)

Admin and public users use **separate cookies** — logging in on one side does not auto-login the other.

| Area | Login URL | Cookie | Scheme |
|------|-----------|--------|--------|
| Public site | `/Auth/Login` | `.AuctionHouse.User` | `Identity.Application` |
| Admin | `/Admin/Account/Login` | `.AuctionHouse.Admin` (path `/Admin`) | `Admin` |

See [identity/6_dual_session.md](identity/6_dual_session.md) for architecture and manual test checklist.

### Public user authentication (ASP.NET Core Identity)

Public login/signup uses **User scheme** (`SignInManager` / `UserManager`), not session flags.

| Route | Method | Description |
|-------|--------|-------------|
| `/Auth/Login` | GET/POST | Sign in with email + password (User scheme) |
| `/Auth/SignUp` | GET/POST | Register new user (`UserRole.User`) |
| `/Auth/Logout` | POST | Sign out User cookie only |

Header modal (`_AuthModal`) posts to the same actions. Protected pages (e.g. `/Order`) require User scheme authentication.

### Admin authentication

| Route | Method | Description |
|-------|--------|-------------|
| `/Admin/Account/Login` | GET/POST | Admin login (Admin scheme, role `Admin`) |
| `/Admin/Account/Logout` | POST | Sign out Admin cookie only |

After deploy, clear old Identity cookies in the browser if both areas still appear linked.

---

## Auction listing verification

Seller submissions from `/Sell` start as **`confirming`** (formerly `pending_review`) and are hidden from public `/Auction` and Home DB sections until an admin approves them.

| Who | Flow |
|-----|------|
| Seller | Submit → pending review → (optional) edit & resubmit after reject |
| Admin | **Verify Auctions** queue → Approve / Reject with reason |
| Admin direct create | **Auctions → Create** can set `live` immediately (bypass review) |

See [identity/7_auction_verification.md](identity/7_auction_verification.md) for status values, approve/reject rules, and manual test checklist.

Apply migration after pull:

```bash
dotnet ef database update
```

---

## Dynamic permissions (Admin)

Admin actions use **permission policies** (`[RequirePermission("auctions.verify")]`) instead of only `Roles = Admin`. Permissions are stored in DB and loaded into the Admin cookie at login.

| Area | Details |
|------|---------|
| Superuser | Identity role **Admin** bypasses all permission checks |
| Roles | **User** (public site) and **Admin** (admin panel only) |

See [identity/8_dynamic_permissions.md](identity/8_dynamic_permissions.md).

---

### Test accounts (after `UserSeeder` runs on empty DB)

| Email | Password | Notes |
|-------|----------|-------|
| `user1@auctionhouse.local` | `User@123` | Active regular user |
| `user3@auctionhouse.local` | `User@123` | Active regular user |
| `user4@auctionhouse.local` | `User@123` | **Inactive** — login rejected |
| `user12@auctionhouse.local` | `User@123` | Admin role — use `/Admin/Account/Login`, not `/Auth/Login` |
| `admin@auctionhouse.com` | `User@123` | System admin (full permissions) |

Seeder creates `user1` … `user150@auctionhouse.local`, all with password **`User@123`**.

### Password policy

- Minimum 6 characters
- At least one uppercase, one lowercase, one digit
- Unique email required

### Username rule on sign-up

Username is generated from the email local-part (e.g. `john@gmail.com` → `john`). If taken, a numeric suffix is appended (`john1`, `john2`, …).

## Product Detail (`/Auction/Detail/{id}`)

- **URL `id` = Auction ID** (not Product ID).
- Data source: `AuctionService.GetProductDetailAsync` → `AuctionHouseDbContext` (`auctions`, `products`, `users`, `bids`).
- `MockProductDetailData` is no longer used for the public detail page.
- On first run, `AuctionCatalogSeeder` creates **~60 auction listings** from `SpreadsheetAuctionCatalog` (requires `UserSeeder` first). **15** of them get a `buy_now_price` for instant purchase.
- **Auction** list: `/Auction` — live auctions.
- **Buy Now** list: `/BuyNow` — auctions where `buy_now_price IS NOT NULL` (same listing can appear in both auction bidding and buy now).

### UI fields without DB columns (temporary defaults)

| Field | Default |
|-------|---------|
| `LotNumber`, `WatcherCount` | `0` |
| `Seller.Rating` | `0` |

### Sell form → Product Detail field mapping

| Sell form field | DB column / table |
|-----------------|-------------------|
| Short Description | `products.short_description` |
| Subtitle | `products.subtitle` |
| Year, Set Name, Language, Card Number | `products.year`, `set_name`, `language`, `card_number` |
| Grade, Certificate Number | `products.grade_label`, `cert_number` |
| Grading sub-scores | `products.grading_centering/corners/edges/surface` |
| Product Origin | `products.product_origin` |
| Gallery image 1 | `products.primary_image` |
| Gallery images 2–5 | `product_images` |
| Documents | `product_documents` |
| Estimated Value | `products.estimated_value` |
| Auction Event Name | `auctions.auction_event_name` |
| Buy Now Price | `auctions.buy_now_price` |

Migration: `AddProductDetailSellFields`.

Test URLs after seed: `/Auction`, `/BuyNow`, `/Auction/Detail/{id}`.

## Payment Center (`/Order`)

Each payable transaction is an independent invoice (`orders` row + `order_items`).

### Invoice sources (`order_source`)

| Source | Created when | Payment deadline | Checkout rule |
|--------|----------------|------------------|---------------|
| `auction_win` | Auction ends with a winner | 48 hours | **Mandatory** — always included in checkout |
| `buy_now` | Buyer clicks Buy Now / Add to cart | 7 days | **Optional** — buyer selects which invoices to pay now |

### Business rules

1. `/Order` lists all non-expired `pending_payment` invoices for the logged-in buyer.
2. Auction-win invoices show a disabled, checked checkbox (cannot be deselected).
3. Buy-now invoices can be checked/unchecked; summary totals update to selected invoices only.
4. `POST /Order/Complete` validates shipping, resolves checkout selection server-side, then:
   - **PayPal:** creates checkout for the selected total only; capture marks only those invoices `paid`.
   - **COD:** marks selected invoices `paid` immediately and shows success message.
5. Expired invoices are auto-cancelled:
   - `buy_now` → listing returns to `live` if `end_date` is still in the future.
   - `auction_win` → auction moves to `ended` (no payment).
6. Paid invoices → related auction status becomes `completed`.

Migration: `AddOrderSourceToOrders` (backfills `BN-*` references to `buy_now`).

Unit tests: `dotnet test` in `OnlineAuction.Tests` (`OrderCheckoutSelectionTests`).

## Order flow (legacy notes)

1. Auction ends → `AuctionFinalizationWorker` creates `orders` + `order_items` for winners
2. `/Order` (Payment Center) loads `pending_payment` orders from DB (not session)
3. Shipping form: Full Name, Address, City, Phone — validated server-side, saved on selected `orders` only
4. Complete Order saves shipping + payment method; PayPal redirect/capture for selected invoices
5. Header badge = pending payment count from DB
6. Expired deadlines → order `cancelled` automatically with auction side-effects above

Migration: `AddOrderShippingFields`. `WonOrderStore` removed.

## PayPal checkout (Task 2)

Flow: `/Order` → select invoices + shipping + PayPal → PayPal Sandbox approve → capture → `/Payment/Confirmation?orderId={id}` (DB-backed).

### Configuration

Set credentials via **User Secrets** (recommended) or environment variables — do not commit `ClientSecret`.

```bash
cd OnlineAuction
dotnet user-secrets set "PayPal:ClientId" "YOUR_SANDBOX_CLIENT_ID"
dotnet user-secrets set "PayPal:ClientSecret" "YOUR_SANDBOX_CLIENT_SECRET"
dotnet user-secrets set "PayPal:Mode" "sandbox"
```

`appsettings.json` contains empty placeholders. `ReturnUrl` / `CancelUrl` in config are fallbacks; the app builds live URLs from the current request when starting checkout.

| Key | Description |
|-----|-------------|
| `PayPal:ClientId` | REST app client ID (public in frontend only if using JS SDK — we use server-side redirect) |
| `PayPal:ClientSecret` | REST app secret — **server only** |
| `PayPal:Mode` | `sandbox` or `live` |
| `PayPal:CurrencyCode` | Default `USD` |

### Sandbox testing

**Important:** The PayPal login page after checkout is **Sandbox only**. Do **not** use your real PayPal.com email/password. Use a **Sandbox Personal (Buyer)** account from Developer Dashboard → Testing Tools → Sandbox Accounts (e.g. `sb-xxx@personal.example.com`). Click the account → View/Edit → set or copy the password.

1. Create a [PayPal Developer](https://developer.paypal.com/) app (Sandbox).
2. Use Sandbox **Personal** (buyer) and **Business** (merchant) accounts from the developer dashboard.
3. Run migrations: `dotnet ef database update`
4. Log in as winning bidder (`user3@auctionhouse.local` / `User@123`), open `/Order`, complete shipping, choose PayPal, submit.
5. Approve payment on PayPal Sandbox → redirected to Confirmation with paid order from DB.
6. Cancel on PayPal → back to `/Order`, order stays `pending_payment`.

### Endpoints

| Route | Description |
|-------|-------------|
| `POST /Order/Complete` | Save shipping; if PayPal → create checkout + redirect |
| `GET /Payment/PayPalReturn?token=` | Capture payment (authorized buyer only) |
| `GET /Payment/PayPalCancel?token=` | Cancel pending PayPal session |
| `GET /Payment/Confirmation?orderId=` | Paid order confirmation from DB |

Migration: `AddPayPalOrderIdToPayments`.

---

## Notifications (in-app + FCM Web Push)

### Overview

- **In-app:** Header dropdown loads from `notifications` table (per user). Badge = unread count from DB.
- **Push:** Firebase Cloud Messaging (FCM) when browser grants permission. Works foreground (toast + badge) and background (system notification via service worker).

Without Firebase config, in-app notifications still work; push is disabled.

### Firebase setup (dev)

1. Create a project at [Firebase Console](https://console.firebase.google.com/).
2. Enable **Cloud Messaging** (Project settings → Cloud Messaging).
3. Register a **Web app** → copy `apiKey`, `messagingSenderId`, `appId`, `projectId`.
4. Cloud Messaging → **Web Push certificates** → generate/copy **VAPID key**.
5. Project settings → **Service accounts** → Generate new private key (JSON). Use:
   - `client_email` → `FirebaseSettings:ClientEmail`
   - `private_key` → `FirebaseSettings:PrivateKey` (escape newlines as `\n` in JSON)
   - `project_id` → `FirebaseSettings:ProjectId`

6. Add **Authorized domains**: `localhost` (and your production domain).

### Configuration (do not commit secrets)

Put real values in `appsettings.Local.json` or User Secrets:

```bash
cd OnlineAuction
dotnet user-secrets set "FirebaseSettings:ProjectId" "your-project-id"
dotnet user-secrets set "FirebaseSettings:ClientEmail" "firebase-adminsdk-...@....iam.gserviceaccount.com"
dotnet user-secrets set "FirebaseSettings:PrivateKey" "-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----\n"
dotnet user-secrets set "FirebaseSettings:WebApiKey" "AIza..."
dotnet user-secrets set "FirebaseSettings:MessagingSenderId" "123456789"
dotnet user-secrets set "FirebaseSettings:AppId" "1:123:web:abc"
dotnet user-secrets set "FirebaseSettings:VapidKey" "BEl..."
```

See `appsettings.Local.json.example` for the full `FirebaseSettings` block.

### Database

```bash
dotnet ef database update
```

Tables: `notifications`, `user_device_tokens`. Migration: `AddNotificationsAndDeviceTokens`.

### API endpoints (authenticated)

| Route | Description |
|-------|-------------|
| `POST /Notification/RegisterDevice` | Save FCM token |
| `POST /Notification/UnregisterDevice` | Remove token on logout |
| `GET /Notification/List` | JSON list + unread count |
| `POST /Notification/MarkRead/{id}` | Mark one read |
| `POST /Notification/MarkAllRead` | Mark all read |

### Test push (localhost)

1. Run app: `dotnet run --launch-profile http` → `http://localhost:5006`
2. Log in (`user1@auctionhouse.local` / `User@123`).
3. Allow browser notifications when prompted.
4. Trigger events:
   - **Outbid:** User A bids, User B outbids on same auction.
   - **Win:** Let auction end (worker ~15s) → winner gets notification.
   - **Payment:** Complete PayPal sandbox checkout.
5. **Foreground:** Toast + dropdown badge updates.
6. **Background:** Minimize tab → trigger event → system notification → click opens `relatedUrl`.

### Supported browsers

| Browser | Web Push (FCM) |
|---------|----------------|
| Chrome / Edge | Yes |
| Firefox | Yes |
| Safari (macOS 13+, iOS 16.4+ PWA) | Limited — test on Chrome/Firefox first |

### Automatic notification events

| Event | Recipient | Type |
|-------|-----------|------|
| Outbid (debounced 5 min/auction) | Previous high bidder | Auction |
| Auction ending within 1 hour | Bidders + approved watchers | Auction |
| Auction won | Winner | Winning |
| Payment captured (PayPal) | Buyer | Payment |
| Refund confirmation page | Buyer | Refund |

## Realtime (SignalR)

The app uses **ASP.NET Core SignalR** for instant in-app updates when a browser tab is open. FCM still handles push when the tab is in the background.

### Hub

| Item | Value |
|------|-------|
| Endpoint | `/hubs/app` |
| Hub class | `Hubs/AppHub.cs` |
| Publisher | `Services/RealtimePublisher.cs` |

### Client events

| Event | Who receives | UI effect |
|-------|--------------|-----------|
| `BidUpdated` | Viewers on auction detail (`JoinAuction`) | Price, bid count, history update without refresh |
| `NotificationReceived` | Logged-in user (`user:{id}` group) | Header dropdown + unread badge |
| `OrderCountUpdated` | Logged-in user | Won-auctions nav badge |

### Fallback

If SignalR disconnects, `header-notifications.js` polls `/Notification/List` every 60 seconds. When the tab becomes visible and the hub is offline, it refreshes once.

### Test realtime (two browsers)

1. Run: `dotnet run --launch-profile http`
2. Open the same auction detail in two windows (can be different users).
3. Place a bid in one window → the other updates price/history immediately.
4. Log in as outbid user → notification badge updates without refresh.
5. Let auction end (background worker ~15s) → detail page shows ended state; winner sees order badge update.

### Files

| File | Role |
|------|------|
| `wwwroot/js/realtime-hub.js` | SignalR client, order badge, dispatches `auction:bid-updated` |
| `wwwroot/js/product-detail.js` | Listens for `auction:bid-updated` |
| `GET /Auction/BidState/{id}` | JSON fallback for bid state |
