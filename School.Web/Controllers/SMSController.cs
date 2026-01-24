using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class SMSController : Controller
    {
        [Route("sms")]
        public IActionResult Index()
        {
            ViewData["Title"] = "SMS Module";
            return View();
        }
    }
}
