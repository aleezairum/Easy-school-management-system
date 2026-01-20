using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class SubjectsController : Controller
    {
        [Route("subjects")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Subject Management";
            return View();
        }
    }
}
