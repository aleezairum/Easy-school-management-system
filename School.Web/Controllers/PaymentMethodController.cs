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
    }
}
