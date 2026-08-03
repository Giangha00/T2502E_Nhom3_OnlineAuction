using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineAuction.Migrations;

[DbContext(typeof(OnlineAuction.Data.AuctionHouseDbContext))]
[Migration("20260730120000_AddNotificationLocalizationArgs")]
public class AddNotificationLocalizationArgs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "localization_args_json",
            table: "notifications",
            type: "TEXT",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "localization_args_json",
            table: "notifications");
    }
}
