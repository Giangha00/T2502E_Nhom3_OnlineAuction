using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineAuction.Migrations
{
    /// <inheritdoc />
    [Migration("20260716120000_AllowZeroOrderTotalForCoveredDeposits")]
    public partial class AllowZeroOrderTotalForCoveredDeposits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "chk_orders_amounts",
                table: "orders");

            migrationBuilder.AddCheckConstraint(
                name: "chk_orders_amounts",
                table: "orders",
                sql: "`subtotal` > 0 AND `total_amount` >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "chk_orders_amounts",
                table: "orders");

            migrationBuilder.AddCheckConstraint(
                name: "chk_orders_amounts",
                table: "orders",
                sql: "`subtotal` > 0 AND `total_amount` > 0");
        }
    }
}
