using School.API.DTOs;
using School.API.Repositories.Interfaces.HR;
using School.API.Services.Interfaces.HR;

namespace School.API.Services.Implementations.HR
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _repository;

        public EmployeeService(IEmployeeRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<EmployeeListDto>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<EmployeeDto?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto)
        {
            var entity = await _repository.CreateAsync(dto);
            return new EmployeeDto
            {
                Id = entity.Id,
                EmployeeCode = entity.EmployeeCode,
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                FirstNameUrdu = entity.FirstNameUrdu,
                LastNameUrdu = entity.LastNameUrdu,
                FullName = entity.FullName,
                CNIC = entity.CNIC,
                Email = entity.Email,
                Phone = entity.Phone,
                Mobile = entity.Mobile,
                Gender = entity.Gender,
                DateOfBirth = entity.DateOfBirth,
                JoiningDate = entity.JoiningDate,
                LeavingDate = entity.LeavingDate,
                Address = entity.Address,
                City = entity.City,
                Photo = entity.Photo,
                Qualification = entity.Qualification,
                Specialization = entity.Specialization,
                Experience = entity.Experience,
                Department = entity.Department,
                DesignationId = entity.DesignationId,
                HRGradeId = entity.HRGradeId,
                AcademicGradeId = entity.AcademicGradeId,
                BasicSalary = entity.BasicSalary,
                Allowances = entity.Allowances,
                Deductions = entity.Deductions,
                NetSalary = entity.NetSalary,
                BankName = entity.BankName,
                BankAccountNo = entity.BankAccountNo,
                EmergencyContactName = entity.EmergencyContactName,
                EmergencyContactPhone = entity.EmergencyContactPhone,
                EmergencyContactRelation = entity.EmergencyContactRelation,
                EmployeeType = entity.EmployeeType,
                Status = entity.Status,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        public async Task<EmployeeDto?> UpdateAsync(int id, UpdateEmployeeDto dto)
        {
            var entity = await _repository.UpdateAsync(id, dto);
            if (entity == null) return null;

            return new EmployeeDto
            {
                Id = entity.Id,
                EmployeeCode = entity.EmployeeCode,
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                FirstNameUrdu = entity.FirstNameUrdu,
                LastNameUrdu = entity.LastNameUrdu,
                FullName = entity.FullName,
                CNIC = entity.CNIC,
                Email = entity.Email,
                Phone = entity.Phone,
                Mobile = entity.Mobile,
                Gender = entity.Gender,
                DateOfBirth = entity.DateOfBirth,
                JoiningDate = entity.JoiningDate,
                LeavingDate = entity.LeavingDate,
                Address = entity.Address,
                City = entity.City,
                Photo = entity.Photo,
                Qualification = entity.Qualification,
                Specialization = entity.Specialization,
                Experience = entity.Experience,
                Department = entity.Department,
                DesignationId = entity.DesignationId,
                HRGradeId = entity.HRGradeId,
                AcademicGradeId = entity.AcademicGradeId,
                BasicSalary = entity.BasicSalary,
                Allowances = entity.Allowances,
                Deductions = entity.Deductions,
                NetSalary = entity.NetSalary,
                BankName = entity.BankName,
                BankAccountNo = entity.BankAccountNo,
                EmergencyContactName = entity.EmergencyContactName,
                EmergencyContactPhone = entity.EmergencyContactPhone,
                EmergencyContactRelation = entity.EmergencyContactRelation,
                EmployeeType = entity.EmployeeType,
                Status = entity.Status,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        public async Task<IEnumerable<EmployeeDropdownDto>> GetDropdownAsync()
        {
            return await _repository.GetDropdownAsync();
        }
    }
}
