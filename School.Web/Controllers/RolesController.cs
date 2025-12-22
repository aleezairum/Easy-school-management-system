using Microsoft.AspNetCore.Mvc;
using School.Web.Models;

namespace School.Web.Controllers
{
    public class RolesController : Controller
    {
        [Route("roles")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Role Permissions";
            return View();
        }
    }
}
