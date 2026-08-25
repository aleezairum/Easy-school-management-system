using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class TeacherController : Controller
    {
        [Route("teacher")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Teachers";
            return View();
        }
    }
}
