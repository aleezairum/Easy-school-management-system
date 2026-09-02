using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.Repositories.Interfaces.Academic;

namespace School.API.Repositories.Implementations.Academic
{
    public class StaffSubjectRepository : IStaffSubjectRepository
    {
        private readonly SchoolDbContext _context;

        public StaffSubjectRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<StaffSubjectRowDto>> GetAsync(int teacherId, int classId, int sectionId, int examId)
        {
            return await _context
                .Set<StaffSubjectRowDto>()
                .FromSqlRaw("EXEC SpGet_SMSStaffSubject @VID={0}, @TeacherID={1}, @ClassID={2}, @SectionID={3}, @ExamID={4}",
                    0, teacherId, classId, sectionId, examId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ResponseDto> SaveAsync(StaffSubjectDto dto, int userId, string userIp)
        {
            var list = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpSave_SMSStaffSubject @VID={0}, @ExamID={1}, @TeacherID={2}, @ClassID={3}, @SectionID={4}, " +
                    "@SubjectID={5}, @LectureNo={6}, @IsActive={7}, @UserID={8}, @UserIP={9}",
                    dto.VID,
                    dto.ExamId,
                    dto.StaffId,
                    dto.ClassId,
                    dto.SectionId,
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
                .FromSqlRaw("EXEC SpDelete_SMSStaffSubject @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return list.FirstOrDefault();
        }

        public async Task<ResponseDto?> CopyAsync(CopyStaffSubjectDto dto, int userId, string userIp)
        {
            var list = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpCopy_SMSStaffSubject @FromExamID={0}, @ToExamID={1}, @UserID={2}, @UserIP={3}",
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
