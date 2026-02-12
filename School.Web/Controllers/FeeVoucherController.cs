using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class FeeVoucherController : Controller
    {
        [Route("feevoucher")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Fee Vouchers";
            return View();
        }

        [Route("feevoucher/form")]
        [Route("feevoucher/form/{id?}")]
        public IActionResult Form(int? id)
        {
            ViewData["Title"] = id.HasValue ? "Edit Fee Voucher" : "Create Fee Voucher";
            ViewData["VoucherId"] = id;
            return View();
        }

        [Route("feevoucher/print/{id}")]
        public IActionResult Print(int id)
        {
            ViewData["Title"] = "Print Fee Voucher";
            ViewData["VoucherId"] = id;
            return View();
        }
    }
}
