using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class FeeTypeController : Controller
    {
        [Route("feetype")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Fee Type";
            return View();
        }
    }
}
