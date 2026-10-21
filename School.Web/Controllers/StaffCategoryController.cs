using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    // The staff category list. Thin view-shell like the other masters — the
    // CRUD lives in the API (StaffCategoryController there).
    public class StaffCategoryController : Controller
    {
        [Route("staff-category")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Staff Categories";
            return View();
        }
    }
}
