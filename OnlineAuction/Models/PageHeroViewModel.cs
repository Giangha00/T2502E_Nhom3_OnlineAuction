namespace OnlineAuction.Models;

public class PageHeroViewModel
{
    public string Label { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Centered { get; set; }
    public string? ImageUrl { get; set; }
    public string ImageAlt { get; set; } = "Auction House banner";
    public string? ActionText { get; set; }
    public string? ActionController { get; set; }
    public string? ActionName { get; set; } = "Index";
}
