# Product Documents (Certificates)

This document describes how certificate and verification PDFs are stored, displayed, and downloaded in CardMarket.

## Data model

Table: `product_documents`

| Column | Description |
|--------|-------------|
| `id` | Primary key |
| `product_id` | FK to `products` |
| `name` | Display name (e.g. PSA Certificate, BGS Certificate) |
| `file_url` | Cloudinary URL |
| `file_type` | Usually `PDF` |
| `deleted_at` | Soft delete timestamp |

Entity: `ProductDocument` (`Entities/ProductDocument.cs`)

All read paths filter `deleted_at IS NULL` and order by `name`.

## Upload flows

### Seller (Create Auction / Buy Now)

- Forms: `/Sell/Create`, `/Sell/BuyNow`
- Partial: `Views/Sell/Partials/_DocumentUploader.cshtml`
- JS: `wwwroot/js/create-auction.js`, `create-buy-now.js`
- Validation: PDF only, max 5MB per file, max 5 documents per product
- Document name: seller selects type (PSA Certificate, BGS Certificate, Product Verification, Warranty) → stored in `DocumentNames` → `ProductDocument.Name`
- Storage: `PhotoService.AddPhotoAsync` → Cloudinary folder `auction-house/documents`

### Admin (Create / Edit Product)

- Form: `Areas/Admin/Views/Product/_Form.cshtml`
- Service: `AdminProductService`
- Same validation and Cloudinary folder as seller
- Soft delete: tick **Remove** on existing document → sets `deleted_at`
- Max 5 active documents per product (existing + new uploads)

## Public display

- Auction detail: tab **Certificates & Documents** in `_ProductDetailTabs.cshtml` (only when documents exist)
- Buy Now detail: same tab layout under `Views/BuyNow/Partials/_ProductDetailTabs.cshtml`
- List partial: `Views/Auction/Partials/_DocumentSection.cshtml`
  - **View** → opens Cloudinary URL in new tab
  - **Download** → `GET /ProductDocument/Download/{id}`

Documents are **not** shown in the About tab (moved to dedicated Certificates tab).

## Download authorization

Service: `ProductDocumentDownloadService`

| Caller | Rule |
|--------|------|
| Anonymous / logged-in user | Allowed when product has at least one auction with status: `live`, `ending_soon`, `ended`, `awaiting_payment`, `completed` |
| Pending review / rejected | Denied (403) for public download |
| Admin | Allowed for any non-deleted document (`isAdminRequest: true`) |

Endpoints:

- Public: `GET /ProductDocument/Download/{id}`
- Admin: `GET /Admin/Product/DownloadDocument/{id}` (requires `ProductsManage`)

Cloudinary downloads use `fl_attachment/` transformation for a proper attachment filename.

## Admin review

- **Product Details** (`/Admin/Product/Details/{id}`): table with View, Download, Preview (iframe modal)
- **Auction Verification** (`/Admin/AuctionVerification/Details/{id}`): same document actions for pre-approval review

Shared partial: `Areas/Admin/Views/Shared/_ProductDocumentsSection.cshtml`

## i18n keys

- `Product_Certificates_Tab`
- `Product_Documents_Empty`
- `Product_DownloadCertificate`
- `Admin_Documents_Preview`
- `Admin_Documents_Section`

## Tests

`OnlineAuction.Tests/ProductDocumentDownloadTests.cs` covers:

1. Live auction → public download allowed
2. Pending review → public download denied
3. Deleted document → 404
4. Admin → download allowed regardless of auction status

## Out of scope (this sprint)

- OCR / automatic certificate parsing
- Third-party PSA/BGS API verification
- Deleting files from Cloudinary on soft delete
