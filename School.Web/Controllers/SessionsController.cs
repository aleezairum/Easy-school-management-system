using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class SessionsController : Controller
    {
        [Route("sessions")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Academic Sessions";
            return View();
        }
    }
}
