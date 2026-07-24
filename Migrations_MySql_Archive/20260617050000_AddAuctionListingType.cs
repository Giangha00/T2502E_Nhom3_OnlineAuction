using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineAuction.Migrations;

public partial class AddAuctionListingType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "listing_type",
            table: "auctions",
            type: "varchar(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "auction");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "listing_type",
            table: "auctions");
    }
}
