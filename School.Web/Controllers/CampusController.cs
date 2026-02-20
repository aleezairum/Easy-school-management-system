using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class CampusController : Controller
    {
        [Route("campus")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Campus Management";
            return View();
        }
    }
}
