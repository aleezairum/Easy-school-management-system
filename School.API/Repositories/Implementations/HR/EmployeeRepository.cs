using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.HR;
using School.API.DTOs;
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

        public async Task<IEnumerable<EmployeeListDto>> GetAllAsync()
        {
            return await _context.Employees
                .Include(e => e.Designation)
                .Include(e => e.HRGrade)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new EmployeeListDto
                {
                    Id = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FullName,
                    CNIC = e.CNIC,
                    Mobile = e.Mobile,
                    Photo = e.Photo,
                    Department = e.Department,
                    DesignationName = e.Designation != null ? e.Designation.Name : null,
                    HRGradeName = e.HRGrade != null ? e.HRGrade.Name : null,
                    EmployeeType = e.EmployeeType,
                    Status = e.Status,
                    JoiningDate = e.JoiningDate,
                    IsActive = e.IsActive
                })
                .ToListAsync();
        }

        public async Task<EmployeeDto?> GetByIdAsync(int id)
        {
            return await _context.Employees
                .Include(e => e.Designation)
                .Include(e => e.HRGrade)
                .Include(e => e.AcademicGrade)
                .Where(e => e.Id == id)
                .Select(e => new EmployeeDto
                {
                    Id = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    FirstNameUrdu = e.FirstNameUrdu,
                    LastNameUrdu = e.LastNameUrdu,
                    FullName = e.FullName,
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
                    AcademicGradeName = e.AcademicGrade != null ? e.AcademicGrade.Name : null,
                    BasicSalary = e.BasicSalary,
                    Allowances = e.Allowances,
                    Deductions = e.Deductions,
                    NetSalary = e.NetSalary,
                    BankName = e.BankName,
                    BankAccountNo = e.BankAccountNo,
                    EmergencyContactName = e.EmergencyContactName,
                    EmergencyContactPhone = e.EmergencyContactPhone,
                    EmergencyContactRelation = e.EmergencyContactRelation,
                    EmployeeType = e.EmployeeType,
                    Status = e.Status,
                    IsActive = e.IsActive,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.UpdatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<Employee> CreateAsync(CreateEmployeeDto dto)
        {
            var employee = new Employee
            {
                EmployeeCode = dto.EmployeeCode,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                FirstNameUrdu = dto.FirstNameUrdu,
                LastNameUrdu = dto.LastNameUrdu,
                CNIC = dto.CNIC,
                Email = dto.Email,
                Phone = dto.Phone,
                Mobile = dto.Mobile,
                Gender = dto.Gender,
                DateOfBirth = dto.DateOfBirth,
                JoiningDate = dto.JoiningDate,
                Address = dto.Address,
                City = dto.City,
                Photo = dto.Photo,
                Qualification = dto.Qualification,
                Specialization = dto.Specialization,
                Experience = dto.Experience,
                Department = dto.Department,
                DesignationId = dto.DesignationId,
                HRGradeId = dto.HRGradeId,
                AcademicGradeId = dto.AcademicGradeId,
                BasicSalary = dto.BasicSalary,
                Allowances = dto.Allowances,
                Deductions = dto.Deductions,
                NetSalary = dto.NetSalary,
                BankName = dto.BankName,
                BankAccountNo = dto.BankAccountNo,
                EmergencyContactName = dto.EmergencyContactName,
                EmergencyContactPhone = dto.EmergencyContactPhone,
                EmergencyContactRelation = dto.EmergencyContactRelation,
                EmployeeType = dto.EmployeeType,
                Status = dto.Status,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
            return employee;
        }

        public async Task<Employee?> UpdateAsync(int id, UpdateEmployeeDto dto)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return null;

            employee.EmployeeCode = dto.EmployeeCode;
            employee.FirstName = dto.FirstName;
            employee.LastName = dto.LastName;
            employee.FirstNameUrdu = dto.FirstNameUrdu;
            employee.LastNameUrdu = dto.LastNameUrdu;
            employee.CNIC = dto.CNIC;
            employee.Email = dto.Email;
            employee.Phone = dto.Phone;
            employee.Mobile = dto.Mobile;
            employee.Gender = dto.Gender;
            employee.DateOfBirth = dto.DateOfBirth;
            employee.JoiningDate = dto.JoiningDate;
            employee.LeavingDate = dto.LeavingDate;
            employee.Address = dto.Address;
            employee.City = dto.City;
            employee.Photo = dto.Photo;
            employee.Qualification = dto.Qualification;
            employee.Specialization = dto.Specialization;
            employee.Experience = dto.Experience;
            employee.Department = dto.Department;
            employee.DesignationId = dto.DesignationId;
            employee.HRGradeId = dto.HRGradeId;
            employee.AcademicGradeId = dto.AcademicGradeId;
            employee.BasicSalary = dto.BasicSalary;
            employee.Allowances = dto.Allowances;
            employee.Deductions = dto.Deductions;
            employee.NetSalary = dto.NetSalary;
            employee.BankName = dto.BankName;
            employee.BankAccountNo = dto.BankAccountNo;
            employee.EmergencyContactName = dto.EmergencyContactName;
            employee.EmergencyContactPhone = dto.EmergencyContactPhone;
            employee.EmergencyContactRelation = dto.EmergencyContactRelation;
            employee.EmployeeType = dto.EmployeeType;
            employee.Status = dto.Status;
            employee.IsActive = dto.IsActive;
            employee.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return employee;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return false;

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<EmployeeDropdownDto>> GetDropdownAsync()
        {
            return await _context.Employees
                .Where(e => e.IsActive)
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .Select(e => new EmployeeDropdownDto
                {
                    Id = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FullName
                })
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Employees.AnyAsync(e => e.Id == id);
        }

        public async Task<bool> EmployeeCodeExistsAsync(string employeeCode, int? excludeId = null)
        {
            return await _context.Employees
                .AnyAsync(e => e.EmployeeCode.ToLower() == employeeCode.ToLower() && (!excludeId.HasValue || e.Id != excludeId.Value));
        }

        public async Task<bool> CNICExistsAsync(string cnic, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(cnic)) return false;
            return await _context.Employees
                .AnyAsync(e => e.CNIC != null && e.CNIC.ToLower() == cnic.ToLower() && (!excludeId.HasValue || e.Id != excludeId.Value));
        }
    }
}
