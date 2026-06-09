using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.DTOs.Academic;
using School.API.Repositories.Interfaces.Academic;

namespace School.API.Repositories.Implementations.Academic
{
    public class CertificateRepository : ICertificateRepository
    {
        private readonly SchoolDbContext _context;

        public CertificateRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<LeavingCertificateDto?> GetLeavingAsync(
            int studentId,
            DateTime? leavingDate,
            string? examination,
            string? marksObtained,
            string? totalMarks)
        {
            var parameters = new[]
            {
                new SqlParameter("@StudentID",     studentId),
                new SqlParameter("@LeavingDate",   (object?)leavingDate   ?? DBNull.Value),
                new SqlParameter("@Examination",   (object?)examination   ?? DBNull.Value),
                new SqlParameter("@MarksObtained", (object?)marksObtained ?? DBNull.Value),
                new SqlParameter("@TotalMarks",    (object?)totalMarks    ?? DBNull.Value)
            };

            var rows = await _context.Database
                .SqlQueryRaw<LeavingCertificateDto>(
                    "EXEC SpGet_SMSStudentLeavingCertificate @StudentID, @LeavingDate, @Examination, @MarksObtained, @TotalMarks",
                    parameters)
                .ToListAsync();

            return rows.FirstOrDefault();
        }

        public async Task<ProvisionalCertificateDto?> GetProvisionalAsync(
            int studentId,
            int? sessionId,
            string? boardRollNo,
            string? examRollNo,
            string? examination,
            string? marksObtained,
            string? totalMarks,
            string? conduct,
            string? moralCharacter,
            DateTime? issueDate)
        {
            var parameters = new[]
            {
                new SqlParameter("@StudentID",      studentId),
                new SqlParameter("@SessionID",      (object?)sessionId      ?? DBNull.Value),
                new SqlParameter("@BoardRollNo",    (object?)boardRollNo    ?? DBNull.Value),
                new SqlParameter("@ExamRollNo",     (object?)examRollNo     ?? DBNull.Value),
                new SqlParameter("@Examination",    (object?)examination    ?? DBNull.Value),
                new SqlParameter("@MarksObtained",  (object?)marksObtained  ?? DBNull.Value),
                new SqlParameter("@TotalMarks",     (object?)totalMarks     ?? DBNull.Value),
                new SqlParameter("@Conduct",        (object?)conduct        ?? DBNull.Value),
                new SqlParameter("@MoralCharacter", (object?)moralCharacter ?? DBNull.Value),
                new SqlParameter("@IssueDate",      (object?)issueDate      ?? DBNull.Value)
            };

            var rows = await _context.Database
                .SqlQueryRaw<ProvisionalCertificateDto>(
                    "EXEC SpGet_SMSStudentProvisionalCertificate @StudentID, @SessionID, @BoardRollNo, @ExamRollNo, @Examination, @MarksObtained, @TotalMarks, @Conduct, @MoralCharacter, @IssueDate",
                    parameters)
                .ToListAsync();

            return rows.FirstOrDefault();
        }
    }
}
