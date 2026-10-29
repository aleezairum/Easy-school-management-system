using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class AcademyStaffSubjectsController : Controller
    {
        [Route("academy-staff-subjects")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Academy Subject Allocation";
            return View();
        }
    }
}
