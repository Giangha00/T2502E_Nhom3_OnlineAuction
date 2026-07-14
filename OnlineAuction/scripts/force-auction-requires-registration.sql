-- Registration is always required for Admin-created / auction listings; toggle removed.
-- Prefer applying EF migration: 20260714093000_ForceAuctionRequiresRegistration
-- Manual backfill (MySQL) if needed:

UPDATE `auctions`
SET `requires_registration` = TRUE
WHERE `requires_registration` = FALSE
  AND `deleted_at` IS NULL
  AND `listing_type` = 'auction';

-- Verify remaining false rows (should only be Buy Now / deleted):
-- SELECT id, listing_type, requires_registration, deleted_at
-- FROM auctions
-- WHERE requires_registration = FALSE;
