using System.Net;

namespace OnlineAuction.Data;

/// <summary>
/// Seed catalog used by the auction database and home page listings.
/// </summary>
public static class SpreadsheetAuctionCatalog
{
    public const string TestAuctionEventName = "RareCard Vault Soccer Card Auctions";

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
        CreateEntry("2004 Stadion World Stars Cristiano Ronaldo #722 PSA 10 GEM MINT",
            "A highly desirable early-career Cristiano Ronaldo collectible featuring the Portuguese superstar during his rise to global fame.",
            "https://cdn-vault.fanaticscollect.com/2020/12/28/11/medium/v93446_2020122813013580L_31.jpg",
            23_000, "PSA 10", "Stadion World Stars", 2004, "#722",
            "Player: Cristiano Ronaldo | Team: Manchester United / Portugal | Grade: PSA 10 Gem Mint | Population: Limited PSA 10 population",
            14_400, 5),
        CreateEntry("2003 Panini Sports Mega Craques Cristiano Ronaldo Rookie #137 BGS 8.5",
            "One of Ronaldo's most iconic rookie cards, showcasing his Sporting CP era before becoming a worldwide football legend.",
            "https://cdn-vault.fanaticscollect.com/2026/1/28/dt4/medium/v2553795_20260128162049543M_1.jpg",
            17_000, "BGS 8.5", "Panini Sports Mega Craques", 2003, "#137",
            "Type: Rookie Card (RC) | Team: Sporting CP | Grade: BGS 8.5",
            15_840, 2),
        CreateEntry("2003 Panini Sports Mega Craques Cristiano Ronaldo Rookie #137 PSA 10",
            "Considered one of the premier Cristiano Ronaldo rookie cards.",
            "https://dilxwvfkfup17.cloudfront.net/eyJ0YWciOiIiLCJ2YWx1ZSI6IjRRMlQvQm9jNCtUSHRxMDY4S2ZJbnhGeDdrM0p0ejcraXV6Z2lwR0kwKzA2WXhwZExSQmNveDZLQnNrTVFKeVhHa1Y2MUNZU25vZnBLOXhMQlNNbWlRPT0iLCJtYWMiOiJjYzNhZmY3NWE2MzUwMjkwMmQyNzViM2NkM2Q2NzYxNDg3NmMxM2VkN2Y0MmI0YmUwODgzYTRhNDQ5NTBhZTY1IiwiaXYiOiI2MjdjMFBxQ2NhRzF3Y2JGKzJzcEN3PT0ifQ==",
            195_000, "PSA 10", "Panini Sports Mega Craques", 2003, "#137",
            "Type: Rookie Card (RC) | Team: Sporting CP | Grade: PSA 10 Gem Mint | Investment Tier: Elite",
            17_280, 7),
        CreateEntry("2002 Panini Futebol Portugal Stickers Cristiano Ronaldo Rookie RC #306 PSA 10",
            "Featuring Ronaldo as a teenage prospect at Sporting CP.",
            "https://cdn-vault.fanaticscollect.com/2026/5/11/wr7/small/v3652354_20260511155901072M_1.jpg",
            86_000, "PSA 10", "Panini Futebol Portugal Stickers", 2002, "#306",
            "Type: Rookie Sticker | Team: Sporting CP | Grade: PSA 10",
            18_720, 4),
        CreateEntry("2003 Upper Deck Manchester United Cristiano Ronaldo Patch #MWSR BGS 9.5",
            "This premium memorabilia card includes a match-worn jersey swatch from Ronaldo's early Manchester United years.",
            "https://www.fanaticscollect.com/buy-now/bd41f976-cd0d-4f8b-a35a-d6e686dea74f/2003-panini-sports-mega-craques-cristiano-ronaldo-rookie-137-psa-10-gem-mint",
            200_000, "BGS 9.5", "Upper Deck Manchester United", 2003, "#MWSR",
            "Player: Cristiano Ronaldo | Key Features: Match-Worn Shirt/Patch Memorabilia, early Manchester United era",
            20_160, 8),
        CreateEntry("2016 Panini Noir Black & White Prime Cristiano Ronaldo Patch Auto /10 #MB-CR7 BGS 8.5",
            "An elegant low-numbered autograph patch card from Panini Noir featuring a certified Ronaldo signature and premium memorabilia piece.",
            "https://www.fanaticscollect.com/buy-now/6f00b145-f089-401c-8008-b6e50d8fc9b2/2003-upper-deck-manchester-united-cristiano-ronaldo-patch-mwsr-bgs-95-gem-mint",
            13_000, "BGS 8.5", "Panini Noir Black & White Prime", 2016, "#MB-CR7",
            "Serial Number: /10 | Key Features: Certified Autograph, Prime Patch, ultra-rare print run",
            21_600, 3),
        CreateEntry("2004 Panini Sports Mega Cracks La Liga Lionel Messi ROOKIE #71 BGS 9.5 GEM MINT",
            "The official and iconic rookie card of Lionel Messi from the 2004 Panini Sports Mega Cracks La Liga series.",
            "https://cdn-vault.fanaticscollect.com/2024/10/16/rm2/medium/v993740_2023101611373744R_81.jpg",
            230_000, "BGS 9.5", "Panini Sports Mega Cracks La Liga", 2004, "#71",
            "Player: Lionel Messi | Card Number: #71 (71BIS) | Centering: 9.5 | Corners: 9.5 | Edges: 9.5 | Surface: 9.5 | Rarity: Quad-9.5 Gem Mint rookie",
            23_040, 9),
        CreateEntry("2024 Topps Dynasty UEFA Club Competitions Gold Ronaldo PATCH AUTO 1/1 #APL-RO3",
            "An ultra-rare one-of-one masterpiece featuring Cristiano Ronaldo with certified autograph and premium player-worn patch.",
            "https://cdn-vault.fanaticscollect.com/2026/2/6/ab1/small/v2581673_20260206111615617M_11.jpg",
            30_000, "PSA 9", "Topps Dynasty UEFA Club Competitions", 2024, "#APL-RO3",
            "Parallel: Gold | Serial Number: 1/1 | Autograph: On-card Topps authenticated | Memorabilia: Multi-colored premium patch",
            24_480, 4),
        CreateEntry("2002 Panini Futebol Portugal Stickers Cristiano Ronaldo ROOKIE RC #306 PSA 10",
            "A highly collectible and iconic rookie sticker of Cristiano Ronaldo from the 2002 Panini Futebol Portugal collection.",
            "https://cdn-vault.fanaticscollect.com/2020/12/28/11/medium/v93445_2020122811360025L_61.jpg",
            130_000, "PSA 10", "Panini Futebol Portugal Stickers", 2002, "#306",
            "Player: Cristiano Ronaldo | Type: Rookie Sticker | Grade: PSA 10 Gem Mint | Significance: Earliest Sporting CP-era Ronaldo issue",
            25_920, 6),
        CreateEntry("2022 Topps Dynasty UEFA Gold Zinedine Zidane AUTO DNA 10 1/1 #A-ZZ2 PSA 10 GEM MINT",
            "A rare one-of-one Topps Dynasty UEFA Gold autograph card featuring French football icon Zinedine Zidane.",
            "https://cdn-vault.fanaticscollect.com/2026/2/24/wr2/medium/v2709207_20260224132054229M_1.jpg",
            30_000, "PSA 10", "Topps Dynasty UEFA", 2022, "#A-ZZ2",
            "Player: Zinedine Zidane | Serial Number: 1/1 | Card Grade: PSA 10 Gem Mint | Autograph Grade: DNA 10 | Population: 1 of 1",
            27_360, 4),
        CreateEntry("1991 Panini Soccer Zinedine Zidane ROOKIE #43 PSA 8 NM-MT",
            "A historic rookie card featuring Zinedine Zidane during his early professional career with AS Cannes.",
            "https://cdn-vault.fanaticscollect.com/2021/5/12/5/medium/v153874_2021051012465400M_1947.jpg",
            35_000, "PSA 8", "Panini Soccer / Panini Foot '92", 1991, "#43",
            "Team Featured: AS Cannes | Grade: PSA 8 NM-MT | Significance: One of Zidane's most important rookie cards",
            28_800, 2),
        CreateEntry("2023 Topps Museum Collection UEFA Archival Gold Lamine Yamal ROOKIE AUTO /50 PSA 8 NM-MT",
            "A rare rookie autograph card featuring Lamine Yamal from the premium Topps Museum Collection UEFA series.",
            "https://cdn-vault.fanaticscollect.com/2026/2/12/rm1/medium/v2663176_2021021014011479R_21.jpg",
            40_000, "PSA 8", "Topps Museum Collection UEFA Club Competitions", 2023, "#YL3",
            "Subset: Archival Autographs | Parallel: Gold | Serial Number: /50 | Player: Lamine Yamal | Grade: PSA 8 NM-MT",
            30_240, 5),
        CreateEntry("1979 Panini Calciatori Stickers Diego Maradona ROOKIE #312 BVG 9.5 GEM MINT",
            "An extraordinarily rare rookie sticker featuring Diego Maradona from the 1979 Panini Calciatori Stickers collection.",
            "https://cdn-vault.fanaticscollect.com/2020/12/28/11/medium/v93445_2020122811543020L_119.jpg",
            70_000, "BVG 9.5", "Panini Calciatori Stickers", 1979, "#312",
            "Player: Diego Maradona | Centering: 9.5 | Corners: 9.5 | Edges: 9.5 | Surface: 9.5 | Population: BVG top-pop vintage rookie sticker",
            31_680, 6),
        CreateEntry("2013 Icons Messi Limited Collection 73rd Goal '11 Lionel Messi PATCH AUTO DNA 10 1/1 PSA 9 MINT",
            "A one-of-one historical masterpiece commemorating Messi's 73-goal season with match-worn patch and autograph.",
            "https://cdn-vault.fanaticscollect.com/2026/2/13/wr1/medium/v2470357_20260213083340648M_1.jpg",
            300_000, "PSA 9", "Icons Messi Limited Collection", 2013, "11-JSY-AU",
            "Theme: 73rd Goal '11 | Serial Number: 1/1 | Autograph Grade: DNA 10 | Key Features: Match-worn patch and on-card signature",
            33_120, 10),
        CreateEntry("2016 Topps Dynasty Pele PATCH AUTO 1/1 #AP-P2 BGS 9.5 GEM MINT",
            "A one-of-one premium Pele patch autograph card from the prestigious 2016 Topps Dynasty collection.",
            "https://cdn-vault.fanaticscollect.com/2025/9/10/bs1/medium/v1577925_20250910105535366M_1.jpg",
            178_000, "BGS 9.5", "Topps Dynasty Soccer", 2016, "#AP-P2",
            "Player: Pele | Serial Number: 1/1 | Edges: 10 | Autograph Grade: 10 | Key Features: CBF logo patch and bold blue signature",
            34_560, 8),
        CreateEntry("2019 Topps Chrome Bundesliga Orange Refractor Erling Haaland RC AUTO /25 SGC 9.5",
            "A premium rookie autograph card featuring Erling Haaland from 2019 Topps Chrome Bundesliga.",
            "https://cdn-vault.fanaticscollect.com/2022/7/31/rm1/medium/v408558_2022073112015527M_7.jpg",
            63_000, "SGC 9.5", "Topps Chrome Bundesliga", 2019, "#72",
            "Parallel: Orange Refractor | Serial Number: /25 | Team: Borussia Dortmund | Grade: SGC 9.5 Mint+ | Autograph: Certified",
            36_000, 5),
        CreateEntry("2014 Panini Prizm World Cup Fans Of The Game Kobe Bryant AUTO #1 PSA 8 NM-MT",
            "A unique cross-sport collectible featuring Kobe Bryant from the 2014 Panini Prizm World Cup Fans of the Game insert set.",
            "https://cdn-vault.fanaticscollect.com/2026/5/5/bw3/medium/v2812031_20260505074516360M_1.jpg",
            38_000, "PSA 8", "Panini Prizm World Cup", 2014, "#1",
            "Insert Set: Fans of the Game Autographs | Player: Kobe Bryant | Grade: PSA 8 NM-MT | Key Features: On-card autograph",
            37_440, 3),
        CreateEntry("2009 Abril Gol Cards Soccer Neymar Jr. ROOKIE #154 PSA 9 MINT",
            "A rare rookie card featuring Neymar Jr. during his early rise with Santos FC.",
            "https://cdn-vault.fanaticscollect.com/2021/10/21/rc2/medium/v248698_2021102111360917M_1.jpg",
            79_000, "PSA 9", "Abril Gol Cards Soccer", 2009, "#154",
            "Player: Neymar Jr. | Team Featured: Santos FC | Grade: PSA 9 Mint | Population: 1 of 1 none graded higher",
            38_880, 5),
        CreateEntry("2002 Panini Futebol Portugal Stickers Cristiano Ronaldo ROOKIE RC #306 PSA 10",
            "A pristine PSA 10 Cristiano Ronaldo rookie sticker from the 2002 Panini Futebol Portugal collection.",
            "https://cdn-vault.fanaticscollect.com/2020/12/28/11/medium/v93445_2020122811360025L_61.jpg",
            85_000, "PSA 10", "Panini Futebol Portugal Stickers", 2002, "#306",
            "Player: Cristiano Ronaldo | Type: Rookie Sticker | Grade: PSA 10 Gem Mint | Significance: Sporting CP rookie-era issue",
            40_320, 6),
        CreateEntry("1992 Sports Illustrated For Kids Series 2 Mia Hamm ROOKIE AUTO DNA 10 #71 PSA 10",
            "A historically significant rookie card featuring Mia Hamm with certified autograph.",
            "https://cdn-vault.fanaticscollect.com/2024/9/3/rm1/medium/v947380_2024090309323394M_3.jpg",
            40_000, "PSA 10", "Sports Illustrated For Kids Series 2", 1992, "#71",
            "Player: Mia Hamm | Card Grade: PSA 10 Gem Mint | Autograph Grade: DNA 10 | Population: PSA/DNA 1 of 1",
            41_760, 4),
        CreateEntry("2016 Select Gold Prizm Christian Pulisic ROOKIE /10 #287 BGS 9.5 GEM MINT",
            "A rare Gold Prizm rookie card featuring Christian Pulisic during his Borussia Dortmund breakthrough.",
            "https://cdn-vault.fanaticscollect.com/2025/10/6/rm3/medium/v1653016_2020100507284336R_73.jpg",
            50_000, "BGS 9.5", "Panini Select Soccer", 2016, "#287",
            "Parallel: Gold Prizm | Serial Number: /10 | Centering: 9 | Corners: 9.5 | Edges: 10 | Surface: 9.5 | Field Level subset",
            43_200, 5),
        CreateEntry("2017 Topps Chrome UEFA Green Refractor Kylian Mbappe ROOKIE /99 #41 PSA 10 GEM",
            "A valuable Green Refractor rookie card featuring Kylian Mbappe from 2017 Topps Chrome UEFA Champions League.",
            "https://cdn-vault.fanaticscollect.com/2024/2/9/rm1/medium/v809479_2024020908315326R_5.jpg",
            20_000, "PSA 10", "Topps Chrome UEFA Champions League", 2017, "#41",
            "Parallel: Green Refractor | Serial Number: /99 | Player: Kylian Mbappe | Grade: PSA 10 Gem Mint",
            44_640, 4),
        CreateEntry("2014 Panini Prizm World Cup Signatures Pele AUTO #S-PEL BGS 9.5 GEM MINT",
            "A prestigious autographed Pele card from the iconic 2014 Panini Prizm World Cup Signatures set.",
            "https://cdn-vault.fanaticscollect.com/2022/3/1/bs2/medium/v310512_2022030104465291R_1.jpg",
            96_000, "BGS 9.5", "Panini Prizm World Cup", 2014, "#S-PEL",
            "Subset: Signatures | Player: Pele | Corners: 9 | Surface: 10 | Autograph Grade: 10 | Key Features: Blue ink on-card signature",
            46_080, 7),
        CreateEntry("2019 Topps Chrome UEFA Teammate Superfractor Erling Haaland Reus RC 1/1 BGS 9.5",
            "A one-of-one Superfractor featuring Erling Haaland and Marco Reus from 2019 Topps Chrome UEFA Champions League.",
            "https://cdn-vault.fanaticscollect.com/2021/9/12/1/medium/v221181_2021091208315359R_75.jpg",
            500_000, "BGS 9.5", "Topps Chrome UEFA Champions League", 2019, "1/1",
            "Insert Set: Teammate Sensations | Parallel: Superfractor | Players: Erling Haaland & Marco Reus | Serial Number: 1/1 | Surface: 9",
            47_520, 12)
    ];

    private static Entry CreateEntry(
        string name,
        string summary,
        string primaryImage,
        decimal startingPrice,
        string gradeLabel,
        string setName,
        int year,
        string cardNumber,
        string details,
        int endMinutes,
        int existingBidCount = 0) =>
        new(
            "Soccer Card",
            name.Trim(),
            $"Overview: {summary} | Year: {year} | Brand/Set: {setName} | Card Number: {cardNumber} | Grade: {gradeLabel} | {details}",
            primaryImage,
            startingPrice,
            gradeLabel,
            setName,
            year,
            "English",
            cardNumber,
            "graded",
            endMinutes,
            existingBidCount);

    public static string BuildShortDescription(string description)
    {
        var parts = description.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0].Replace("Overview:", string.Empty).Trim() : string.Empty;
    }

    public static string BuildDescriptionHtml(string description)
    {
        var parts = description.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return "<p>-</p>";
        }

        var overview = parts[0].Replace("Overview:", string.Empty).Trim();
        var items = parts
            .Skip(1)
            .Select(part => $"<li>{WebUtility.HtmlEncode(part)}</li>");

        return $"<p>{WebUtility.HtmlEncode(overview)}</p><p><strong>Item specifics</strong></p><ul>{string.Concat(items)}</ul>";
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
