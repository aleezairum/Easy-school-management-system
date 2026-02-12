using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class ChallanVoucherController : Controller
    {
        [Route("challan")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Fee Challans";
            return View();
        }

        [Route("challan/form")]
        [Route("challan/form/{id?}")]
        public IActionResult Form(int? id)
        {
            ViewData["Title"] = id.HasValue ? "Edit Fee Challan" : "Create Fee Challan";
            ViewData["ChallanId"] = id;
            return View();
        }

        [Route("challan/print/{id}")]
        public IActionResult Print(int id)
        {
            ViewData["Title"] = "Print Fee Challan";
            ViewData["ChallanId"] = id;
            return View();
        }
    }
}
