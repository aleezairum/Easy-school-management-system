using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class HRGradeController : Controller
    {
        [Route("hrgrade")]
        public IActionResult Index()
        {
            ViewData["Title"] = "HR Grades";
            return View();
        }
    }
}
