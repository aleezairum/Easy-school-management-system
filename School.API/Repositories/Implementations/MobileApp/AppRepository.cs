using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.DTOs.MobileApp;

namespace School.API.Repositories.Implementations.MobileApp
{
    /// <summary>
    /// The mobile app's reads. Every method takes ids the caller has already been
    /// allowed (AppService checks the scope); none of them decides who may see what.
    /// </summary>
    /// <remarks>
    /// "Enrolled" throughout means the student's latest academic record has
    /// StatusID 1 - the same test the WhatsApp bot and the fee procedures use.
    /// </remarks>
    public class AppRepository
    {
        private readonly SchoolDbContext _context;
        public AppRepository(SchoolDbContext context) => _context = context;

        private SqlConnection Connect() => new(_context.Database.GetConnectionString());

        private async Task<List<T>> QueryAsync<T>(string sql, Func<SqlDataReader, T> map, params (string, object?)[] args)
        {
            var list = new List<T>();
            await using var cn = Connect();
            await cn.OpenAsync();
            await using var cmd = new SqlCommand(sql, cn);
            foreach (var (n, v) in args) cmd.Parameters.AddWithValue(n, v ?? DBNull.Value);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) list.Add(map(r));
            return list;
        }

        private static string? S(SqlDataReader r, int i) => r.IsDBNull(i) ? null : Convert.ToString(r.GetValue(i));
        private static int I(SqlDataReader r, int i) => r.IsDBNull(i) ? 0 : Convert.ToInt32(r.GetValue(i));
        private static decimal D(SqlDataReader r, int i) => r.IsDBNull(i) ? 0 : Convert.ToDecimal(r.GetValue(i));

        // ── students ──────────────────────────────────────────────────

        private const string StudentCardSql = @"
            SELECT S.VID, S.Name, S.RollNo, P.FatherName, ISNULL(S.CampusID, 0), CA.VName,
                   ISNULL(C.VID, 0), C.VName, ISNULL(SEC.VID, 0), SEC.VName, S.Gender, A.StatusID, S.ParentID
            FROM SMSStudent S
            LEFT JOIN SMSParent P ON P.VID = S.ParentID
            OUTER APPLY (SELECT TOP 1 X.SectionID, X.StatusID FROM SMSStudentAcademic X
                         WHERE X.StudentID = S.VID AND ISNULL(X.IsActive, 1) = 1
                         ORDER BY X.SessionID DESC, X.VID DESC) A
            LEFT JOIN SMSSection SEC ON SEC.VID = A.SectionID
            LEFT JOIN SMSClass   C   ON C.VID = SEC.ClassID
            LEFT JOIN SMSCampus  CA  ON CA.VID = S.CampusID";

        private static AppStudentCardDto MapStudent(SqlDataReader r) => new()
        {
            StudentId = I(r, 0), Name = S(r, 1) ?? "", RollNo = S(r, 2), FatherName = S(r, 3),
            CampusId = I(r, 4), CampusName = S(r, 5), ClassId = I(r, 6), ClassName = S(r, 7),
            SectionId = I(r, 8), SectionName = S(r, 9), Gender = S(r, 10)
        };

        /// <summary>One student, enrolled or not (null when there is no such student).</summary>
        public async Task<(AppStudentCardDto Card, bool Enrolled, int? ParentId)?> GetStudentAsync(int studentId)
        {
            var rows = await QueryAsync(StudentCardSql + " WHERE S.VID = @Id;",
                r => (MapStudent(r), I(r, 11) == 1, r.IsDBNull(12) ? (int?)null : I(r, 12)), ("@Id", studentId));
            return rows.Count == 0 ? null : rows[0];
        }

        /// <summary>The enrolled children of one parent row.</summary>
        public Task<List<AppStudentCardDto>> GetFamilyAsync(int parentId) =>
            QueryAsync(StudentCardSql + " WHERE S.ParentID = @P AND A.StatusID = 1 ORDER BY S.Name;", MapStudent, ("@P", parentId));

        /// <summary>Enrolled children whose father/mother/guardian/student mobile is this number (a teacher's own children).</summary>
        public Task<List<AppStudentCardDto>> GetChildrenByMobileAsync(string? mobile)
        {
            var digits = new string((mobile ?? "").Where(char.IsDigit).ToArray());
            if (digits.Length < 10) return Task.FromResult(new List<AppStudentCardDto>());
            var last10 = digits[^10..];
            const string phone = "RIGHT(REPLACE(REPLACE(REPLACE(ISNULL({0},''),' ',''),'-',''),'+',''), 10) = @L";
            return QueryAsync(StudentCardSql + $@" WHERE A.StatusID = 1 AND ({string.Format(phone, "S.MobileNo")} OR {string.Format(phone, "P.FatherMobile")}
                 OR {string.Format(phone, "P.MotherMobile")} OR {string.Format(phone, "P.GuardianMobile")}) ORDER BY S.Name;", MapStudent, ("@L", last10));
        }

        /// <summary>The enrolled students of one section, by roll number.</summary>
        public Task<List<AppStudentCardDto>> GetSectionStudentsAsync(int sectionId) =>
            QueryAsync(StudentCardSql + " WHERE A.StatusID = 1 AND A.SectionID = @Sec ORDER BY TRY_CONVERT(INT, S.RollNo), S.RollNo, S.Name;",
                MapStudent, ("@Sec", sectionId));

        public Task<List<AppStudentCardDto>> SearchStudentsAsync(string term, int take) =>
            // TOP goes on the card query itself: wrapped as a derived table, its unnamed ISNULL columns are refused.
            QueryAsync(StudentCardSql.Replace("SELECT S.VID", "SELECT TOP (@Take) S.VID")
                       + " WHERE A.StatusID = 1 AND (S.Name LIKE @T OR S.RollNo = @Exact OR P.FatherName LIKE @T) ORDER BY S.Name;",
                MapStudent, ("@Take", take), ("@T", "%" + term + "%"), ("@Exact", term));

        // ── attendance ────────────────────────────────────────────────

        public Task<List<(DateTime Date, int Code, bool Late, string? Remarks)>> GetStudentAttendanceAsync(int studentId, DateTime from, DateTime to) =>
            QueryAsync(@"SELECT VDate, Attendence, IsLate, Remarks FROM StdAttendence
                         WHERE StudentID = @S AND IsActive = 1 AND VDate BETWEEN @F AND @T ORDER BY VDate;",
                r => (r.GetDateTime(0), I(r, 1), !r.IsDBNull(2) && r.GetBoolean(2), S(r, 3)),
                ("@S", studentId), ("@F", from), ("@T", to));

        public async Task<Dictionary<DateTime, string?>> GetHolidaysAsync(DateTime from, DateTime to) =>
            (await QueryAsync(@"IF OBJECT_ID('dbo.HrHoliday') IS NOT NULL
                                SELECT HDate, Name FROM HrHoliday WHERE ISNULL(IsActive, 1) = 1 AND HDate BETWEEN @F AND @T;",
                r => (r.GetDateTime(0).Date, S(r, 1)), ("@F", from), ("@T", to)))
            .GroupBy(x => x.Item1).ToDictionary(g => g.Key, g => g.First().Item2);

        /// <summary>The section's register for one day: every enrolled student with their mark ("" when not marked).</summary>
        public Task<List<(int StudentId, int? Code, bool Late)>> GetSectionDayAsync(int sectionId, DateTime date) =>
            QueryAsync(@"SELECT StudentID, Attendence, IsLate FROM StdAttendence WHERE SectionID = @Sec AND VDate = @D AND IsActive = 1;",
                r => (I(r, 0), r.IsDBNull(1) ? (int?)null : I(r, 1), !r.IsDBNull(2) && r.GetBoolean(2)), ("@Sec", sectionId), ("@D", date.Date));

        public async Task<int?> GetCurrentSessionIdAsync() =>
            (await QueryAsync(@"SELECT TOP 1 VID FROM AcademicSessionYear ORDER BY CASE WHEN IsCurrent = 1 THEN 0 ELSE 1 END, VID DESC;",
                r => (int?)I(r, 0))).FirstOrDefault();

        // ── results (finalised exams only, as on WhatsApp) ────────────

        public Task<List<AppExamDto>> GetStudentExamsAsync(int studentId) =>
            QueryAsync(@"SELECT E.VID, E.VName, SUM(ISNULL(R.TotalMarks,0)), SUM(ISNULL(R.MarksObtained,0)),
                                SUM(CASE WHEN R.IsAbsent = 1 OR ISNULL(R.IsPassed,0) = 0 THEN 1 ELSE 0 END), MIN(R.VDate)
                         FROM SMSExamResult R JOIN SMSExam E ON E.VID = R.ExamID
                         WHERE R.StudentID = @S AND R.IsActive = 1
                           AND EXISTS (SELECT 1 FROM SMSExamFinalize F WHERE F.ExamID = R.ExamID)
                           AND NOT EXISTS (SELECT 1 FROM SMSExamExclude X WHERE X.ExamID = R.ExamID AND X.StudentID = R.StudentID)
                         GROUP BY E.VID, E.VName ORDER BY MIN(R.VDate) DESC;",
                r =>
                {
                    var total = D(r, 2); var got = D(r, 3);
                    return new AppExamDto
                    {
                        ExamId = I(r, 0), ExamName = S(r, 1) ?? "", TotalMarks = total, ObtainedMarks = got,
                        Percent = total > 0 ? Math.Round(got * 100m / total, 2) : null, FailedSubjects = I(r, 4),
                        FirstPaper = r.IsDBNull(5) ? null : r.GetDateTime(5)
                    };
                }, ("@S", studentId));

        public Task<List<AppExamSubjectDto>> GetStudentExamSubjectsAsync(int studentId, int examId) =>
            QueryAsync(@"SELECT SU.VName, R.VDate, ISNULL(R.TotalMarks,0), ISNULL(R.PassMarks,0), R.MarksObtained, ISNULL(R.IsAbsent,0), ISNULL(R.IsPassed,0)
                         FROM SMSExamResult R JOIN SMSSubject SU ON SU.VID = R.SubjectID
                         WHERE R.StudentID = @S AND R.ExamID = @E AND R.IsActive = 1
                           AND EXISTS (SELECT 1 FROM SMSExamFinalize F WHERE F.ExamID = R.ExamID)
                           AND NOT EXISTS (SELECT 1 FROM SMSExamExclude X WHERE X.ExamID = R.ExamID AND X.StudentID = R.StudentID)
                         ORDER BY R.VDate, SU.VName;",
                r => new AppExamSubjectDto
                {
                    SubjectName = S(r, 0) ?? "", Date = r.IsDBNull(1) ? null : r.GetDateTime(1), TotalMarks = D(r, 2), PassMarks = D(r, 3),
                    Obtained = r.IsDBNull(4) ? null : D(r, 4), Absent = Convert.ToBoolean(r.GetValue(5)), Passed = Convert.ToBoolean(r.GetValue(6))
                }, ("@S", studentId), ("@E", examId));

        // ── diary ─────────────────────────────────────────────────────

        /// <summary>Diaries for the class/section between two dates (SectionID NULL = the whole class).</summary>
        public async Task<List<AppDiaryDto>> GetDiaryAsync(int classId, int sectionId, DateTime from, DateTime to)
        {
            var rows = await QueryAsync(@"SELECT D.VID, D.DiaryDate, D.Notes, I.VID
                         FROM SMSDailyDiary D LEFT JOIN SMSDailyDiaryImage I ON I.DiaryID = D.VID
                         WHERE D.ClassID = @C AND (D.SectionID IS NULL OR D.SectionID = @Sec) AND D.DiaryDate BETWEEN @F AND @T
                         ORDER BY D.DiaryDate DESC, D.VID, I.SortOrder, I.VID;",
                r => (Id: I(r, 0), Date: r.GetDateTime(1), Notes: S(r, 2), Img: r.IsDBNull(3) ? (int?)null : I(r, 3)),
                ("@C", classId), ("@Sec", sectionId), ("@F", from.Date), ("@T", to.Date));
            return rows.GroupBy(x => x.Id).Select(g => new AppDiaryDto
            {
                DiaryId = g.Key, Date = g.First().Date, Notes = g.First().Notes,
                ImageIds = g.Where(x => x.Img.HasValue).Select(x => x.Img!.Value).ToList()
            }).ToList();
        }

        /// <summary>A diary picture with the class/section it belongs to, for the access check.</summary>
        public async Task<(byte[] Data, string ContentType, int ClassId, int? SectionId)?> GetDiaryImageAsync(int imageId)
        {
            var rows = await QueryAsync(@"SELECT I.ImageData, I.ContentType, D.ClassID, D.SectionID
                         FROM SMSDailyDiaryImage I JOIN SMSDailyDiary D ON D.VID = I.DiaryID WHERE I.VID = @Id;",
                r => ((byte[])r.GetValue(0), S(r, 1) ?? "image/jpeg", I(r, 2), r.IsDBNull(3) ? (int?)null : I(r, 3)), ("@Id", imageId));
            return rows.Count == 0 ? null : rows[0];
        }

        // ── announcements ─────────────────────────────────────────────

        /// <summary>Running today for this audience (1 parents, 2 staff; 0 = everyone) on these campuses.</summary>
        public Task<List<AppAnnouncementDto>> GetAnnouncementsAsync(byte audience, IEnumerable<int> campusIds)
        {
            var campuses = string.Join(",", campusIds.Distinct().Select(c => c.ToString()));
            return QueryAsync($@"SELECT VID, Title, Message, StartDate, EndDate, CASE WHEN ImageData IS NULL THEN 0 ELSE 1 END
                         FROM SMSAnnouncement
                         WHERE IsActive = 1 AND CAST(GETDATE() AS DATE) BETWEEN StartDate AND ISNULL(EndDate, '9999-12-31')
                           AND Audience IN (0, @A)
                           AND (CampusID IS NULL{(campuses.Length > 0 ? $" OR CampusID IN ({campuses})" : "")})
                         ORDER BY StartDate DESC, VID DESC;",
                r => new AppAnnouncementDto
                {
                    Id = I(r, 0), Title = S(r, 1) ?? "", Message = S(r, 2) ?? "", StartDate = r.GetDateTime(3),
                    EndDate = r.IsDBNull(4) ? null : r.GetDateTime(4), HasImage = I(r, 5) == 1
                }, ("@A", (int)audience));
        }

        public async Task<(byte[] Data, string ContentType)?> GetAnnouncementImageAsync(int id)
        {
            var rows = await QueryAsync("SELECT ImageData, ImageContentType FROM SMSAnnouncement WHERE VID = @Id AND ImageData IS NOT NULL;",
                r => ((byte[])r.GetValue(0), S(r, 1) ?? "image/jpeg"), ("@Id", id));
            return rows.Count == 0 ? null : rows[0];
        }

        // ── staff ─────────────────────────────────────────────────────

        public async Task<(AppStaffCardDto Card, string? Mobile)?> GetStaffAsync(int employeeId)
        {
            var rows = await QueryAsync(@"SELECT E.Id, E.EmployeeCode, LTRIM(RTRIM(E.FirstName + ' ' + ISNULL(E.LastName,''))), D.Name, E.CampusID, CA.VName,
                                 CASE WHEN ISNULL(E.Photo,'') = '' THEN 0 ELSE 1 END, E.Mobile
                         FROM Employees E LEFT JOIN Designations D ON D.Id = E.DesignationId LEFT JOIN SMSCampus CA ON CA.VID = E.CampusID
                         WHERE E.Id = @Id;",
                r => (new AppStaffCardDto
                {
                    EmployeeId = I(r, 0), EmployeeCode = S(r, 1), Name = S(r, 2) ?? "", Designation = S(r, 3),
                    CampusId = r.IsDBNull(4) ? null : I(r, 4), CampusName = S(r, 5), HasPhoto = I(r, 6) == 1
                }, S(r, 7)), ("@Id", employeeId));
            return rows.Count == 0 ? null : rows[0];
        }

        public Task<List<AppStaffDayDto>> GetStaffDaysAsync(int employeeId, DateTime from, DateTime to) =>
            QueryAsync(@"SELECT AttDate, Status, TimeIn, TimeOut, ISNULL(LateMinutes,0), ISNULL(EarlyMinutes,0)
                         FROM HrAttendance WHERE EmployeeId = @E AND AttDate BETWEEN @F AND @T ORDER BY AttDate;",
                r => new AppStaffDayDto
                {
                    Date = r.GetDateTime(0), Status = StaffStatus(I(r, 1)),
                    TimeIn = r.IsDBNull(2) ? null : Clock(r.GetValue(2)), TimeOut = r.IsDBNull(3) ? null : Clock(r.GetValue(3)),
                    LateMinutes = I(r, 4), EarlyMinutes = I(r, 5)
                }, ("@E", employeeId), ("@F", from.Date), ("@T", to.Date));

        private static string StaffStatus(int s) => s switch
        {
            1 => "Present", 2 => "Absent", 3 => "Leave", 4 => "Half Day", 5 => "Holiday", 6 => "Weekly Off", 7 => "Short Leave", 8 => "Exempt", _ => ""
        };

        private static string Clock(object v) => v switch
        {
            TimeSpan t => DateTime.Today.Add(t).ToString("hh:mm tt"),
            DateTime d => d.ToString("hh:mm tt"),
            _ => Convert.ToString(v) ?? ""
        };

        /// <summary>The classes/sections a teacher is allocated to, for the newest exam they have allocations in.</summary>
        public async Task<List<AppClassDto>> GetTeacherClassesAsync(int employeeId)
        {
            var rows = await QueryAsync(@"
                ;WITH Latest AS (SELECT TOP 1 ExamID FROM SMSStaffSubject WHERE TeacherID = @E AND ISNULL(IsActive,1) = 1 ORDER BY ExamID DESC)
                SELECT A.ClassID, C.VName, A.SectionID, SEC.VName, SU.VName
                FROM SMSStaffSubject A
                JOIN Latest L ON L.ExamID = A.ExamID
                LEFT JOIN SMSClass C ON C.VID = A.ClassID
                LEFT JOIN SMSSection SEC ON SEC.VID = A.SectionID
                LEFT JOIN SMSSubject SU ON SU.VID = A.SubjectID
                WHERE A.TeacherID = @E AND ISNULL(A.IsActive,1) = 1
                ORDER BY A.ClassID, SEC.VName, SU.VName;",
                r => (Cid: I(r, 0), Cn: S(r, 1), Sid: I(r, 2), Sn: S(r, 3), Sub: S(r, 4)), ("@E", employeeId));
            return rows.Where(x => x.Sid > 0).GroupBy(x => x.Sid).Select(g => new AppClassDto
            {
                ClassId = g.First().Cid, ClassName = g.First().Cn, SectionId = g.Key, SectionName = g.First().Sn,
                Subjects = g.Select(x => x.Sub).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().Select(x => x!).ToList()
            }).ToList();
        }

        public async Task<(int ClassId, string? ClassName, string? SectionName)?> GetSectionAsync(int sectionId)
        {
            var rows = await QueryAsync("SELECT SEC.ClassID, C.VName, SEC.VName FROM SMSSection SEC LEFT JOIN SMSClass C ON C.VID = SEC.ClassID WHERE SEC.VID = @S;",
                r => (I(r, 0), S(r, 1), S(r, 2)), ("@S", sectionId));
            return rows.Count == 0 ? null : rows[0];
        }
    }
}
