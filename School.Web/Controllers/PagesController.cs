using Microsoft.AspNetCore.Mvc;
using School.Web.Models;

namespace School.Web.Controllers
{
    public class PagesController : Controller
    {
        [Route("pages")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Add Pages";
            return View();
        }

        public IActionResult Create()
        {
            ViewData["Title"] = "Add Pages";
            return View("Index");
        }

        [HttpPost]
        public IActionResult Save(PageViewModel model)
        {
            // This will be handled via AJAX to API
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            // This will be handled via AJAX to API
            return RedirectToAction("Index");
        }
    }
}
