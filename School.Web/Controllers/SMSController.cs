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

        // WhatsApp message log — every queued/sent message with the Meta
        // template used, the parameters sent and the delivery outcome.
        [Route("sms/messages")]
        public IActionResult Messages()
        {
            ViewData["Title"] = "WhatsApp Messages";
            return View();
        }

        // Two-way chat: incoming messages from parents and free-form replies
        // (allowed inside WhatsApp's 24-hour customer service window).
        [Route("sms/chat")]
        public IActionResult Chat()
        {
            ViewData["Title"] = "WhatsApp Chat";
            return View();
        }
    }
}
