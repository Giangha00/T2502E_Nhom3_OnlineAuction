# Release smoke report (template)

Copy this file (or let `Invoke-ReleaseSmoke.ps1` generate one under `scripts/smoke/reports/`).

| Field | Value |
|-------|-------|
| Date (local) | YYYY-MM-DD HH:mm |
| Environment | local-dev / staging / sprint-demo |
| Base URL | http://localhost:5006 |
| Build | _dll timestamp or CI build id_ |
| Git commit | `_short sha_` |
| Duration | _x.y_ min (budget ≤ 20) |
| Pass rate | **N / 4 (xx%)** |
| Gate | **RELEASE GATE: OPEN** / **BLOCKED** |

## Cases

| ID | Name | Result | Detail |
|----|------|--------|--------|
| AUTH-REG-01 | Sign up + confirm | PASS / FAIL | |
| AUTH-LOGIN-01 | Login | PASS / FAIL | |
| AUCTION_REG-03 | Register + deposit | PASS / FAIL | |
| BID-01 | Place bid | PASS / FAIL | |

## Open bugs

- _BUG-### — title (status: open)_  
- or: _None logged for this run._

## Definition

| Outcome | Action |
|---------|--------|
| All 4 PASS | Related feature may merge / demo / release |
| Any FAIL | **Block release** of Auth, Auction registration+deposit, or Bid (whichever failed) until fixed or waived with written risk acceptance |

## Run checklist (DoD)

- [ ] Ran ≥ 1 time on sprint demo **or** before a large PR
- [ ] Report attached to PR / demo notes
- [ ] Open bugs listed above (if any)

## How to run

```powershell
cd OnlineAuction
# appsettings.Local.json → "SmokeTesting": { "Enabled": true }
dotnet run --launch-profile http
# other terminal:
.\scripts\smoke\Invoke-ReleaseSmoke.ps1 -OpenBugs "BUG-12: ..."
```
