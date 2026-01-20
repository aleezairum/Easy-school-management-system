using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class DesignationController : Controller
    {
        [Route("designation")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Designations";
            return View();
        }
    }
}
