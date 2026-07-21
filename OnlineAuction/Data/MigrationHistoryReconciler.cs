using Microsoft.EntityFrameworkCore;

namespace OnlineAuction.Data;

public static class MigrationHistoryReconciler
{
    private const string ProductVersion = "9.0.17";
    private const string AddProductTemplatesMigrationId = "20260629133637_AddProductTemplates";
    private const string AddPlatformFeeToOrdersMigrationId = "20260610120000_AddPlatformFeeToOrders";

    private static readonly (string TableName, string MigrationId)[] KnownOrphans =
    [
        ("complaints", "20260627143448_AddComplaintsTable")
    ];

    public static async Task ReconcileAsync(
        AuctionHouseDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        // MySQL-only: uses MySQL information_schema / backtick DDL for orphan repair.
        if (db.Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) != true)
        {
            return;
        }

        await ReconcilePartialProductTemplatesMigrationAsync(db, logger, cancellationToken);
        await ReconcileMissingPlatformFeeColumnAsync(db, logger, cancellationToken);

        foreach (var (tableName, migrationId) in KnownOrphans)
        {
            if (!await TableExistsAsync(db, tableName, cancellationToken))
            {
                continue;
            }

            if (await MigrationAppliedAsync(db, migrationId, cancellationToken))
            {
                continue;
            }

            logger.LogWarning(
                "Table {TableName} exists but migration {MigrationId} is missing from __ef_migrations_history. Marking as applied.",
                tableName,
                migrationId);

            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO __ef_migrations_history (MigrationId, ProductVersion) VALUES ({0}, {1})",
                [migrationId, ProductVersion],
                cancellationToken);
        }
    }

    private static async Task ReconcilePartialProductTemplatesMigrationAsync(
        AuctionHouseDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(db, "product_templates", cancellationToken)
            || !await ColumnExistsAsync(db, "products", "product_template_id", cancellationToken)
            || await MigrationAppliedAsync(db, AddProductTemplatesMigrationId, cancellationToken))
        {
            return;
        }

        logger.LogWarning(
            "Detected partial application of migration {MigrationId}. Completing product template backfill.",
            AddProductTemplatesMigrationId);

        await db.Database.ExecuteSqlRawAsync(LinkProductsToTemplatesSql, cancellationToken);

        if (!await ForeignKeyExistsAsync(db, "products", "fk_products_product_template", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE `products`
                    ADD CONSTRAINT `fk_products_product_template`
                    FOREIGN KEY (`product_template_id`)
                    REFERENCES `product_templates` (`id`)
                    ON DELETE RESTRICT;
                """,
                cancellationToken);
        }

        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO __ef_migrations_history (MigrationId, ProductVersion) VALUES ({0}, {1})",
            [AddProductTemplatesMigrationId, ProductVersion],
            cancellationToken);
    }

    private static async Task ReconcileMissingPlatformFeeColumnAsync(
        AuctionHouseDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(db, "orders", cancellationToken)
            || await ColumnExistsAsync(db, "orders", "platform_fee", cancellationToken))
        {
            return;
        }

        logger.LogWarning(
            "Column orders.platform_fee is missing. Applying orphan migration {MigrationId}.",
            AddPlatformFeeToOrdersMigrationId);

        await db.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE `orders`
                ADD COLUMN `platform_fee` decimal(18,2) NOT NULL DEFAULT 0;
            """,
            cancellationToken);

        if (!await MigrationAppliedAsync(db, AddPlatformFeeToOrdersMigrationId, cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO __ef_migrations_history (MigrationId, ProductVersion) VALUES ({0}, {1})",
                [AddPlatformFeeToOrdersMigrationId, ProductVersion],
                cancellationToken);
        }
    }

    private const string LinkProductsToTemplatesSql =
        """
        UPDATE products p
        INNER JOIN product_templates pt
            ON pt.deleted_at IS NULL
            AND pt.category_id = p.category_id
            AND UPPER(TRIM(pt.name)) COLLATE utf8mb4_unicode_ci = UPPER(TRIM(p.name)) COLLATE utf8mb4_unicode_ci
            AND UPPER(TRIM(COALESCE(pt.set_name, ''))) COLLATE utf8mb4_unicode_ci = UPPER(TRIM(COALESCE(p.set_name, ''))) COLLATE utf8mb4_unicode_ci
            AND UPPER(TRIM(COALESCE(pt.card_number, ''))) COLLATE utf8mb4_unicode_ci = UPPER(TRIM(COALESCE(p.card_number, ''))) COLLATE utf8mb4_unicode_ci
            AND UPPER(TRIM(COALESCE(pt.grade_label, ''))) COLLATE utf8mb4_unicode_ci = UPPER(TRIM(COALESCE(p.grade_label, ''))) COLLATE utf8mb4_unicode_ci
        SET p.product_template_id = pt.id
        WHERE p.deleted_at IS NULL
            AND p.product_template_id IS NULL;
        """;

    private static async Task<bool> ColumnExistsAsync(
        AuctionHouseDbContext db,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        var (schemaColumn, schemaExpression) = GetInformationSchemaDatabaseFilter(db);
        var countExpression = GetCountExpression(db);
        var count = await db.Database
            .SqlQueryRaw<long>(
                $@"
                SELECT {countExpression} AS Value
                FROM information_schema.COLUMNS
                WHERE {schemaColumn} = {schemaExpression}
                  AND TABLE_NAME = {{0}}
                  AND COLUMN_NAME = {{1}}
                ",
                tableName,
                columnName)
            .FirstOrDefaultAsync(cancellationToken);

        return count > 0;
    }

    private static async Task<bool> ForeignKeyExistsAsync(
        AuctionHouseDbContext db,
        string tableName,
        string constraintName,
        CancellationToken cancellationToken)
    {
        var (schemaColumn, schemaExpression) = GetInformationSchemaConstraintFilter(db);
        var countExpression = GetCountExpression(db);
        var count = await db.Database
            .SqlQueryRaw<long>(
                $@"
                SELECT {countExpression} AS Value
                FROM information_schema.TABLE_CONSTRAINTS
                WHERE {schemaColumn} = {schemaExpression}
                  AND TABLE_NAME = {{0}}
                  AND CONSTRAINT_NAME = {{1}}
                  AND CONSTRAINT_TYPE = 'FOREIGN KEY'
                ",
                tableName,
                constraintName)
            .FirstOrDefaultAsync(cancellationToken);

        return count > 0;
    }

    private static async Task<bool> TableExistsAsync(
        AuctionHouseDbContext db,
        string tableName,
        CancellationToken cancellationToken)
    {
        var (schemaColumn, schemaExpression) = GetInformationSchemaDatabaseFilter(db);
        var countExpression = GetCountExpression(db);
        var count = await db.Database
            .SqlQueryRaw<long>(
                $@"
                SELECT {countExpression} AS Value
                FROM information_schema.TABLES
                WHERE {schemaColumn} = {schemaExpression}
                  AND TABLE_NAME = {{0}}
                ",
                tableName)
            .FirstOrDefaultAsync(cancellationToken);

        return count > 0;
    }

    private static (string SchemaColumn, string SchemaExpression) GetInformationSchemaDatabaseFilter(AuctionHouseDbContext db) =>
        db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true
            ? ("TABLE_CATALOG", "DB_NAME()")
            : ("TABLE_SCHEMA", "DATABASE()");

    private static (string SchemaColumn, string SchemaExpression) GetInformationSchemaConstraintFilter(AuctionHouseDbContext db) =>
        db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true
            ? ("CONSTRAINT_CATALOG", "DB_NAME()")
            : ("CONSTRAINT_SCHEMA", "DATABASE()");

    private static string GetCountExpression(AuctionHouseDbContext db) =>
        db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true
            ? "COUNT_BIG(*)"
            : "COUNT(*)";

    private static async Task<bool> MigrationAppliedAsync(
        AuctionHouseDbContext db,
        string migrationId,
        CancellationToken cancellationToken)
    {
        var count = await db.Database
            .SqlQueryRaw<long>(
                "SELECT COUNT(*) AS Value FROM __ef_migrations_history WHERE MigrationId = {0}",
                migrationId)
            .FirstOrDefaultAsync(cancellationToken);

        return count > 0;
    }
}
