using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineAuction.Migrations;

/// <summary>
/// Backfill: auction listings always require registration.
/// Buy Now listings are left unchanged (registration is not used).
/// </summary>
public partial class ForceAuctionRequiresRegistration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE `auctions`
            SET `requires_registration` = TRUE
            WHERE `requires_registration` = FALSE
              AND `deleted_at` IS NULL
              AND `listing_type` = 'auction';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
