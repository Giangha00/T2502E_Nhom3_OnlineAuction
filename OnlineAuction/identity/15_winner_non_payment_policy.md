# Feature #15 — Winner Non-Payment Policy

## Selected policy (recommended for this project)

When an auction winner does **not** complete payment within **48 hours**:

1. **Order** → `cancelled`
2. **Winner registration deposit** → `forfeited` (no PayPal refund; retained by platform)
3. **Audit** → append rows to `winner_non_payment_logs`
4. **Recovery**
   - If an eligible **runner-up bidder** exists → promote bid #2, create a **second-chance** `auction_win` order (`awaiting_payment`, new `WinnerId`, 48h deadline by default)
   - If no eligible runner-up → auction → `ended`, `WinnerId` cleared, seller notified to **relist**
5. **Notifications**
   - Defaulting winner: payment expired + deposit forfeited (if applicable)
   - Seller: buyer did not pay (+ second-chance or relist message)
   - Second-chance bidder: new winning opportunity

## Explicit non-goals

- No automatic relist to `live` (auction time has ended)
- No partial deposit refund in this sprint (full forfeit only)
- Loser deposits already refunded at auction end are **not** affected

## Active order rule

Only **one non-cancelled** order per auction at a time. Cancelled orders remain for audit; second-chance orders use reference suffix `-SC2`, `-SC3`, …

## Configuration

`appsettings.json`:

```json
"WinnerNonPayment": {
  "SecondChancePaymentHours": 48
}
```

## Key code paths

| Step | Component |
|------|-----------|
| 48h expiry detection | `OrderService.CancelAllExpiredPendingOrdersAsync` |
| Recovery orchestration | `WinnerNonPaymentRecoveryService` |
| Second-chance order | `OrderCreationService.TryCreatePendingPaymentOrderWithinUnitOfWorkAsync` |
| Admin visibility | Admin Auction Details → forfeited deposits + recovery log |

## Manual E2E checklist

1. End auction with ≥2 bidders; winner gets order + 48h deadline
2. Do not pay; run lifecycle worker or open `/Order` after deadline
3. Verify: winner deposit `forfeited`, order `cancelled`, runner-up gets second-chance order OR seller relist notification
4. Verify loser refunded deposits unchanged
