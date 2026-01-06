using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class AuthController : Controller
    {
        [Route("login")]
        [Route("")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [Route("login")]
        [Route("")]
        public IActionResult Login(string username, string password)
        {
            // Static validation for demo purposes
            if (username == "admin" && password == "admin123")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.Error = "Invalid username or password";
            return View();
        }

        public IActionResult Logout()
        {
            return RedirectToAction("Login");
        }
    }
}
