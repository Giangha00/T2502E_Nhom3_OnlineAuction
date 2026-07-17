# Release smoke pack

Short pre-merge / pre-release smoke: **Signup → Login → Register+Deposit → Bid** (≤ 20 minutes).

## Cases

| ID | Flow |
|----|------|
| AUTH-REG-01 | Sign up + email confirm (smoke helper if Gmail unavailable) |
| AUTH-LOGIN-01 | Login with cookie session |
| AUCTION_REG-03 | Register + deposit (PayPal initiate optional; smoke complete) |
| BID-01 | Place bid at `minNextBid` |

## Gate definition

**Smoke fail → block release** of the related feature area (Auth / registration-deposit / Bid). Do not merge a large PR or ship a demo build that breaks this path without an explicit waiver.

## Prerequisites

1. MySQL seeded; app on `http://localhost:5006` (`dotnet run --launch-profile http`)
2. In `appsettings.Local.json`:

```json
"SmokeTesting": {
  "Enabled": true
}
```

3. Development environment only — `/Smoke/*` returns 404 when disabled or non-Development.

## Run

```powershell
cd OnlineAuction
.\scripts\smoke\Invoke-ReleaseSmoke.ps1
.\scripts\smoke\Invoke-ReleaseSmoke.ps1 -AuctionId 12 -OpenBugs "BUG-21: paypal flake"
.\scripts\smoke\Invoke-ReleaseSmoke.ps1 -SkipSignup   # uses user1@auctionhouse.local (skips AUTH-REG-01 real signup)
```

Exit code `1` = gate blocked.

## Artifacts

| Artifact | Path |
|----------|------|
| Runner | `scripts/smoke/Invoke-ReleaseSmoke.ps1` |
| Report template | `scripts/smoke/SMOKE_REPORT_TEMPLATE.md` |
| Generated reports | `scripts/smoke/reports/smoke-report-*.md` |

## DoD

- [ ] Executed ≥ once on sprint demo **or** before a large PR
- [ ] One-page report filled (pass rate, open bugs, env, build/commit)
- [ ] Failures block related release until fixed
