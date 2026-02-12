using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class ExamController : Controller
    {
        [Route("exam")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Exams";
            return View();
        }

        [Route("exam/form")]
        [Route("exam/form/{id?}")]
        public IActionResult Form(int? id)
        {
            ViewData["Title"] = id.HasValue ? "Edit Exam" : "Add Exam";
            ViewData["ExamId"] = id;
            return View();
        }

        [Route("exam/results/{examId}")]
        public IActionResult Results(int examId)
        {
            ViewData["Title"] = "Exam Results";
            ViewData["ExamId"] = examId;
            return View();
        }

        [Route("exam/marks-entry")]
        [Route("exam/award-list")]
        public IActionResult MarksEntry()
        {
            ViewData["Title"] = "Award List - Marks Entry";
            return View();
        }
    }
}
