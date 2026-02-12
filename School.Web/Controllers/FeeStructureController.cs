using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class FeeStructureController : Controller
    {
        [Route("feestructure")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Fee Structure";
            return View();
        }
    }
}
