using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    // There is no teacher screen, and no teacher table either: SMSTeacher was
    // renamed into Employees, so staff are created and edited in one place and
    // ticking "Teaching staff" there is all that makes someone a teacher.
    // The route is kept so existing links and bookmarks still land somewhere useful.
    public class TeacherController : Controller
    {
        [Route("teacher")]
        public IActionResult Index() => RedirectToActionPermanent("Index", "Employee");
    }
}
