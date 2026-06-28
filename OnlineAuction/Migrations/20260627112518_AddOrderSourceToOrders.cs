using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineAuction.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderSourceToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "order_source",
                table: "orders",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "auction_win")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(
                "UPDATE `orders` SET `order_source` = 'buy_now' WHERE `order_reference` LIKE 'BN-%';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "order_source",
                table: "orders");
        }
    }
}
