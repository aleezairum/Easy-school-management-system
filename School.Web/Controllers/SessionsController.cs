using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class SessionsController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Academic Sessions";
            return View();
        }
    }
}
