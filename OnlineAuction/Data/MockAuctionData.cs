using OnlineAuction.Models;

namespace OnlineAuction.Data;

public static class MockAuctionData
{
    private static readonly string[] CategoryOrder =
    [
        "Cars",
        "Watches",
        "Cards",
        "Billiard Sticks",
        "Jewelry"
    ];

    public static List<AuctionItemViewModel> GetAllAuctions() =>
    [
        new()
        {
            Id = 1,
            Name = "1967 Ford Mustang Fastback",
            Category = "Cars",
            ImageUrl = "https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?w=600&h=750&fit=crop",
            StartingPrice = 28000,
            CurrentPrice = 34500,
            Status = "Live",
            TimeRemaining = "3d 6h left"
        },
        new()
        {
            Id = 2,
            Name = "Porsche 911 Carrera 1989",
            Category = "Cars",
            ImageUrl = "https://images.unsplash.com/photo-1503376780353-7e6692767b70?w=600&h=750&fit=crop",
            StartingPrice = 42000,
            CurrentPrice = 51800,
            Status = "Live",
            TimeRemaining = "1d 14h left"
        },
        new()
        {
            Id = 3,
            Name = "Rolex Submariner Date 1680",
            Category = "Watches",
            ImageUrl = "https://images.unsplash.com/photo-1611652022419-a9419f74343d?w=600&h=750&fit=crop",
            StartingPrice = 6800,
            CurrentPrice = 9450,
            Status = "Ending Soon",
            TimeRemaining = "1h 12m left"
        },
        new()
        {
            Id = 4,
            Name = "Omega Speedmaster Professional",
            Category = "Watches",
            ImageUrl = "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=600&h=750&fit=crop",
            StartingPrice = 3200,
            CurrentPrice = 4100,
            Status = "Live",
            TimeRemaining = "2d 8h left"
        },
        new()
        {
            Id = 5,
            Name = "Charizard Holo 1st Edition",
            Category = "Cards",
            ImageUrl = "https://images.unsplash.com/photo-1612036782180-6f0b6cd846fe?w=600&h=750&fit=crop",
            StartingPrice = 8500,
            CurrentPrice = 12400,
            Status = "Live",
            TimeRemaining = "4d 2h left"
        },
        new()
        {
            Id = 6,
            Name = "Michael Jordan Rookie PSA 10",
            Category = "Cards",
            ImageUrl = "https://images.unsplash.com/photo-1639762681485-074b7f938ba0?w=600&h=750&fit=crop",
            StartingPrice = 15000,
            CurrentPrice = 18200,
            Status = "Live",
            TimeRemaining = "5d 11h left"
        },
        new()
        {
            Id = 7,
            Name = "McDermott G-Core Pool Cue",
            Category = "Billiard Sticks",
            ImageUrl = "https://images.unsplash.com/photo-1609710228159-0fa9bd7c0827?w=600&h=750&fit=crop",
            StartingPrice = 450,
            CurrentPrice = 720,
            Status = "Live",
            TimeRemaining = "18h 45m left"
        },
        new()
        {
            Id = 8,
            Name = "Predator 314 Shaft Limited",
            Category = "Billiard Sticks",
            ImageUrl = "https://images.unsplash.com/photo-1578662996442-48f60103fc96?w=600&h=750&fit=crop",
            StartingPrice = 380,
            CurrentPrice = 540,
            Status = "Ending Soon",
            TimeRemaining = "45m left"
        },
        new()
        {
            Id = 9,
            Name = "Diamond Tennis Bracelet 18K",
            Category = "Jewelry",
            ImageUrl = "https://images.unsplash.com/photo-1515562141207-7a88fb7ce338?w=600&h=750&fit=crop",
            StartingPrice = 5200,
            CurrentPrice = 6800,
            Status = "Live",
            TimeRemaining = "2d 3h left"
        },
        new()
        {
            Id = 10,
            Name = "Sapphire Engagement Ring 2ct",
            Category = "Jewelry",
            ImageUrl = "https://images.unsplash.com/photo-1605100804763-247f67b3557e?w=600&h=750&fit=crop",
            StartingPrice = 7600,
            CurrentPrice = 9200,
            Status = "Live",
            TimeRemaining = "1d 20h left"
        },
        new()
        {
            Id = 11,
            Name = "Chevrolet Camaro SS 1969",
            Category = "Cars",
            ImageUrl = "https://images.unsplash.com/photo-1552519507-da3b142c6e3d?w=600&h=750&fit=crop",
            StartingPrice = 35000,
            CurrentPrice = 41200,
            Status = "Live",
            TimeRemaining = "2d 5h left"
        },
        new()
        {
            Id = 12,
            Name = "BMW M3 E46 Coupe",
            Category = "Cars",
            ImageUrl = "https://images.unsplash.com/photo-1555215695-3004980ad54e?w=600&h=750&fit=crop",
            StartingPrice = 22000,
            CurrentPrice = 26800,
            Status = "Ending Soon",
            TimeRemaining = "3h 20m left"
        },
        new()
        {
            Id = 13,
            Name = "Tag Heuer Carrera Chronograph",
            Category = "Watches",
            ImageUrl = "https://images.unsplash.com/photo-1614164185128-e4ec99c436d7?w=600&h=750&fit=crop",
            StartingPrice = 2800,
            CurrentPrice = 3350,
            Status = "Live",
            TimeRemaining = "3d 1h left"
        },
        new()
        {
            Id = 14,
            Name = "Seiko 6139 Pogue Automatic",
            Category = "Watches",
            ImageUrl = "https://images.unsplash.com/photo-1587836374828-4dbafa94cf0e?w=600&h=750&fit=crop",
            StartingPrice = 1900,
            CurrentPrice = 2450,
            Status = "Live",
            TimeRemaining = "6h 50m left"
        },
        new()
        {
            Id = 15,
            Name = "Pikachu Illustrator Promo",
            Category = "Cards",
            ImageUrl = "https://images.unsplash.com/photo-1613771404721-1f92d799e49f?w=600&h=750&fit=crop",
            StartingPrice = 22000,
            CurrentPrice = 28500,
            Status = "Live",
            TimeRemaining = "6d 4h left"
        },
        new()
        {
            Id = 16,
            Name = "LeBron James Topps Chrome RC",
            Category = "Cards",
            ImageUrl = "https://images.unsplash.com/photo-1546519638-68e109498ffc?w=600&h=750&fit=crop",
            StartingPrice = 9800,
            CurrentPrice = 11300,
            Status = "Live",
            TimeRemaining = "2d 18h left"
        },
        new()
        {
            Id = 17,
            Name = "Meucci Elite Series Cue",
            Category = "Billiard Sticks",
            ImageUrl = "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=600&h=750&fit=crop",
            StartingPrice = 620,
            CurrentPrice = 890,
            Status = "Live",
            TimeRemaining = "1d 6h left"
        },
        new()
        {
            Id = 18,
            Name = "Pearl Necklace South Sea 14K",
            Category = "Jewelry",
            ImageUrl = "https://images.unsplash.com/photo-1515562141207-7a88fb7ce338?w=600&h=750&fit=crop",
            StartingPrice = 4300,
            CurrentPrice = 5100,
            Status = "Ending Soon",
            TimeRemaining = "55m left"
        }
    ];

    public static AuctionItemViewModel? GetAuctionById(int id) =>
        GetAllAuctions().FirstOrDefault(a => a.Id == id);

    public static SellerViewModel? GetSellerForAuction(int auctionId)
    {
        var sellers = GetBestSellers();
        if (sellers.Count == 0) return null;
        return sellers[(auctionId - 1) % sellers.Count];
    }

    public static List<AuctionItemViewModel> GetAuctionsBySellerId(int sellerId)
    {
        var sellers = GetBestSellers();
        var sellerIndex = sellers.FindIndex(s => s.Id == sellerId);
        if (sellerIndex < 0) return [];

        return GetAllAuctions()
            .Where(a => (a.Id - 1) % sellers.Count == sellerIndex)
            .ToList();
    }

    public static List<AuctionItemViewModel> GetAuctionsByIds(IEnumerable<int> ids) =>
        GetAllAuctions().Where(a => ids.Contains(a.Id)).ToList();

    public static List<AuctionItemViewModel> GetFeaturedAuctions() =>
        GetAllAuctions().Take(6).ToList();

    public static List<AuctionItemViewModel> GetWonAuctions() =>
    [
        new()
        {
            Id = 3,
            Name = "Rolex Submariner Date 1680",
            Category = "Watches",
            ImageUrl = "https://images.unsplash.com/photo-1611652022419-a9419f74343d?w=600&h=750&fit=crop",
            StartingPrice = 6800,
            CurrentPrice = 9450,
            Status = "Won",
            TimeRemaining = "Pay within 3 days"
        },
        new()
        {
            Id = 8,
            Name = "1952 Topps Mickey Mantle Rookie",
            Category = "Cards",
            ImageUrl = "https://images.unsplash.com/photo-1613771404721-1f92d799e49f?w=600&h=750&fit=crop",
            StartingPrice = 12000,
            CurrentPrice = 18750,
            Status = "Won",
            TimeRemaining = "Pay within 3 days"
        }
    ];

    public static IReadOnlyList<string> GetCategoryNames() => CategoryOrder;

    public static List<CategoryViewModel> GetCategories()
    {
        var counts = GetAllAuctions()
            .GroupBy(a => a.Category)
            .ToDictionary(g => g.Key, g => g.Count());

        return CategoryOrder
            .Select(name => new CategoryViewModel
            {
                Name = name,
                ItemCount = counts.GetValueOrDefault(name, 0)
            })
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
