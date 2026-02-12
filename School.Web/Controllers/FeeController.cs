using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class FeeController : Controller
    {
        [Route("fee/apply")]
        [Route("apply-fee")]
        public IActionResult ApplyFee()
        {
            ViewData["Title"] = "Apply Fee";
            return View();
        }
    }
}
