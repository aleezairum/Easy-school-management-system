using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    // HR & Payroll module — HR-Payroll-Final-Plan.md section 7 (Screens).
    // Each action is a thin view-shell; all data comes from the API via API_BASE,
    // matching the existing pattern (see DesignationController).
    public class HrController : Controller
    {
        [Route("hr/setup")]
        public IActionResult Setup()
        {
            ViewData["Title"] = "Payroll Setup";
            return View();
        }

        [Route("hr/attendance")]
        public IActionResult Attendance()
        {
            ViewData["Title"] = "Attendance";
            return View();
        }

        [Route("hr/leave")]
        public IActionResult Leave()
        {
            ViewData["Title"] = "Leave";
            return View();
        }

        [Route("hr/payroll")]
        public IActionResult Payroll()
        {
            ViewData["Title"] = "Payroll Run";
            return View();
        }

        [Route("hr/payroll/{id:int}")]
        public IActionResult PayrollReview(int id)
        {
            ViewData["Title"] = "Payroll Review";
            ViewData["RunId"] = id;
            return View();
        }

        [Route("hr/payslip/{id:int}")]
        public IActionResult Payslip(int id)
        {
            ViewData["Title"] = "Payslip";
            ViewData["PayslipId"] = id;
            return View();
        }

        [Route("hr/salary-sheet/{runId:int}")]
        public IActionResult SalarySheet(int runId)
        {
            ViewData["Title"] = "Salary Sheet";
            ViewData["RunId"] = runId;
            return View();
        }

        [Route("hr/reports/attendance")]
        public IActionResult AttendanceReport()
        {
            ViewData["Title"] = "Attendance Reports";
            return View();
        }
    }
}
