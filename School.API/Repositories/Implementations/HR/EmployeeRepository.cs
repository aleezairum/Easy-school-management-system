using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.HR;
using School.API.DTOs;
using School.API.DTOs.HR;
using School.API.Repositories.Interfaces.HR;

namespace School.API.Repositories.Implementations.HR
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly SchoolDbContext _context;

        public EmployeeRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<EmployeeListDto>> GetAllAsync()
        {
            return await _context.Employees
                .AsNoTracking()
                .Include(e => e.Designation)
                .Include(e => e.HRGrade)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new EmployeeListDto
                {
                    Id = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FirstName + (e.LastName != null ? " " + e.LastName : ""),
                    FatherName = e.FatherName,
                    CNIC = e.CNIC,
                    Mobile = e.Mobile,
                    Photo = e.Photo,
                    Department = e.Department,
                    DesignationId = e.DesignationId,
                    DesignationName = e.Designation != null ? e.Designation.Name : null,
                    HRGradeName = e.HRGrade != null ? e.HRGrade.Name : null,
                    EmployeeType = e.EmployeeType,
                    Status = e.Status,
                    IsTeachingStaff = e.IsTeachingStaff,
                    JoiningDate = e.JoiningDate,
                    IsActive = e.IsActive
                })
                .ToListAsync();
        }

        public async Task<EmployeeDto?> GetByIdAsync(int id)
        {
            return await _context.Employees
                .AsNoTracking()
                .Include(e => e.Designation)
                .Include(e => e.HRGrade)
                .Include(e => e.Shift)
                .Where(e => e.Id == id)
                .Select(e => new EmployeeDto
                {
                    Id = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    FirstNameUrdu = e.FirstNameUrdu,
                    LastNameUrdu = e.LastNameUrdu,
                    FullName = e.FirstName + (e.LastName != null ? " " + e.LastName : ""),
                    FatherName = e.FatherName,
                    CNIC = e.CNIC,
                    Email = e.Email,
                    Phone = e.Phone,
                    Mobile = e.Mobile,
                    Gender = e.Gender,
                    DateOfBirth = e.DateOfBirth,
                    JoiningDate = e.JoiningDate,
                    LeavingDate = e.LeavingDate,
                    Address = e.Address,
                    City = e.City,
                    Photo = e.Photo,
                    Qualification = e.Qualification,
                    Specialization = e.Specialization,
                    Experience = e.Experience,
                    Department = e.Department,
                    DesignationId = e.DesignationId,
                    DesignationName = e.Designation != null ? e.Designation.Name : null,
                    HRGradeId = e.HRGradeId,
                    HRGradeName = e.HRGrade != null ? e.HRGrade.Name : null,
                    AcademicGradeId = e.AcademicGradeId,
                    ShiftId = e.ShiftId,
                    ShiftName = e.Shift != null ? e.Shift.Name : null,
                    MachineCardNo = e.MachineCardNo,
                    OffDay1 = e.OffDay1,
                    OffDay2 = e.OffDay2,
                    BankName = e.BankName,
                    BankAccountNo = e.BankAccountNo,
                    EmergencyContactName = e.EmergencyContactName,
                    EmergencyContactPhone = e.EmergencyContactPhone,
                    EmergencyContactRelation = e.EmergencyContactRelation,
                    EmployeeType = e.EmployeeType,
                    Status = e.Status,
                    IsTeachingStaff = e.IsTeachingStaff,
                    IsActive = e.IsActive,
                    CreatedAt = e.CreatedAt,
                    CreatedBy = e.CreatedBy,
                    UpdatedAt = e.UpdatedAt,
                    UpdatedBy = e.UpdatedBy
                })
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// The teaching staff, in the shape the old SMSTeacher screens expect.
        /// Pass an <paramref name="examId"/> to narrow it to the staff who
        /// actually hold a subject assignment in that exam.
        /// </summary>
        /// <remarks>
        /// VID is the employee's own Id — the same value SMSStaffSubject and the
        /// exam reports join on, because this table IS the old SMSTeacher.
        ///
        /// Subject assignments are per-exam, so an exam-report filter that
        /// offered every teacher would happily produce an empty report for
        /// someone who has nothing assigned in the exam being reported on.
        /// SMSStaffSubject has no EF entity, hence the raw id lookup.
        /// </remarks>
        public async Task<List<TeacherDto>> GetTeachingStaffAsync(int examId = 0)
        {
            var query = TeachingStaff();

            if (examId > 0)
            {
                var assigned = await _context.Database
                    .SqlQueryRaw<int>(
                        "SELECT DISTINCT TeacherID AS Value FROM dbo.SMSStaffSubject WHERE ExamID = {0} AND IsActive = 1",
                        examId)
                    .ToListAsync();

                query = query.Where(t => assigned.Contains(t.VID));
            }

            return await query
                .OrderBy(t => t.VName)
                .ToListAsync();
        }

        public Task<TeacherDto?> GetTeachingStaffByIdAsync(int id) =>
            TeachingStaff().FirstOrDefaultAsync(t => t.VID == id);

        private IQueryable<TeacherDto> TeachingStaff() =>
            _context.Employees
                .AsNoTracking()
                .Where(e => e.IsTeachingStaff)
                .Select(e => new TeacherDto
                {
                    VID = e.Id,
                    VName = e.FirstName + (e.LastName != null ? " " + e.LastName : ""),
                    FatherName = e.FatherName,
                    CNIC = e.CNIC,
                    Mobile = e.Mobile,
                    Email = e.Email,
                    Address = e.Address,
                    Qualification = e.Qualification,
                    IsActive = e.IsActive
                });

        public async Task<List<EmployeeDropdownDto>> GetDropdownAsync()
        {
            return await _context.Employees
                .AsNoTracking()
                .Where(e => e.IsActive)
                .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
                .Select(e => new EmployeeDropdownDto
                {
                    Id = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FirstName + (e.LastName != null ? " " + e.LastName : "")
                })
                .ToListAsync();
        }

        public async Task<Employee> CreateAsync(SaveEmployeeDto dto, int userId, string userIp)
        {
            var entity = new Employee();
            ApplyDto(entity, dto);
            entity.CreatedAt = DateTime.UtcNow;
            entity.CreatedBy = userId;
            entity.CreatedIp = userIp;

            _context.Employees.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<Employee?> UpdateAsync(int id, SaveEmployeeDto dto, int userId, string userIp)
        {
            var entity = await _context.Employees.FindAsync(id);
            if (entity == null) return null;

            ApplyDto(entity, dto);
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = userId;
            entity.UpdatedIp = userIp;

            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.Id == id);
            if (entity == null) return false;
            if (entity.User != null)
                throw new InvalidOperationException("Cannot delete an employee linked to a user account.");

            _context.Employees.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public Task<bool> ExistsAsync(int id) => _context.Employees.AnyAsync(e => e.Id == id);

        public Task<bool> CodeExistsAsync(string code, int? excludeId = null) =>
            _context.Employees.AnyAsync(e => e.EmployeeCode.ToLower() == code.ToLower() && (!excludeId.HasValue || e.Id != excludeId.Value));

        // Codes look like PREFIX + 3 digits (T001, EMP007). Takes the highest
        // numeric suffix in use for the prefix and adds one.
        public async Task<string> NextEmployeeCodeAsync(string prefix)
        {
            var codes = await _context.Employees
                .AsNoTracking()
                .Where(e => e.EmployeeCode.StartsWith(prefix))
                .Select(e => e.EmployeeCode)
                .ToListAsync();

            var next = codes
                .Select(c => int.TryParse(c.Substring(prefix.Length), out var n) ? n : 0)
                .DefaultIfEmpty(0)
                .Max() + 1;

            return $"{prefix}{next:D3}";
        }

        private static void ApplyDto(Employee e, SaveEmployeeDto dto)
        {
            e.EmployeeCode = dto.EmployeeCode;
            e.FirstName = dto.FirstName;
            e.LastName = dto.LastName;
            e.FirstNameUrdu = dto.FirstNameUrdu;
            e.LastNameUrdu = dto.LastNameUrdu;
            e.FatherName = dto.FatherName;
            e.CNIC = dto.CNIC;
            e.Email = dto.Email;
            e.Phone = dto.Phone;
            e.Mobile = dto.Mobile;
            e.Gender = dto.Gender;
            e.DateOfBirth = dto.DateOfBirth;
            e.JoiningDate = dto.JoiningDate;
            e.LeavingDate = dto.LeavingDate;
            e.Address = dto.Address;
            e.City = dto.City;
            e.Photo = dto.Photo;
            e.Qualification = dto.Qualification;
            e.Specialization = dto.Specialization;
            e.Experience = dto.Experience;
            e.Department = dto.Department;
            e.DesignationId = dto.DesignationId;
            e.HRGradeId = dto.HRGradeId;
            e.AcademicGradeId = dto.AcademicGradeId;
            e.ShiftId = dto.ShiftId;
            e.MachineCardNo = dto.MachineCardNo;
            e.OffDay1 = dto.OffDay1;
            e.OffDay2 = dto.OffDay2;
            e.IsTeachingStaff = dto.IsTeachingStaff;
            e.BankName = dto.BankName;
            e.BankAccountNo = dto.BankAccountNo;
            e.EmergencyContactName = dto.EmergencyContactName;
            e.EmergencyContactPhone = dto.EmergencyContactPhone;
            e.EmergencyContactRelation = dto.EmergencyContactRelation;
            e.EmployeeType = dto.EmployeeType;
            e.Status = dto.Status;
            e.IsActive = dto.IsActive;
        }
    }
}
