namespace OnlineAuction.Helpers;

public static class CategoryImages
{
    private static readonly Dictionary<string, string> ImageByName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Pokémon"] = "/images/categories/pokemon.png",
        ["One Piece"] = "/images/categories/one-piece.png",
        ["Yu-Gi-Oh!"] = "/images/categories/yu-gi-oh.jpg",
        ["Sports"] = "/images/categories/sports.jpg",
        ["Magic: The Gathering"] =
            "https://cards.scryfall.io/large/front/b/0/b0faa7f2-b547-42c4-a810-839da50dadfe.jpg?1559591477"
    };

    public static string GetImageUrl(string categoryName) =>
        ImageByName.TryGetValue(categoryName, out var image)
            ? image
            : "/images/categories/one-piece.png";
}
