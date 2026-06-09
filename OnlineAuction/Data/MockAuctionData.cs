using OnlineAuction.Models;

namespace OnlineAuction.Data;

public static class MockAuctionData
{
    public static List<AuctionItemViewModel> GetAllAuctions() =>
    [
        new()
        {
            Id = 1,
            Name = "Abstract Expression No. 7",
            Category = "Fine Art",
            ImageUrl = "https://images.unsplash.com/photo-1541961017774-22349e4a1262?w=600&h=750&fit=crop",
            StartingPrice = 850,
            CurrentPrice = 1420,
            Status = "Live",
            TimeRemaining = "2d 14h left"
        },
        new()
        {
            Id = 2,
            Name = "Vintage Leica M3 Camera",
            Category = "Collectibles",
            ImageUrl = "https://images.unsplash.com/photo-1526170375885-4d8ecf77b99f?w=600&h=750&fit=crop",
            StartingPrice = 1200,
            CurrentPrice = 2850,
            Status = "Live",
            TimeRemaining = "5h 32m left"
        },
        new()
        {
            Id = 3,
            Name = "Mid-Century Walnut Chair",
            Category = "Furniture",
            ImageUrl = "https://images.unsplash.com/photo-1586023492125-27b2c045efd7?w=600&h=750&fit=crop",
            StartingPrice = 400,
            CurrentPrice = 675,
            Status = "Live",
            TimeRemaining = "1d 8h left"
        },
        new()
        {
            Id = 4,
            Name = "Ceramic Vase — Kyoto Studio",
            Category = "Decor",
            ImageUrl = "https://images.unsplash.com/photo-1578749556568-bc2c40e68b7a?w=600&h=750&fit=crop",
            StartingPrice = 150,
            CurrentPrice = 310,
            Status = "Live",
            TimeRemaining = "18h 45m left"
        },
        new()
        {
            Id = 5,
            Name = "First Edition — The Great Gatsby",
            Category = "Books",
            ImageUrl = "https://images.unsplash.com/photo-1544947950-fa07a98d237f?w=600&h=750&fit=crop",
            StartingPrice = 2000,
            CurrentPrice = 4200,
            Status = "Live",
            TimeRemaining = "3d 2h left"
        },
        new()
        {
            Id = 6,
            Name = "Swiss Automatic Watch 1962",
            Category = "Watches",
            ImageUrl = "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=600&h=750&fit=crop",
            StartingPrice = 950,
            CurrentPrice = 1780,
            Status = "Live",
            TimeRemaining = "6h 10m left"
        },
        new()
        {
            Id = 7,
            Name = "Oil Landscape — Provence 1924",
            Category = "Fine Art",
            ImageUrl = "https://images.unsplash.com/photo-1579783902614-a3fb3927b6a5?w=600&h=750&fit=crop",
            StartingPrice = 3200,
            CurrentPrice = 5100,
            Status = "Live",
            TimeRemaining = "4d 6h left"
        },
        new()
        {
            Id = 8,
            Name = "Signed Vinyl — Abbey Road",
            Category = "Collectibles",
            ImageUrl = "https://images.unsplash.com/photo-1493225457124-a3eb161ffa5f?w=600&h=750&fit=crop",
            StartingPrice = 600,
            CurrentPrice = 980,
            Status = "Ending Soon",
            TimeRemaining = "45m left"
        },
        new()
        {
            Id = 9,
            Name = "Teak Dining Table Set",
            Category = "Furniture",
            ImageUrl = "https://images.unsplash.com/photo-1617806118773-12e932ad114a?w=600&h=750&fit=crop",
            StartingPrice = 750,
            CurrentPrice = 1120,
            Status = "Live",
            TimeRemaining = "2d 3h left"
        },
        new()
        {
            Id = 10,
            Name = "Handwoven Persian Rug",
            Category = "Decor",
            ImageUrl = "https://images.unsplash.com/photo-1600166898405-da953520ced3?w=600&h=750&fit=crop",
            StartingPrice = 1100,
            CurrentPrice = 1890,
            Status = "Live",
            TimeRemaining = "1d 20h left"
        },
        new()
        {
            Id = 11,
            Name = "Rare Comic — Amazing Fantasy #15",
            Category = "Books",
            ImageUrl = "https://images.unsplash.com/photo-1612036782180-6f0b6cd846fe?w=600&h=750&fit=crop",
            StartingPrice = 4500,
            CurrentPrice = 7200,
            Status = "Live",
            TimeRemaining = "5d 11h left"
        },
        new()
        {
            Id = 12,
            Name = "Rolex Submariner 1680",
            Category = "Watches",
            ImageUrl = "https://images.unsplash.com/photo-1524593362214-995a5aa60ca0?w=600&h=750&fit=crop",
            StartingPrice = 6800,
            CurrentPrice = 9450,
            Status = "Ending Soon",
            TimeRemaining = "1h 12m left"
        }
    ];

    public static List<AuctionItemViewModel> GetFeaturedAuctions() =>
        GetAllAuctions().Take(6).ToList();

    public static List<CategoryViewModel> GetCategories()
    {
        var auctions = GetAllAuctions();
        return auctions
            .GroupBy(a => a.Category)
            .Select(g => new CategoryViewModel { Name = g.Key, ItemCount = g.Count() })
            .OrderBy(c => c.Name)
            .ToList();
    }

    public static List<SellerViewModel> GetBestSellers() =>
    [
        new()
        {
            Id = 1,
            Username = "ElenaVoss",
            AvatarUrl = "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=200&h=200&fit=crop&crop=face",
            AuctionCount = 48,
            SuccessfulSales = 41,
            Rating = 4.9
        },
        new()
        {
            Id = 2,
            Username = "MarcusChen",
            AvatarUrl = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=200&h=200&fit=crop&crop=face",
            AuctionCount = 36,
            SuccessfulSales = 33,
            Rating = 4.8
        },
        new()
        {
            Id = 3,
            Username = "SofiaArtGallery",
            AvatarUrl = "https://images.unsplash.com/photo-1438761681033-6461ffad8d80?w=200&h=200&fit=crop&crop=face",
            AuctionCount = 72,
            SuccessfulSales = 68,
            Rating = 5.0
        },
        new()
        {
            Id = 4,
            Username = "JamesRetro",
            AvatarUrl = "https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=200&h=200&fit=crop&crop=face",
            AuctionCount = 29,
            SuccessfulSales = 25,
            Rating = 4.6
        }
    ];
}
