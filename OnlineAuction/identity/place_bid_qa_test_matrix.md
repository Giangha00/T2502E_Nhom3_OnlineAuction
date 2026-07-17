# Place Bid — QA Test Matrix

**Scope:** `AuctionController.PlaceBid`, `BidService`, `AuctionRegistrationService.GetBidBlockMessageAsync`, `_ProductBidPanel`, antiforgery, rate limit, SignalR refresh.

**Branch / date:** _fill when executing_

---

## Preconditions (sample)

| Item | Value |
|------|--------|
| Auction status | `live` or `ending_soon` |
| Schedule | `StartDate ≤ now < EndDate` (UTC) |
| Buyer registration | `approved` when `RequiresRegistration = true` |
| Known fields | `CurrentPrice`, `BidStep`, `minNextBid = CurrentPrice + BidStep` |
| Test accounts | Seller: `user1@auctionhouse.local` / `User@123` · Buyer: second account with approved registration |
| App URL | `http://localhost:5006` |

---

## Test matrix

| ID | Case | Steps (summary) | Expected | Auto test | Manual | Result | Evidence |
|----|------|-----------------|----------|-----------|--------|--------|----------|
| BID-01 | Valid bid (amount ≥ current + step) | Login as approved buyer → open live auction detail → bid `minNextBid` or higher on valid step | HTTP 200 JSON `success:true`; `CurrentPrice` updated; bid history count +1; winning flag on new bid | `BidServicePlaceBidTests.PlaceBid_ValidAmount_UpdatesPriceAndHistory` | Required | | |
| BID-02 | Bid below minimum | Bid `CurrentPrice + BidStep - 1` | Reject `"Your bid must be at least $…"`; price unchanged | `PlaceBid_BelowMinimum_RejectsWithoutChangingPrice` | Required | | |
| BID-03 | Invalid increment / step | Bid ≥ min but not multiple of step (e.g. current 100, step 10 → 115) | Reject `"Your bid must increase by at least $… per step."` | `BidIncrementValidationTests` (Theory) | Optional | | |
| BID-04 | Not logged in | POST `/Auction/PlaceBid` without auth cookie | HTTP **401** `"Please sign in to place a bid."` | — (controller) | Optional | | |
| BID-05 | Not registered / not approved | Buyer without registration or `pending`/`rejected` | Block with registration message; no new bid row | `PlaceBid_PendingRegistration_BlocksBid` | Required | | |
| BID-06 | Seller self-bid | Login as listing seller → place bid | Reject `"You cannot bid on your own listing."` | `PlaceBid_SellerSelfBid_Rejects` | Required | | |
| BID-07 | Before `StartDate` | Auction `live` but `now < StartDate` (mis-scheduled) | Reject `"The live auction has not started yet."` | `PlaceBid_BeforeStartDate_Rejects` | Required | | |
| BID-08 | After `EndDate` / ended | `EndDate` in past or status `ended` | Reject `"This auction has ended."` | `PlaceBid_AfterEndDate_Rejects` | Required | | |
| BID-09 | Disallowed status | `pending_review`, `awaiting_payment`, `completed`, … | Status-specific block message | `PlaceBid_DisallowedStatus_Rejects` (Theory) | Optional | | |
| BID-10 | Two buyers consecutive | Buyer A valid bid → Buyer B higher valid bid | B wins `IsWinning`; `CurrentPrice` = B's amount; A outbid | `PlaceBid_TwoBuyers_SecondHigherBidWins` | Required | | |
| BID-11 | Same buyer raises own bid | Buyer A bids → Buyer A bids higher (allowed) | Success; price updated; only latest A bid winning | `PlaceBid_SameBuyerRaise_SucceedsAndUpdatesWinningBid` | Optional | | |
| BID-12 | Antiforgery missing/invalid | POST without `__RequestVerificationToken` | **400** antiforgery failure; no bid | — (manual / integration) | Optional | | |
| BID-13 | Rate limit spam | >10 bids/min/user on same auction (if enabled) | HTTP **429** + localized rate-limit message | `BidRateLimitServiceTests` | Optional | | |
| BID-14 | UI disabled when `CanBid=false` | Open detail as pending-registration buyer | `#placeBidBtn` has `disabled`; JS blocks submit | `ProductDetailCanBidTests` | Required | | |
| BID-15 | Realtime price refresh | Successful bid from another session/browser | Other client receives `BidUpdated` / price DOM update | — (manual 2-browser) | Optional | | |

---

## Manual E2E — minimum set (BID-01…08, BID-10, BID-14)

### Setup

1. `dotnet ef database update` + `dotnet run --launch-profile http`
2. Pick a **live** auction with `RequiresRegistration` or seed one via Admin.
3. Ensure buyer has **approved** registration (Admin → Registrations).

### BID-01 — Happy path

1. Login buyer → `/Auction/Detail/{id}`
2. Note `#currentPriceDisplay` and `#minBidDisplay`
3. Click **Bid** (or set amount = min, submit)
4. **Pass if:** success toast/message; price label updates; bid count increases; Network → `PlaceBid` → 200 + `currentPrice`

### BID-02 — Below minimum

1. DevTools → edit `#bidAmount` to `min - 1` (or use curl with low amount + valid antiforgery)
2. Submit
3. **Pass if:** error message; price unchanged after refresh

### BID-05 — Registration gate

1. Login buyer **without** approved registration (or pending)
2. **Pass if:** `#placeBidBtn` disabled OR submit returns registration block message

### BID-06 — Seller self-bid

1. Login as seller of listing
2. **Pass if:** no bid form / self-bid blocked server-side

### BID-07 / BID-08 — Schedule

Use SQL to adjust `start_date` / `end_date` / `status`, reload detail, attempt bid.

### BID-10 — Two buyers

1. Browser A: buyer1 bids valid amount
2. Browser B: buyer2 bids higher (valid step)
3. **Pass if:** price = buyer2; history shows buyer2 winning

### BID-14 — UI disabled

1. Pending-registration buyer on detail page
2. **Pass if:** `data-can-bid="false"` and `#placeBidBtn[disabled]`

---

## Automated tests

```bash
dotnet test --filter "FullyQualifiedName~BidServicePlaceBidTests|FullyQualifiedName~BidIncrementValidationTests|FullyQualifiedName~ProductDetailCanBidTests|FullyQualifiedName~BidRateLimitServiceTests"
```

| Automated file | Maps to |
|----------------|---------|
| `BidServicePlaceBidTests.cs` | BID-01, 02, 05, 06, 07, 08, 09, 10, 11 |
| `BidIncrementValidationTests.cs` | BID-03 |
| `ProductDetailCanBidTests.cs` | BID-14 |
| `BidRateLimitServiceTests.cs` | BID-13 |

---

## Definition of Done

- [ ] Matrix filled with Pass/Fail for all 15 IDs
- [ ] Manual minimum executed with screenshots or Network HAR for BID-01 + 2 negative cases (e.g. BID-02, BID-06)
- [ ] `dotnet test` green locally / CI
- [ ] Anti-snipe / finalize drift logged as **bug reference only** (no fix in this task)

---

## Known references (audit — do not fix here)

- **Anti-snipe:** last-minute bid extends `EndDate` by `AntiSnipeExtensionMinutes` (default 5) when remaining &lt; `AntiSnipeThresholdMinutes` (default 5). Verify separately from basic Place Bid.
- **Finalize worker:** auction → `awaiting_payment` runs on schedule worker; out of Place Bid scope.
