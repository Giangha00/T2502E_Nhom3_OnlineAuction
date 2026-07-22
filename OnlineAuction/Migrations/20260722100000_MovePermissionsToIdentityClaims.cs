using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineAuction.Migrations;

/// <summary>
/// Moves delegated admin permissions from custom tables into ASP.NET Identity user_claims,
/// then drops permissions / role_permissions / user_permissions.
/// </summary>
[DbContext(typeof(OnlineAuction.Data.AuctionHouseDbContext))]
[Migration("20260722100000_MovePermissionsToIdentityClaims")]
public class MovePermissionsToIdentityClaims : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Copy existing user_permissions into Identity user_claims before dropping tables.
        // Safe if tables were already empty or previously migrated.
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[user_permissions]', N'U') IS NOT NULL
               AND OBJECT_ID(N'[permissions]', N'U') IS NOT NULL
            BEGIN
                INSERT INTO [user_claims] ([UserId], [ClaimType], [ClaimValue])
                SELECT up.[user_id], N'permission', p.[code]
                FROM [user_permissions] AS up
                INNER JOIN [permissions] AS p ON p.[id] = up.[permission_id]
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM [user_claims] AS uc
                    WHERE uc.[UserId] = up.[user_id]
                      AND uc.[ClaimType] = N'permission'
                      AND uc.[ClaimValue] = p.[code]
                );
            END
            """);

        migrationBuilder.DropTable(name: "role_permissions");
        migrationBuilder.DropTable(name: "user_permissions");
        migrationBuilder.DropTable(name: "permissions");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "permissions",
            columns: table => new
            {
                id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                module = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_permissions", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "role_permissions",
            columns: table => new
            {
                role_id = table.Column<int>(type: "int", nullable: false),
                permission_id = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_role_permissions", x => new { x.role_id, x.permission_id });
                table.ForeignKey(
                    name: "fk_role_permissions_permission",
                    column: x => x.permission_id,
                    principalTable: "permissions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_role_permissions_role",
                    column: x => x.role_id,
                    principalTable: "roles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "user_permissions",
            columns: table => new
            {
                user_id = table.Column<int>(type: "int", nullable: false),
                permission_id = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user_permissions", x => new { x.user_id, x.permission_id });
                table.ForeignKey(
                    name: "fk_user_permissions_permission",
                    column: x => x.permission_id,
                    principalTable: "permissions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_user_permissions_user",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ux_permissions_code",
            table: "permissions",
            column: "code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_role_permissions_permission_id",
            table: "role_permissions",
            column: "permission_id");

        migrationBuilder.CreateIndex(
            name: "ux_role_permissions_role_permission",
            table: "role_permissions",
            columns: new[] { "role_id", "permission_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_user_permissions_permission_id",
            table: "user_permissions",
            column: "permission_id");

        migrationBuilder.CreateIndex(
            name: "ux_user_permissions_user_permission",
            table: "user_permissions",
            columns: new[] { "user_id", "permission_id" },
            unique: true);

        // Best-effort restore from Identity claims into custom tables.
        migrationBuilder.Sql(
            """
            INSERT INTO [permissions] ([code], [name], [module], [description])
            SELECT DISTINCT uc.[ClaimValue], uc.[ClaimValue], N'Migrated', NULL
            FROM [user_claims] AS uc
            WHERE uc.[ClaimType] = N'permission'
              AND NOT EXISTS (
                  SELECT 1 FROM [permissions] AS p WHERE p.[code] = uc.[ClaimValue]
              );

            INSERT INTO [user_permissions] ([user_id], [permission_id])
            SELECT uc.[UserId], p.[id]
            FROM [user_claims] AS uc
            INNER JOIN [permissions] AS p ON p.[code] = uc.[ClaimValue]
            WHERE uc.[ClaimType] = N'permission'
              AND NOT EXISTS (
                  SELECT 1
                  FROM [user_permissions] AS up
                  WHERE up.[user_id] = uc.[UserId] AND up.[permission_id] = p.[id]
              );
            """);
    }
}
