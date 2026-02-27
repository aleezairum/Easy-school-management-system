using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.Repositories.Interfaces.Academic;

namespace School.API.Repositories.Implementations.Academic
{
    public class StudentRepository : IStudentRepository
    {
        private readonly SchoolDbContext _context;

        public StudentRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<StudentSaveDto>> GetAllAsync()
        {
            var resultList = await _context
                .Set<StudentSaveDto>()
                .FromSqlRaw("EXEC SpGet_SMSStudent @VID={0}", 0)
                .AsNoTracking()
                .ToListAsync();

            return resultList;
        }

        public async Task<StudentSaveDto?> GetByIdAsync(int vid)
        {
            var resultList = await _context
                .Set<StudentSaveDto>()
                .FromSqlRaw("EXEC SpGet_SMSStudent @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return resultList.FirstOrDefault();
        }

        public async Task<ResponseDto?> DeleteByIdAsync(int vid)
        {
            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw("EXEC SpDelete_SMSStudent @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return resultList.FirstOrDefault();
        }

        public async Task<ResponseDto> ToggleStatusAsync(int vid, int userId, string userIp)
        {
            var student = await _context.Students.FindAsync(vid);
            if (student == null)
                return new ResponseDto { VID = vid, ReturnCode = -1, ReturnMessage = "Student not found" };

            student.StudentActive = !(student.StudentActive ?? true);
            student.UpdatedBy = userId;
            student.UpdatedDate = DateTime.UtcNow;
            student.UpdatedIp = userIp;

            await _context.SaveChangesAsync();

            var newStatus = student.StudentActive == true ? "Active" : "Inactive";
            return new ResponseDto { VID = vid, ReturnCode = 0, ReturnMessage = newStatus };
        }

        public async Task<ResponseDto> SaveAsync(StudentSaveDto dto, int userId, string userIp)
        {
            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpSave_SMSStudent @VID={0}, @CampusID={1}, @SessionID={2}, @ClassID={3}, @GradeID={4}, @FeeTypeID={5}, @Amount={6}, @IsActive={7}, @UserID={8}, @UserIP={9}",
                    dto.VID,
                    //dto.CampusID,
                    //dto.AcademicSessionID,
                    //dto.ClassID,
                    //dto.GradeID ?? 0,
                    //dto.FeeTypeID,
                    //dto.Amount,
                    //dto.IsActive,
                    userId,
                    userIp)
                .AsNoTracking()
                .ToListAsync();

            var result = resultList.FirstOrDefault();
            return result;
        }
    }
}
