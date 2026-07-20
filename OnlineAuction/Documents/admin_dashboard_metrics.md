# Admin Dashboard metrics catalog

Source of truth for `/Admin/Dashboard` KPIs implemented in `AdminDashboardService`.

**Default filter:** last **30** UTC calendar days (inclusive). Max range 365 days.

## Snapshot vs period

| Kind | Metrics | Date filter |
|------|---------|-------------|
| **Period** | GMV, fees, proceeds, new regs, active users, registration chart, top buyers/sellers, category bid volume | Yes (`DateFrom`–`DateTo`) |
| **Snapshot (as of now)** | Ongoing / Ended / Cancelled / Pending verification auctions, Success rate | **No** — UI badge “As of now” |

## COD / missing payment rule

1. Prefer `payments` with `status = success`, `deleted_at IS NULL`, `paid_at` in period.
2. **Plus** paid/delivered `orders` in period that have **no** success payment row (orphan COD / legacy). Amount = `orders.total_amount`; fees/proceeds from the order columns.
3. Registration deposits (`auction_registration_deposits`) are **never** in GMV or commission.

Paid order statuses: `paid`, `delivered` (not `shipped`).

---

## A. Filter bar

| UI | Behavior |
|----|----------|
| Date From – Date To | Filters all period KPIs |
| Registration granularity | day / week / month for registration chart |
| Export Excel | Same `GetDashboardAsync` numbers as UI |

## B. Overview strip

| ID | UI | Tables | Formula | Unit |
|----|-----|--------|---------|------|
| B1 | GMV | payments (+ orphan orders) | Success payments `amount` in period + orphan paid order `total_amount` | $ |
| B2 | New Registrations | users | `COUNT` active, `deleted_at` null, `created_at` in period | count |
| B3 | Active Users | bids ∪ orders | Distinct `bidder_id` (bids in period) ∪ `buyer_id` (paid/delivered orders in period) | count |
| B4 | Success Rate | auctions + order_items + orders | Snapshot %: auctions (excl. pending_review/rejected) with ≥1 paid/delivered order item | % |

## C. Revenue

| ID | UI | Tables | Formula | Unit |
|----|-----|--------|---------|------|
| C1 | GMV | same as B1 | same as B1 | $ |
| C2 | Commission | orders via payments / orphan | `SUM(platform_fee) + SUM(seller_fee)` | $ |
| C3 | Buyer Checkout Fees | orders.platform_fee | `SUM(platform_fee)` | $ |
| C4 | Seller Success Fees | orders.seller_fee | `SUM(seller_fee)` | $ |
| C5 | Seller Proceeds | orders.seller_proceeds | `SUM(seller_proceeds)` | $ |
| C6 | Revenue donut | derived C3–C4 | Buyer fee vs seller fee mix | chart |
| C7 | Share % of GMV | derived | metric / GMV (0 if GMV=0) | % |
| C8 | Trend ▲▼ | same tables, previous equal-length window | (cur−prev)/prev | % |

## D. Users

| ID | UI | Formula |
|----|-----|---------|
| D1 | New Registrations | B2 |
| D2 | Active Users | B3 |
| D3 | Registration chart | Group `users.created_at` by day/week/month |
| D4 | Top Buyers | Top 10 by `SUM(bid.amount)` in period |
| D5 | Top Sellers | Top 10 by `SUM(seller_proceeds)` + listing counts in period |

## E. Auctions

| ID | UI | Formula | Filter |
|----|-----|---------|--------|
| E1 | Ongoing | status ∈ scheduled, live, ending_soon | Snapshot |
| E2 | Ended | ended, awaiting_payment, completed | Snapshot |
| E3 | Cancelled | cancelled, rejected | Snapshot |
| E4 | Success Rate | B4 | Snapshot |
| E5 | Status donut | E1–E3 | Snapshot |
| E6 | Category breakdown | Top categories by bid volume in period | Period |

## F. Extra (this sprint)

| ID | UI | Formula |
|----|-----|---------|
| F1 | Pending verification | Snapshot count `status = pending_review` |

Follow-ups (not in this PR): F2–F8 (registrations funnel, deposits, pending payment orders, fraud alerts, complaints, watchlist, Buy Now vs Auction GMV).

## Empty state

Charts/lists show “No data” only when aggregates are 0 / lists empty — service always queries DB.
