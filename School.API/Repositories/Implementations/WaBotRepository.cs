// Repositories/Implementations/WaBotRepository.cs
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.DTOs.SMS;
using School.API.Repositories.Interfaces;

namespace School.API.Repositories.Implementations
{
    /// <summary>
    /// Lookups for the WhatsApp auto-reply.
    /// </summary>
    /// <remarks>
    /// Uses ADO.NET rather than EF, following WaChatRepository's contact
    /// lookup: these are small read-only shapes that would otherwise each need
    /// registering as a keyless entity in SchoolDbContext for no benefit.
    ///
    /// Everything is scoped by StudentID. A parent messaging from their mobile
    /// can only ever be shown the children that number is registered against —
    /// there is no way to ask this for another family's data.
    /// </remarks>
    public class WaBotRepository : IWaBotRepository
    {
        private readonly SchoolDbContext _context;
        private readonly ILogger<WaBotRepository> _logger;

        public WaBotRepository(SchoolDbContext context, ILogger<WaBotRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        private SqlConnection Connect() => new(_context.Database.GetConnectionString());

        // Numbers are stored inconsistently (+92…, 0300…, with spaces and
        // dashes), so everything is compared on the last 10 digits.
        private static string? Last10(string? raw)
        {
            var digits = new string((raw ?? string.Empty).Where(char.IsDigit).ToArray());
            return digits.Length < 10 ? null : digits[^10..];
        }

        public async Task<List<BotStudentDto>> GetStudentsByPhoneAsync(string waId)
        {
            var last10 = Last10(waId);
            if (last10 is null) return new List<BotStudentDto>();

            const string sql = @"
                DECLARE @L VARCHAR(10) = @Last10;

                SELECT DISTINCT
                       S.VID              AS StudentId,
                       S.ParentID         AS ParentId,
                       S.Name             AS Name,
                       S.RollNo           AS RollNo,
                       ISNULL(C.VID, 0)   AS ClassId,
                       ISNULL(SEC.VID, 0) AS SectionId,
                       C.VName            AS ClassName,
                       SEC.VName          AS SectionName,
                       P.FatherName       AS FatherName
                FROM SMSStudent S
                LEFT JOIN SMSParent P ON P.VID = S.ParentID
                OUTER APPLY (
                    SELECT TOP 1 A.SectionID, A.StatusID
                    FROM SMSStudentAcademic A
                    WHERE A.StudentID = S.VID AND ISNULL(A.IsActive, 1) = 1
                    ORDER BY A.SessionID DESC, A.VID DESC
                ) A
                LEFT JOIN SMSSection SEC ON SEC.VID = A.SectionID
                LEFT JOIN SMSClass   C   ON C.VID   = SEC.ClassID
                -- Currently enrolled only. SMSStudent.IsActive is 1 for every
                -- row in the table and filters nothing; enrolment lives on the
                -- latest academic record, where StatusID 1 = Active, 5 = Passed
                -- Out and 7 = Dropped Out. Without this a parent of a child who
                -- left years ago would still be served their fees, attendance
                -- and results. This is the same test SpGetPending_SMSStudentFee_Msg
                -- and SpRPT_SMSStudentFee apply.
                WHERE A.StatusID = 1
                  AND (
                        RIGHT(REPLACE(REPLACE(REPLACE(ISNULL(S.MobileNo,''),' ',''),'-',''),'+',''), 10) = @L
                     OR RIGHT(REPLACE(REPLACE(REPLACE(ISNULL(P.FatherMobile,''),' ',''),'-',''),'+',''), 10) = @L
                     OR RIGHT(REPLACE(REPLACE(REPLACE(ISNULL(P.MotherMobile,''),' ',''),'-',''),'+',''), 10) = @L
                     OR RIGHT(REPLACE(REPLACE(REPLACE(ISNULL(P.GuardianMobile,''),' ',''),'-',''),'+',''), 10) = @L
                  )
                ORDER BY S.Name;";

            var list = new List<BotStudentDto>();
            try
            {
                await using var cn = Connect();
                await cn.OpenAsync();
                await using var cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Last10", last10);

                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(new BotStudentDto
                    {
                        StudentId = r.GetInt32(0),
                        ParentId = r.IsDBNull(1) ? null : r.GetInt32(1),
                        Name = r.IsDBNull(2) ? "" : r.GetString(2),
                        RollNo = r.IsDBNull(3) ? null : r.GetString(3),
                        ClassId = r.GetInt32(4),
                        SectionId = r.GetInt32(5),
                        ClassName = r.IsDBNull(6) ? null : r.GetString(6),
                        SectionName = r.IsDBNull(7) ? null : r.GetString(7),
                        FatherName = r.IsDBNull(8) ? null : r.GetString(8)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[WA-BOT] Student lookup failed for {WaId}", waId);
            }
            return list;
        }

        public async Task<List<BotDueDto>> GetPendingDuesAsync(int studentId)
        {
            // Same definition of "due" as SpGetPending_SMSStudentFee_Msg: an
            // unpaid row dated on or before today. A fee raised for next month
            // is not a due yet.
            const string sql = @"
                SELECT F.VDate, FT.VName, F.FeeAmount - F.ClearedAmount AS Pending
                FROM SMSStudentFee F
                JOIN SMSFeeType FT ON FT.VID = F.FeeTypeID
                WHERE F.StudentID = @sid
                  AND ISNULL(F.IsActive, 1) = 1
                  AND F.FeeAmount - F.ClearedAmount > 0
                  AND CAST(F.VDate AS DATE) <= CAST(GETDATE() AS DATE)
                ORDER BY F.VDate;";

            var list = new List<BotDueDto>();
            try
            {
                await using var cn = Connect();
                await cn.OpenAsync();
                await using var cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@sid", studentId);

                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(new BotDueDto
                    {
                        VDate = r.GetDateTime(0),
                        FeeTypeName = r.IsDBNull(1) ? null : r.GetString(1),
                        PendingAmount = r.GetDecimal(2)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[WA-BOT] Pending dues failed for student {Id}", studentId);
            }
            return list;
        }

        public async Task<BotVoucherDto?> GetLastVoucherAsync(int studentId)
        {
            // A voucher belongs to the family, so it is found through the
            // detail lines that settled THIS student's fees.
            const string headSql = @"
                SELECT TOP 1 H.VID, H.VNo, H.VDate, H.PaidAmount, PM.VName
                FROM FeeVoucherMain H
                LEFT JOIN PaymentMethod PM ON PM.VID = H.PaymentMethodID
                WHERE ISNULL(H.IsActive, 1) = 1
                  AND EXISTS (
                      SELECT 1
                      FROM FeeVoucherDetail D
                      JOIN SMSStudentFee F ON F.VID = D.StudentFeeID
                      WHERE D.MID = H.VID AND F.StudentID = @sid
                        AND ISNULL(D.IsActive, 1) = 1)
                ORDER BY H.VDate DESC, H.VID DESC;";

            const string lineSql = @"
                SELECT FT.VName + ' ' + FORMAT(F.VDate, 'MMM-yyyy'), D.PaidAmount
                FROM FeeVoucherDetail D
                JOIN SMSStudentFee F ON F.VID = D.StudentFeeID
                JOIN SMSFeeType   FT ON FT.VID = F.FeeTypeID
                WHERE D.MID = @mid AND F.StudentID = @sid
                  AND ISNULL(D.IsActive, 1) = 1
                ORDER BY F.VDate;";

            try
            {
                await using var cn = Connect();
                await cn.OpenAsync();

                BotVoucherDto? voucher = null;
                int mid = 0;

                await using (var cmd = new SqlCommand(headSql, cn))
                {
                    cmd.Parameters.AddWithValue("@sid", studentId);
                    await using var r = await cmd.ExecuteReaderAsync();
                    if (await r.ReadAsync())
                    {
                        mid = r.GetInt32(0);
                        voucher = new BotVoucherDto
                        {
                            Vid = mid,
                            VNo = r.IsDBNull(1) ? mid.ToString() : r.GetString(1),
                            VDate = r.GetDateTime(2),
                            PaidAmount = r.GetDecimal(3),
                            PaymentMethod = r.IsDBNull(4) ? null : r.GetString(4)
                        };
                    }
                }

                if (voucher is null) return null;

                await using (var cmd = new SqlCommand(lineSql, cn))
                {
                    cmd.Parameters.AddWithValue("@mid", mid);
                    cmd.Parameters.AddWithValue("@sid", studentId);
                    await using var r = await cmd.ExecuteReaderAsync();
                    while (await r.ReadAsync())
                    {
                        voucher.Lines.Add(
                            $"{(r.IsDBNull(0) ? "Fee" : r.GetString(0))} — {r.GetDecimal(1):N0}");
                    }
                }

                return voucher;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[WA-BOT] Last voucher failed for student {Id}", studentId);
                return null;
            }
        }

        public async Task<BotLedgerSummaryDto?> GetLedgerSummaryAsync(int studentId)
        {
            // Reuses the ledger procedure the printed report uses, so the
            // figures a parent is quoted here always match the paper statement.
            try
            {
                await using var cn = Connect();
                await cn.OpenAsync();
                await using var cmd = new SqlCommand("dbo.SpRPT_StudentLedger", cn)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@StudentID", studentId);
                cmd.Parameters.AddWithValue("@DateFrom", DBNull.Value);
                cmd.Parameters.AddWithValue("@DateTo", DBNull.Value);

                var summary = new BotLedgerSummaryDto();
                var lines = new List<string>();

                await using var r = await cmd.ExecuteReaderAsync();
                int iRowType = r.GetOrdinal("RowType"),
                    iDate = r.GetOrdinal("LineDate"),
                    iType = r.GetOrdinal("VoucherType"),
                    iDesc = r.GetOrdinal("Description"),
                    iDebit = r.GetOrdinal("Debit"),
                    iCredit = r.GetOrdinal("Credit"),
                    iBal = r.GetOrdinal("Balance");

                while (await r.ReadAsync())
                {
                    if (r.GetInt32(iRowType) == 0) continue;   // opening balance row

                    var debit = r.GetDecimal(iDebit);
                    var credit = r.GetDecimal(iCredit);

                    summary.TotalDebit += debit;
                    summary.TotalCredit += credit;
                    summary.ClosingBalance = r.GetDecimal(iBal);

                    var date = r.IsDBNull(iDate) ? "" : r.GetDateTime(iDate).ToString("dd MMM yyyy");
                    var kind = r.IsDBNull(iType) ? "" : r.GetString(iType);
                    var desc = r.IsDBNull(iDesc) ? "" : r.GetString(iDesc);
                    var amount = debit > 0 ? $"Dr {debit:N0}" : $"Cr {credit:N0}";

                    lines.Add($"{date} · {kind} · {desc} · {amount}");
                }

                // Only the tail is sent — a full ledger would be unreadable in
                // a chat bubble, and the printed report exists for that.
                summary.RecentLines = lines.TakeLast(8).ToList();
                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[WA-BOT] Ledger summary failed for student {Id}", studentId);
                return null;
            }
        }

        public async Task<BotAttendanceDto?> GetAttendanceSummaryAsync(
            int studentId, DateTime from, DateTime to)
        {
            // 1 = Present, 0 = Absent, 2 = Leave — the same codes the
            // attendance screen writes (Views/Attendance/Form.cshtml).
            const string sql = @"
                SELECT SUM(CASE WHEN Attendence = 1 THEN 1 ELSE 0 END),
                       SUM(CASE WHEN Attendence = 0 THEN 1 ELSE 0 END),
                       SUM(CASE WHEN Attendence = 2 THEN 1 ELSE 0 END)
                FROM StdAttendence
                WHERE StudentID = @sid
                  AND ISNULL(IsActive, 1) = 1
                  AND VDate BETWEEN @from AND @to;";

            try
            {
                await using var cn = Connect();
                await cn.OpenAsync();
                await using var cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@sid", studentId);
                cmd.Parameters.AddWithValue("@from", from.Date);
                cmd.Parameters.AddWithValue("@to", to.Date);

                await using var r = await cmd.ExecuteReaderAsync();
                if (!await r.ReadAsync()) return null;

                return new BotAttendanceDto
                {
                    Present = r.IsDBNull(0) ? 0 : r.GetInt32(0),
                    Absent = r.IsDBNull(1) ? 0 : r.GetInt32(1),
                    Leave = r.IsDBNull(2) ? 0 : r.GetInt32(2)
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[WA-BOT] Attendance failed for student {Id}", studentId);
                return null;
            }
        }

        public async Task<List<BotResultDto>> GetLatestResultAsync(int studentId)
        {
            // The student's most recent exam, subject by subject.
            const string sql = @"
                DECLARE @ExamID INT = (
                    SELECT TOP 1 R.ExamID
                    FROM SMSExamResult R
                    WHERE R.StudentID = @sid AND ISNULL(R.IsActive, 1) = 1
                    ORDER BY R.VDate DESC, R.VID DESC);

                SELECT R.ExamID, E.VName, SUB.VName, R.TotalMarks, R.MarksObtained
                FROM SMSExamResult R
                LEFT JOIN SMSExam    E   ON E.VID   = R.ExamID
                LEFT JOIN SMSSubject SUB ON SUB.VID = R.SubjectID
                WHERE R.StudentID = @sid
                  AND R.ExamID = @ExamID
                  AND ISNULL(R.IsActive, 1) = 1
                ORDER BY SUB.VName;";

            var list = new List<BotResultDto>();
            try
            {
                await using var cn = Connect();
                await cn.OpenAsync();
                await using var cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@sid", studentId);

                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(new BotResultDto
                    {
                        ExamId = r.GetInt32(0),
                        ExamName = r.IsDBNull(1) ? null : r.GetString(1),
                        SubjectName = r.IsDBNull(2) ? null : r.GetString(2),
                        TotalMarks = r.IsDBNull(3) ? 0 : r.GetDecimal(3),
                        ObtainedMarks = r.IsDBNull(4) ? 0 : r.GetDecimal(4)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[WA-BOT] Result failed for student {Id}", studentId);
            }
            return list;
        }
    }
}
