using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    // Custom reports over students and employees: the user picks the columns and
    // the filters. Thin view-shell like the rest — the column catalogue and the
    // query both live in the API (ReportFieldCatalog).
    public class CustomReportController : Controller
    {
        [Route("custom-report")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Custom Reports";
            return View();
        }
    }
}
