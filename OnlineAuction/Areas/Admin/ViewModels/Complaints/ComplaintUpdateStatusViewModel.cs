using System.ComponentModel.DataAnnotations;

namespace OnlineAuction.Areas.Admin.ViewModels.Complaints;

public class ComplaintUpdateStatusViewModel
{
    public int ComplaintId { get; set; }

    [Required]
    public string Action { get; set; } = string.Empty;

    public string? AdminNotes { get; set; }

    public string? ResolutionNote { get; set; }
}
