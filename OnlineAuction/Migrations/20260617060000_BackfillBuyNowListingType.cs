using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineAuction.Migrations;

public partial class BackfillBuyNowListingType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE `auctions`
            SET `listing_type` = 'buynow'
            WHERE `listing_type` = 'auction'
              AND `bid_step` = 0.01
              AND DATEDIFF(`end_date`, `start_date`) >= 364;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE `auctions`
            SET `listing_type` = 'auction'
            WHERE `listing_type` = 'buynow'
              AND `bid_step` = 0.01
              AND DATEDIFF(`end_date`, `start_date`) >= 364;
            """);
    }
}
