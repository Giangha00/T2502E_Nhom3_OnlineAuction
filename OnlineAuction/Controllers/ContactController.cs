using Microsoft.AspNetCore.Mvc;

namespace PageContact.Controllers
{
    public class ContactController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}