using OnlineAuction.Entities;

namespace OnlineAuction.Helpers;

public static class AuctionListingPhases
{
    public const string RegistrationOpen = "registration_open";
    public const string RegistrationClosed = "registration_closed";
    public const string LiveAuction = "live_auction";
    public const string LiveEndingSoon = "live_ending_soon";
    public const string Upcoming = "upcoming";
    public const string Ended = "ended";
}

public sealed record AuctionListingPhaseInfo(
    string Phase,
    DateTime CountdownTarget,
    string CountdownKind);

public static class AuctionScheduleHelper
{
    public static readonly TimeSpan DefaultLiveDuration = TimeSpan.FromHours(1);
    public static readonly TimeSpan LiveEndingSoonThreshold = TimeSpan.FromMinutes(30);

    public static bool IsRegistrationOpen(Auction auction, DateTime? utcNow = null)
    {
        if (!auction.RequiresRegistration)
        {
            return false;
        }

        if (auction.Status is not (AuctionStatuses.Scheduled or AuctionStatuses.Live or AuctionStatuses.EndingSoon))
        {
            return false;
        }

        var now = utcNow ?? DateTime.UtcNow;
        var registrationStart = DateTimeUtilities.AsUtc(auction.RegistrationStartDate);
        var registrationEnd = DateTimeUtilities.AsUtc(auction.RegistrationEndDate);

        return now >= registrationStart && now < registrationEnd;
    }

    public static bool IsLiveOpen(Auction auction, DateTime? utcNow = null)
    {
        if (auction.Status is not (AuctionStatuses.Live or AuctionStatuses.EndingSoon))
        {
            return false;
        }

        var now = utcNow ?? DateTime.UtcNow;
        var liveStart = DateTimeUtilities.AsUtc(auction.StartDate);

        return now >= liveStart && DateTimeUtilities.IsInFutureUtc(auction.EndDate);
    }

    public static bool IsPubliclyListed(Auction auction, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var liveEnd = DateTimeUtilities.AsUtc(auction.EndDate);

        if (now >= liveEnd)
        {
            return false;
        }

        if (IsLiveOpen(auction, now))
        {
            return true;
        }

        if (auction.Status != AuctionStatuses.Scheduled)
        {
            return false;
        }

        return true;
    }

    public static DateTime GetCountdownTarget(Auction auction, DateTime? utcNow = null) =>
        ResolveListingPhase(auction, utcNow).CountdownTarget;

    public static AuctionListingPhaseInfo ResolveListingPhase(Auction auction, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var registrationStart = DateTimeUtilities.AsUtc(auction.RegistrationStartDate);
        var registrationEnd = DateTimeUtilities.AsUtc(auction.RegistrationEndDate);
        var liveStart = DateTimeUtilities.AsUtc(auction.StartDate);
        var liveEnd = DateTimeUtilities.AsUtc(auction.EndDate);

        if (IsLiveOpen(auction, now))
        {
            var remaining = DateTimeUtilities.RemainingUtc(liveEnd);
            var phase = remaining <= LiveEndingSoonThreshold
                ? AuctionListingPhases.LiveEndingSoon
                : AuctionListingPhases.LiveAuction;

            return new AuctionListingPhaseInfo(phase, liveEnd, "live_end");
        }

        if (IsRegistrationOpen(auction, now))
        {
            return new AuctionListingPhaseInfo(
                AuctionListingPhases.RegistrationOpen,
                registrationEnd,
                "registration_end");
        }

        if (auction.Status == AuctionStatuses.Scheduled &&
            now >= registrationEnd &&
            now < liveStart)
        {
            return new AuctionListingPhaseInfo(
                AuctionListingPhases.RegistrationClosed,
                liveStart,
                "live_start");
        }

        if (now < registrationStart)
        {
            return new AuctionListingPhaseInfo(
                AuctionListingPhases.Upcoming,
                registrationStart,
                "registration_start");
        }

        if (!DateTimeUtilities.IsInFutureUtc(liveEnd))
        {
            return new AuctionListingPhaseInfo(
                AuctionListingPhases.Ended,
                liveEnd,
                "live_end");
        }

        return new AuctionListingPhaseInfo(
            AuctionListingPhases.RegistrationClosed,
            liveStart,
            "live_start");
    }

    public static void ApplyTestAuctionSchedule(Auction auction, int seedIndex, DateTime now)
    {
        var phase = seedIndex % 4;

        switch (phase)
        {
            case 0:
                auction.Status = AuctionStatuses.Scheduled;
                auction.RegistrationStartDate = DateTimeUtilities.AsUtc(now.AddDays(-2));
                auction.RegistrationEndDate = DateTimeUtilities.AsUtc(now.AddDays(5));
                auction.StartDate = DateTimeUtilities.AsUtc(now.AddDays(5));
                auction.EndDate = DateTimeUtilities.AsUtc(now.AddDays(5).Add(DefaultLiveDuration));
                break;
            case 1:
                auction.Status = AuctionStatuses.Scheduled;
                auction.RegistrationStartDate = DateTimeUtilities.AsUtc(now.AddDays(-5));
                auction.RegistrationEndDate = DateTimeUtilities.AsUtc(now.AddDays(-1));
                auction.StartDate = DateTimeUtilities.AsUtc(now.AddDays(2));
                auction.EndDate = DateTimeUtilities.AsUtc(now.AddDays(2).Add(DefaultLiveDuration));
                break;
            case 2:
                auction.Status = AuctionStatuses.Live;
                auction.RegistrationStartDate = DateTimeUtilities.AsUtc(now.AddDays(-7));
                auction.RegistrationEndDate = DateTimeUtilities.AsUtc(now.AddHours(-1));
                auction.StartDate = DateTimeUtilities.AsUtc(now.AddMinutes(-20));
                auction.EndDate = DateTimeUtilities.AsUtc(now.AddMinutes(40));
                break;
            default:
                auction.Status = AuctionStatuses.Live;
                auction.RegistrationStartDate = DateTimeUtilities.AsUtc(now.AddDays(-7));
                auction.RegistrationEndDate = DateTimeUtilities.AsUtc(now.AddHours(-2));
                auction.StartDate = DateTimeUtilities.AsUtc(now.AddMinutes(-50));
                auction.EndDate = DateTimeUtilities.AsUtc(now.AddMinutes(10));
                break;
        }
    }

    /// <summary>
    /// Demo schedule: registration open now, closes in a few minutes, live starts right after.
    /// </summary>
    public static void ApplyFullFlowDemoSchedule(Auction auction, DateTime now)
    {
        const int registrationWindowMinutes = 10;

        auction.Status = AuctionStatuses.Scheduled;
        auction.RegistrationStartDate = DateTimeUtilities.AsUtc(now.AddMinutes(-1));
        auction.RegistrationEndDate = DateTimeUtilities.AsUtc(now.AddMinutes(registrationWindowMinutes));
        auction.StartDate = auction.RegistrationEndDate;
        auction.EndDate = DateTimeUtilities.AsUtc(auction.StartDate.Add(DefaultLiveDuration));
    }

    public static string? ValidateSchedule(
        DateTime registrationStart,
        DateTime registrationEnd,
        DateTime liveStart,
        DateTime liveEnd)
    {
        registrationStart = DateTimeUtilities.AsUtc(registrationStart);
        registrationEnd = DateTimeUtilities.AsUtc(registrationEnd);
        liveStart = DateTimeUtilities.AsUtc(liveStart);
        liveEnd = DateTimeUtilities.AsUtc(liveEnd);

        if (registrationEnd <= registrationStart)
        {
            return "Registration end must be after registration start.";
        }

        if (liveEnd <= liveStart)
        {
            return "Live auction end must be after live auction start.";
        }

        if (registrationEnd > liveStart)
        {
            return "Registration must end before or when the live auction starts.";
        }

        return null;
    }

    public static (DateTime RegistrationStart, DateTime RegistrationEnd, DateTime LiveStart, DateTime LiveEnd)
        CreateDefaultSchedule(DateTime? baseTime = null)
    {
        var now = baseTime ?? DateTime.Now;
        var registrationStart = now.AddHours(1);
        var registrationEnd = now.AddDays(7);
        var liveStart = registrationEnd;
        var liveEnd = liveStart.Add(DefaultLiveDuration);

        return (registrationStart, registrationEnd, liveStart, liveEnd);
    }
}
