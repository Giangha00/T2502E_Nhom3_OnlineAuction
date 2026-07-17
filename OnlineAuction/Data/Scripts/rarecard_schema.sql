-- ============================================================
-- RareCard — Full schema for XAMPP / phpMyAdmin
-- MySQL 8+ | utf8mb4 | DROP + CREATE | No seed data
--
-- Includes:
--   ASP.NET Identity tables
--   9 business tables + audit fields
--   auction_registrations (buyer đăng ký tham gia đấu giá)
--
-- Fresh install: run this entire file in phpMyAdmin.
-- Existing DB: use rarecard_upgrade.sql instead (no data loss).
-- EF Core: dotnet ef database update
-- ============================================================

CREATE DATABASE IF NOT EXISTS online_auction
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE online_auction;

SET FOREIGN_KEY_CHECKS = 0;

-- Identity
DROP TABLE IF EXISTS user_tokens;
DROP TABLE IF EXISTS user_logins;
DROP TABLE IF EXISTS user_claims;
DROP TABLE IF EXISTS user_roles;
DROP TABLE IF EXISTS role_claims;

-- Business
DROP TABLE IF EXISTS auction_registrations;
DROP TABLE IF EXISTS payments;
DROP TABLE IF EXISTS order_items;
DROP TABLE IF EXISTS orders;
DROP TABLE IF EXISTS bids;
DROP TABLE IF EXISTS auctions;
DROP TABLE IF EXISTS products;
DROP TABLE IF EXISTS categories;

-- Users / roles last among dependent
DROP TABLE IF EXISTS roles;
DROP TABLE IF EXISTS users;

-- EF history (nếu từng chạy migration)
DROP TABLE IF EXISTS `__EFMigrationsHistory`;

SET FOREIGN_KEY_CHECKS = 1;

-- ============================================================
-- ASP.NET IDENTITY
-- ============================================================

CREATE TABLE roles (
    Id                  INT             NOT NULL AUTO_INCREMENT,
    Name                VARCHAR(256)    NULL,
    NormalizedName      VARCHAR(256)    NULL,
    ConcurrencyStamp    LONGTEXT        NULL,
    PRIMARY KEY (Id),
    UNIQUE KEY RoleNameIndex (NormalizedName)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE users (
    id                      INT             NOT NULL AUTO_INCREMENT,
    full_name               VARCHAR(120)    NOT NULL,
    email                   VARCHAR(160)    NOT NULL,
    normalized_email        VARCHAR(160)    NULL,
    phone_number            VARCHAR(20)     NOT NULL,
    username                VARCHAR(50)     NOT NULL,
    normalized_username     VARCHAR(50)     NULL,
    password_hash           VARCHAR(255)    NULL,
    security_stamp          VARCHAR(256)    NULL,
    concurrency_stamp       VARCHAR(256)    NULL,
    email_confirmed         TINYINT(1)      NOT NULL DEFAULT 0,
    phone_number_confirmed  TINYINT(1)      NOT NULL DEFAULT 0,
    two_factor_enabled      TINYINT(1)      NOT NULL DEFAULT 0,
    lockout_end             DATETIME(6)     NULL,
    lockout_enabled         TINYINT(1)      NOT NULL DEFAULT 0,
    access_failed_count     INT             NOT NULL DEFAULT 0,
    role                    TINYINT         NOT NULL DEFAULT 1 COMMENT '1=User, 2=Admin',
    status                  TINYINT         NOT NULL DEFAULT 1 COMMENT '1=active, 0=inactive',
    avatar_url              VARCHAR(260)    NULL,

    created_at              DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at              DATETIME        NULL ON UPDATE CURRENT_TIMESTAMP,
    created_by              INT             NULL,
    updated_by              INT             NULL,
    deleted_at              DATETIME        NULL,
    deleted_by              INT             NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_users_email (email),
    UNIQUE KEY uk_users_username (username),
    KEY EmailIndex (normalized_email),
    KEY UserNameIndex (normalized_username),
    KEY ix_users_role (role),
    KEY ix_users_deleted_at (deleted_at),

    CONSTRAINT fk_users_created_by
        FOREIGN KEY (created_by) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT fk_users_updated_by
        FOREIGN KEY (updated_by) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT fk_users_deleted_by
        FOREIGN KEY (deleted_by) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE role_claims (
    Id          INT         NOT NULL AUTO_INCREMENT,
    RoleId      INT         NOT NULL,
    ClaimType   LONGTEXT    NULL,
    ClaimValue  LONGTEXT    NULL,
    PRIMARY KEY (Id),
    KEY IX_role_claims_RoleId (RoleId),
    CONSTRAINT FK_role_claims_roles_RoleId
        FOREIGN KEY (RoleId) REFERENCES roles (Id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE user_claims (
    Id          INT         NOT NULL AUTO_INCREMENT,
    UserId      INT         NOT NULL,
    ClaimType   LONGTEXT    NULL,
    ClaimValue  LONGTEXT    NULL,
    PRIMARY KEY (Id),
    KEY IX_user_claims_UserId (UserId),
    CONSTRAINT FK_user_claims_users_UserId
        FOREIGN KEY (UserId) REFERENCES users (id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE user_logins (
    LoginProvider           VARCHAR(255)    NOT NULL,
    ProviderKey             VARCHAR(255)    NOT NULL,
    ProviderDisplayName     LONGTEXT        NULL,
    UserId                  INT             NOT NULL,
    PRIMARY KEY (LoginProvider, ProviderKey),
    KEY IX_user_logins_UserId (UserId),
    CONSTRAINT FK_user_logins_users_UserId
        FOREIGN KEY (UserId) REFERENCES users (id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE user_roles (
    UserId      INT     NOT NULL,
    RoleId      INT     NOT NULL,
    PRIMARY KEY (UserId, RoleId),
    KEY IX_user_roles_RoleId (RoleId),
    CONSTRAINT FK_user_roles_roles_RoleId
        FOREIGN KEY (RoleId) REFERENCES roles (Id)
        ON DELETE CASCADE,
    CONSTRAINT FK_user_roles_users_UserId
        FOREIGN KEY (UserId) REFERENCES users (id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE user_tokens (
    UserId          INT             NOT NULL,
    LoginProvider   VARCHAR(255)    NOT NULL,
    Name            VARCHAR(255)    NOT NULL,
    Value           LONGTEXT        NULL,
    PRIMARY KEY (UserId, LoginProvider, Name),
    CONSTRAINT FK_user_tokens_users_UserId
        FOREIGN KEY (UserId) REFERENCES users (id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
-- 1. CATEGORIES
-- ============================================================
CREATE TABLE categories (
    id              INT             NOT NULL AUTO_INCREMENT,
    name            VARCHAR(50)     NOT NULL,
    slug            VARCHAR(60)     NOT NULL,
    sort_order      INT             NOT NULL DEFAULT 0,
    is_active       TINYINT(1)      NOT NULL DEFAULT 1,

    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NULL ON UPDATE CURRENT_TIMESTAMP,
    created_by      INT             NULL,
    updated_by      INT             NULL,
    deleted_at      DATETIME        NULL,
    deleted_by      INT             NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_categories_name (name),
    UNIQUE KEY uk_categories_slug (slug),
    KEY ix_categories_deleted_at (deleted_at),

    CONSTRAINT fk_categories_created_by
        FOREIGN KEY (created_by) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT fk_categories_updated_by
        FOREIGN KEY (updated_by) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT fk_categories_deleted_by
        FOREIGN KEY (deleted_by) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
-- 2. PRODUCTS
-- ============================================================
CREATE TABLE products (
    id                  INT             NOT NULL AUTO_INCREMENT,
    seller_id           INT             NOT NULL,
    category_id         INT             NOT NULL,
    name                VARCHAR(120)    NOT NULL,
    short_description   VARCHAR(300)    NULL,
    description_html    TEXT            NULL,
    condition           VARCHAR(20)     NOT NULL DEFAULT 'graded',
    year                INT             NULL,
    set_name            VARCHAR(120)    NULL,
    grade_label         VARCHAR(20)     NULL,
    cert_number         VARCHAR(50)     NULL,
    primary_image       VARCHAR(500)    NOT NULL,
    import_price        DECIMAL(18,2)   NULL COMMENT 'Giá nhập — seller, không public',

    created_at          DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at          DATETIME        NULL ON UPDATE CURRENT_TIMESTAMP,
    created_by          INT             NULL,
    updated_by          INT             NULL,
    deleted_at          DATETIME        NULL,
    deleted_by          INT             NULL,

    PRIMARY KEY (id),
    KEY ix_products_seller_id (seller_id),
    KEY ix_products_category_id (category_id),
    KEY ix_products_deleted_at (deleted_at),

    CONSTRAINT fk_products_seller
        FOREIGN KEY (seller_id) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE RESTRICT,
    CONSTRAINT fk_products_category
        FOREIGN KEY (category_id) REFERENCES categories (id)
        ON UPDATE CASCADE ON DELETE RESTRICT,
    CONSTRAINT fk_products_created_by
        FOREIGN KEY (created_by) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT fk_products_updated_by
        FOREIGN KEY (updated_by) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT fk_products_deleted_by
        FOREIGN KEY (deleted_by) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT chk_products_import_price
        CHECK (import_price IS NULL OR import_price >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
-- 3. AUCTIONS (listing: auction | buynow)
-- ============================================================
CREATE TABLE auctions (
    id                      INT             NOT NULL AUTO_INCREMENT,
    product_id              INT             NOT NULL,
    starting_price          DECIMAL(18,2)   NOT NULL,
    bid_step                DECIMAL(18,2)   NOT NULL,
    current_price           DECIMAL(18,2)   NOT NULL,
    buy_now_price           DECIMAL(18,2)   NULL COMMENT 'NULL = không bật Buy Now trên listing auction',
    listing_type            VARCHAR(20)     NOT NULL DEFAULT 'auction' COMMENT 'auction | buynow',
    requires_registration   TINYINT(1)      NOT NULL DEFAULT 1 COMMENT '1=phải đăng ký trước khi bid',
    status                  VARCHAR(20)     NOT NULL DEFAULT 'live',
    start_date              DATETIME        NOT NULL,
    end_date                DATETIME        NOT NULL,
    winner_id               INT             NULL,

    created_at              DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at              DATETIME        NULL ON UPDATE CURRENT_TIMESTAMP,
    created_by              INT             NULL,
    updated_by              INT             NULL,
    deleted_at              DATETIME        NULL,
    deleted_by              INT             NULL,

    PRIMARY KEY (id),
    KEY ix_auctions_product_id (product_id),
    KEY ix_auctions_status_end_date (status, end_date),
    KEY ix_auctions_listing_type (listing_type),
    KEY ix_auctions_deleted_at (deleted_at),

    CONSTRAINT fk_auctions_product
        FOREIGN KEY (product_id) REFERENCES products (id)
        ON UPDATE CASCADE ON DELETE RESTRICT,
    CONSTRAINT fk_auctions_winner
        FOREIGN KEY (winner_id) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT fk_auctions_created_by
        FOREIGN KEY (created_by) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT fk_auctions_updated_by
        FOREIGN KEY (updated_by) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT fk_auctions_deleted_by
        FOREIGN KEY (deleted_by) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT chk_auctions_listing_type
        CHECK (listing_type IN ('auction', 'buynow')),
    CONSTRAINT chk_auctions_prices
        CHECK (
            starting_price > 0
            AND bid_step > 0
            AND current_price >= 0
            AND (buy_now_price IS NULL OR buy_now_price > starting_price)
        ),
    CONSTRAINT chk_auctions_dates
        CHECK (end_date > start_date)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
-- 4. AUCTION_REGISTRATIONS (buyer đăng ký tham gia)
-- ============================================================
CREATE TABLE auction_registrations (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    auction_id      INT             NOT NULL,
    user_id         INT             NOT NULL,
    status          VARCHAR(20)     NOT NULL DEFAULT 'pending'
        COMMENT 'pending | approved | rejected | cancelled',
    registered_at   DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    reviewed_at     DATETIME        NULL,
    reviewed_by     INT             NULL COMMENT 'admin duyệt',
    reject_reason   VARCHAR(300)    NULL,

    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NULL ON UPDATE CURRENT_TIMESTAMP,
    created_by      INT             NULL,
    updated_by      INT             NULL,
    deleted_at      DATETIME        NULL,
    deleted_by      INT             NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_registrations_auction_user (auction_id, user_id),
    KEY ix_registrations_auction_status (auction_id, status),
    KEY ix_registrations_user_status (user_id, status),
    KEY ix_registrations_deleted_at (deleted_at),

    CONSTRAINT fk_registrations_auction
        FOREIGN KEY (auction_id) REFERENCES auctions (id)
        ON UPDATE CASCADE ON DELETE RESTRICT,
    CONSTRAINT fk_registrations_user
        FOREIGN KEY (user_id) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE RESTRICT,
    CONSTRAINT fk_registrations_reviewed_by
        FOREIGN KEY (reviewed_by) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT fk_registrations_created_by
        FOREIGN KEY (created_by) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT fk_registrations_updated_by
        FOREIGN KEY (updated_by) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT fk_registrations_deleted_by
        FOREIGN KEY (deleted_by) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT chk_registrations_status
        CHECK (status IN ('pending', 'approved', 'rejected', 'cancelled'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
-- 5. BIDS
-- ============================================================
CREATE TABLE bids (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    auction_id      INT             NOT NULL,
    bidder_id       INT             NOT NULL,
    amount          DECIMAL(18,2)   NOT NULL,
    bid_type        VARCHAR(20)     NOT NULL DEFAULT 'manual' COMMENT 'manual | buy_now',
    is_winning      TINYINT(1)      NOT NULL DEFAULT 0,
    placed_at       DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,

    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NULL ON UPDATE CURRENT_TIMESTAMP,
    created_by      INT             NULL,
    updated_by      INT             NULL,
    deleted_at      DATETIME        NULL,
    deleted_by      INT             NULL,

    PRIMARY KEY (id),
    KEY ix_bids_auction_placed_at (auction_id, placed_at),
    KEY ix_bids_bidder_id (bidder_id),
    KEY ix_bids_deleted_at (deleted_at),

    CONSTRAINT fk_bids_auction
        FOREIGN KEY (auction_id) REFERENCES auctions (id)
        ON UPDATE CASCADE ON DELETE RESTRICT,
    CONSTRAINT fk_bids_bidder
        FOREIGN KEY (bidder_id) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE RESTRICT,
    CONSTRAINT fk_bids_created_by
        FOREIGN KEY (created_by) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT fk_bids_updated_by
        FOREIGN KEY (updated_by) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT fk_bids_deleted_by
        FOREIGN KEY (deleted_by) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT chk_bids_amount
        CHECK (amount > 0),
    CONSTRAINT chk_bids_bid_type
        CHECK (bid_type IN ('manual', 'buy_now'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
-- 6. ORDERS
-- ============================================================
CREATE TABLE orders (
    id                  INT             NOT NULL AUTO_INCREMENT,
    order_reference     VARCHAR(30)     NOT NULL,
    buyer_id            INT             NOT NULL,
    subtotal            DECIMAL(18,2)   NOT NULL,
    shipping_fee        DECIMAL(18,2)   NOT NULL DEFAULT 45.00,
    vault_insurance     DECIMAL(18,2)   NOT NULL COMMENT 'MAX(60, subtotal * 0.00721)',
    total_amount        DECIMAL(18,2)   NOT NULL,
    status              VARCHAR(20)     NOT NULL DEFAULT 'pending_payment',
    payment_deadline    DATETIME        NOT NULL,

    created_at          DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at          DATETIME        NULL ON UPDATE CURRENT_TIMESTAMP,
    created_by          INT             NULL,
    updated_by          INT             NULL,
    deleted_at          DATETIME        NULL,
    deleted_by          INT             NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_orders_reference (order_reference),
    KEY ix_orders_buyer_status (buyer_id, status),
    KEY ix_orders_deleted_at (deleted_at),

    CONSTRAINT fk_orders_buyer
        FOREIGN KEY (buyer_id) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE RESTRICT,
    CONSTRAINT fk_orders_created_by
        FOREIGN KEY (created_by) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT fk_orders_updated_by
        FOREIGN KEY (updated_by) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT fk_orders_deleted_by
        FOREIGN KEY (deleted_by) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT chk_orders_amounts
        CHECK (subtotal > 0 AND total_amount >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
-- 7. ORDER_ITEMS
-- ============================================================
CREATE TABLE order_items (
    id              INT             NOT NULL AUTO_INCREMENT,
    order_id        INT             NOT NULL,
    auction_id      INT             NOT NULL,
    item_name       VARCHAR(160)    NOT NULL,
    item_grade      VARCHAR(20)     NULL,
    item_image_url  VARCHAR(500)    NULL,
    winning_bid     DECIMAL(18,2)   NOT NULL,

    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NULL ON UPDATE CURRENT_TIMESTAMP,
    created_by      INT             NULL,
    updated_by      INT             NULL,
    deleted_at      DATETIME        NULL,
    deleted_by      INT             NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_order_items_order_auction (order_id, auction_id),
    KEY ix_order_items_deleted_at (deleted_at),

    CONSTRAINT fk_order_items_order
        FOREIGN KEY (order_id) REFERENCES orders (id)
        ON UPDATE CASCADE ON DELETE CASCADE,
    CONSTRAINT fk_order_items_auction
        FOREIGN KEY (auction_id) REFERENCES auctions (id)
        ON UPDATE CASCADE ON DELETE RESTRICT,
    CONSTRAINT fk_order_items_created_by
        FOREIGN KEY (created_by) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT fk_order_items_updated_by
        FOREIGN KEY (updated_by) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT fk_order_items_deleted_by
        FOREIGN KEY (deleted_by) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT chk_order_items_winning_bid
        CHECK (winning_bid > 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
-- 8. PAYMENTS
-- ============================================================
CREATE TABLE payments (
    id              INT             NOT NULL AUTO_INCREMENT,
    order_id        INT             NOT NULL,
    amount          DECIMAL(18,2)   NOT NULL,
    status          VARCHAR(20)     NOT NULL DEFAULT 'pending',
    transaction_id  VARCHAR(100)    NULL COMMENT 'PayPal Capture ID',
    paid_at         DATETIME        NULL,

    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NULL ON UPDATE CURRENT_TIMESTAMP,
    created_by      INT             NULL,
    updated_by      INT             NULL,
    deleted_at      DATETIME        NULL,
    deleted_by      INT             NULL,

    PRIMARY KEY (id),
    KEY ix_payments_order_id (order_id),
    KEY ix_payments_status (status),
    KEY ix_payments_transaction_id (transaction_id),
    KEY ix_payments_deleted_at (deleted_at),

    CONSTRAINT fk_payments_order
        FOREIGN KEY (order_id) REFERENCES orders (id)
        ON UPDATE CASCADE ON DELETE RESTRICT,
    CONSTRAINT fk_payments_created_by
        FOREIGN KEY (created_by) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT fk_payments_updated_by
        FOREIGN KEY (updated_by) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT fk_payments_deleted_by
        FOREIGN KEY (deleted_by) REFERENCES users (id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT chk_payments_amount
        CHECK (amount > 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
