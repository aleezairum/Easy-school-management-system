using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.Student;
using School.API.DTOs.Common;
using School.API.DTOs.Student;
using School.API.Repositories.Interfaces.Student;

namespace School.API.Repositories.Implementations.Student
{
    public class StdAttendenceRepository : IStdAttendenceRepository
    {
        private readonly SchoolDbContext _context;

        public StdAttendenceRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<StdAttendence>> GetAllAsync(StdAttendenceFilterDto filter)
        {
            return await _context
                .Set<StdAttendence>()
                .FromSqlRaw(
                    "EXEC SpGet_StdAttendence @VID={0}, @SessionID={1}, @StudentID={2}, @SectionID={3}, @DateFrom={4}, @DateTo={5}",
                    filter.VID ?? (object)DBNull.Value,
                    filter.SessionID ?? (object)DBNull.Value,
                    filter.StudentID ?? (object)DBNull.Value,
                    filter.SectionID ?? (object)DBNull.Value,
                    filter.DateFrom ?? (object)DBNull.Value,
                    filter.DateTo ?? (object)DBNull.Value)
                .AsNoTracking()
                .ToListAsync();
        }

        // ── Monthly register ───────────────────────────────────────────

        /// <remarks>
        /// The students are the section's current ones (latest academic record,
        /// status active) and anyone marked in the section during the month - a
        /// child who left or moved mid-month keeps the days they were there.
        /// A day is a school day when anybody in the section was marked on it;
        /// otherwise it shows as Sunday, a holiday (HrHoliday) or not taken.
        /// </remarks>
        public async Task<List<AttendanceRegisterDto>> GetRegisterAsync(int classId, int? sectionId, int year, int month)
        {
            var from = new DateTime(year, month, 1);
            var to = from.AddMonths(1).AddDays(-1);
            var days = to.Day;

            var conn = _context.Database.GetDbConnection();
            var close = conn.State != System.Data.ConnectionState.Open;
            if (close) await conn.OpenAsync();
            try
            {
                async Task<List<object?[]>> Rows(string sql)
                {
                    await using var cmd = conn.CreateCommand();
                    cmd.CommandText = sql;
                    foreach (var (n, v) in new (string, object?)[] { ("@Class", classId), ("@Section", sectionId), ("@From", from), ("@To", to) })
                    {
                        var p = cmd.CreateParameter(); p.ParameterName = n; p.Value = v ?? DBNull.Value; cmd.Parameters.Add(p);
                    }
                    var list = new List<object?[]>();
                    await using var r = await cmd.ExecuteReaderAsync();
                    while (await r.ReadAsync())
                    {
                        var row = new object?[r.FieldCount];
                        for (var i = 0; i < r.FieldCount; i++) row[i] = r.IsDBNull(i) ? null : r.GetValue(i);
                        list.Add(row);
                    }
                    return list;
                }

                var sections = await Rows(@"
                    SELECT SE.VID, SE.VName, CL.VName
                    FROM SMSSection SE LEFT JOIN SMSClass CL ON CL.VID = SE.ClassID
                    WHERE SE.ClassID = @Class AND (@Section IS NULL OR SE.VID = @Section)
                    ORDER BY SE.VName;");
                if (sections.Count == 0) return new List<AttendanceRegisterDto>();

                var students = await Rows(@"
                    ;WITH Latest AS (
                        SELECT S.VID AS StudentID, A.SectionID, A.StatusID
                        FROM SMSStudent S
                        OUTER APPLY (SELECT TOP 1 SA.SectionID, SA.StatusID FROM SMSStudentAcademic SA
                                     WHERE SA.StudentID = S.VID AND ISNULL(SA.IsActive, 1) = 1
                                     ORDER BY SA.SessionID DESC, SA.VID DESC) A),
                    Members AS (
                        SELECT L.StudentID, L.SectionID FROM Latest L
                        JOIN SMSSection SE ON SE.VID = L.SectionID
                        WHERE L.StatusID = 1 AND SE.ClassID = @Class AND (@Section IS NULL OR SE.VID = @Section)
                        UNION
                        SELECT DISTINCT T.StudentID, T.SectionID FROM StdAttendence T
                        JOIN SMSSection SE ON SE.VID = T.SectionID
                        WHERE T.IsActive = 1 AND T.VDate BETWEEN @From AND @To
                          AND SE.ClassID = @Class AND (@Section IS NULL OR SE.VID = @Section))
                    SELECT M.SectionID, M.StudentID, ST.RollNo, ST.Name, PA.FatherName,
                           CASE WHEN L.StatusID = 1 THEN 0 ELSE 1 END AS IsLeft
                    FROM Members M
                    JOIN SMSStudent ST ON ST.VID = M.StudentID
                    LEFT JOIN SMSParent PA ON PA.VID = ST.ParentID
                    LEFT JOIN Latest L ON L.StudentID = M.StudentID
                    ORDER BY M.SectionID, TRY_CONVERT(INT, ST.RollNo), ST.RollNo, ST.Name;");

                var marks = await Rows(@"
                    SELECT T.SectionID, T.StudentID, DAY(T.VDate), T.Attendence, T.IsLate
                    FROM StdAttendence T JOIN SMSSection SE ON SE.VID = T.SectionID
                    WHERE T.IsActive = 1 AND T.VDate BETWEEN @From AND @To
                      AND SE.ClassID = @Class AND (@Section IS NULL OR SE.VID = @Section);");

                var holidays = await Rows(@"
                    IF OBJECT_ID('dbo.HrHoliday') IS NOT NULL
                        SELECT DAY(HDate), Name FROM HrHoliday WHERE ISNULL(IsActive, 1) = 1 AND HDate BETWEEN @From AND @To;");
                var holidayName = holidays.GroupBy(h => Convert.ToInt32(h[0])).ToDictionary(g => g.Key, g => g.First()[1]?.ToString());

                // what each student has on each day, per section
                var mark = new Dictionary<(int Sec, int Stu, int Day), string>();
                foreach (var m in marks)
                {
                    var code = Convert.ToInt32(m[3]) switch
                    {
                        1 => Convert.ToBoolean(m[4] ?? false) ? "T" : "P",
                        2 => "L",
                        _ => "A"
                    };
                    mark[(Convert.ToInt32(m[0]), Convert.ToInt32(m[1]), Convert.ToInt32(m[2]))] = code;
                }

                var result = new List<AttendanceRegisterDto>();
                foreach (var sec in sections)
                {
                    var secId = Convert.ToInt32(sec[0]);
                    var taken = mark.Keys.Where(k => k.Sec == secId).Select(k => k.Day).ToHashSet();
                    var reg = new AttendanceRegisterDto
                    {
                        SectionId = secId, SectionName = sec[1]?.ToString(), ClassName = sec[2]?.ToString(),
                        Year = year, Month = month, SchoolDays = taken.Count
                    };
                    for (var d = 1; d <= days; d++)
                    {
                        var date = new DateTime(year, month, d);
                        reg.Days.Add(new RegisterDayDto
                        {
                            Day = d,
                            Weekday = date.ToString("ddd", System.Globalization.CultureInfo.InvariantCulture),
                            Kind = taken.Contains(d) ? "school"
                                 : holidayName.ContainsKey(d) ? "holiday"
                                 : date.DayOfWeek == DayOfWeek.Sunday ? "sunday" : "none",
                            Holiday = holidayName.TryGetValue(d, out var hn) ? hn : null
                        });
                    }

                    foreach (var s in students.Where(s => Convert.ToInt32(s[0]) == secId))
                    {
                        var stuId = Convert.ToInt32(s[1]);
                        var row = new RegisterStudentDto
                        {
                            StudentId = stuId, RollNo = s[2]?.ToString(), Name = s[3]?.ToString(), FatherName = s[4]?.ToString(),
                            Left = Convert.ToInt32(s[5]) == 1
                        };
                        for (var d = 1; d <= days; d++)
                        {
                            var c = mark.TryGetValue((secId, stuId, d), out var v) ? v : "";
                            row.Marks.Add(c);
                            if (c is "P" or "T") row.Present++;
                            if (c == "T") row.Late++;
                            if (c == "A") row.Absent++;
                            if (c == "L") row.Leave++;
                        }
                        var marked = row.Present + row.Absent + row.Leave;
                        row.Percent = marked > 0 ? Math.Round(row.Present * 100m / marked, 1) : null;
                        reg.Students.Add(row);
                    }
                    result.Add(reg);
                }
                return result;
            }
            finally
            {
                if (close) await conn.CloseAsync();
            }
        }

        public async Task<StdAttendence?> GetByIdAsync(int vid)
        {
            var result = await _context
                .Set<StdAttendence>()
                .FromSqlRaw(
                    "EXEC SpGet_StdAttendence @VID={0}, @SessionID={1}, @StudentID={2}, @SectionID={3}, @DateFrom={4}, @DateTo={5}",
                    vid,
                    (object)DBNull.Value,
                    (object)DBNull.Value,
                    (object)DBNull.Value,
                    (object)DBNull.Value,
                    (object)DBNull.Value)
                .AsNoTracking()
                .ToListAsync();

            return result.FirstOrDefault();
        }

        public async Task<ResponseDto?> DeleteByIdAsync(int vid, int userId, string userIp)
        {
            var result = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpDelete_StdAttendence @VID={0}, @UserID={1}, @UserIP={2}",
                    vid,
                    userId,
                    userIp)
                .AsNoTracking()
                .ToListAsync();

            return result.FirstOrDefault();
        }

        public async Task<ResponseDto?> DeleteByFilterAsync(StdAttendenceFilterDto filter, int userId, string userIp)
        {
            var result = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpDelete_StdAttendenceByFilter @VID={0}, @SessionID={1}, @StudentID={2}, @SectionID={3}, @DateFrom={4}, @DateTo={5}, @UserID={6}, @UserIP={7}",
                    filter.VID ?? (object)DBNull.Value,
                    filter.SessionID ?? (object)DBNull.Value,
                    filter.StudentID ?? (object)DBNull.Value,
                    filter.SectionID ?? (object)DBNull.Value,
                    filter.DateFrom ?? (object)DBNull.Value,
                    filter.DateTo ?? (object)DBNull.Value,
                    userId,
                    (object?)userIp ?? DBNull.Value)
                .AsNoTracking()
                .ToListAsync();

            return result.FirstOrDefault();
        }

        public async Task<ResponseDto> SaveAsync(StdAttendenceSaveDto dto, int userId, string userIp)
        {
            var result = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpSave_StdAttendence @VID={0}, @SessionID={1}, @StudentID={2}, @SectionID={3}, @VDate={4}, @Attendence={5}, @IsLate={6}, @IsAgain={7}, @Remarks={8}, @ISentSMS={9}, @UserID={10}, @UserIP={11}",
                    dto.VID,
                    dto.SessionID,
                    dto.StudentID,
                    dto.SectionID,
                    dto.VDate,
                    dto.Attendence,
                    dto.IsLate,
                    dto.IsAgain,
                    dto.Remarks ?? (object)DBNull.Value,
                    dto.ISentSMS,
                    userId,
                    userIp)
                .AsNoTracking()
                .ToListAsync();

            return result.FirstOrDefault();
        }
    }
}