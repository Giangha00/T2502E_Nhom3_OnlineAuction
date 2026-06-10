namespace OnlineAuction.Models
{
    public class ContactPageViewModel
    {
        public ContactInfoModel? ContactInfo { get; set; }
        public List<FAQItemModel>? FAQItems { get; set; }
        public LocationModel? Location { get; set; }
    }

    public class ContactInfoModel
    {
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? WorkingHours { get; set; }
    }

    public class FAQItemModel
    {
        public string? Question { get; set; }
        public string? Answer { get; set; }
    }

    public class LocationModel
    {
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}


