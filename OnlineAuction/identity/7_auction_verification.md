# Auction listing verification workflow

Seller listings are **not public** until an admin approves them. This is separate from buyer **auction registration** (`auction_registrations`).

## Status flow

| Status | Meaning | Public listing? |
|--------|---------|-----------------|
| `pending_review` | Seller submitted from `/Sell` or `/Sell/BuyNow` | No |
| `rejected` | Admin rejected with reason | No |
| `scheduled` | Approved but `StartDate` is in the future | No (until start) |
| `live` / `ending_soon` | Approved and active | Yes |

Admin-created auctions from **Admin → Auctions → Create** bypass review and can be set to `live` immediately.

## Admin UI

| Route | Purpose |
|-------|---------|
| `/Admin/AuctionVerification` | Pending queue (sidebar badge shows count) |
| `/Admin/AuctionVerification/Details/{id}` | Review product, grading, media, approve/reject |

Dashboard widget **Pending Verifications** links to the queue.

## Seller UX

- Create listing → success: *Listing submitted for review*
- **Account → Selling** shows badges: Pending Review / Rejected
- Rejected listings show `reject_reason`; seller can edit and resubmit → `pending_review` again

## Approve rules

- `EndDate > StartDate`, `StartingPrice > 0`, `BidStep > 0` (auction listings)
- Product has a non-placeholder primary image
- Product has short description or full description
- Sets `verified_at`, `verified_by`; clears `reject_reason`
- Notifies seller (in-app notification)

## Reject rules

- `reject_reason` required (min 10 chars)
- Sets `status = rejected`, stores reason and verifier metadata
- Notifies seller with reason

## Manual test checklist

1. Seller creates auction → DB `pending_review`, not on `/Auction`
2. Admin approves → `live` (or `scheduled`), appears on public listing, bid works
3. Admin rejects with reason → seller list shows Rejected + reason, not public
4. Reject without reason → validation error
5. Approve already-live auction → idempotent success message
6. Non-admin hits `/Admin/AuctionVerification` → 403
7. Bid on `pending_review` via direct URL → blocked message
8. Admin CRUD still works; seeded catalog auctions remain `live`

## Schema (migration `AddAuctionVerificationFields`)

Columns on `auctions`: `submitted_at`, `verified_at`, `verified_by` (FK → `users`), `reject_reason`, check constraint `chk_auctions_status`.
