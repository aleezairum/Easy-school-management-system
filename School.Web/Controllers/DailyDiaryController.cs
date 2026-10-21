using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    // Class and section wise daily diary upload. View-shell only - the API's
    // DailyDiaryController stores the pictures.
    public class DailyDiaryController : Controller
    {
        [Route("daily-diary")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Daily Diary";
            return View();
        }
    }
}
