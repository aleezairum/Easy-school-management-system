using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class TimeTableController : Controller
    {
        [Route("timetable")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Time Table";
            return View();
        }

        [Route("timetable/form")]
        [Route("timetable/form/{id?}")]
        public IActionResult Form(int? id)
        {
            ViewData["Title"] = id.HasValue ? "Edit Time Table Entry" : "Add Time Table Entry";
            ViewData["TimeTableId"] = id;
            return View();
        }
    }
}
