using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class SectionsController : Controller
    {
        [Route("sections")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Section Management";
            return View();
        }
    }
}
