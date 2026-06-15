-- ============================================================
-- RareCard Online Auction — MySQL Schema (XAMPP)
-- 6 core business tables | utf8mb4
-- Run via EF migration: dotnet ef database update
-- ============================================================

CREATE DATABASE IF NOT EXISTS online_auction
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE online_auction;

-- Identity support tables (roles, user_claims, user_logins, user_roles, user_tokens, role_claims)
-- are created automatically by ASP.NET Core Identity migrations.

-- Core tables created by migration RareCardSchema:
--   users, products, auctions, bids, orders, order_items
--
-- Foreign keys preserved:
--   products.seller_id        -> users.id          (RESTRICT)
--   auctions.product_id       -> products.id       (RESTRICT)
--   auctions.winner_id        -> users.id          (SET NULL)
--   bids.auction_id           -> auctions.id       (RESTRICT)
--   bids.bidder_id            -> users.id          (RESTRICT)
--   orders.buyer_id           -> users.id          (RESTRICT)
--   order_items.order_id      -> orders.id         (CASCADE)
--   order_items.auction_id    -> auctions.id       (RESTRICT)
