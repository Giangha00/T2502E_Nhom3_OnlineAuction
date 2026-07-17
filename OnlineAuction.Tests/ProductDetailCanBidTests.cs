using OnlineAuction.Entities;
using OnlineAuction.Helpers;
using OnlineAuction.Services;
using Xunit;

namespace OnlineAuction.Tests;

/// <summary>
/// BID-14: UI eligibility flags for _ProductBidPanel (data-can-bid, disabled button).
/// </summary>
public class ProductDetailCanBidTests
{
    private static DateTime Now => DateTime.UtcNow;

    [Fact]
    public void ComputeCanBid_ApprovedRegistration_ReturnsTrue()
    {
        var auction = CreateLiveAuction(requiresRegistration: true);

        var canBid = ProductDetailMapper.ComputeCanBid(
            auction,
            currentUserId: 2,
            registrationStatus: AuctionRegistrationStatuses.Approved,
            isSeller: false,
            auctionAcceptsBids: ProductDetailMapper.CanAcceptBids(auction));

        Assert.True(canBid);
    }

    [Fact]
    public void ComputeCanBid_PendingRegistration_ReturnsFalse()
    {
        var auction = CreateLiveAuction(requiresRegistration: true);

        var canBid = ProductDetailMapper.ComputeCanBid(
            auction,
            currentUserId: 2,
            registrationStatus: AuctionRegistrationStatuses.Pending,
            isSeller: false,
            auctionAcceptsBids: ProductDetailMapper.CanAcceptBids(auction));

        Assert.False(canBid);
    }

    [Fact]
    public void ComputeCanBid_Seller_ReturnsFalse()
    {
        var auction = CreateLiveAuction(requiresRegistration: true);

        var canBid = ProductDetailMapper.ComputeCanBid(
            auction,
            currentUserId: 10,
            registrationStatus: AuctionRegistrationStatuses.Approved,
            isSeller: true,
            auctionAcceptsBids: ProductDetailMapper.CanAcceptBids(auction));

        Assert.False(canBid);
    }

    [Fact]
    public void CanAcceptBids_EndedAuction_ReturnsFalse()
    {
        var auction = CreateLiveAuction();
        auction.EndDate = Now.AddMinutes(-5);

        Assert.False(ProductDetailMapper.CanAcceptBids(auction));
    }

    [Fact]
    public void CanAcceptBids_LiveWithinWindow_ReturnsTrue()
    {
        var auction = CreateLiveAuction();

        Assert.True(ProductDetailMapper.CanAcceptBids(auction));
    }

    private static Auction CreateLiveAuction(bool requiresRegistration = false)
    {
        return new Auction
        {
            Id = 1,
            Status = AuctionStatuses.Live,
            RequiresRegistration = requiresRegistration,
            CurrentPrice = 100m,
            BidStep = 10m,
            RegistrationStartDate = Now.AddDays(-2),
            RegistrationEndDate = Now.AddHours(-1),
            StartDate = Now.AddHours(-1),
            EndDate = Now.AddHours(1)
        };
    }
}
