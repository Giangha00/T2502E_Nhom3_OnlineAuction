# Bid rate limiting & fraud protection

## Defaults (`BidFraudDetection`)

| Setting | Default | Meaning |
|--------|---------|---------|
| `Enabled` | `true` | Master switch for fraud + rate features |
| `RateLimitingEnabled` | `true` | Enforce bid rate limits |
| `MaxBidsPerMinutePerUser` | `10` | Hard limit per user per auction → **HTTP 429**, no bid insert |
| `MaxBidsPerMinutePerAuction` | `30` | Hard limit for all bidders on one auction → **429** |
| `MaxBidsPerMinutePerIp` | `20` | Hard limit per IP per auction → **429** |
| `ChallengeAfterBidsPerMinute` | `8` | Soft threshold: require challenge token |
| `ChallengeEnabled` | `true` | Enable challenge hook |
| `ChallengeProvider` | `Stub` | `Stub` or `None` |
| `StubChallengeAcceptedTokens` | `["stub-ok"]` | Admin-configurable accepted tokens |
| `ChallengeAfterFraudAlert` | `true` | Fraud alert marks user as challenge-required |
| `ChallengeRequiredMinutes` | `15` | Challenge flag TTL |
| `HighSeverityAction` | `ShadowBan` | `Alert` \| `Reject` \| `ShadowBan` |
| `ShadowBanDurationMinutes` | `30` | Temporary ban after high-severity hit |
| `SameIpAccountThreshold` | `2` | Distinct accounts from same IP |
| `RapidBidWindowSeconds` | `60` | Rapid-bid window |
| `RapidBidCountThreshold` | `5` | Bids in window to flag rapid bidding |
| `CollusionRoundTripThreshold` | `3` | Alternating collusion heuristic |
| `AbnormalJumpPercent` | `50` | Price jump % vs previous |
| `NewAccountHoursThreshold` | `24` | New-account window |
| `AntiSnipeThresholdMinutes` | `5` | Extend when remaining &lt; this |
| `AntiSnipeExtensionMinutes` | `5` | Extension length |

## Rate-limit keys (distributed cache)

- `bid-rate:user:{auctionId}:{bidderId}`
- `bid-rate:auction:{auctionId}`
- `bid-rate:ip:{auctionId}:{ip}`

Window: **1 minute** fixed window.

## Single-instance vs multi-instance

Default DI uses `AddDistributedMemoryCache()` — counters / shadow-ban / challenge flags are **process-local**.

For multi-instance, register a shared store, for example:

```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "OnlineAuction:";
});
```

and remove (or do not rely on) the in-process `AddDistributedMemoryCache()` for this purpose.

Until then, each instance has its own limits (documented single-instance limit).

## Fraud high-severity enforcement

High-severity rules today:

- `same_ip_multiple_accounts`
- `collusion_round_trip`
- `seller_related_bidder` (phone / email / seller IP reuse)

`HighSeverityAction`:

- `Alert` — create admin alert only (legacy behavior)
- `Reject` — pre-bid: no insert + alert; post-bid high hits fall back to shadow-ban
- `ShadowBan` — block current pre-bid attempt + temporary ban (default)

Admin fraud alerts remain on **Admin → Auction → Details**.

## Challenge hook

1. Soft rate (`ChallengeAfterBidsPerMinute`) or fraud alert flag → `requiresChallenge: true` (HTTP 403)
2. Client sends `X-Bid-Challenge-Token` (or form `challengeToken`)
3. Stub provider accepts tokens from `StubChallengeAcceptedTokens`

## Simulate hard limit (429)

See `scripts/simulate-bid-rate-limit.ps1`.
