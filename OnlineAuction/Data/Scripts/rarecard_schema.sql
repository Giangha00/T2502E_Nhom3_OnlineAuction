-- ============================================================
-- RareCard — 8 tables + audit fields
-- MySQL XAMPP | utf8mb4 | Apply via: dotnet ef database update
-- ============================================================

CREATE DATABASE IF NOT EXISTS online_auction
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE online_auction;

-- Business tables:
--   users, categories, products, auctions, bids, orders, order_items, payments
--
-- Key changes:
--   categories (renamed from product_types)
--   products.category_id -> categories.id
--   auctions.buy_now_price
--   bids.bid_type (manual | buy_now)
