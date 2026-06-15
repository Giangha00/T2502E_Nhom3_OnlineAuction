using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Models;

namespace OnlineAuction.Controllers
{
    public class ContactController : Controller
    {
        public IActionResult Index()
        {
            var model = new ContactPageViewModel
            {
                ContactInfo = new ContactInfoModel
                {
                    Address = "900 Collector's Plaza, 14th Floor, Manhattan, New York 10001",
                    Phone = "+1 (888) RARE-CRD",
                    Email = "support@rarecard.com",
                    Discord = "RareCard Discord"
                },
                Location = new LocationModel
                {
                    Name = "HQ Location",
                    Address = "900 Collector's Plaza, 14th Floor, Manhattan, New York 10001",
                    Phone = "+1 (888) RARE-CRD",
                    Latitude = 40.7484,
                    Longitude = -73.9857
                },
                FAQItems = new List<FAQItemModel>
                {
                    new FAQItemModel
                    {
                        Question = "How do I place a bid?",
                        Answer = "To place a bid, first create an account, then browse auctions and click on the item you want. Enter your bid amount and confirm."
                    },
                    new FAQItemModel
                    {
                        Question = "Is there a shipping fee?",
                        Answer = "Shipping fees vary based on the item location and destination. Costs are calculated during checkout."
                    },
                    new FAQItemModel
                    {
                        Question = "How long does an auction last?",
                        Answer = "Auction durations vary from 3 to 30 days depending on the seller's settings."
                    }
                }
            };

            return View(model);
        }
    }
}