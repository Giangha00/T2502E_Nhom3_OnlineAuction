using Microsoft.EntityFrameworkCore;

namespace OnlineAuction.Data;

public static class MigrationHistoryReconciler
{
    private const string ProductVersion = "9.0.17";

    private static readonly (string TableName, string MigrationId)[] KnownOrphans =
    [
        ("complaints", "20260627143448_AddComplaintsTable")
    ];

    public static async Task ReconcileAsync(
        AuctionHouseDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (db.Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) != true
            && db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) != true)
        {
            return;
        }

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
