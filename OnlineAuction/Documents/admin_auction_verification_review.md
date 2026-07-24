# Admin Auction Verification — Sell → Review field map

Scope: `/Admin/AuctionVerification/Details` (and light Index polish).  
Sell sources: `/Sell/Create` (Auction), `/Sell/BuyNow`.

## Always shown (Product as submitted)

| Sell field | Review field | Notes |
|---|---|---|
| Primary + Gallery images | Hero media + thumbs + lightbox | Click to enlarge |
| Name | ProductName | Hero title |
| Subtitle | Subtitle | Added to ViewModel / UI |
| Category | CategoryName | Meta line |
| Condition | Condition | Meta line |
| Short description | ShortDescription | Under title |
| Full description (HTML) | DescriptionHtml | Collapsible; collapsed by default when long |
| Year | Year | Card details grid |
| Set | SetName | |
| Language | Language | |
| Card # | CardNumber | |
| Grade / Authenticator | GradeLabel | Combined label as stored on product |
| Cert # | CertNumber | |
| Documents / PDFs | Documents | View / download |
| Seller | SellerName, SellerEmail, User Details link | Compact in Decision column |

## Auction-only

| Sell field | Review field | Notes |
|---|---|---|
| Starting price | StartingPrice | |
| Bid step | BidStep | |
| Optional Buy now price | BuyNowPrice | Only if set |
| Registration Start / End | RegistrationStartDate / RegistrationEndDate | Timeline step 1 |
| Live Start / End | StartDate / EndDate | Timeline step 2 |
| Listing type | Badge **Auction** | Header |

## Buy Now-only

| Sell field | Review field | Notes |
|---|---|---|
| Price | BuyNowPrice (fallback StartingPrice) | Primary term |
| Listing type | Badge **Buy Now** | Header |

**Hidden on Buy Now review:** Bid step, Registration dates, Event, “Registration required”, auction-style schedule.

**Buy Now EndDate:** Seller form does **not** collect EndDate. Backend sets a long system availability window (`now + 1 year`). Review does **not** show EndDate as a seller-submitted field.

## Hidden / conditional (avoid surplus)

| Field | Rule |
|---|---|
| Centering / Corners / Edges / Surface | Hide entire sub-grades block unless any value is present |
| AuctionEventName | Hide if null / whitespace |
| RequiresRegistration | Not shown as a separate row (auction policy always requires registration) |
| Status `confirming` / pending review | Header badge only |

## Manual test cases

| ID | Case | Expected |
|---|---|---|
| REV-01 | Auction from `/Sell/Create` | Specs + Reg + Live dates; no empty Grading 4-box section |
| REV-02 | Buy Now listing | Price only; no Bid step / Reg schedule |
| REV-03 | Subtitle on Sell | Shown under product name |
| REV-04 | No Event name | No Event row |
| REV-05 | Gallery | Lightbox / thumb switch works |
| REV-06 | Long description | Collapsed by default; expandable |
| REV-07 | Approve / Reject | Still works; Reject min 10 chars |
| REV-08 | Mobile | Decision panel above content; media usable |

## Out of scope

- Approve / Reject backend rules  
- Sell client form changes  
- Admin Auction Create sync  
