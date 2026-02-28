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
            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    @"EXEC SpSave_SMSStudent 
                @StudentID={0},
                @FatherName={1}, @FatherNameUrdu={2}, @FatherCNIC={3}, @FatherMobile={4}, @IsFatherSMS={5}, @Occupation={6},
                @MotherName={7}, @MotherNameUrdu={8}, @MotherCNIC={9}, @MotherMobile={10}, @IsMotherSMS={11},
                @GuardianName={12}, @GuardianNameUrdu={13}, @GuardianCNIC={14}, @GuardianMobile={15}, @IsGuardianSMS={16}, @GuardianRelation={17},
                @RegistrationNo={18}, @AdmissionNo={19}, @AdmissionDate={20},
                @Name={21}, @Name_Urdu={22}, @MobileNo={23}, @IsSMS={24},
                @Gender={25}, @DOB={26}, @BirthPlace={27}, @FormBNo={28}, @Religion={29}, @Caste={30}, @Address={31},
                @SessionID={32}, @SectionID={33}, @RollNo={34}, @GradeID={35}, @StatusID={36}, @IsAvailAcademy={37},
                @UserID={38}, @UserIP={39}",
                    dto.VID,
                    dto.FatherName, dto.FatherNameUrdu, dto.FatherCNIC, dto.FatherMobile, dto.IsFatherSMS ?? false, dto.Occupation,
                    dto.MotherName, dto.MotherNameUrdu, dto.MotherCNIC, dto.MotherMobile, dto.IsMotherSMS ?? false,
                    dto.GuardianName, dto.GuardianNameUrdu, dto.GuardianCNIC, dto.GuardianMobile, dto.IsGuardianSMS ?? false, dto.GuardianRelation,
                    dto.RegistrationNo, dto.AdmissionNo, dto.AdmissionDate,
                    dto.Name, dto.Name_Urdu, dto.MobileNo, dto.IsSMS ?? false,
                    dto.Gender, dto.DOB, dto.BirthPlace, dto.FormBNo, dto.Religion, dto.Caste, dto.Address,
                    dto.SessionID, dto.SectionID, dto.ClassRollNo, dto.GradeID ?? 0, dto.StatusID ?? 0, dto.IsAvailAcademy ?? false,
                    userId,
                    userIp)
                .AsNoTracking()
                .ToListAsync();

            var result = resultList.FirstOrDefault();
            return result;
        }
    }
}
