using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class PaymentMethodController : Controller
    {
        [Route("paymentmethod")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Payment Methods";
            return View();
        }

        [Route("paymentmethod/form")]
        [Route("paymentmethod/form/{id?}")]
        public IActionResult Form(int? id)
        {
            ViewData["Title"] = id.HasValue ? "Edit Payment Method" : "Add Payment Method";
            ViewData["PaymentMethodId"] = id;
            return View();
        }
    }
}
