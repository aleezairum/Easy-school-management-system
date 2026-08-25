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
            int totalCampuses   = await SafeScalarInt("SELECT COUNT(*) AS Value FROM Campus");

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
                    .SqlQueryRaw<RecentStudentRow>(@"
                        SELECT TOP 5
                            VID            AS Id,
                            ISNULL(Name, '')         AS Name,
                            ISNULL(FatherName, '')   AS FatherName,
                            ISNULL(RollNo, '')       AS RollNo,
                            ISNULL(ClassName, '')    AS ClassName,
                            ISNULL(SectionName, '')  AS SectionName,
                            ISNULL(AdmissionDate, InsertedDate) AS AdmissionDate,
                            ISNULL(IsActive, 0)      AS IsActive
                        FROM SMSStudent
                        ORDER BY ISNULL(AdmissionDate, InsertedDate) DESC, VID DESC")
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
