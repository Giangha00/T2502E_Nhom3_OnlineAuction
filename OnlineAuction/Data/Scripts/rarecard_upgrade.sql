-- ============================================================
-- RareCard — Incremental upgrade (giữ nguyên dữ liệu + FK)
-- Chạy trên DB online_auction đã có sẵn từ migration trước.
-- Không DROP bảng. Chỉ ADD cột/bảng/index/constraint mới.
-- ============================================================

USE online_auction;

-- 1. auctions.requires_registration (mặc định 1 = phải đăng ký trước khi bid)
SET @col_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'auctions'
      AND COLUMN_NAME = 'requires_registration'
);
SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE auctions ADD COLUMN requires_registration TINYINT(1) NOT NULL DEFAULT 1 COMMENT ''1=phải đăng ký trước khi bid'' AFTER listing_type',
    'SELECT ''auctions.requires_registration already exists'' AS info'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- 2. Index listing_type
SET @idx_exists := (
    SELECT COUNT(*)
    FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'auctions'
      AND INDEX_NAME = 'ix_auctions_listing_type'
);
SET @sql := IF(
    @idx_exists = 0,
    'CREATE INDEX ix_auctions_listing_type ON auctions (listing_type)',
    'SELECT ''ix_auctions_listing_type already exists'' AS info'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- 3. Check constraint listing_type (MySQL 8+)
SET @chk_exists := (
    SELECT COUNT(*)
    FROM information_schema.TABLE_CONSTRAINTS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'auctions'
      AND CONSTRAINT_NAME = 'chk_auctions_listing_type'
);
SET @sql := IF(
    @chk_exists = 0,
    'ALTER TABLE auctions ADD CONSTRAINT chk_auctions_listing_type CHECK (listing_type IN (''auction'', ''buynow''))',
    'SELECT ''chk_auctions_listing_type already exists'' AS info'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- 4. Bảng auction_registrations (FK → auctions, users)
CREATE TABLE IF NOT EXISTS auction_registrations (
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
