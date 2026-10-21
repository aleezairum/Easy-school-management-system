using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class FeePromiseController : Controller
    {
        [Route("fee/promise")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Promise to Pay";
            return View();
        }
    }
}
