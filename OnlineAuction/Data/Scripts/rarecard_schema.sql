-- ============================================================
-- RareCard Online Auction — MySQL (XAMPP)
-- 7 business tables | No INSERT | utf8mb4
-- Apply via: dotnet ef database update
-- Identity tables (roles, user_claims, ...) created by EF migrations
-- ============================================================

CREATE DATABASE IF NOT EXISTS online_auction
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE online_auction;

-- Business tables: users, products, auctions, bids, orders, order_items, payments
--
-- FK relationships:
--   products.seller_id     -> users.id       (RESTRICT)
--   auctions.product_id    -> products.id    (RESTRICT)
--   auctions.winner_id     -> users.id       (SET NULL)
--   bids.auction_id        -> auctions.id    (RESTRICT)
--   bids.bidder_id         -> users.id       (RESTRICT)
--   orders.buyer_id        -> users.id       (RESTRICT)
--   order_items.order_id   -> orders.id      (CASCADE)
--   order_items.auction_id -> auctions.id    (RESTRICT)
--   payments.order_id      -> orders.id      (RESTRICT)
