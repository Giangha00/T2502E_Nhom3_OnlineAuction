using OnlineAuction.Models;

namespace OnlineAuction.Data;

public static class MockUserDetailData
{
    public static UserDetailViewModel? GetUserDetail(int id)
    {
        var seller = MockAuctionData.GetBestSellers().FirstOrDefault(s => s.Id == id);
        if (seller is null)
        {
            return null;
        }

        var profileExtras = GetProfileExtras(id);
        var auctions = MockAuctionData.GetAuctionsBySellerId(id);
        var related = MockAuctionData.GetAllAuctions()
            .Where(a => !auctions.Any(x => x.Id == a.Id))
            .Take(3)
            .ToList();

        return new UserDetailViewModel
        {
            Profile = new UserProfileViewModel
            {
                Id = seller.Id,
                Username = seller.Username,
                FullName = profileExtras.FullName,
                AvatarUrl = seller.AvatarUrl,
                Role = "Seller",
                MemberSince = profileExtras.MemberSince
            },
            BasicInfo = new UserBasicInfoViewModel
            {
                FullName = profileExtras.FullName,
                Email = profileExtras.Email,
                PhoneNumber = profileExtras.Phone
            },
            Statistics = new SellerStatisticsViewModel
            {
                TotalAuctions = seller.AuctionCount,
                CompletedAuctions = seller.SuccessfulSales,
                TotalSales = profileExtras.TotalSales,
                Rating = seller.Rating
            },
            Auctions = auctions,
            Rating = new SellerRatingViewModel
            {
                AverageRating = seller.Rating,
                ReviewCount = profileExtras.ReviewCount,
                Reviews = GetReviewsForSeller(id)
            },
            RelatedAuctions = related
        };
    }

    private static (string FullName, string Email, string Phone, int MemberSince, int TotalSales, int ReviewCount) GetProfileExtras(int id) =>
        id switch
        {
            1 => ("Elena Voss", "elena.voss@gmail.com", "+84 912 345 678", 2022, 120, 98),
            2 => ("Marcus Chen", "marcus.chen@gmail.com", "+84 987 654 321", 2023, 95, 76),
            3 => ("Sofia Nguyen", "sofia.gallery@gmail.com", "+84 901 234 567", 2021, 156, 142),
            4 => ("James Retro", "james.retro@gmail.com", "+84 933 221 100", 2024, 88, 54),
            _ => ("John Smith", "john@gmail.com", "+84 xxx xxx xxx", 2026, 120, 120)
        };

    private static List<SellerReviewViewModel> GetReviewsForSeller(int id) =>
        id switch
        {
            1 =>
            [
                new() { ReviewerName = "Michael", Rating = 5, Comment = "Great seller! Fast shipping and item exactly as described.", ReviewDate = new DateTime(2026, 6, 10) },
                new() { ReviewerName = "Anna", Rating = 5, Comment = "Professional communication throughout the auction.", ReviewDate = new DateTime(2026, 5, 28) },
                new() { ReviewerName = "David", Rating = 4.5, Comment = "Smooth transaction. Would buy again.", ReviewDate = new DateTime(2026, 5, 12) }
            ],
            2 =>
            [
                new() { ReviewerName = "Michael", Rating = 5, Comment = "Great seller!", ReviewDate = new DateTime(2026, 6, 10) },
                new() { ReviewerName = "Lisa", Rating = 4.5, Comment = "Reliable seller with quality items.", ReviewDate = new DateTime(2026, 4, 20) }
            ],
            3 =>
            [
                new() { ReviewerName = "Tom", Rating = 5, Comment = "Outstanding gallery pieces and packaging.", ReviewDate = new DateTime(2026, 6, 8) },
                new() { ReviewerName = "Sarah", Rating = 5, Comment = "Best seller on the platform!", ReviewDate = new DateTime(2026, 5, 30) },
                new() { ReviewerName = "Kevin", Rating = 5, Comment = "Highly recommend for art collectors.", ReviewDate = new DateTime(2026, 5, 15) }
            ],
            4 =>
            [
                new() { ReviewerName = "Michael", Rating = 4, Comment = "Good vintage finds. Delivery took a bit longer.", ReviewDate = new DateTime(2026, 6, 2) },
                new() { ReviewerName = "Emma", Rating = 4.5, Comment = "Authentic retro items as advertised.", ReviewDate = new DateTime(2026, 4, 8) }
            ],
            _ =>
            [
                new() { ReviewerName = "Michael", Rating = 5, Comment = "Great seller!", ReviewDate = new DateTime(2026, 6, 10) }
            ]
        };
}
