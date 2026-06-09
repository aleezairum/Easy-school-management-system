using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class DashboardController : Controller
    {
        [Route("dashboard")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
