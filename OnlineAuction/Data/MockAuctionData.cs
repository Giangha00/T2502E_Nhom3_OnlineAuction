using OnlineAuction.Models;

namespace OnlineAuction.Data;

public static class MockAuctionData
{
    private static readonly string[] CategoryOrder =
    [
        "Pokémon",
        "One Piece",
        "Yu-Gi-Oh!",
        "Sports"
    ];

    private static readonly Dictionary<string, (string Image, string DisplayCount)> CategoryMeta = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Pokémon"] = ("/images/categories/pokemon.png", "1,240+ Items"),
        ["One Piece"] = ("/images/categories/one-piece.png", "860+ Items"),
        ["Yu-Gi-Oh!"] = ("/images/categories/yu-gi-oh.jpg", "540+ Items"),
        ["Sports"] = ("/images/categories/sports.jpg", "920+ Items"),
        ["Magic: The Gathering"] = ("https://cards.scryfall.io/large/front/b/0/b0faa7f2-b547-42c4-a810-839da50dadfe.jpg?1559591477", "320+ Items")
    };

    public static string GetCategoryImageUrl(string categoryName) =>
        CategoryMeta.TryGetValue(categoryName, out var meta)
            ? meta.Image
            : "/images/categories/pokemon.png";

    public static List<AuctionItemViewModel> GetAllAuctions() =>
    [
        new() { Id = 1, Name = "Charizard 1st Edition Holo", Category = "Pokémon", Subtitle = "Base Set · 1999 · Wizards of the Coast", Grade = "PSA 10", Year = 1999, IsHot = true, ImageUrl = "https://images.unsplash.com/photo-1612036782180-6f0b6cd846fe?w=600&h=750&fit=crop", StartingPrice = 85000, CurrentPrice = 124500, Status = "Live", TimeRemaining = "2d 14h left" },
        new() { Id = 2, Name = "Blue-Eyes White Dragon LOB", Category = "Yu-Gi-Oh!", Subtitle = "Legend of Blue Eyes · 2002 · Konami", Grade = "PSA 10", Year = 2002, IsHot = true, ImageUrl = "https://images.unsplash.com/photo-1606107557195-0a29cbf1f2b3?w=600&h=750&fit=crop", StartingPrice = 12000, CurrentPrice = 18500, Status = "Live", TimeRemaining = "1d 8h left" },
        new() { Id = 3, Name = "Gear 5 Luffy Manga Rare", Category = "One Piece", Subtitle = "OP-05 Awakening · 2023 · Bandai", Grade = "BGS 9.5", Year = 2023, IsHot = true, ImageUrl = "https://images.unsplash.com/photo-1613771404721-1f92d799e49f?w=600&h=750&fit=crop", StartingPrice = 4200, CurrentPrice = 6800, Status = "Ending Soon", TimeRemaining = "5h 42m left" },
        new() { Id = 4, Name = "LeBron James Topps Chrome RC", Category = "Sports", Subtitle = "2003 Topps Chrome · Upper Deck", Grade = "PSA 10", Year = 2003, IsHot = true, ImageUrl = "https://images.unsplash.com/photo-1546519638-68e109498ffc?w=600&h=750&fit=crop", StartingPrice = 98000, CurrentPrice = 142000, Status = "Live", TimeRemaining = "3d 2h left" },
        new() { Id = 5, Name = "Black Lotus Alpha", Category = "Magic: The Gathering", Subtitle = "Alpha Edition · 1993 · Wizards", Grade = "CGC 8.5", Year = 1993, IsHot = false, ImageUrl = "https://images.unsplash.com/photo-1518709268805-4e9042af2176?w=600&h=750&fit=crop", StartingPrice = 180000, CurrentPrice = 245000, Status = "Ending Soon", TimeRemaining = "1h 12m left" },
        new() { Id = 6, Name = "Pikachu Illustrator Promo", Category = "Pokémon", Subtitle = "CoroCoro Promo · 1998 · Japanese", Grade = "PSA 9", Year = 1998, IsHot = false, ImageUrl = "https://images.unsplash.com/photo-1639762681485-074b7f938ba0?w=600&h=750&fit=crop", StartingPrice = 220000, CurrentPrice = 285000, Status = "Live", TimeRemaining = "6d 4h left" },
        new() { Id = 7, Name = "Shanks SEC Parallel", Category = "One Piece", Subtitle = "OP-09 · 2024 · Bandai", Grade = "PSA 10", Year = 2024, IsHot = false, ImageUrl = "https://images.unsplash.com/photo-1578632767115-351597cf2477?w=600&h=750&fit=crop", StartingPrice = 1800, CurrentPrice = 2650, Status = "Live", TimeRemaining = "18h 45m left" },
        new() { Id = 8, Name = "Dark Magician Girl 1st Ed", Category = "Yu-Gi-Oh!", Subtitle = "Magician's Force · 2003", Grade = "PSA 10", Year = 2003, IsHot = false, ImageUrl = "https://images.unsplash.com/photo-1565538810643-b5bdb4dfa845?w=600&h=750&fit=crop", StartingPrice = 8500, CurrentPrice = 11200, Status = "Ending Soon", TimeRemaining = "45m left" },
        new() { Id = 9, Name = "Mickey Mantle 1952 Topps", Category = "Sports", Subtitle = "1952 Topps · #311 · PSA Authentic", Grade = "PSA 8", Year = 1952, IsHot = false, ImageUrl = "https://images.unsplash.com/photo-1606107557195-0a29b4b9efab?w=600&h=750&fit=crop", StartingPrice = 45000, CurrentPrice = 62000, Status = "Live", TimeRemaining = "2d 3h left" },
        new() { Id = 10, Name = "Mox Sapphire Unlimited", Category = "Magic: The Gathering", Subtitle = "Unlimited · 1993 · Wizards", Grade = "BGS 9", Year = 1993, IsHot = false, ImageUrl = "https://images.unsplash.com/photo-1606169046337-54513793d481?w=600&h=750&fit=crop", StartingPrice = 28000, CurrentPrice = 34500, Status = "Live", TimeRemaining = "1d 20h left" },
        new() { Id = 11, Name = "Umbreon Gold Star", Category = "Pokémon", Subtitle = "EX Unseen Forces · 2005", Grade = "PSA 10", Year = 2005, IsHot = false, ImageUrl = "https://images.unsplash.com/photo-1613771404721-1f92d799e49f?w=600&h=750&fit=crop", StartingPrice = 15000, CurrentPrice = 19800, Status = "Live", TimeRemaining = "2d 5h left" },
        new() { Id = 12, Name = "Luffy Gear 5 Manga Alt", Category = "One Piece", Subtitle = "ST-01 Starter · 2023", Grade = "PSA 10", Year = 2023, IsHot = false, ImageUrl = "https://images.unsplash.com/photo-1612036782180-6f0b6cd846fe?w=600&h=750&fit=crop", StartingPrice = 3200, CurrentPrice = 4100, Status = "Ending Soon", TimeRemaining = "3h 20m left" },
        new() { Id = 13, Name = "Michael Jordan Fleer RC", Category = "Sports", Subtitle = "1986 Fleer · #57 · PSA 10", Grade = "PSA 10", Year = 1986, IsHot = false, ImageUrl = "https://images.unsplash.com/photo-1639762681485-074b7f938ba0?w=600&h=750&fit=crop", StartingPrice = 95000, CurrentPrice = 118000, Status = "Live", TimeRemaining = "3d 1h left" },
        new() { Id = 14, Name = "Red-Eyes B. Dragon LOB", Category = "Yu-Gi-Oh!", Subtitle = "Legend of Blue Eyes · 2002", Grade = "PSA 9", Year = 2002, IsHot = false, ImageUrl = "https://images.unsplash.com/photo-1606107557195-0a29cbf1f2b3?w=600&h=750&fit=crop", StartingPrice = 4200, CurrentPrice = 5800, Status = "Live", TimeRemaining = "6h 50m left" },
        new() { Id = 15, Name = "Time Walk Alpha", Category = "Magic: The Gathering", Subtitle = "Alpha · 1993 · Wizards", Grade = "PSA 8", Year = 1993, IsHot = false, ImageUrl = "https://images.unsplash.com/photo-1518709268805-4e9042af2176?w=600&h=750&fit=crop", StartingPrice = 12000, CurrentPrice = 15800, Status = "Live", TimeRemaining = "6d 4h left" },
        new() { Id = 16, Name = "Rayquaza Gold Star", Category = "Pokémon", Subtitle = "EX Deoxys · 2005", Grade = "PSA 10", Year = 2005, IsHot = false, ImageUrl = "https://images.unsplash.com/photo-1612036782180-6f0b6cd846fe?w=600&h=750&fit=crop", StartingPrice = 22000, CurrentPrice = 28500, Status = "Live", TimeRemaining = "2d 18h left" },
        new() { Id = 17, Name = "Patrick Mahomes Prizm RC", Category = "Sports", Subtitle = "2017 Prizm · #269 · PSA 10", Grade = "PSA 10", Year = 2017, IsHot = false, ImageUrl = "https://images.unsplash.com/photo-1546519638-68e109498ffc?w=600&h=750&fit=crop", StartingPrice = 18000, CurrentPrice = 22400, Status = "Live", TimeRemaining = "1d 6h left" },
        new() { Id = 18, Name = "Ace SEC Comic Parallel", Category = "One Piece", Subtitle = "OP-03 Pillars · 2023", Grade = "BGS 9.5", Year = 2023, IsHot = false, ImageUrl = "https://images.unsplash.com/photo-1613771404721-1f92d799e49f?w=600&h=750&fit=crop", StartingPrice = 2800, CurrentPrice = 3900, Status = "Ending Soon", TimeRemaining = "55m left" }
    ];

    public static List<AuctionItemViewModel> GetHotAuctions() =>
        GetAllAuctions().Where(a => a.IsHot).Take(4).ToList();

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
        return GetAllAuctions().Where(a => (a.Id - 1) % sellers.Count == sellerIndex).ToList();
    }

    public static List<AuctionItemViewModel> GetAuctionsByIds(IEnumerable<int> ids) =>
        GetAllAuctions().Where(a => ids.Contains(a.Id)).ToList();

    public static List<AuctionItemViewModel> GetFeaturedAuctions() =>
        GetAllAuctions().Take(6).ToList();

    public static List<AuctionItemViewModel> GetWonAuctions() =>
    [
        new() { Id = 3, Name = "Gear 5 Luffy Manga Rare", Category = "One Piece", Subtitle = "OP-05 · 2023", Grade = "BGS 9.5", Year = 2023, ImageUrl = "https://images.unsplash.com/photo-1613771404721-1f92d799e49f?w=600&h=750&fit=crop", StartingPrice = 4200, CurrentPrice = 6800, Status = "Won", TimeRemaining = "Pay within 3 days" },
        new() { Id = 8, Name = "Dark Magician Girl 1st Ed", Category = "Yu-Gi-Oh!", Subtitle = "Magician's Force · 2003", Grade = "PSA 10", Year = 2003, ImageUrl = "https://images.unsplash.com/photo-1565538810643-b5bdb4dfa845?w=600&h=750&fit=crop", StartingPrice = 8500, CurrentPrice = 11200, Status = "Won", TimeRemaining = "Pay within 3 days" }
    ];

    public static IReadOnlyList<string> GetCategoryNames() => CategoryOrder;

    public static List<CategoryViewModel> GetCategories()
    {
        var counts = GetAllAuctions().GroupBy(a => a.Category).ToDictionary(g => g.Key, g => g.Count());
        return CategoryOrder.Select(name =>
        {
            var meta = CategoryMeta.GetValueOrDefault(name);
            return new CategoryViewModel
            {
                Name = name,
                ItemCount = counts.GetValueOrDefault(name, 0),
                ImageUrl = meta.Image,
                DisplayCount = meta.DisplayCount
            };
        }).ToList();
    }

    public static List<SellerViewModel> GetBestSellers() =>
    [
        new() { Id = 1, Username = "Elite Collector", AvatarUrl = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=200&h=200&fit=crop&crop=face", AuctionCount = 124, SuccessfulSales = 98, Rating = 4.9 },
        new() { Id = 2, Username = "Card Haven", AvatarUrl = "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=200&h=200&fit=crop&crop=face", AuctionCount = 86, SuccessfulSales = 72, Rating = 4.8 },
        new() { Id = 3, Username = "TCG Vault", AvatarUrl = "https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=200&h=200&fit=crop&crop=face", AuctionCount = 210, SuccessfulSales = 195, Rating = 5.0 },
        new() { Id = 4, Username = "Pro Graded Assets", AvatarUrl = "https://images.unsplash.com/photo-1438761681033-6461ffad8d80?w=200&h=200&fit=crop&crop=face", AuctionCount = 64, SuccessfulSales = 58, Rating = 4.7 },
        new() { Id = 5, Username = "Vault Direct", AvatarUrl = "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=200&h=200&fit=crop&crop=face", AuctionCount = 45, SuccessfulSales = 41, Rating = 4.9 }
    ];

    public static List<VaultPostViewModel> GetVaultPosts() =>
    [
        new() { Tag = "GUIDE", Title = "The Rise of Vintage Pokémon Cards in 2024", Excerpt = "How PSA 10 Base Set holos became the blue-chip asset of the hobby.", ImageUrl = "https://images.unsplash.com/photo-1612036782180-6f0b6cd846fe?w=800&h=500&fit=crop" },
        new() { Tag = "NEWS", Title = "One Piece TCG Breaks Auction Records", Excerpt = "Manga rare parallels are commanding six-figure bids at major houses.", ImageUrl = "https://images.unsplash.com/photo-1613771404721-1f92d799e49f?w=800&h=500&fit=crop" },
        new() { Tag = "GUIDE", Title = "Understanding Card Grading: PSA vs BGS", Excerpt = "A collector's guide to choosing the right slab for your portfolio.", ImageUrl = "https://images.unsplash.com/photo-1606107557195-0a29cbf1f2b3?w=800&h=500&fit=crop" }
    ];
}
