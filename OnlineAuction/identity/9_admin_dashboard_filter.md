# Admin Dashboard — Date Filter & Excel Export

## Overview

The admin dashboard at `/Admin/Dashboard` supports a shared date range for time-based KPIs, charts, and Excel export.

## Filter behaviour

| Item | Detail |
|------|--------|
| URL params | `?dateFrom=yyyy-MM-dd&dateTo=yyyy-MM-dd` |
| Default range | Last **7 days** (inclusive) when params are omitted |
| Max range | **365 days** |
| Validation | `dateFrom` must be ≤ `dateTo`; range must not exceed 365 days |
| Invalid input | Dashboard shows an error banner; metrics are **not** loaded |

Legacy `dateRange=MM/dd/yyyy - MM/dd/yyyy` links are still parsed for backward compatibility, but new UI uses separate `dateFrom` / `dateTo` fields.

## Presets

- **Last 7 days**
- **Last 30 days**
- **This month**
- **Last month**

Presets fill the date inputs; click **Apply** to reload the dashboard.

## Export Excel

- Route: `GET /Admin/Dashboard/Export?dateFrom=...&dateTo=...`
- File name: `dashboard-report-{from}-{to}.xlsx`
- Sheets:
  1. **Overview** — filter period, UTC generation timestamp, KPI summary
  2. **Revenue** — revenue summary + successful payment / listing-fee rows in range
  3. **Auctions** — status snapshot, category bid breakdown, auctions created in range

Export uses the same validation rules as the dashboard filter.

## Tests

Unit tests live in `OnlineAuction.Tests/DashboardFilterValidatorTests.cs` and cover inverted dates and the 365-day limit.

## Permissions

Requires `DashboardView` permission (same as viewing the dashboard).
