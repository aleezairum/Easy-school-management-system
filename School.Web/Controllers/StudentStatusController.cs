using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class StudentStatusController : Controller
    {
        [Route("student-status")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Student Status Management";
            return View();
        }
    }
}
