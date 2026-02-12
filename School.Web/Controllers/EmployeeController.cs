using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class EmployeeController : Controller
    {
        [Route("employee")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Employees";
            return View();
        }

        [Route("employee/form")]
        [Route("employee/form/{id?}")]
        public IActionResult Form(int? id)
        {
            ViewData["Title"] = id.HasValue ? "Edit Employee" : "Add Employee";
            ViewData["EmployeeId"] = id;
            return View();
        }
    }
}
