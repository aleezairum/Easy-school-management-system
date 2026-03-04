using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class InstituteController : Controller
    {
        [Route("my-institute")]
        public IActionResult Index()
        {
            ViewData["Title"] = "My Institute";
            return View();
        }
    }
}
