using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class DashboardController : Controller
    {
        [Route("dashboard")]
        public IActionResult Index()
        {
            // Static dashboard data
            ViewBag.TotalStudents = 1250;
            ViewBag.TotalTeachers = 85;
            ViewBag.TotalClasses = 42;
            ViewBag.TotalSubjects = 28;
            ViewBag.AttendanceRate = 94.5;
            ViewBag.PendingFees = 45000;

            return View();
        }
    }
}
