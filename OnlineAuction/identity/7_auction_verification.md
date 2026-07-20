# Auction listing verification workflow

Seller listings are **not public** until an admin approves them. This is separate from buyer **auction registration** (`auction_registrations`).

## Status flow

| Status | Meaning | Public listing? |
|--------|---------|-----------------|
| `confirming` | Seller submitted from `/Sell` or `/Sell/BuyNow`; awaiting admin confirmation | No |
| `rejected` | Admin rejected with reason | No |
| `scheduled` | Approved; listed only once registration window has opened (`RegistrationStartDate`) | Yes (in registration window) |
| `live` / `ending_soon` | Approved and active | Yes |

> **Rename note:** `pending_review` was renamed to `confirming`. Migration `RenamePendingReviewToConfirming` updates existing rows and the `chk_auctions_status` constraint. Code keeps `AuctionStatuses.PendingReview` as a temporary alias equal to `Confirming`.

Admin-created auctions from **Admin → Auctions → Create** bypass review and can be set to `live` immediately.

**Detail URL policy:** Guest / other buyers get 404 for `confirming` / `rejected`. Owner seller and Admin (Admin cookie) may preview.

**Public list policy (unchanged):** `/Auction` and Home auction sections only include `live` / `ending_soon`, or `scheduled` inside the registration window. Approved-but-not-yet-open (`scheduled` before `RegistrationStartDate`) stays off the catalog.

## Admin UI

| Route | Purpose |
|-------|---------|
| `/Admin/AuctionVerification` | Confirming queue (sidebar badge shows count) |
| `/Admin/AuctionVerification/Details/{id}` | Review product, grading, media, approve/reject |

Dashboard widget **Pending Verifications** links to the queue (counts `confirming`).

## Seller UX

- Create listing → success: *Your listing is confirming / awaiting admin confirmation*
- **Account → Selling / Submissions** shows badges: Confirming / Đang chờ xác nhận / Rejected
- Rejected listings show `reject_reason`; seller can edit and resubmit → `confirming` again
- Public seller profile does **not** show confirming listings (owner profile / submissions only)

## Approve rules

- `EndDate > StartDate`, `StartingPrice > 0`, `BidStep > 0` (auction listings)
- Product has a non-placeholder primary image
- Product has short description or full description
- Sets `verified_at`, `verified_by`; clears `reject_reason`
- Status becomes `scheduled` or `live` based on current schedule window
- Notifies seller (in-app notification)

## Listing fee (feature #27)

Seller **listing fee** is platform revenue charged when admin approves a listing. It is **not** the buyer **registration deposit** (`auction_registration_deposits`).

| Step | Fee charged? |
|------|----------------|
| Seller submits listing → `confirming` | No |
| Admin rejects → `rejected` | No |
| Admin approves → `live` / `scheduled` | Yes — record in `listing_fees` (if enabled) |

### Config (`appsettings.json` → `PlatformFee`)

| Key | Example | Purpose |
|-----|---------|---------|
| `RegistrationDepositPercent` | `10.00` | Bidder registration deposit (% of item value) |
| `BuyerCheckoutFeePercent` | `2.50` | Buyer fee on won/buy-now checkout |
| `SellerSuccessFeePercent` | `10.00` | Seller fee when order is paid |
| `MinimumRegistrationDeposit` | `1.00` | Minimum registration deposit (USD) |

There is **no listing fee** on admin approval in the current product config path documented here. Seller listings are free to submit; platform revenue comes from the three fees above at registration, checkout, and successful sale.

## Reject rules

- `reject_reason` required (min 10 chars)
- Sets `status = rejected`, stores reason and verifier metadata
- Notifies seller with reason

## Gates while confirming

- Place Bid → blocked (“confirming and not yet open for bidding”)
- Auction registration / deposit → blocked
- Watchlist add (non-owner) → blocked
- Buy Now catalog / purchase → not listed until approved (`live` / `ending_soon`)

## Manual test checklist

1. Seller creates auction → DB `confirming`, not on `/Auction` / Home (CF-01, CF-02)
2. Direct `/Auction/Detail/{id}` as other user → 404 (CF-03); owner/admin can preview
3. Admin approves (in list window) → `live`/`scheduled`, appears on `/Auction` when list rules match (CF-04)
4. Admin rejects with reason → seller list shows Rejected + reason, not public (CF-05)
5. Bid / Register on `confirming` → blocked (CF-06)
6. Migration remaps `pending_review` → `confirming` (CF-07)
7. Admin pending badge counts `confirming` (CF-08)
8. Non-admin hits `/Admin/AuctionVerification` → 403
9. Admin CRUD still works; seeded catalog auctions remain `live`

## Schema (migration `RenamePendingReviewToConfirming`)

Updates `auctions.status` values and check constraint `chk_auctions_status` to use `confirming` instead of `pending_review`.
