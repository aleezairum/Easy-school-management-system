using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            // Static validation for demo purposes
            if (email == "admin@school.com" && password == "admin123")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.Error = "Invalid email or password";
            return View();
        }

        public IActionResult Logout()
        {
            return RedirectToAction("Login");
        }
    }
}
