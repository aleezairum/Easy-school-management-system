using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.DTOs.Academic;
using School.API.Repositories.Interfaces.Academic;

namespace School.API.Repositories.Implementations.Academic
{
    /// <summary>
    /// Result finalising and the teachers' result exclude list
    /// (Scripts/ExamResultControls.sql).
    /// </summary>
    public class ExamControlRepository : IExamControlRepository
    {
        private readonly SchoolDbContext _context;

        public ExamControlRepository(SchoolDbContext context)
        {
            _context = context;
        }

        private SqlConnection Connect() => new(_context.Database.GetConnectionString());

        // ── Finalise ───────────────────────────────────────────────────

        public async Task<List<ExamFinalizeDto>> GetFinalizedAsync()
        {
            var list = new List<ExamFinalizeDto>();
            await using var cn = Connect();
            await cn.OpenAsync();
            await using var cmd = new SqlCommand("SELECT ExamID, FinalizedDate, FinalizedBy FROM SMSExamFinalize;", cn);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                list.Add(new ExamFinalizeDto
                {
                    ExamId = r.GetInt32(0),
                    FinalizedDate = r.GetDateTime(1),
                    FinalizedBy = r.IsDBNull(2) ? null : r.GetInt32(2)
                });
            return list;
        }

        public async Task<bool> IsFinalizedAsync(int examId)
        {
            await using var cn = Connect();
            await cn.OpenAsync();
            await using var cmd = new SqlCommand("SELECT COUNT(*) FROM SMSExamFinalize WHERE ExamID = @e;", cn);
            cmd.Parameters.AddWithValue("@e", examId);
            return (int)(await cmd.ExecuteScalarAsync() ?? 0) > 0;
        }

        public async Task FinalizeAsync(int examId, int? userId)
        {
            const string sql = @"
                IF NOT EXISTS (SELECT 1 FROM SMSExamFinalize WHERE ExamID = @e)
                    INSERT SMSExamFinalize (ExamID, FinalizedBy, FinalizedDate) VALUES (@e, @u, GETDATE());";
            await using var cn = Connect();
            await cn.OpenAsync();
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@e", examId);
            cmd.Parameters.AddWithValue("@u", (object?)userId ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UnfinalizeAsync(int examId)
        {
            await using var cn = Connect();
            await cn.OpenAsync();
            await using var cmd = new SqlCommand("DELETE FROM SMSExamFinalize WHERE ExamID = @e;", cn);
            cmd.Parameters.AddWithValue("@e", examId);
            await cmd.ExecuteNonQueryAsync();
        }

        // ── Exclude list ───────────────────────────────────────────────

        public async Task<List<ExamExcludeRowDto>> GetExcludeListAsync(int examId, int? classId, int? sectionId)
        {
            // With a class: its currently enrolled students (latest academic
            // record, StatusID 1), each marked excluded or not. Without one:
            // just the students already on this exam's list, wherever they are.
            const string sql = @"
                SELECT S.VID, ISNULL(S.RollNo, ''), ISNULL(S.Name, ''), P.FatherName, C.VName, SEC.VName,
                       CASE WHEN X.VID IS NULL THEN 0 ELSE 1 END, X.Reason
                FROM SMSStudent S
                OUTER APPLY (SELECT TOP 1 A.SectionID, A.StatusID FROM SMSStudentAcademic A
                             WHERE A.StudentID = S.VID AND ISNULL(A.IsActive, 1) = 1
                             ORDER BY A.SessionID DESC, A.VID DESC) A
                LEFT JOIN SMSSection SEC ON SEC.VID = A.SectionID
                LEFT JOIN SMSClass   C   ON C.VID   = SEC.ClassID
                LEFT JOIN SMSParent  P   ON P.VID   = S.ParentID
                LEFT JOIN SMSExamExclude X ON X.ExamID = @exam AND X.StudentID = S.VID
                WHERE (@class IS NULL AND X.VID IS NOT NULL)
                   OR (@class IS NOT NULL AND A.StatusID = 1 AND C.VID = @class
                       AND (@section IS NULL OR SEC.VID = @section))
                ORDER BY C.VID, SEC.VName, S.Name;";
            var list = new List<ExamExcludeRowDto>();
            await using var cn = Connect();
            await cn.OpenAsync();
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@exam", examId);
            cmd.Parameters.AddWithValue("@class", (object?)classId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@section", (object?)sectionId ?? DBNull.Value);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                list.Add(new ExamExcludeRowDto
                {
                    StudentId = r.GetInt32(0),
                    RollNo = r.GetString(1),
                    Name = r.GetString(2),
                    FatherName = r.IsDBNull(3) ? null : r.GetString(3),
                    ClassName = r.IsDBNull(4) ? null : r.GetString(4),
                    SectionName = r.IsDBNull(5) ? null : r.GetString(5),
                    Excluded = r.GetInt32(6) == 1,
                    Reason = r.IsDBNull(7) ? null : r.GetString(7)
                });
            return list;
        }

        public async Task<int> SaveExcludeListAsync(SaveExamExcludeDto dto, int? userId)
        {
            const string add = @"
                IF EXISTS (SELECT 1 FROM SMSExamExclude WHERE ExamID = @e AND StudentID = @s)
                    UPDATE SMSExamExclude SET Reason = @r WHERE ExamID = @e AND StudentID = @s;
                ELSE
                    INSERT SMSExamExclude (ExamID, StudentID, Reason, InsertedBy, InsertedDate)
                    VALUES (@e, @s, @r, @u, GETDATE());";
            const string remove = "DELETE FROM SMSExamExclude WHERE ExamID = @e AND StudentID = @s;";

            await using var cn = Connect();
            await cn.OpenAsync();
            await using var tx = (SqlTransaction)await cn.BeginTransactionAsync();
            var excluded = 0;
            foreach (var item in dto.Items)
            {
                await using var cmd = new SqlCommand(item.Excluded ? add : remove, cn, tx);
                cmd.Parameters.AddWithValue("@e", dto.ExamId);
                cmd.Parameters.AddWithValue("@s", item.StudentId);
                if (item.Excluded)
                {
                    var reason = string.IsNullOrWhiteSpace(item.Reason) ? null : item.Reason.Trim();
                    if (reason?.Length > 200) reason = reason[..200];
                    cmd.Parameters.AddWithValue("@r", (object?)reason ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@u", (object?)userId ?? DBNull.Value);
                    excluded++;
                }
                await cmd.ExecuteNonQueryAsync();
            }
            await tx.CommitAsync();
            return excluded;
        }
    }
}
