using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.Repositories.Interfaces.Academic;

namespace School.API.Repositories.Implementations.Academic
{
    /// <summary>
    /// Academy (after-school) subject allocation, per exam. Kept in its own table
    /// (SMSAcademyStaffSubject) so the school's Subject Allocation and its reports
    /// are untouched. SectionId 0 = all sections of the class (stored as NULL).
    /// </summary>
    public class AcademyStaffSubjectRepository : IAcademyStaffSubjectRepository
    {
        private readonly SchoolDbContext _context;

        public AcademyStaffSubjectRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<StaffSubjectRowDto>> GetAsync(int teacherId, int classId, int sectionId, int examId)
        {
            return await _context
                .Set<StaffSubjectRowDto>()
                .FromSqlRaw("EXEC SpGet_SMSAcademyStaffSubject @VID={0}, @TeacherID={1}, @ClassID={2}, @SectionID={3}, @ExamID={4}",
                    0, teacherId, classId, sectionId, examId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ResponseDto?> SaveAsync(StaffSubjectDto dto, int userId, string userIp)
        {
            var list = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpSave_SMSAcademyStaffSubject @VID={0}, @ExamID={1}, @TeacherID={2}, @ClassID={3}, @SectionID={4}, " +
                    "@SubjectID={5}, @LectureNo={6}, @IsActive={7}, @UserID={8}, @UserIP={9}",
                    dto.VID,
                    dto.ExamId,
                    dto.StaffId,
                    dto.ClassId,
                    dto.SectionId > 0 ? (object)dto.SectionId : DBNull.Value,
                    dto.SubjectId,
                    (object?)dto.LectureNo ?? DBNull.Value,
                    dto.IsActive ?? true,
                    userId,
                    userIp)
                .AsNoTracking()
                .ToListAsync();

            return list.FirstOrDefault();
        }

        public async Task<ResponseDto?> DeleteByIdAsync(int vid)
        {
            var list = await _context
                .Set<ResponseDto>()
                .FromSqlRaw("EXEC SpDelete_SMSAcademyStaffSubject @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return list.FirstOrDefault();
        }

        public async Task<ResponseDto?> CopyAsync(CopyStaffSubjectDto dto, int userId, string userIp)
        {
            var list = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpCopy_SMSAcademyStaffSubject @FromExamID={0}, @ToExamID={1}, @UserID={2}, @UserIP={3}",
                    dto.FromExamId,
                    dto.ToExamId,
                    userId,
                    userIp)
                .AsNoTracking()
                .ToListAsync();

            return list.FirstOrDefault();
        }
    }
}
