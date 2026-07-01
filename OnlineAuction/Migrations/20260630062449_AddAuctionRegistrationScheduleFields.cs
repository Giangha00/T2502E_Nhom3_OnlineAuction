using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineAuction.Migrations
{
    /// <inheritdoc />
    public partial class AddAuctionRegistrationScheduleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "registration_start_date",
                table: "auctions",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "registration_end_date",
                table: "auctions",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE auctions
                SET registration_start_date = COALESCE(created_at, start_date),
                    registration_end_date = start_date
                WHERE registration_start_date IS NULL
                   OR registration_end_date IS NULL
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "registration_start_date",
                table: "auctions",
                type: "datetime(6)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "registration_end_date",
                table: "auctions",
                type: "datetime(6)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_watchlist_items_auction_id",
                table: "watchlist_items",
                column: "auction_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_watchlist_items_auction_id",
                table: "watchlist_items");

            migrationBuilder.DropColumn(
                name: "registration_start_date",
                table: "auctions");

            migrationBuilder.DropColumn(
                name: "registration_end_date",
                table: "auctions");
        }
    }
}
