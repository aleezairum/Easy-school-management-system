using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class SalaryController : Controller
    {
        [Route("salary")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Salary Management";
            return View();
        }

        [Route("salary/form")]
        [Route("salary/form/{id?}")]
        public IActionResult Form(int? id)
        {
            ViewData["Title"] = id.HasValue ? "Edit Salary" : "Add Salary";
            ViewData["SalaryId"] = id;
            return View();
        }

        [Route("salary/slip/{id}")]
        public IActionResult Slip(int id)
        {
            ViewData["Title"] = "Salary Slip";
            ViewData["SalaryId"] = id;
            return View();
        }
    }
}
