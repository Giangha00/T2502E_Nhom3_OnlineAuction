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
