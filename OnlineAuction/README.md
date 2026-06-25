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
| Seeder crash on startup | Fixed — FK cleanup before delete; set `RefreshTestAuctionsInDevelopment` to `false` to disable |

In **Development**, Pokémon test auctions (`RareCard Vault Test Auctions`) **auto-refresh on every app start** by default (`RefreshTestAuctionsInDevelopment: true`). Restart Rider to see fresh auctions with new countdown.

To keep orders while testing PayPal, add to `appsettings.Local.json`:

```json
{ "SeedData": { "RefreshTestAuctionsInDevelopment": false } }
```

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

### Test accounts (after `UserSeeder` runs on empty DB)

| Email | Password | Notes |
|-------|----------|-------|
| `user1@auctionhouse.local` | `User@123` | Active regular user |
| `user3@auctionhouse.local` | `User@123` | Active regular user |
| `user4@auctionhouse.local` | `User@123` | **Inactive** — login rejected |
| `user12@auctionhouse.local` | `User@123` | Admin role — use `/Admin/Account/Login`, not `/Auth/Login` |

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
- On first run with an empty catalog, `AuctionCatalogSeeder` creates 5 sample auctions (requires `UserSeeder` first).

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

Test URLs after seed: `/Auction/Detail/1` … `/Auction/Detail/5`.

## Order flow (Task 1 — DB-backed)

1. Auction ends → `AuctionFinalizationWorker` creates `orders` + `order_items` for winners
2. `/Order` loads `pending_payment` orders from DB (not session)
3. Shipping form: Full Name, Address, City, Phone — validated server-side, saved on `orders`
4. Complete Order saves shipping + payment method; PayPal redirect/capture in Task 2
5. Header badge = pending payment count from DB
6. Expired deadlines → order `cancelled` automatically

Migration: `AddOrderShippingFields`. `WonOrderStore` removed.

## PayPal checkout (Task 2)

Flow: `/Order` → shipping + PayPal → PayPal Sandbox approve → capture → `/Payment/Confirmation?orderId={id}` (DB-backed).

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
