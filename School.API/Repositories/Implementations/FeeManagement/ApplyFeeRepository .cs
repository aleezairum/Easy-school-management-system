// Repositories/Implementations/Accounts/ApplyFeeRepository.cs
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.DTOs.Common;
using School.API.DTOs.FeeManagement;
using School.API.Repositories.Interfaces.Accounts;

namespace School.API.Repositories.Implementations.Accounts
{
    public class ApplyFeeRepository : IApplyFeeRepository
    {
        private readonly SchoolDbContext _context;

        public ApplyFeeRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<StudentFeePreviewResultDto>> GetPreviewAsync(ApplyFeePreviewRequestDto dto)
        {
            return await _context
                .Set<StudentFeePreviewResultDto>()
                .FromSqlRaw(
                    "EXEC SpGet_SMSStudentFeeApply @ClassID={0}, @SectionID={1}, @StudentID={2}, @GradeID={3}, @IsAvailAcademy={4}, @FeeTypeID={5}",
                    dto.ClassID,
                    dto.SectionID,
                    dto.StudentID,
                    dto.GradeID,
                    dto.IsAvailAcademy,
                    dto.FeeTypeID)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ResponseDto?> SaveStudentFeeAsync(SaveStudentFeeRequestDto dto, int userId, string userIp)
        {
            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpSave_SMSStudentFee @VID={0}, @VDate={1}, @StudentID={2}, @SessionID={3}, @SectionID={4}, @GradeID={5}, @FeeTypeID={6}, @FeeAmount={7}, @ClearedAmount={8}, @Remarks={9}, @IsClear={10}, @UserID={11}, @UserIP={12}",
                    dto.VID,
                    dto.VDate,
                    dto.StudentID,
                    dto.SessionID,
                    dto.SectionID,
                    dto.GradeID,
                    dto.FeeTypeID,
                    dto.FeeAmount,
                    dto.ClearedAmount,
                    dto.Remarks ?? (object)DBNull.Value,
                    dto.IsClear,
                    userId,
                    userIp)
                .AsNoTracking()
                .ToListAsync();

            return resultList.FirstOrDefault();
        }
        public async Task<List<StudentFeeListDto>> GetListAsync(GetStudentFeeRequestDto dto)
        {
            return await _context
                .Set<StudentFeeListDto>()
                .FromSqlRaw(
                    "EXEC SpGet_SMSStudentFee @VID={0}, @StudentID={1}, @SessionID={2}, @SectionID={3}, @DateFrom={4}, @DateTo={5}",
                    dto.VID ?? (object)DBNull.Value,
                    dto.StudentID ?? (object)DBNull.Value,
                    dto.SessionID ?? (object)DBNull.Value,
                    dto.SectionID ?? (object)DBNull.Value,
                    dto.DateFrom,
                    dto.DateTo)
                .AsNoTracking()
                .ToListAsync();
        }
        public async Task<ResponseDto> DeleteAsync(int vid, int userId, string userIp)
        {
            var parameters = new[]
            {
        new SqlParameter("@VID",    vid),
        new SqlParameter("@UserID", userId),
        new SqlParameter("@UserIP", userIp)
    };

            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw("EXEC SpDelete_SMSStudentFee @VID, @UserID, @UserIP", parameters)
                .AsNoTracking()
                .ToListAsync();

            return resultList.FirstOrDefault();
        }
    }
}