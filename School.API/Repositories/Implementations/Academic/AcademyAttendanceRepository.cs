using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.DTOs.Academic;
using School.API.Repositories.Interfaces.Academic;

namespace School.API.Repositories.Implementations.Academic
{
    /// <summary>
    /// Academy (after-school) attendance, kept in its own table
    /// (Scripts/AcademyAttendance.sql) so it never mixes with the school day.
    /// </summary>
    public class AcademyAttendanceRepository : IAcademyAttendanceRepository
    {
        private readonly SchoolDbContext _context;

        public AcademyAttendanceRepository(SchoolDbContext context)
        {
            _context = context;
        }

        private SqlConnection Connect() => new(_context.Database.GetConnectionString());

        // A student's current enrolment: the newest academic row, as everywhere else.
        private const string Enrolled = @"
            FROM SMSStudent S
            CROSS APPLY (SELECT TOP 1 X.SectionID, X.SessionID, X.StatusID, X.IsAvailAcademy
                         FROM SMSStudentAcademic X
                         WHERE X.StudentID = S.VID AND ISNULL(X.IsActive, 1) = 1
                         ORDER BY X.SessionID DESC, X.VID DESC) A
            JOIN SMSSection SE ON SE.VID = A.SectionID
            WHERE A.StatusID = 1 AND ISNULL(A.IsAvailAcademy, 0) = 1
              AND SE.ClassID = @c AND (@s IS NULL OR A.SectionID = @s)";

        public async Task<List<AcademyAttendanceRowDto>> GetAsync(int classId, int? sectionId, DateTime date)
        {
            var list = new List<AcademyAttendanceRowDto>();
            await using var cn = Connect();
            await cn.OpenAsync();
            await using var cmd = new SqlCommand(@"
                SELECT S.VID, S.RollNo, S.Name, P.FatherName, A.SectionID, SE.VName, M.Attendence, M.Remarks
                FROM SMSStudent S
                LEFT JOIN SMSParent P ON P.VID = S.ParentID
                CROSS APPLY (SELECT TOP 1 X.SectionID, X.StatusID, X.IsAvailAcademy
                             FROM SMSStudentAcademic X
                             WHERE X.StudentID = S.VID AND ISNULL(X.IsActive, 1) = 1
                             ORDER BY X.SessionID DESC, X.VID DESC) A
                JOIN SMSSection SE ON SE.VID = A.SectionID
                OUTER APPLY (SELECT Q.Attendence, Q.Remarks FROM SMSAcademyAttendance Q
                             WHERE Q.StudentID = S.VID AND Q.VDate = @d) M
                WHERE A.StatusID = 1 AND ISNULL(A.IsAvailAcademy, 0) = 1
                  AND SE.ClassID = @c AND (@s IS NULL OR A.SectionID = @s)
                  -- not yet admitted on that day
                  AND (S.AdmissionDate IS NULL OR CAST(S.AdmissionDate AS DATE) <= @d)
                ORDER BY SE.VName, TRY_CONVERT(INT, S.RollNo), S.RollNo, S.Name;", cn);
            cmd.Parameters.AddWithValue("@c", classId);
            cmd.Parameters.AddWithValue("@s", (object?)sectionId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@d", date.Date);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                list.Add(new AcademyAttendanceRowDto
                {
                    StudentId = r.GetInt32(0),
                    RollNo = r.IsDBNull(1) ? null : Convert.ToString(r.GetValue(1)),
                    Name = r.IsDBNull(2) ? "" : r.GetString(2),
                    FatherName = r.IsDBNull(3) ? null : r.GetString(3),
                    SectionId = r.GetInt32(4),
                    SectionName = r.IsDBNull(5) ? null : r.GetString(5),
                    Attendence = r.IsDBNull(6) ? null : Convert.ToInt32(r.GetValue(6)),
                    Remarks = r.IsDBNull(7) ? null : r.GetString(7)
                });
            return list;
        }

        public async Task<int> SaveAsync(SaveAcademyAttendanceDto dto, int? userId)
        {
            // The student's section and session are taken from the enrolment, not the form.
            const string save = @"
                DECLARE @sec INT, @ses INT;
                SELECT TOP 1 @sec = X.SectionID, @ses = X.SessionID FROM SMSStudentAcademic X
                WHERE X.StudentID = @st AND ISNULL(X.IsActive, 1) = 1 ORDER BY X.SessionID DESC, X.VID DESC;
                IF EXISTS (SELECT 1 FROM SMSAcademyAttendance WHERE StudentID = @st AND VDate = @d)
                    UPDATE SMSAcademyAttendance
                       SET Attendence = @a, Remarks = @r, SectionID = @sec, SessionID = @ses, UpdatedBy = @u, UpdatedDate = GETDATE()
                     WHERE StudentID = @st AND VDate = @d;
                ELSE
                    INSERT SMSAcademyAttendance (StudentID, SessionID, SectionID, VDate, Attendence, Remarks, InsertedBy)
                    VALUES (@st, @ses, @sec, @d, @a, @r, @u);";

            await using var cn = Connect();
            await cn.OpenAsync();
            await using var tx = (SqlTransaction)await cn.BeginTransactionAsync();
            var n = 0;
            foreach (var item in dto.Items)
            {
                if (item.Attendence is < 0 or > 2) continue;
                var remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
                if (remarks?.Length > 200) remarks = remarks[..200];
                await using var cmd = new SqlCommand(save, cn, tx);
                cmd.Parameters.AddWithValue("@st", item.StudentId);
                cmd.Parameters.AddWithValue("@d", dto.Date.Date);
                cmd.Parameters.AddWithValue("@a", (byte)item.Attendence);
                cmd.Parameters.AddWithValue("@r", (object?)remarks ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@u", (object?)userId ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
                n++;
            }
            await tx.CommitAsync();
            return n;
        }

        public async Task<int> DeleteAsync(int classId, int? sectionId, DateTime date)
        {
            await using var cn = Connect();
            await cn.OpenAsync();
            await using var cmd = new SqlCommand($@"
                DELETE Q FROM SMSAcademyAttendance Q
                WHERE Q.VDate = @d AND Q.StudentID IN (SELECT S.VID {Enrolled});", cn);
            cmd.Parameters.AddWithValue("@c", classId);
            cmd.Parameters.AddWithValue("@s", (object?)sectionId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@d", date.Date);
            return await cmd.ExecuteNonQueryAsync();
        }
    }
}
