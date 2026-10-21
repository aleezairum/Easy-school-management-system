using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School.API.Data;

namespace School.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly SchoolDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(SchoolDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // SMSStudentAcademic.StatusID values (lookup: SMSStudentStatus)
        private const int StatusActive    = 1;   // Active
        private const int StatusDroppedOut = 7;  // Dropped Out

        [HttpGet]
        public async Task<IActionResult> GetDashboard([FromQuery] int sessionId = 0)
        {
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);

            // ── Resolve the academic session: explicit selection wins, else the
            //    current session (IsCurrent), else the latest one. ──
            if (sessionId <= 0)
            {
                sessionId = await SafeScalarInt(
                    "SELECT TOP 1 VID AS Value FROM AcademicSessionYear WHERE IsCurrent = 1 ORDER BY VID DESC");
                if (sessionId <= 0)
                    sessionId = await SafeScalarInt(
                        "SELECT TOP 1 VID AS Value FROM AcademicSessionYear ORDER BY VID DESC");
            }
            DateTime? sessionFrom = await SafeScalarDate(
                "SELECT TOP 1 DateFrom AS Value FROM AcademicSessionYear WHERE VID = {0}", sessionId);
            DateTime? sessionTo = await SafeScalarDate(
                "SELECT TOP 1 DateTo AS Value FROM AcademicSessionYear WHERE VID = {0}", sessionId);

            // ── Student counts are taken from the per-session enrolment table
            //    (SMSStudentAcademic) — SMSStudent.IsActive is 1 for everyone, so
            //    "active" / "dropout" only mean anything against the session status. ──
            int enrolledStudents = await SafeScalarInt(
                "SELECT COUNT(*) AS Value FROM SMSStudentAcademic WHERE SessionID = {0} AND IsActive = 1", sessionId);
            int activeStudents = await SafeScalarInt(
                "SELECT COUNT(*) AS Value FROM SMSStudentAcademic WHERE SessionID = {0} AND IsActive = 1 AND StatusID = {1}",
                sessionId, StatusActive);
            int dropoutStudents = await SafeScalarInt(
                "SELECT COUNT(*) AS Value FROM SMSStudentAcademic WHERE SessionID = {0} AND IsActive = 1 AND StatusID = {1}",
                sessionId, StatusDroppedOut);
            // New admissions = students whose AdmissionDate falls inside this session's date range.
            int newAdmissions = (sessionFrom.HasValue && sessionTo.HasValue)
                ? await SafeScalarInt(
                    "SELECT COUNT(*) AS Value FROM SMSStudent WHERE AdmissionDate >= {0} AND AdmissionDate <= {1}",
                    sessionFrom.Value, sessionTo.Value)
                : 0;

            int totalEmployees  = await SafeScalarInt("SELECT COUNT(*) AS Value FROM Employees WHERE IsActive = 1");
            int totalClasses    = await SafeScalarInt("SELECT COUNT(*) AS Value FROM SMSClass");
            int totalSections   = await SafeScalarInt("SELECT COUNT(*) AS Value FROM SMSSection");
            int totalSubjects   = await SafeScalarInt("SELECT COUNT(*) AS Value FROM SMSSubject");
            // SMSCampus, not "Campus" — there is no table by that name, so this
            // threw on every request and SafeScalarInt quietly returned 0. The
            // card read "0 campus(es)" while the school has had two on file all
            // along; only the warning in the log said otherwise.
            //
            // ISNULL(IsActive, 1): a campus added straight through SQL can leave
            // the flag unset, and an unset flag should not hide a campus.
            int totalCampuses   = await SafeScalarInt(
                "SELECT COUNT(*) AS Value FROM SMSCampus WHERE ISNULL(IsActive, 1) = 1");

            // ── Fee voucher stats (FeeVoucherMain table: VDate, PaidAmount) ──
            decimal todayCollection = await SafeScalarDecimal(
                "SELECT ISNULL(SUM(PaidAmount), 0) AS Value FROM FeeVoucherMain WHERE CAST(VDate AS DATE) = {0}", today);
            decimal monthCollection = await SafeScalarDecimal(
                "SELECT ISNULL(SUM(PaidAmount), 0) AS Value FROM FeeVoucherMain WHERE CAST(VDate AS DATE) >= {0}", monthStart);
            int todayVoucherCount = await SafeScalarInt(
                "SELECT COUNT(*) AS Value FROM FeeVoucherMain WHERE CAST(VDate AS DATE) = {0}", today);
            int monthVoucherCount = await SafeScalarInt(
                "SELECT COUNT(*) AS Value FROM FeeVoucherMain WHERE CAST(VDate AS DATE) >= {0}", monthStart);

            // ── Outstanding fees dated on or before today ──
            // Deliberately mirrors SpGetPending_SMSStudentFee_Msg (the SP behind
            // the fee-reminder SMS) so the dashboard figure and the reminder run
            // always agree: unpaid balance, dated up to today, active fee row,
            // and only students currently enrolled (StatusID = Active).
            string pendingFeeFrom = $@"
                FROM SMSStudentFee F
                LEFT JOIN SMSStudentAcademic SA ON SA.StudentID = F.StudentID
                WHERE F.FeeAmount - F.ClearedAmount > 0
                  AND CAST(F.VDate AS DATE) <= {{0}}
                  AND F.IsActive = 1
                  AND SA.StatusID = {StatusActive}";

            decimal pendingFeeTotal = await SafeScalarDecimal(
                "SELECT ISNULL(SUM(F.FeeAmount - F.ClearedAmount), 0) AS Value" + pendingFeeFrom, today);
            int pendingFeeStudents = await SafeScalarInt(
                "SELECT COUNT(DISTINCT F.StudentID) AS Value" + pendingFeeFrom, today);

            // ── The same money, owed by children who have already LEFT ──
            // The card above counts only students still enrolled, so the day a
            // child is marked Dropped Out, Transferred or Passed Out their
            // arrears vanish from the dashboard entirely. This is the figure
            // the Unpaid Fee report shows as "students who have left".
            //
            // EXISTS rather than a join to SMSStudentAcademic: a student has one
            // enrolment row per session, and joining would count their fees once
            // per row. "Left" means they have an enrolment history and not one
            // active row in it, which also makes this and the card above add up
            // without overlapping.
            string leftFeeFrom = $@"
                FROM SMSStudentFee F
                WHERE F.FeeAmount - F.ClearedAmount > 0
                  AND CAST(F.VDate AS DATE) <= {{0}}
                  AND F.IsActive = 1
                  AND EXISTS (SELECT 1 FROM SMSStudentAcademic SA
                               WHERE SA.StudentID = F.StudentID)
                  AND NOT EXISTS (SELECT 1 FROM SMSStudentAcademic SA
                                   WHERE SA.StudentID = F.StudentID
                                     AND SA.StatusID = {StatusActive})";

            decimal leftPendingFeeTotal = await SafeScalarDecimal(
                "SELECT ISNULL(SUM(F.FeeAmount - F.ClearedAmount), 0) AS Value" + leftFeeFrom, today);
            int leftPendingFeeStudents = await SafeScalarInt(
                "SELECT COUNT(DISTINCT F.StudentID) AS Value" + leftFeeFrom, today);

            // ── Today's attendance % (StdAttendence: Attendence=1 means Present) ──
            int todayMarked = await SafeScalarInt(
                "SELECT COUNT(*) AS Value FROM StdAttendence WHERE CAST(VDate AS DATE) = {0}", today);
            decimal todayAttendancePercent = 0;
            if (todayMarked > 0)
            {
                int todayPresent = await SafeScalarInt(
                    "SELECT COUNT(*) AS Value FROM StdAttendence WHERE CAST(VDate AS DATE) = {0} AND Attendence = 1", today);
                todayAttendancePercent = Math.Round((decimal)todayPresent * 100m / todayMarked, 1);
            }

            // ── Recent students (raw SQL on SMSStudent) ────────────────
            var recentStudents = new List<object>();
            try
            {
                var rows = await _context.Database
                    // Father from SMSParent, class and section from the student's
                    // latest academic record - SMSStudent holds neither.
                    .SqlQueryRaw<RecentStudentRow>(@"
                        SELECT TOP 5
                            S.VID                      AS Id,
                            ISNULL(S.Name, '')         AS Name,
                            ISNULL(P.FatherName, '')   AS FatherName,
                            ISNULL(S.RollNo, '')       AS RollNo,
                            ISNULL(C.VName, '')        AS ClassName,
                            ISNULL(SEC.VName, '')      AS SectionName,
                            ISNULL(S.AdmissionDate, S.InsertedDate) AS AdmissionDate,
                            ISNULL(S.IsActive, 0)      AS IsActive
                        FROM SMSStudent S
                        LEFT JOIN SMSParent P ON P.VID = S.ParentID
                        OUTER APPLY (SELECT TOP 1 A.SectionID FROM SMSStudentAcademic A
                                     WHERE A.StudentID = S.VID AND ISNULL(A.IsActive, 1) = 1
                                     ORDER BY A.SessionID DESC, A.VID DESC) A
                        LEFT JOIN SMSSection SEC ON SEC.VID = A.SectionID
                        LEFT JOIN SMSClass   C   ON C.VID   = SEC.ClassID
                        ORDER BY ISNULL(S.AdmissionDate, S.InsertedDate) DESC, S.VID DESC")
                    .ToListAsync();

                foreach (var r in rows)
                {
                    recentStudents.Add(new
                    {
                        id = r.Id,
                        name = r.Name,
                        fatherName = r.FatherName,
                        rollNo = r.RollNo,
                        className = r.ClassName,
                        sectionName = r.SectionName,
                        admissionDate = r.AdmissionDate,
                        isActive = r.IsActive
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Dashboard: recent students query failed");
            }

            // ── Recent fee vouchers (FeeVoucherMain joined to PaymentMethod) ──
            var recentVouchers = new List<object>();
            try
            {
                var rows = await _context.Database
                    .SqlQueryRaw<RecentVoucherRow>(@"
                        SELECT TOP 5
                            v.VID                       AS Id,
                            ISNULL(v.VNo, '')           AS VoucherNumber,
                            v.VDate                     AS PaymentDate,
                            v.PaidAmount                AS AmountPaid,
                            ISNULL(pm.VName, '')        AS PaymentMethod
                        FROM FeeVoucherMain v
                        LEFT JOIN PaymentMethod pm ON pm.VID = v.PaymentMethodID
                        ORDER BY v.VDate DESC, v.VID DESC")
                    .ToListAsync();

                foreach (var r in rows)
                {
                    recentVouchers.Add(new
                    {
                        id = r.Id,
                        voucherNumber = r.VoucherNumber,
                        paymentDate = r.PaymentDate,
                        amount = r.AmountPaid,
                        paymentMethod = r.PaymentMethod
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Dashboard: recent vouchers query failed");
            }

            return Ok(new
            {
                totals = new
                {
                    sessionId,
                    students = enrolledStudents,
                    activeStudents,
                    newAdmissions,
                    dropoutStudents,
                    employees = totalEmployees,
                    classes = totalClasses,
                    sections = totalSections,
                    subjects = totalSubjects,
                    campuses = totalCampuses
                },
                fees = new
                {
                    todayCollection,
                    monthCollection,
                    todayVoucherCount,
                    monthVoucherCount,
                    pendingFeeTotal,
                    pendingFeeStudents,
                    leftPendingFeeTotal,
                    leftPendingFeeStudents,
                    pendingAsOf = today
                },
                attendance = new
                {
                    todayPercent = todayAttendancePercent
                },
                recentStudents,
                recentVouchers
            });
        }

        private async Task<decimal> SafeScalarDecimal(string sql, params object[] parameters)
        {
            try
            {
                var rows = await _context.Database.SqlQueryRaw<DecimalRow>(sql, parameters).ToListAsync();
                return rows.FirstOrDefault()?.Value ?? 0m;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Dashboard scalar decimal failed: {Sql}", sql);
                return 0m;
            }
        }

        private async Task<int> SafeScalarInt(string sql, params object[] parameters)
        {
            try
            {
                var rows = await _context.Database.SqlQueryRaw<IntRow>(sql, parameters).ToListAsync();
                return rows.FirstOrDefault()?.Value ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Dashboard scalar int failed: {Sql}", sql);
                return 0;
            }
        }

        private async Task<DateTime?> SafeScalarDate(string sql, params object[] parameters)
        {
            try
            {
                var rows = await _context.Database.SqlQueryRaw<DateRow>(sql, parameters).ToListAsync();
                return rows.FirstOrDefault()?.Value;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Dashboard scalar date failed: {Sql}", sql);
                return null;
            }
        }

        public class RecentVoucherRow
        {
            public int Id { get; set; }
            public string VoucherNumber { get; set; } = "";
            public DateTime PaymentDate { get; set; }
            public decimal AmountPaid { get; set; }
            public string PaymentMethod { get; set; } = "";
        }

        public class RecentStudentRow
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string FatherName { get; set; } = "";
            public string RollNo { get; set; } = "";
            public string ClassName { get; set; } = "";
            public string SectionName { get; set; } = "";
            public DateTime? AdmissionDate { get; set; }
            public bool IsActive { get; set; }
        }

        public class DecimalRow { public decimal Value { get; set; } }
        public class IntRow { public int Value { get; set; } }
        public class DateRow { public DateTime? Value { get; set; } }
    }
}
