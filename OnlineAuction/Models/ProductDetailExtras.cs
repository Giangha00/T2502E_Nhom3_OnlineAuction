namespace OnlineAuction.Models;

public class BidHistoryItemViewModel
{
    public int BidderId { get; set; }
    public string BidderName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime BidTime { get; set; }
    public string Status { get; set; } = "OUTBID";
    public bool IsWinning => Status == "WINNING";
}

public class GradingScoreViewModel
{
    public string Centering { get; set; } = "10";
    public string Corners { get; set; } = "10";
    public string Edges { get; set; } = "10";
    public string Surface { get; set; } = "10";
}
