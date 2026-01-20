using Microsoft.AspNetCore.Mvc;
using School.Web.Models;

namespace School.Web.Controllers
{
    public class UsersController : Controller
    {
        [Route("users")]
        public IActionResult Index()
        {
            ViewData["Title"] = "User Management";
            return View();
        }
    }
}
