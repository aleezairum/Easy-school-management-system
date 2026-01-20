using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class AcademicGradeController : Controller
    {
        [Route("academicgrade")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Academic Grades";
            return View();
        }
    }
}
