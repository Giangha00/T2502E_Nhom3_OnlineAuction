using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineAuction.Migrations;

/// <inheritdoc />
[DbContext(typeof(OnlineAuction.Data.AuctionHouseDbContext))]
[Migration("20260720110000_RenamePendingReviewToConfirming")]
public class RenamePendingReviewToConfirming : Migration
{
    private const string NewStatusConstraint =
        "status IN ('confirming','rejected','scheduled','live','ending_soon','ended','awaiting_payment','completed','cancelled')";

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "chk_auctions_status",
            table: "auctions");

        migrationBuilder.Sql(
            """
            UPDATE auctions
            SET status = 'confirming'
            WHERE status = 'pending_review'
            """);

        migrationBuilder.AddCheckConstraint(
            name: "chk_auctions_status",
            table: "auctions",
            sql: NewStatusConstraint);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "chk_auctions_status",
            table: "auctions");

        migrationBuilder.Sql(
            """
            UPDATE auctions
            SET status = 'pending_review'
            WHERE status = 'confirming'
            """);

        migrationBuilder.AddCheckConstraint(
            name: "chk_auctions_status",
            table: "auctions",
            sql: "status IN ('pending_review','rejected','scheduled','live','ending_soon','ended','awaiting_payment','completed','cancelled')");
    }
}
