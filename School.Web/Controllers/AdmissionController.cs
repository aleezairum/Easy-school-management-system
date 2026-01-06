using Microsoft.AspNetCore.Mvc;
using School.Web.Models;

namespace School.Web.Controllers
{
    public class AdmissionController : Controller
    {
        [Route("admission/form")]
        public IActionResult Form()
        {
            ViewData["Title"] = "Admission Form";
            var model = new AdmissionFormViewModel();
            return View(model);
        }

        [Route("admission/form/print")]
        public IActionResult PrintForm()
        {
            ViewData["Title"] = "Admission Form - Print";
            var model = new AdmissionFormViewModel();
            return View("FormPrint", model);
        }
    }
}
