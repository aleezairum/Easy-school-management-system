using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class ClassesController : Controller
    {
        [Route("classes")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Class Management";
            return View();
        }
    }
}
