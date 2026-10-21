using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    // School announcements, given on the WhatsApp menu. View-shell only - the
    // API's AnnouncementController stores them.
    public class AnnouncementController : Controller
    {
        [Route("announcements")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Announcements";
            return View();
        }
    }
}
