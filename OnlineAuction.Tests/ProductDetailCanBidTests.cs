using OnlineAuction.Entities;
using OnlineAuction.Helpers;
using OnlineAuction.Services;
using Xunit;

namespace OnlineAuction.Tests;

public class ProductDetailCanBidTests
{
    [Fact]
    public void ComputeCanBid_LiveAuction_NoRegistrationRequired_ReturnsTrue()
    {
        var auction = CreateAuction(requiresRegistration: false);

        var canBid = ProductDetailMapper.ComputeCanBid(
            auction,
            currentUserId: 42,
            registrationStatus: null,
            isSeller: false,
            auctionAcceptsBids: ProductDetailMapper.CanAcceptBids(auction));

        Assert.True(canBid);
    }

    [Fact]
    public void ComputeCanBid_RequiresRegistration_Approved_ReturnsTrue()
    {
        var auction = CreateAuction(requiresRegistration: true);

        var canBid = ProductDetailMapper.ComputeCanBid(
            auction,
            currentUserId: 42,
            registrationStatus: AuctionRegistrationStatuses.Approved,
            isSeller: false,
            auctionAcceptsBids: ProductDetailMapper.CanAcceptBids(auction));

        Assert.True(canBid);
    }

    [Fact]
    public void ComputeCanBid_RequiresRegistration_Pending_ReturnsFalse()
    {
        var auction = CreateAuction(requiresRegistration: true);

        var canBid = ProductDetailMapper.ComputeCanBid(
            auction,
            currentUserId: 42,
            registrationStatus: AuctionRegistrationStatuses.Pending,
            isSeller: false,
            auctionAcceptsBids: ProductDetailMapper.CanAcceptBids(auction));

        Assert.False(canBid);
    }

    [Fact]
    public void ComputeCanBid_SellerOwnListing_ReturnsFalse()
    {
        var auction = CreateAuction(requiresRegistration: false);

        var canBid = ProductDetailMapper.ComputeCanBid(
            auction,
            currentUserId: 10,
            registrationStatus: null,
            isSeller: true,
            auctionAcceptsBids: ProductDetailMapper.CanAcceptBids(auction));

        Assert.False(canBid);
    }

    [Fact]
    public void ComputeCanBid_GuestUser_ReturnsFalse()
    {
        var auction = CreateAuction(requiresRegistration: false);

        var canBid = ProductDetailMapper.ComputeCanBid(
            auction,
            currentUserId: null,
            registrationStatus: null,
            isSeller: false,
            auctionAcceptsBids: ProductDetailMapper.CanAcceptBids(auction));

        Assert.False(canBid);
    }

    private static Auction CreateAuction(bool requiresRegistration)
    {
        var now = DateTime.UtcNow;
        return new Auction
        {
            Status = AuctionStatuses.Live,
            ListingType = ListingTypes.Auction,
            RequiresRegistration = requiresRegistration,
            StartingPrice = 100m,
            CurrentPrice = 100m,
            BidStep = 10m,
            RegistrationStartDate = now.AddDays(-7),
            RegistrationEndDate = now.AddHours(-1),
            StartDate = now.AddHours(-1),
            EndDate = now.AddDays(1),
            CreatedAt = now,
            SubmittedAt = now,
            Product = new Product
            {
                SellerId = 10,
                CategoryId = 1,
                Name = "Test Card",
                CreatedAt = now
            }
        };
    }
}
