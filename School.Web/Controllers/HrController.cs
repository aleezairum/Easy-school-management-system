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

        [Route("hr/school-timing")]
        public IActionResult SchoolTiming()
        {
            ViewData["Title"] = "School Timings";
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

        // Incentive, security, fine and advance — the amounts the office types
        // by hand for a month, which the salary sheet has always carried.
        [Route("hr/monthly-amounts")]
        public IActionResult MonthlyAmounts()
        {
            ViewData["Title"] = "Monthly Amounts";
            return View();
        }

        // Advances handed to staff, by date. The month's total reaches the
        // payroll and shows, read only, on Monthly Amounts.
        [Route("hr/advances")]
        public IActionResult Advances()
        {
            ViewData["Title"] = "Staff Advances";
            return View();
        }

        // Extra days for staff who took no day off in a month with more than a
        // set number of working days — paid at the sheet's Daily Pay.
        [Route("hr/extra-days")]
        public IActionResult ExtraDays()
        {
            ViewData["Title"] = "Extra Daily";
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

        [Route("hr/reports/staff-statement")]
        public IActionResult StaffStatement()
        {
            ViewData["Title"] = "Staff Statement";
            return View();
        }

        [Route("hr/reports/attendance")]
        public IActionResult AttendanceReport()
        {
            ViewData["Title"] = "Staff Attendance Report";
            return View();
        }

        // Face-attendance machines (Hikvision): set up, people on them, punch log.
        [Route("hr/attendance-machines")]
        public IActionResult AttendanceMachines()
        {
            ViewData["Title"] = "Attendance Machines";
            return View();
        }

        // Each employee's month, day by day, printed one card per page.
        [Route("hr/reports/attendance-card")]
        public IActionResult AttendanceCard()
        {
            ViewData["Title"] = "Staff Attendance Card";
            return View();
        }

        // The generated payroll's salary slips, printed two to a page.
        [Route("hr/reports/salary-slips")]
        public IActionResult SalarySlips()
        {
            ViewData["Title"] = "Salary Slips";
            return View();
        }
    }
}
