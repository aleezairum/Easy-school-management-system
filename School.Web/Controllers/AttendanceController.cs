using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class AttendanceController : Controller
    {
        [Route("attendance")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Attendance";
            return View();
        }

        [Route("attendance/bulk")]
        public IActionResult Bulk()
        {
            ViewData["Title"] = "Bulk Attendance Entry";
            return View();
        }

        [Route("attendance/report")]
        public IActionResult Report()
        {
            ViewData["Title"] = "Attendance Report";
            return View();
        }
    }
}
