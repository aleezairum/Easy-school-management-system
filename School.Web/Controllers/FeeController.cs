using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    public class FeeController : Controller
    {
        [Route("fee/apply")]
        [Route("apply-fee")]
        public IActionResult ApplyFee()
        {
            ViewData["Title"] = "Apply Fee";
            return View();
        }
        [Route("feeList")]
        public IActionResult ApplyFeeList()
        {
            ViewData["Title"] = "Apply Fee List";
            return View();
        }

        [Route("fee-report")]
        public IActionResult FeeReport()
        {
            ViewData["Title"] = "Fee Reports";
            return View();
        }

        // Statements of account: one student, or one family. Both are the same
        // report over the same columns, so they share a single form and the
        // Ledger Type radio picks the account.
        [Route("fee/ledger")]
        [Route("ledger")]
        public IActionResult Ledger()
        {
            ViewData["Title"] = "Ledger";
            return View();
        }
    }
}
