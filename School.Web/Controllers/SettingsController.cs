using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class SettingsController : Controller
    {
        // GET /Settings/Backup — matches the "Backup & Restore" menu entry.
        public IActionResult Backup()
        {
            ViewData["Title"] = "Backup & Restore";
            return View();
        }
    }
}
