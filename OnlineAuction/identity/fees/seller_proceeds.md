# Marketplace fees & seller proceeds (Phase 1)

## Config (`PlatformFee` in appsettings)

| Key | Default | Meaning |
|-----|---------|---------|
| `RegistrationDepositPercent` | 10.00 | Bidder deposit % of product value |
| `BuyerCheckoutFeePercent` | 2.50 | Buyer fee on order **Subtotal** → stored as `Order.PlatformFee` |
| `SellerSuccessFeePercent` | 10.00 | Seller fee on order **Subtotal** when paid → `Order.SellerFee` |
| `MinimumRegistrationDeposit` | 1.00 | Floor for registration deposit (USD) |

Bound via `PlatformFeeSettings` / `Configure<PlatformFeeSettings>(...)`.

## Formulas

All money amounts round with `MidpointRounding.AwayFromZero` to 2 decimals (`MarketplaceFeeCalculator`).

```
BuyerCheckoutFee (PlatformFee) = Round(Subtotal × BuyerCheckoutFeePercent / 100, 2)
SellerSuccessFee (SellerFee)   = Round(Subtotal × SellerSuccessFeePercent / 100, 2)
SellerProceeds                 = max(0, Round(Subtotal − SellerFee, 2))
```

### What is / is not seller proceeds

| Included in SellerProceeds | Excluded |
|----------------------------|----------|
| Item Subtotal (winning bid / buy-now price) minus seller success fee | Shipping fee |
| | Vault insurance |
| | Buyer checkout fee (`PlatformFee`) |
| | Registration deposit (buyer-side; applied to reduce buyer total) |

## When values are written

| Event | PlatformFee | SellerFee + SellerProceeds | Payment row |
|-------|-------------|----------------------------|-------------|
| Order created | Set | 0 | No |
| PayPal capture / IPN success | Already set | `ApplySellerSettlement` | Existing payment → `success` |
| COD complete | Already set | `ApplySellerSettlement` | **Created** `success`, `TransactionId = COD-{OrderReference}`, `Amount = TotalAmount` |

Phase 1 does **not** transfer money to the seller via PayPal. Proceeds are ledger fields for Admin/Seller reporting. Manual payout / Phase 2.

## Dashboard KPIs

| KPI | Definition |
|-----|------------|
| **GMV** | Sum of `Payment.Amount` where `Status = success` and `PaidAt` in range (PayPal **and** COD) |
| **Commission** | Sum of `PlatformFee` + `SellerFee` on those paid/delivered orders (via successful payments) |
| **Buyer fee** | Sum of `PlatformFee` |
| **Seller fee** | Sum of `SellerFee` |
| **Seller proceeds** | Sum of `SellerProceeds` |

Top sellers “Net Proceeds” column uses `SellerProceeds` (not gross winning bid).

## Seller UI

Owner profile shows Gross Sales (subtotal), Seller Success Fee, and Net Proceeds for paid/delivered orders.

## Code map

- Calculator: `Services/MarketplaceFeeCalculator.cs`
- Settings: `Configurations/PlatformFeeSettings.cs`
- Order fields: `Entities/AuctionOrder.cs` → `platform_fee`, `seller_fee`, `seller_proceeds`
- Pay paths: `OrderService` (COD), `OrderPaymentService` (PayPal)
- Dashboard: `Areas/Admin/Services/AdminDashboardService.cs`
- Policy/FAQ: aligned with formulas and Phase 2 payout note
