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
                    Address = "123 Auction Street, Hanoi, Vietnam 10000",
                    Phone = "+84 123 456 789",
                    Email = "support@auctionhouse.com",
                    WorkingHours = "Mon - Fri: 08:00 - 18:00"
                },
                Location = new LocationModel
                {
                    Name = "Main Office",
                    Address = "123 Auction Street, Hanoi 10000, Vietnam",
                    Phone = "+84 123 456 789",
                    Latitude = 21.0285,
                    Longitude = 105.8542
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