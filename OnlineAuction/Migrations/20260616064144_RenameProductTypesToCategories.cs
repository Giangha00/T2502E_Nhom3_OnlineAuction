using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineAuction.Migrations
{
    /// <inheritdoc />
    public partial class RenameProductTypesToCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET @db = DATABASE();

                -- Drop products FK pointing at product_types (any constraint name)
                SET @fk = (
                    SELECT CONSTRAINT_NAME
                    FROM information_schema.KEY_COLUMN_USAGE
                    WHERE TABLE_SCHEMA = @db
                      AND TABLE_NAME = 'products'
                      AND COLUMN_NAME = 'product_type_id'
                      AND REFERENCED_TABLE_NAME IS NOT NULL
                    LIMIT 1
                );
                SET @sql = IF(
                    @fk IS NOT NULL,
                    CONCAT('ALTER TABLE `products` DROP FOREIGN KEY `', @fk, '`'),
                    'SELECT 1'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;

                -- Rename product_types -> categories when needed
                SET @pt = (
                    SELECT COUNT(*)
                    FROM information_schema.TABLES
                    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'product_types'
                );
                SET @cat = (
                    SELECT COUNT(*)
                    FROM information_schema.TABLES
                    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'categories'
                );
                SET @sql = IF(
                    @pt > 0 AND @cat = 0,
                    'RENAME TABLE `product_types` TO `categories`',
                    'SELECT 1'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;

                -- Rename audit FKs on categories (old product_types names)
                SET @fk = (
                    SELECT COUNT(*)
                    FROM information_schema.TABLE_CONSTRAINTS
                    WHERE TABLE_SCHEMA = @db
                      AND TABLE_NAME = 'categories'
                      AND CONSTRAINT_NAME = 'fk_product_types_created_by'
                );
                SET @sql = IF(
                    @fk > 0,
                    'ALTER TABLE `categories`
                        DROP FOREIGN KEY `fk_product_types_created_by`,
                        DROP FOREIGN KEY `fk_product_types_deleted_by`,
                        DROP FOREIGN KEY `fk_product_types_updated_by`',
                    'SELECT 1'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;

                SET @fk = (
                    SELECT COUNT(*)
                    FROM information_schema.TABLE_CONSTRAINTS
                    WHERE TABLE_SCHEMA = @db
                      AND TABLE_NAME = 'categories'
                      AND CONSTRAINT_NAME = 'fk_categories_created_by'
                );
                SET @sql = IF(
                    @fk = 0 AND @cat + @pt > 0,
                    'ALTER TABLE `categories`
                        ADD CONSTRAINT `fk_categories_created_by`
                            FOREIGN KEY (`created_by`) REFERENCES `users` (`id`)
                            ON UPDATE CASCADE ON DELETE SET NULL,
                        ADD CONSTRAINT `fk_categories_deleted_by`
                            FOREIGN KEY (`deleted_by`) REFERENCES `users` (`id`)
                            ON UPDATE CASCADE ON DELETE SET NULL,
                        ADD CONSTRAINT `fk_categories_updated_by`
                            FOREIGN KEY (`updated_by`) REFERENCES `users` (`id`)
                            ON UPDATE CASCADE ON DELETE SET NULL',
                    'SELECT 1'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;

                -- Rename category indexes (MariaDB: drop old + add new, no RENAME INDEX)
                SET @old = (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'categories' AND INDEX_NAME = 'uk_product_types_name');
                SET @new = (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'categories' AND INDEX_NAME = 'uk_categories_name');
                SET @sql = IF(@old > 0 AND @new = 0, 'ALTER TABLE `categories` DROP INDEX `uk_product_types_name`, ADD UNIQUE INDEX `uk_categories_name` (`name`)', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @old = (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'categories' AND INDEX_NAME = 'uk_product_types_slug');
                SET @new = (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'categories' AND INDEX_NAME = 'uk_categories_slug');
                SET @sql = IF(@old > 0 AND @new = 0, 'ALTER TABLE `categories` DROP INDEX `uk_product_types_slug`, ADD UNIQUE INDEX `uk_categories_slug` (`slug`)', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @old = (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'categories' AND INDEX_NAME = 'ix_product_types_deleted_at');
                SET @new = (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'categories' AND INDEX_NAME = 'ix_categories_deleted_at');
                SET @sql = IF(@old > 0 AND @new = 0, 'ALTER TABLE `categories` DROP INDEX `ix_product_types_deleted_at`, ADD INDEX `ix_categories_deleted_at` (`deleted_at`)', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @old = (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'categories' AND INDEX_NAME = 'IX_product_types_created_by');
                SET @new = (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'categories' AND INDEX_NAME = 'IX_categories_created_by');
                SET @sql = IF(@old > 0 AND @new = 0, 'ALTER TABLE `categories` DROP INDEX `IX_product_types_created_by`, ADD INDEX `IX_categories_created_by` (`created_by`)', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @old = (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'categories' AND INDEX_NAME = 'IX_product_types_deleted_by');
                SET @new = (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'categories' AND INDEX_NAME = 'IX_categories_deleted_by');
                SET @sql = IF(@old > 0 AND @new = 0, 'ALTER TABLE `categories` DROP INDEX `IX_product_types_deleted_by`, ADD INDEX `IX_categories_deleted_by` (`deleted_by`)', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @old = (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'categories' AND INDEX_NAME = 'IX_product_types_updated_by');
                SET @new = (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'categories' AND INDEX_NAME = 'IX_categories_updated_by');
                SET @sql = IF(@old > 0 AND @new = 0, 'ALTER TABLE `categories` DROP INDEX `IX_product_types_updated_by`, ADD INDEX `IX_categories_updated_by` (`updated_by`)', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                -- product_type_id -> category_id
                SET @col_pt = (
                    SELECT COUNT(*)
                    FROM information_schema.COLUMNS
                    WHERE TABLE_SCHEMA = @db
                      AND TABLE_NAME = 'products'
                      AND COLUMN_NAME = 'product_type_id'
                );
                SET @col_cat = (
                    SELECT COUNT(*)
                    FROM information_schema.COLUMNS
                    WHERE TABLE_SCHEMA = @db
                      AND TABLE_NAME = 'products'
                      AND COLUMN_NAME = 'category_id'
                );
                SET @sql = IF(
                    @col_pt > 0 AND @col_cat = 0,
                    'ALTER TABLE `products` CHANGE `product_type_id` `category_id` INT NOT NULL',
                    'SELECT 1'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;

                SET @idx = (
                    SELECT COUNT(*)
                    FROM information_schema.STATISTICS
                    WHERE TABLE_SCHEMA = @db
                      AND TABLE_NAME = 'products'
                      AND INDEX_NAME = 'ix_products_product_type_id'
                );
                SET @sql = IF(
                    @idx > 0,
                    'ALTER TABLE `products` DROP INDEX `ix_products_product_type_id`',
                    'SELECT 1'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;

                SET @idx = (
                    SELECT COUNT(*)
                    FROM information_schema.STATISTICS
                    WHERE TABLE_SCHEMA = @db
                      AND TABLE_NAME = 'products'
                      AND INDEX_NAME = 'ix_products_category_id'
                );
                SET @sql = IF(
                    @idx = 0,
                    'CREATE INDEX `ix_products_category_id` ON `products` (`category_id`)',
                    'SELECT 1'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;

                SET @fk = (
                    SELECT COUNT(*)
                    FROM information_schema.TABLE_CONSTRAINTS
                    WHERE TABLE_SCHEMA = @db
                      AND TABLE_NAME = 'products'
                      AND CONSTRAINT_NAME = 'fk_products_category'
                );
                SET @sql = IF(
                    @fk = 0,
                    'ALTER TABLE `products`
                        ADD CONSTRAINT `fk_products_category`
                            FOREIGN KEY (`category_id`) REFERENCES `categories` (`id`)
                            ON UPDATE CASCADE ON DELETE RESTRICT',
                    'SELECT 1'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;

                -- auctions.buy_now_price
                SET @col = (
                    SELECT COUNT(*)
                    FROM information_schema.COLUMNS
                    WHERE TABLE_SCHEMA = @db
                      AND TABLE_NAME = 'auctions'
                      AND COLUMN_NAME = 'buy_now_price'
                );
                SET @sql = IF(
                    @col = 0,
                    'ALTER TABLE `auctions` ADD COLUMN `buy_now_price` DECIMAL(18,2) NULL',
                    'SELECT 1'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;

                SET @chk = (
                    SELECT COUNT(*)
                    FROM information_schema.TABLE_CONSTRAINTS
                    WHERE TABLE_SCHEMA = @db
                      AND TABLE_NAME = 'auctions'
                      AND CONSTRAINT_NAME = 'chk_auctions_prices'
                );
                SET @sql = IF(
                    @chk > 0,
                    'ALTER TABLE `auctions` DROP CONSTRAINT `chk_auctions_prices`',
                    'SELECT 1'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;

                SET @chk = (
                    SELECT COUNT(*)
                    FROM information_schema.TABLE_CONSTRAINTS
                    WHERE TABLE_SCHEMA = @db
                      AND TABLE_NAME = 'auctions'
                      AND CONSTRAINT_NAME = 'chk_auctions_prices'
                );
                SET @sql = IF(
                    @chk = 0,
                    'ALTER TABLE `auctions`
                        ADD CONSTRAINT `chk_auctions_prices`
                        CHECK (
                            `starting_price` > 0
                            AND `bid_step` > 0
                            AND `current_price` >= 0
                            AND (`buy_now_price` IS NULL OR `buy_now_price` > `starting_price`)
                        )',
                    'SELECT 1'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;

                -- bids.bid_type
                SET @col = (
                    SELECT COUNT(*)
                    FROM information_schema.COLUMNS
                    WHERE TABLE_SCHEMA = @db
                      AND TABLE_NAME = 'bids'
                      AND COLUMN_NAME = 'bid_type'
                );
                SET @sql = IF(
                    @col = 0,
                    'ALTER TABLE `bids`
                        ADD COLUMN `bid_type` VARCHAR(20) NOT NULL DEFAULT ''manual''',
                    'SELECT 1'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;

                SET @chk = (
                    SELECT COUNT(*)
                    FROM information_schema.TABLE_CONSTRAINTS
                    WHERE TABLE_SCHEMA = @db
                      AND TABLE_NAME = 'bids'
                      AND CONSTRAINT_NAME = 'chk_bids_bid_type'
                );
                SET @sql = IF(
                    @chk = 0,
                    'ALTER TABLE `bids`
                        ADD CONSTRAINT `chk_bids_bid_type`
                        CHECK (`bid_type` IN (''manual'', ''buy_now''))',
                    'SELECT 1'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "chk_bids_bid_type",
                table: "bids");

            migrationBuilder.DropColumn(
                name: "bid_type",
                table: "bids");

            migrationBuilder.DropCheckConstraint(
                name: "chk_auctions_prices",
                table: "auctions");

            migrationBuilder.DropColumn(
                name: "buy_now_price",
                table: "auctions");

            migrationBuilder.AddCheckConstraint(
                name: "chk_auctions_prices",
                table: "auctions",
                sql: "`starting_price` > 0 AND `bid_step` > 0 AND `current_price` >= 0");

            migrationBuilder.Sql("ALTER TABLE `products` DROP FOREIGN KEY `fk_products_category`;");
            migrationBuilder.Sql("ALTER TABLE `products` DROP INDEX `ix_products_category_id`;");
            migrationBuilder.Sql("CREATE INDEX `ix_products_product_type_id` ON `products` (`category_id`);");
            migrationBuilder.Sql("ALTER TABLE `products` CHANGE `category_id` `product_type_id` INT NOT NULL;");

            migrationBuilder.Sql(@"
                ALTER TABLE `categories`
                    RENAME INDEX `uk_categories_name` TO `uk_product_types_name`,
                    RENAME INDEX `uk_categories_slug` TO `uk_product_types_slug`,
                    RENAME INDEX `ix_categories_deleted_at` TO `ix_product_types_deleted_at`,
                    RENAME INDEX `IX_categories_created_by` TO `IX_product_types_created_by`,
                    RENAME INDEX `IX_categories_deleted_by` TO `IX_product_types_deleted_by`,
                    RENAME INDEX `IX_categories_updated_by` TO `IX_product_types_updated_by`;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE `categories`
                    DROP FOREIGN KEY `fk_categories_created_by`,
                    DROP FOREIGN KEY `fk_categories_deleted_by`,
                    DROP FOREIGN KEY `fk_categories_updated_by`;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE `categories`
                    ADD CONSTRAINT `fk_product_types_created_by`
                        FOREIGN KEY (`created_by`) REFERENCES `users` (`id`)
                        ON UPDATE CASCADE ON DELETE SET NULL,
                    ADD CONSTRAINT `fk_product_types_deleted_by`
                        FOREIGN KEY (`deleted_by`) REFERENCES `users` (`id`)
                        ON UPDATE CASCADE ON DELETE SET NULL,
                    ADD CONSTRAINT `fk_product_types_updated_by`
                        FOREIGN KEY (`updated_by`) REFERENCES `users` (`id`)
                        ON UPDATE CASCADE ON DELETE SET NULL;
            ");

            migrationBuilder.Sql("RENAME TABLE `categories` TO `product_types`;");

            migrationBuilder.Sql(@"
                ALTER TABLE `products`
                    ADD CONSTRAINT `fk_products_product_type`
                        FOREIGN KEY (`product_type_id`) REFERENCES `product_types` (`id`)
                        ON UPDATE CASCADE ON DELETE RESTRICT;
            ");
        }
    }
}
