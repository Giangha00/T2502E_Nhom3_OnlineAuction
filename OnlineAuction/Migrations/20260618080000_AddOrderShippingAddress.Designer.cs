using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OnlineAuction.Data;

#nullable disable

namespace OnlineAuction.Migrations
{
    [DbContext(typeof(AuctionHouseDbContext))]
    [Migration("20260618080000_AddOrderShippingAddress")]
    public partial class AddOrderShippingAddress
    {
    }
}
