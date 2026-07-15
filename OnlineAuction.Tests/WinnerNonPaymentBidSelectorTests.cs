using OnlineAuction.Entities;
using OnlineAuction.Services;
using Xunit;

namespace OnlineAuction.Tests;

public class WinnerNonPaymentBidSelectorTests
{
    [Fact]
    public void SelectRunnerUp_ReturnsHighestEligibleBidder()
    {
        var bids = new List<Bid>
        {
            CreateBid(1, 100, 1, isWinning: true),
            CreateBid(2, 90, 2),
            CreateBid(3, 80, 3)
        };

        var runnerUp = WinnerNonPaymentBidSelector.SelectRunnerUp(bids, [1]);

        Assert.NotNull(runnerUp);
        Assert.Equal(2, runnerUp!.BidderId);
        Assert.Equal(90m, runnerUp.Amount);
    }

    [Fact]
    public void SelectRunnerUp_SkipsFlaggedAndExcludedBidder()
    {
        var bids = new List<Bid>
        {
            CreateBid(1, 100, 1, isWinning: true),
            CreateBid(2, 95, 2, isFlagged: true),
            CreateBid(3, 85, 3)
        };

        var runnerUp = WinnerNonPaymentBidSelector.SelectRunnerUp(bids, [1, 2]);

        Assert.NotNull(runnerUp);
        Assert.Equal(3, runnerUp!.BidderId);
    }

    [Fact]
    public void SelectRunnerUp_ReturnsNullWhenNoEligibleBidder()
    {
        var bids = new List<Bid>
        {
            CreateBid(1, 100, 1, isWinning: true)
        };

        var runnerUp = WinnerNonPaymentBidSelector.SelectRunnerUp(bids, [1]);

        Assert.Null(runnerUp);
    }

    private static Bid CreateBid(long id, decimal amount, int bidderId, bool isWinning = false, bool isFlagged = false)
    {
        return new Bid
        {
            Id = id,
            Amount = amount,
            BidderId = bidderId,
            IsWinning = isWinning,
            IsFlagged = isFlagged,
            PlacedAt = DateTime.UtcNow.AddMinutes(id)
        };
    }
}
