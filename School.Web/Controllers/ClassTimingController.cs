using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    // Class-wise school timing for STUDENTS. The staff timing screen lives on
    // HrController (/hr/school-timing) and feeds attendance and payroll; this
    // one is what parents are told, and the WhatsApp auto-reply reads it.
    //
    // Thin view-shell like the rest — all data comes from the API via API_BASE.
    public class ClassTimingController : Controller
    {
        [Route("class-timing")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Class Timings";
            return View();
        }

        // The parents' timing notice — the Urdu sheet the school pins up and
        // sends home. Its own page because it covers BOTH campuses at once,
        // while the editing grid works one campus at a time.
        [Route("class-timing/notice")]
        public IActionResult Notice()
        {
            ViewData["Title"] = "School Timing Notice";
            return View();
        }
    }
}
