using System.Net;

namespace OnlineAuction.Data;

/// <summary>
/// Sample auction catalog from team spreadsheet (Pokémon test listings).
/// </summary>
public static class SpreadsheetAuctionCatalog
{
    public const string TestAuctionEventName = "RareCard Vault Test Auctions";

    public sealed record Entry(
        string CategoryName,
        string Name,
        string Description,
        string PrimaryImage,
        decimal StartingPrice,
        string GradeLabel,
        string SetName,
        int Year,
        string Language,
        string CardNumber,
        string Condition,
        int EndMinutes,
        int ExistingBidCount = 0);

    public static IReadOnlyList<Entry> GetEntries() =>
    [
        CreateEntry(
            "Pokémon",
            "PSA 10 Charizard Holo #4/102 Base Set Unlimited 1999",
            "Item specifics | Condition: Graded - PSA 10 | Card Name: Charizard | Card Number: 4/102 | Set: Base Set Unlimited | Year: 1999 | Language: English | Manufacturer: Wizards of the Coast | Rarity: Holo Rare | Game: Pokemon TCG | Certification: PSA Gem Mint 10 | Tham khảo eBay sold 2026: ~$30,100",
            "https://images.pokemontcg.io/base1/4_hires.png",
            25_000,
            "PSA 10",
            "Base Set Unlimited",
            1999,
            "English",
            "4/102",
            endMinutes: 10),
        CreateEntry(
            "Pokémon",
            "PSA 10 Charizard 1st Edition Holo #4 Base Set Shadowless 1999",
            "Item specifics | Condition: Graded - PSA 10 | Card Name: Charizard | Card Number: 4/102 | Set: Base Set 1st Edition Shadowless | Year: 1999 | Language: English | Manufacturer: Wizards of the Coast | Rarity: Holo Rare | Features: 1st Edition Shadowless | Certification: PSA Gem Mint 10 | Tham khảo thị trường 2026: $300,000 - $954,800 (Heritage/eBay)",
            "https://images.pokemontcg.io/base1/4_hires.png",
            350_000,
            "PSA 10",
            "Base Set 1st Edition Shadowless",
            1999,
            "English",
            "4/102",
            endMinutes: 9,
            existingBidCount: 1),
        CreateEntry(
            "Pokémon",
            "PSA 10 Blastoise 1st Edition Holo #2 Base Set 1999",
            "Item specifics | Condition: Graded - PSA 10 | Card Name: Blastoise | Card Number: 2/102 | Set: Base Set 1st Edition | Year: 1999 | Language: English | Manufacturer: Wizards of the Coast | Rarity: Holo Rare | Certification: PSA Gem Mint 10 | Tham khảo thị trường 2026: ~$45,000",
            "https://images.pokemontcg.io/base1/2_hires.png",
            38_000,
            "PSA 10",
            "Base Set 1st Edition",
            1999,
            "English",
            "2/102",
            endMinutes: 9),
        CreateEntry(
            "Pokémon",
            "PSA 10 Venusaur 1st Edition Holo #15 Base Set 1999",
            "Item specifics | Condition: Graded - PSA 10 | Card Name: Venusaur | Card Number: 15/102 | Set: Base Set 1st Edition | Year: 1999 | Language: English | Manufacturer: Wizards of the Coast | Rarity: Holo Rare | Certification: PSA Gem Mint 10 | Tham khảo thị trường 2026: ~$35,000",
            "https://images.pokemontcg.io/base1/15_hires.png",
            30_000,
            "PSA 10",
            "Base Set 1st Edition",
            1999,
            "English",
            "15/102",
            endMinutes: 8,
            existingBidCount: 1),
        CreateEntry(
            "Pokémon",
            "PSA 10 Mewtwo 1st Edition Holo #10 Base Set 1999",
            "Item specifics | Condition: Graded - PSA 10 | Card Name: Mewtwo | Card Number: 10/102 | Set: Base Set 1st Edition | Year: 1999 | Language: English | Manufacturer: Wizards of the Coast | Rarity: Holo Rare | Certification: PSA Gem Mint 10 | Tham khảo eBay/Heritage 2026: ~$28,000",
            "https://images.pokemontcg.io/base1/10_hires.png",
            22_000,
            "PSA 10",
            "Base Set 1st Edition",
            1999,
            "English",
            "10/102",
            endMinutes: 8),
        CreateEntry(
            "Pokémon",
            "PSA 10 Umbreon Gold Star #17 EX Unseen Forces 2005",
            "Item specifics | Condition: Graded - PSA 10 | Card Name: Umbreon Gold Star | Card Number: 17/115 | Set: EX Unseen Forces | Year: 2005 | Language: English | Manufacturer: Wizards of the Coast | Rarity: Gold Star Ultra Rare | Certification: PSA Gem Mint 10 | Tham khảo thị trường 2026: ~$15,000 - $20,000",
            "https://images.pokemontcg.io/ex10/17_hires.png",
            12_000,
            "PSA 10",
            "EX Unseen Forces",
            2005,
            "English",
            "17/115",
            endMinutes: 7),
        CreateEntry(
            "Pokémon",
            "PSA 10 Rayquaza Gold Star #107 EX Deoxys 2005",
            "Item specifics | Condition: Graded - PSA 10 | Card Name: Rayquaza Gold Star | Card Number: 107/107 | Set: EX Deoxys | Year: 2005 | Language: English | Manufacturer: Wizards of the Coast | Rarity: Gold Star Ultra Rare | Certification: PSA Gem Mint 10 | Tham khảo thị trường 2026: ~$22,000 - $28,500",
            "https://images.pokemontcg.io/ex8/107_hires.png",
            18_000,
            "PSA 10",
            "EX Deoxys",
            2005,
            "English",
            "107/107",
            endMinutes: 7,
            existingBidCount: 2),
        CreateEntry(
            "Pokémon",
            "PSA 10 Shining Charizard #107 Neo Destiny 2002",
            "Item specifics | Condition: Graded - PSA 10 | Card Name: Shining Charizard | Card Number: 107/105 | Set: Neo Destiny | Year: 2002 | Language: English | Manufacturer: Wizards of the Coast | Rarity: Shining Holo Rare | Certification: PSA Gem Mint 10 | Tham khảo thị trường 2026: ~$20,000 - $25,000",
            "https://images.pokemontcg.io/neo4/107_hires.png",
            16_000,
            "PSA 10",
            "Neo Destiny",
            2002,
            "English",
            "107/105",
            endMinutes: 6),
        CreateEntry(
            "Pokémon",
            "PSA 10 Lugia 1st Edition Holo #9 Neo Genesis 2000",
            "Item specifics | Condition: Graded - PSA 10 | Card Name: Lugia | Card Number: 9/111 | Set: Neo Genesis 1st Edition | Year: 2000 | Language: English | Manufacturer: Wizards of the Coast | Rarity: Holo Rare | Certification: PSA Gem Mint 10 | Tham khảo thị trường 2026: ~$12,000 - $18,000",
            "https://images.pokemontcg.io/neo1/9_hires.png",
            10_000,
            "PSA 10",
            "Neo Genesis 1st Edition",
            2000,
            "English",
            "9/111",
            endMinutes: 6,
            existingBidCount: 1),
        CreateEntry(
            "Pokémon",
            "PSA 10 Pikachu Red Cheeks #58 Base Set Unlimited 1999",
            "Item specifics | Condition: Graded - PSA 10 | Card Name: Pikachu | Card Number: 58/102 | Set: Base Set Unlimited | Year: 1999 | Language: English | Manufacturer: Wizards of the Coast | Rarity: Common (Red Cheeks variant) | Certification: PSA Gem Mint 10 | Tham khảo eBay 2026: ~$800 - $1,500",
            "https://images.pokemontcg.io/base1/58_hires.png",
            600,
            "PSA 10",
            "Base Set Unlimited",
            1999,
            "English",
            "58/102",
            endMinutes: 5),
        CreateEntry(
            "Pokémon",
            "PSA 9 Pikachu Illustrator Promo 1998 Japanese",
            "Item specifics | Condition: Graded - PSA 9 | Card Name: Pikachu Illustrator | Set: CoroCoro Comic Promo | Year: 1998 | Language: Japanese | Country of Origin: Japan | Manufacturer: Media Factory | Rarity: Promo (39 copies awarded) | Certification: PSA Mint 9 | Tham khảo Goldin Auction 2026: PSA 10 bán $16,492,000",
            "https://images.pokemontcg.io/basep/1_hires.png",
            4_000_000,
            "PSA 9",
            "CoroCoro Comic Promo",
            1998,
            "Japanese",
            "—",
            endMinutes: 5,
            existingBidCount: 1),
        CreateEntry(
            "Pokémon",
            "PSA 10 Gyarados 1st Edition Holo #6 Base Set 1999",
            "Item specifics | Condition: Graded - PSA 10 | Card Name: Gyarados | Card Number: 6/102 | Set: Base Set 1st Edition | Year: 1999 | Language: English | Manufacturer: Wizards of the Coast | Rarity: Holo Rare | Certification: PSA Gem Mint 10 | Tham khảo eBay 2026: ~$8,000 - $12,000",
            "https://images.pokemontcg.io/base1/6_hires.png",
            6_500,
            "PSA 10",
            "Base Set 1st Edition",
            1999,
            "English",
            "6/102",
            endMinutes: 4),
        CreateEntry(
            "Pokémon",
            "PSA 10 Alakazam 1st Edition Holo #1 Base Set 1999",
            "Item specifics | Condition: Graded - PSA 10 | Card Name: Alakazam | Card Number: 1/102 | Set: Base Set 1st Edition | Year: 1999 | Language: English | Manufacturer: Wizards of the Coast | Rarity: Holo Rare | Certification: PSA Gem Mint 10 | Tham khảo eBay 2026: ~$6,500 - $9,000",
            "https://images.pokemontcg.io/base1/1_hires.png",
            5_000,
            "PSA 10",
            "Base Set 1st Edition",
            1999,
            "English",
            "1/102",
            endMinutes: 10),
        CreateEntry(
            "Pokémon",
            "PSA 10 Charizard Holo #4/102 Base Set - Raw Near Mint",
            "Item specifics | Condition: Near Mint / Ungraded | Card Name: Charizard | Card Number: 4/102 | Set: Base Set Unlimited | Year: 1999 | Language: English | Manufacturer: Wizards of the Coast | Rarity: Holo Rare | Tham khảo eBay listing 2026: ~$550 (NM/LP raw)",
            "https://images.pokemontcg.io/base1/4_hires.png",
            400,
            "Near Mint",
            "Base Set Unlimited",
            1999,
            "English",
            "4/102",
            condition: "ungraded",
            endMinutes: 8),
        CreateEntry(
            "Pokémon",
            "PSA 8 Charizard 1st Edition Holo #4 Base Set 1999",
            "Item specifics | Condition: Graded - PSA 8 | Card Name: Charizard | Card Number: 4/102 | Set: Base Set 1st Edition | Year: 1999 | Language: English | Manufacturer: Wizards of the Coast | Rarity: Holo Rare | Certification: PSA NM-MT 8 | Tham khảo thị trường 2026: ~$10,000 - $18,000",
            "https://images.pokemontcg.io/base1/4_hires.png",
            8_000,
            "PSA 8",
            "Base Set 1st Edition",
            1999,
            "English",
            "4/102",
            endMinutes: 6,
            existingBidCount: 1)
    ];

    private static Entry CreateEntry(
        string categoryName,
        string name,
        string description,
        string primaryImage,
        decimal startingPrice,
        string gradeLabel,
        string setName,
        int year,
        string language,
        string cardNumber,
        int endMinutes,
        int existingBidCount = 0,
        string condition = "graded") =>
        new(
            categoryName,
            name,
            description,
            primaryImage,
            startingPrice,
            gradeLabel,
            setName,
            year,
            language,
            cardNumber,
            condition,
            endMinutes,
            existingBidCount);

    public static string BuildShortDescription(string description)
    {
        var parts = description.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? parts[1] : parts[0];
    }

    public static string BuildDescriptionHtml(string description)
    {
        var parts = description.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return "<p>—</p>";
        }

        var items = parts
            .Skip(parts[0].Equals("Item specifics", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
            .Select(part => $"<li>{WebUtility.HtmlEncode(part)}</li>");

        return $"<p><strong>Item specifics</strong></p><ul>{string.Concat(items)}</ul>";
    }

    public static decimal ComputeBidStep(decimal startingPrice) =>
        startingPrice switch
        {
            >= 1_000_000 => 10_000,
            >= 100_000 => 1_000,
            >= 10_000 => 100,
            >= 1_000 => 50,
            _ => 10
        };
}
