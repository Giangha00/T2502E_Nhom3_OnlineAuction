# Admin Auction Status vs Listing Phase

Admin keeps two status layers:

- DB Status is the persisted lifecycle value on `auctions.status`.
- Listing Phase is computed at read time with `AuctionScheduleHelper.ResolveListingPhase`; it is never written to the database.

## Mapping

| DB status / schedule window | Listing phase | Public on `/Auction` |
| --- | --- | --- |
| `scheduled` and now is before `RegistrationStartDate` | `upcoming` | Yes |
| `scheduled` and now is within `RegistrationStartDate` to `RegistrationEndDate` | `registration_open` | Yes |
| `scheduled` and registration is closed but live has not started | `registration_closed` | Yes |
| `live` / `ending_soon` and now is within `StartDate` to `EndDate` | `live_auction` or `live_ending_soon` | Yes |
| `ended`, `awaiting_payment`, `completed`, or any public-capable row after `EndDate` | `ended` | No |
| `confirming` / legacy `pending_review` | `not_listed` | No |
| `rejected` | `not_listed` | No |
| `cancelled` | `not_listed` | No |

`live_ending_soon` is selected by the shared helper when the remaining live window is at or below `AuctionScheduleHelper.LiveEndingSoonThreshold`.
