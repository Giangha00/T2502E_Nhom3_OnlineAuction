namespace OnlineAuction.Models;

public class RelatedListingsCarouselViewModel
{
    public string Title { get; set; } = string.Empty;

    public string SectionKey { get; set; } = string.Empty;

    public List<AuctionItemViewModel> Items { get; set; } = [];
}
