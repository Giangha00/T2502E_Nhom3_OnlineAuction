using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineAuction.Migrations;

public partial class AddSellerProceedsToOrders : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "seller_proceeds",
            table: "orders",
            type: "decimal(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.Sql(
            """
            UPDATE `orders`
            SET `seller_proceeds` = GREATEST(0, ROUND(`subtotal` - `seller_fee`, 2))
            WHERE `seller_fee` > 0;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "seller_proceeds",
            table: "orders");
    }
}
