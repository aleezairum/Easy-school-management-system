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
                       ISNULL(S.CampusID, 0) AS CampusId,
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
                        CampusId = r.GetInt32(4),
                        ClassId = r.GetInt32(5),
                        SectionId = r.GetInt32(6),
                        ClassName = r.IsDBNull(7) ? null : r.GetString(7),
                        SectionName = r.IsDBNull(8) ? null : r.GetString(8),
                        FatherName = r.IsDBNull(9) ? null : r.GetString(9)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[WA-BOT] Student lookup failed for {WaId}", waId);
            }
            return list;
        }

        public async Task<List<BotStaffDto>> GetStaffByPhoneAsync(string waId)
        {
            var last10 = Last10(waId);
            if (last10 is null) return new List<BotStaffDto>();

            // Active staff whose own mobile or phone is this number. Left staff
            // (IsActive = 0) are not served their records, as a parent of a
            // child who has left is not.
            const string sql = @"
                DECLARE @L VARCHAR(10) = @Last10;
                SELECT E.Id, E.EmployeeCode,
                       LTRIM(RTRIM(E.FirstName + ISNULL(' ' + E.LastName, ''))) AS Name,
                       D.Name  AS DesignationName,
                       E.CampusID,
                       C.VName AS CampusName
                FROM Employees E
                LEFT JOIN Designations D ON D.Id = E.DesignationId
                LEFT JOIN SMSCampus   C ON C.VID = E.CampusID
                WHERE E.IsActive = 1
                  AND (  RIGHT(REPLACE(REPLACE(REPLACE(ISNULL(E.Mobile,''),' ',''),'-',''),'+',''), 10) = @L
                      OR RIGHT(REPLACE(REPLACE(REPLACE(ISNULL(E.Phone,''),' ',''),'-',''),'+',''), 10) = @L)
                ORDER BY Name;";

            var list = new List<BotStaffDto>();
            try
            {
                await using var cn = Connect();
                await cn.OpenAsync();
                await using var cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Last10", last10);

                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(new BotStaffDto
                    {
                        EmployeeId = r.GetInt32(0),
                        EmployeeCode = r.IsDBNull(1) ? "" : r.GetString(1),
                        Name = r.IsDBNull(2) ? "" : r.GetString(2),
                        DesignationName = r.IsDBNull(3) ? null : r.GetString(3),
                        CampusId = r.IsDBNull(4) ? null : r.GetInt32(4),
                        CampusName = r.IsDBNull(5) ? null : r.GetString(5)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[WA-BOT] Staff lookup failed for {WaId}", waId);
            }
            return list;
        }

        public async Task<List<BotDiaryPageDto>> GetDiaryPagesAsync(int classId, int sectionId, DateTime date)
        {
            const string sql = @"
                SELECT D.VID, D.Notes, I.FileName, I.ContentType, I.ImageData
                FROM SMSDailyDiary D
                JOIN SMSDailyDiaryImage I ON I.DiaryID = D.VID
                WHERE D.DiaryDate = @d AND D.ClassID = @c
                  AND (D.SectionID = @s OR D.SectionID IS NULL)
                ORDER BY CASE WHEN D.SectionID IS NULL THEN 1 ELSE 0 END, D.VID, I.SortOrder, I.VID;";

            var list = new List<BotDiaryPageDto>();
            try
            {
                await using var cn = Connect();
                await cn.OpenAsync();
                await using var cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@d", date.Date);
                cmd.Parameters.AddWithValue("@c", classId);
                cmd.Parameters.AddWithValue("@s", sectionId);
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    list.Add(new BotDiaryPageDto
                    {
                        DiaryId = r.GetInt32(0),
                        Notes = r.IsDBNull(1) ? null : r.GetString(1),
                        FileName = r.GetString(2),
                        ContentType = r.GetString(3),
                        Data = (byte[])r[4]
                    });
            }
            catch (Exception ex)
            {
                // Before DailyDiaryAndAnnouncements.sql runs the table is not
                // there; the parent is told there is no diary, not given an error.
                _logger.LogWarning(ex, "[WA-BOT] Diary lookup failed for class {Class} section {Section}", classId, sectionId);
            }
            return list;
        }

        public async Task<List<BotAnnouncementDto>> GetCurrentAnnouncementsAsync(byte audience, IEnumerable<int?> campusIds)
        {
            var campuses = campusIds.Where(c => c.HasValue).Select(c => c!.Value).Distinct().ToList();
            var sql = $@"
                SELECT VID, Title, Message, StartDate, EndDate, ImageFileName, ImageData
                FROM SMSAnnouncement
                WHERE IsActive = 1
                  AND StartDate <= CAST(GETDATE() AS DATE)
                  AND (EndDate IS NULL OR EndDate >= CAST(GETDATE() AS DATE))
                  AND Audience IN (0, @aud)
                  AND (CampusID IS NULL{(campuses.Count > 0 ? $" OR CampusID IN ({string.Join(",", campuses)})" : "")})
                ORDER BY StartDate DESC, VID DESC;";

            var list = new List<BotAnnouncementDto>();
            try
            {
                await using var cn = Connect();
                await cn.OpenAsync();
                await using var cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@aud", (int)audience);
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    list.Add(new BotAnnouncementDto
                    {
                        Id = r.GetInt32(0),
                        Title = r.GetString(1),
                        Message = r.GetString(2),
                        StartDate = r.GetDateTime(3),
                        EndDate = r.IsDBNull(4) ? null : r.GetDateTime(4),
                        ImageFileName = r.IsDBNull(5) ? null : r.GetString(5),
                        ImageData = r.IsDBNull(6) ? null : (byte[])r[6]
                    });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[WA-BOT] Announcement lookup failed");
            }
            return list;
        }

        public async Task<(int ExamId, string ExamName)?> GetTeacherLatestExamAsync(int employeeId)
        {
            // The newest exam in which a subject assigned to this teacher (Staff
            // Subjects screen) actually has marks entered. "Newest" by when the
            // marks went in, not by exam id: a schedule test entered after a term
            // is the more recent result. Only exams whose result the office has
            // finalised (Exams screen), so half-entered marks never go out.
            const string sql = @"
                SELECT TOP 1 S.ExamID, E.VName
                FROM SMSStaffSubject S
                JOIN SMSExam E ON E.VID = S.ExamID
                CROSS APPLY (SELECT MAX(R.VDate) AS LastMark
                             FROM SMSExamResult R
                             WHERE R.ExamID = S.ExamID AND R.ClassID = S.ClassID
                               AND R.SectionID = S.SectionID AND R.SubjectID = S.SubjectID
                               AND R.IsActive = 1) M
                WHERE S.TeacherID = @tid AND S.IsActive = 1 AND M.LastMark IS NOT NULL
                  AND EXISTS (SELECT 1 FROM SMSExamFinalize F WHERE F.ExamID = S.ExamID)
                GROUP BY S.ExamID, E.VName
                ORDER BY MAX(M.LastMark) DESC, S.ExamID DESC;";
            try
            {
                await using var cn = Connect();
                await cn.OpenAsync();
                await using var cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@tid", employeeId);
                await using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                    return (r.GetInt32(0), r.IsDBNull(1) ? "" : r.GetString(1));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[WA-BOT] Teacher exam lookup failed for {Id}", employeeId);
            }
            return null;
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

                    var date = r.IsDBNull(iDate) ? "" : r.GetDateTime(iDate).ToString("dd-MMM-yyyy");
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

        public async Task<BotTodayAttendanceDto?> GetTodayAttendanceAsync(int studentId, DateTime date)
        {
            // No row means the register has not been filled for that date. That
            // is returned as null so the reply can say "not marked yet" rather
            // than implying the child was absent.
            const string sql = @"
                SELECT TOP 1 A.Attendence, ISNULL(A.IsLate, 0), A.Remarks
                FROM StdAttendence A
                WHERE A.StudentID = @sid
                  AND ISNULL(A.IsActive, 1) = 1
                  AND A.VDate = @date
                ORDER BY A.VID DESC;";

            try
            {
                await using var cn = Connect();
                await cn.OpenAsync();
                await using var cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@sid", studentId);
                cmd.Parameters.AddWithValue("@date", date.Date);

                await using var r = await cmd.ExecuteReaderAsync();
                if (!await r.ReadAsync()) return null;

                return new BotTodayAttendanceDto
                {
                    Code = r.IsDBNull(0) ? null : r.GetInt32(0),
                    IsLate = !r.IsDBNull(1) && r.GetBoolean(1),
                    Remarks = r.IsDBNull(2) ? null : r.GetString(2)
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[WA-BOT] Today's attendance failed for student {Id}", studentId);
                return null;
            }
        }

        public async Task<List<BotResultDto>> GetLatestResultAsync(int studentId)
        {
            // The student's most recent FINALISED exam, subject by subject. An
            // exam still being marked is not shown (Exams screen -> Finalize).
            const string sql = @"
                DECLARE @ExamID INT = (
                    SELECT TOP 1 R.ExamID
                    FROM SMSExamResult R
                    WHERE R.StudentID = @sid AND ISNULL(R.IsActive, 1) = 1
                      AND EXISTS (SELECT 1 FROM SMSExamFinalize F WHERE F.ExamID = R.ExamID)
                      AND NOT EXISTS (SELECT 1 FROM SMSExamExclude X WHERE X.ExamID = R.ExamID AND X.StudentID = R.StudentID)
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

        // ── Enquiry lookups ──────────────────────────────────────────────
        //
        // The fee schedule is not scoped to a student — it answers what any
        // caller may ask. The class timings are: they come back for every class
        // and the caller matches them to the children the number is registered
        // against, so no one is told another family's anything.

        public async Task<List<BotClassTimingDto>> GetClassTimingsAsync()
        {
            // Every active row in one go, rather than a query per child. The
            // table holds one row per class per campus — a few dozen at most —
            // so fetching the lot costs less than round-tripping for a family
            // with three children, and the caller matches in memory.
            const string sql = @"
                SELECT T.CampusID, T.ClassID, T.TimeIn, T.TimeOut,
                       T.FridayTimeIn, T.FridayTimeOut
                FROM SMSClassTiming T
                WHERE ISNULL(T.IsActive, 1) = 1;";

            var list = new List<BotClassTimingDto>();
            try
            {
                await using var cn = Connect();
                await cn.OpenAsync();
                await using var cmd = new SqlCommand(sql, cn);

                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(new BotClassTimingDto
                    {
                        CampusId      = r.GetInt32(0),
                        ClassId       = r.GetInt32(1),
                        TimeIn        = r.GetTimeSpan(2),
                        TimeOut       = r.GetTimeSpan(3),
                        FridayTimeIn  = r.IsDBNull(4) ? null : r.GetTimeSpan(4),
                        FridayTimeOut = r.IsDBNull(5) ? null : r.GetTimeSpan(5)
                    });
                }
            }
            catch (Exception ex)
            {
                // A missing table (the script not yet run on this database) must
                // not take the whole reply down — the parent still gets their
                // dues and attendance, just no timings.
                _logger.LogWarning(ex, "[WA-BOT] Class timing lookup failed");
            }
            return list;
        }

        public async Task<BotFeeStructureDto?> GetFeeStructureAsync()
        {
            // The CURRENT session only. Fees are re-entered every year and the
            // old rows stay behind, so quoting anything else would hand a new
            // parent last year's figures.
            const string sql = @"
                DECLARE @SessionID INT =
                    (SELECT TOP 1 VID FROM AcademicSessionYear
                     WHERE ISNULL(IsCurrent, 0) = 1 AND ISNULL(IsActive, 1) = 1
                     ORDER BY VID DESC);

                -- No session flagged current: the newest active one is the
                -- working year in every other report too.
                IF @SessionID IS NULL
                    SET @SessionID = (SELECT TOP 1 VID FROM AcademicSessionYear
                                      WHERE ISNULL(IsActive, 1) = 1
                                      ORDER BY DateFrom DESC, VID DESC);

                SELECT VName, DateFrom, DateTo
                FROM AcademicSessionYear WHERE VID = @SessionID;

                SELECT VID, VName, ISNULL(MinPercentage, 0), ISNULL(MaxPercentage, 0)
                FROM AcademicGrades
                WHERE ISNULL(IsActive, 1) = 1
                ORDER BY MinPercentage DESC, VID;

                SELECT FS.CampusID,
                       ISNULL(CP.VName, 'Main Campus'),
                       FS.FeeTypeID,
                       FT.VName,
                       FT.Frequency,
                       FS.ClassID,
                       LTRIM(RTRIM(CL.VName)),
                       FS.GradeID,
                       FS.Amount
                FROM SMSFeeStructure FS
                JOIN SMSFeeType FT ON FT.VID = FS.FeeTypeID
                JOIN SMSClass   CL ON CL.VID = FS.ClassID
                LEFT JOIN SMSCampus CP ON CP.VID = FS.CampusID
                WHERE FS.SessionID = @SessionID
                  AND ISNULL(FS.IsActive, 1) = 1
                  AND ISNULL(FT.IsActive, 1) = 1
                  AND ISNULL(CL.IsActive, 1) = 1
                ORDER BY FS.CampusID,
                         -- One-off charges head the sheet, the recurring ones follow.
                         CASE WHEN FT.Frequency = 'Once' THEN 0 ELSE 1 END,
                         FS.FeeTypeID,
                         -- SMSClass.VID is the school's own progression
                         -- (Montessori, Play Group, Nursery ... 10). The names
                         -- cannot be sorted: half of them are words.
                         FS.ClassID,
                         FS.GradeID;";

            try
            {
                await using var cn = Connect();
                await cn.OpenAsync();
                await using var cmd = new SqlCommand(sql, cn);

                await using var r = await cmd.ExecuteReaderAsync();

                if (!await r.ReadAsync()) return null;
                var data = new BotFeeStructureDto
                {
                    SessionName = r.IsDBNull(0) ? string.Empty : r.GetString(0).Trim(),
                    SessionFrom = r.IsDBNull(1) ? null : r.GetDateTime(1),
                    SessionTo   = r.IsDBNull(2) ? null : r.GetDateTime(2)
                };

                await r.NextResultAsync();
                while (await r.ReadAsync())
                    data.Grades.Add(new BotFeeGradeDto
                    {
                        GradeId       = r.GetInt32(0),
                        Name          = r.IsDBNull(1) ? string.Empty : r.GetString(1).Trim(),
                        MinPercentage = r.GetDecimal(2),
                        MaxPercentage = r.GetDecimal(3)
                    });

                await r.NextResultAsync();
                while (await r.ReadAsync())
                {
                    var campusId  = r.GetInt32(0);
                    var feeTypeId = r.GetInt32(2);
                    var classId   = r.GetInt32(5);
                    var gradeId   = r.GetInt32(7);

                    var campus = data.Campuses.FirstOrDefault(c => c.CampusId == campusId);
                    if (campus is null)
                    {
                        campus = new BotFeeCampusDto { CampusId = campusId, Name = r.GetString(1).Trim() };
                        data.Campuses.Add(campus);
                    }

                    var schedule = campus.Schedules.FirstOrDefault(s => s.FeeTypeId == feeTypeId);
                    if (schedule is null)
                    {
                        schedule = new BotFeeScheduleDto
                        {
                            FeeTypeId   = feeTypeId,
                            FeeTypeName = r.IsDBNull(3) ? string.Empty : r.GetString(3).Trim(),
                            Frequency   = r.IsDBNull(4) ? null : r.GetString(4).Trim()
                        };
                        campus.Schedules.Add(schedule);
                    }

                    var row = schedule.Classes.FirstOrDefault(c => c.ClassId == classId);
                    if (row is null)
                    {
                        row = new BotFeeClassRowDto
                        {
                            ClassId   = classId,
                            ClassName = r.IsDBNull(6) ? string.Empty : r.GetString(6)
                        };
                        schedule.Classes.Add(row);
                    }

                    row.Amounts[gradeId] = r.GetDecimal(8);
                    if (gradeId != 0) schedule.IsGradeWise = true;
                }

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[WA-BOT] Fee structure lookup failed");
                return null;
            }
        }
    }
}
