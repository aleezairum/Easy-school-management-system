using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using School.Web.Models;

namespace School.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly School.Web.Services.ApiTokenService _tokens;

        public HomeController(ILogger<HomeController> logger, School.Web.Services.ApiTokenService tokens)
        {
            _logger = logger;
            _tokens = tokens;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Public, no login. Both URLs are registered with Meta as the app's
        // Privacy Policy and Data Deletion links, so they must stay reachable
        // and must keep these exact paths — changing one means updating it in
        // the Meta app dashboard too. Program.cs also allows them past the
        // session guard.
        [Route("privacy")]
        public IActionResult Privacy()
        {
            return View();
        }

        [Route("data-deletion")]
        public IActionResult DataDeletion()
        {
            return View();
        }

        [HttpGet]
        [Route("session/ping")]
        public IActionResult Ping()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized();
            // The page keeps its API token fresh from here (common.js).
            return Ok(new { alive = true, token = _tokens.For(HttpContext) });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
