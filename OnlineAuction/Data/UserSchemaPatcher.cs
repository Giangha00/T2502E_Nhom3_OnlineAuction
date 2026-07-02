using Microsoft.EntityFrameworkCore;

namespace OnlineAuction.Data;

public static class UserSchemaPatcher
{
    public static async Task EnsureAsync(
        AuctionHouseDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (db.Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) != true)
        {
            return;
        }

        await EnsureSuperAdminColumnAsync(db, logger, cancellationToken);
        await EnsureUserPermissionsTableAsync(db, logger, cancellationToken);
    }

    private static async Task EnsureSuperAdminColumnAsync(
        AuctionHouseDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await ColumnExistsAsync(db, "users", "is_super_admin", cancellationToken))
        {
            return;
        }

        logger.LogWarning(
            "Column users.is_super_admin is missing. Applying schema patch before seeders.");

        await db.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE users
            ADD COLUMN is_super_admin tinyint(1) NOT NULL DEFAULT 0
            """,
            cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            UPDATE users
            SET is_super_admin = 1
            WHERE role = 2
            """,
            cancellationToken);

        await EnsureMigrationHistoryAsync(db, "20260701083000_AddIsSuperAdminToUsers", cancellationToken);
    }

    private static async Task EnsureUserPermissionsTableAsync(
        AuctionHouseDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await TableExistsAsync(db, "user_permissions", cancellationToken))
        {
            return;
        }

        logger.LogWarning(
            "Table user_permissions is missing. Applying schema patch before seeders.");

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE user_permissions (
                user_id int NOT NULL,
                permission_id int NOT NULL,
                PRIMARY KEY (user_id, permission_id),
                CONSTRAINT fk_user_permissions_user
                    FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE,
                CONSTRAINT fk_user_permissions_permission
                    FOREIGN KEY (permission_id) REFERENCES permissions (id) ON DELETE CASCADE
            )
            """,
            cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE UNIQUE INDEX ux_user_permissions_user_permission
            ON user_permissions (user_id, permission_id)
            """,
            cancellationToken);

        await EnsureMigrationHistoryAsync(db, "20260701090000_AddUserPermissions", cancellationToken);
    }

    private static async Task EnsureMigrationHistoryAsync(
        AuctionHouseDbContext db,
        string migrationId,
        CancellationToken cancellationToken)
    {
        var applied = await db.Database
            .SqlQueryRaw<long>(
                "SELECT COUNT(*) AS Value FROM __ef_migrations_history WHERE MigrationId = {0}",
                migrationId)
            .FirstOrDefaultAsync(cancellationToken);

        if (applied == 0)
        {
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO __ef_migrations_history (MigrationId, ProductVersion) VALUES ({0}, {1})",
                [migrationId, "9.0.17"],
                cancellationToken);
        }
    }

    private static async Task<bool> TableExistsAsync(
        AuctionHouseDbContext db,
        string tableName,
        CancellationToken cancellationToken)
    {
        var count = await db.Database
            .SqlQueryRaw<long>(
                """
                SELECT COUNT(*) AS Value
                FROM information_schema.TABLES
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = {0}
                """,
                tableName)
            .FirstOrDefaultAsync(cancellationToken);

        return count > 0;
    }

    private static async Task<bool> ColumnExistsAsync(
        AuctionHouseDbContext db,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        var count = await db.Database
            .SqlQueryRaw<long>(
                """
                SELECT COUNT(*) AS Value
                FROM information_schema.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = {0}
                  AND COLUMN_NAME = {1}
                """,
                tableName,
                columnName)
            .FirstOrDefaultAsync(cancellationToken);

        return count > 0;
    }
}
