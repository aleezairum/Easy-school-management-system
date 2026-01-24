using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class StaffSubjectsController : Controller
    {
        [Route("staff-subjects")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Staff Subjects";
            return View();
        }
    }
}
