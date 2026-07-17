# PayPal capture recovery

This document describes how to recover when PayPal money was captured but local payment/deposit state could not be updated, or when automatic refund fails.

## Normal flow (after this change)

1. Reload local order/deposit state.
2. Reject capture when local state is not payable (`pending_payment` / deposit `pending`).
3. Load PayPal order details and compare expected amount **before** calling `CaptureOrderAsync`.
4. Capture only when PayPal status is capturable and amounts match.
5. If capture succeeds but local persistence fails, or captured amount differs by `>= $0.01`, call `RefundCaptureAsync` automatically.
6. Log correlation fields: `Flow`, `PayPalOrderId`, `CaptureId`, `OrderId`, `DepositId`, `Expected`, `Captured`.
7. Notify admin accounts (`UserRole.Admin`) when an anomaly refund is attempted.

## Log markers

Search application logs for:

- `PayPal capture guard. Stage=pre_capture_amount_mismatch`
- `PayPal auto-refund succeeded`
- `MANUAL_RECOVERY_REQUIRED`

## When auto-refund succeeds

- Buyer should see a failure message on return URL.
- PayPal balance returns to buyer within sandbox/production refund timing.
- Local order stays `pending_payment` or deposit stays non-`paid`.
- Admin receives a system notification for audit.

## When auto-refund fails (`MANUAL_RECOVERY_REQUIRED`)

1. Collect from logs:
   - `PayPalOrderId`
   - `CaptureId`
   - `Expected`
   - `Captured`
   - `OrderId` or `DepositId`
2. Verify capture in PayPal sandbox dashboard.
3. Manually refund in PayPal dashboard **or** retry API:
   ```http
   POST /v2/payments/captures/{capture_id}/refund
   ```
4. Update local records:
   - Order payment rows: keep `pending` / mark related `Payment` as `failed` if needed.
   - Deposit: keep `pending` or set `failed` depending on business decision.
5. Contact buyer if money remains captured after 24h.

## Idempotent return URL

Calling the return URL again is safe:

- Order already `paid` → success response with existing order ids.
- Deposit already `paid` / `applied` → success message, no second capture.

## Sandbox verification checklist

- Happy path order checkout return URL.
- Happy path registration deposit return URL.
- Mismatch PayPal order amount before capture → no capture, local stays unpaid.
- Cancel order while buyer is on PayPal → return URL rejects before capture.
- Double return URL after success → idempotent success.
- Simulated post-capture mismatch → auto refund attempted and logged.
