using Microsoft.Data.SqlClient;
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
            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpToggleStatus_SMSStudent @StudentID={0}, @UserID={1}, @UserIP={2}",
                    vid,
                    userId,
                    userIp)
                .AsNoTracking()
                .ToListAsync();

            var result = resultList.FirstOrDefault();
            return result;
        }

        public async Task<ResponseDto> SaveAsync(StudentSaveDto dto, int userId, string userIp)
        {
            var parameters = new[]
            {
        new SqlParameter("@StudentID",      dto.VID),
        new SqlParameter("@FatherName",     (object?)dto.FatherName     ?? DBNull.Value),
        new SqlParameter("@FatherNameUrdu", (object?)dto.FatherNameUrdu ?? DBNull.Value),
        new SqlParameter("@FatherCNIC",     (object?)dto.FatherCNIC     ?? DBNull.Value),
        new SqlParameter("@FatherMobile",   (object?)dto.FatherMobile   ?? DBNull.Value),
        new SqlParameter("@IsFatherSMS",    dto.IsFatherSMS             ?? false),
        new SqlParameter("@Occupation",     (object?)dto.Occupation     ?? DBNull.Value),
        new SqlParameter("@MotherName",     (object?)dto.MotherName     ?? DBNull.Value),
        new SqlParameter("@MotherNameUrdu", (object?)dto.MotherNameUrdu ?? DBNull.Value),
        new SqlParameter("@MotherCNIC",     (object?)dto.MotherCNIC     ?? DBNull.Value),
        new SqlParameter("@MotherMobile",   (object?)dto.MotherMobile   ?? DBNull.Value),
        new SqlParameter("@IsMotherSMS",    dto.IsMotherSMS             ?? false),
        new SqlParameter("@GuardianType",   dto.GuardianType),
        new SqlParameter("@GuardianName",   (object?)dto.GuardianName   ?? DBNull.Value),
        new SqlParameter("@GuardianNameUrdu",(object?)dto.GuardianNameUrdu ?? DBNull.Value),
        new SqlParameter("@GuardianCNIC",   (object?)dto.GuardianCNIC   ?? DBNull.Value),
        new SqlParameter("@GuardianMobile", (object?)dto.GuardianMobile ?? DBNull.Value),
        new SqlParameter("@IsGuardianSMS",  dto.IsGuardianSMS           ?? false),
        new SqlParameter("@GuardianRelation",(object?)dto.GuardianRelation ?? DBNull.Value),
        new SqlParameter("@RegistrationNo", (object?)dto.RegistrationNo ?? DBNull.Value),
        new SqlParameter("@AdmissionNo",    (object?)dto.AdmissionNo    ?? DBNull.Value),
        new SqlParameter("@AdmissionDate",  (object?)dto.AdmissionDate  ?? DBNull.Value),
        new SqlParameter("@Name",           (object?)dto.Name           ?? DBNull.Value),
        new SqlParameter("@Name_Urdu",      (object?)dto.Name_Urdu      ?? DBNull.Value),
        new SqlParameter("@MobileNo",       (object?)dto.MobileNo       ?? DBNull.Value),
        new SqlParameter("@IsSMS",          dto.IsSMS                   ?? false),
        new SqlParameter("@Gender",         (object?)dto.Gender         ?? DBNull.Value),
        new SqlParameter("@DOB",            (object?)dto.DOB            ?? DBNull.Value),
        new SqlParameter("@BirthPlace",     (object?)dto.BirthPlace     ?? DBNull.Value),
        new SqlParameter("@FormBNo",        (object?)dto.FormBNo        ?? DBNull.Value),
        new SqlParameter("@Religion",       (object?)dto.Religion       ?? DBNull.Value),
        new SqlParameter("@Caste",          (object?)dto.Caste          ?? DBNull.Value),
        new SqlParameter("@Address",        (object?)dto.Address        ?? DBNull.Value),
        new SqlParameter("@SessionID",      dto.SessionID),
        new SqlParameter("@SectionID",      dto.SectionID),
        new SqlParameter("@RollNo",         (object?)dto.ClassRollNo    ?? DBNull.Value),
        new SqlParameter("@GradeID",        (object?)dto.GradeID        ?? DBNull.Value),
        new SqlParameter("@StatusID",       (object?)dto.StatusID       ?? DBNull.Value),
        new SqlParameter("@IsAvailAcademy", dto.IsAvailAcademy          ?? false),
        new SqlParameter("@UserID",         userId),
        new SqlParameter("@UserIP",         (object?)userIp             ?? DBNull.Value)
    };

            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    @"EXEC SpSave_SMSStudent 
                @StudentID, @FatherName, @FatherNameUrdu, @FatherCNIC, @FatherMobile, @IsFatherSMS, @Occupation,
                @MotherName, @MotherNameUrdu, @MotherCNIC, @MotherMobile, @IsMotherSMS,
                @GuardianType, @GuardianName, @GuardianNameUrdu, @GuardianCNIC, @GuardianMobile, @IsGuardianSMS, @GuardianRelation,
                @RegistrationNo, @AdmissionNo, @AdmissionDate,
                @Name, @Name_Urdu, @MobileNo, @IsSMS,
                @Gender, @DOB, @BirthPlace, @FormBNo, @Religion, @Caste, @Address,
                @SessionID, @SectionID, @RollNo, @GradeID, @StatusID, @IsAvailAcademy,
                @UserID, @UserIP",
                    parameters)
                .AsNoTracking()
                .ToListAsync();

            return resultList.FirstOrDefault() ?? new ResponseDto
            {
                VID = dto.VID,
                ReturnCode = -1,
                ReturnMessage = "No response from stored procedure"
            };
        }
        public async Task<ResponseDto> StatusChangeAsync(string StudentIDs, int StatusID, int userId, string userIp)
        {
            var parameters = new[]
            {
                new SqlParameter("@VIDs",      StudentIDs),
                new SqlParameter("@StatusID",     StatusID),
                new SqlParameter("@UserID",         userId),
                new SqlParameter("@UserIP",         (object?)userIp             ?? DBNull.Value)
    };

            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    @"EXEC SpStatusChange_SMSStudentAcademic @VIDs, @StatusID,@UserID, @UserIP",
                    parameters)
                .AsNoTracking()
                .ToListAsync();

            return resultList.FirstOrDefault() ?? new ResponseDto
            {
                VID = 0,
                ReturnCode = -1,
                ReturnMessage = "No response from stored procedure"
            };
        }
        public async Task<ResponseDto> SectionChangeAsync(string StudentIDs, int SectionID, int userId, string userIp)
        {
            var parameters = new[]
            {
                new SqlParameter("@VIDs",      StudentIDs),
                new SqlParameter("@SectionID",     SectionID),
                new SqlParameter("@UserID",         userId),
                new SqlParameter("@UserIP",         (object?)userIp             ?? DBNull.Value)
            };

            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    @"EXEC SpSectionChange_SMSStudentAcademic @VIDs, @SectionID,@UserID, @UserIP",
                    parameters)
                .AsNoTracking()
                .ToListAsync();

            return resultList.FirstOrDefault() ?? new ResponseDto
            {
                VID = 0,
                ReturnCode = -1,
                ReturnMessage = "No response from stored procedure"
            };
        }
    }
}
