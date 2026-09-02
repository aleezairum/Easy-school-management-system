using School.API.DTOs;
using School.API.DTOs.HR;
using School.API.Repositories.Interfaces.HR;
using School.API.Services.Interfaces.HR;

namespace School.API.Services.Implementations.HR
{
    /// <summary>
    /// Staff, all of them, in one table.
    /// </summary>
    /// <remarks>
    /// This used to mirror every teaching employee into a second SMSTeacher row
    /// and stamp the resulting VID back onto the employee. That whole dance is
    /// gone: SMSTeacher was renamed into Employees with its keys intact, so the
    /// employee's own Id is what SMSStaffSubject, SMSClassTimetable and the
    /// SpRPT_SMSExam* reports join on, and IsTeachingStaff is the only switch.
    /// </remarks>
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _repository;

        public EmployeeService(IEmployeeRepository repository)
        {
            _repository = repository;
        }

        public Task<List<EmployeeListDto>> GetAllAsync() => _repository.GetAllAsync();
        public Task<EmployeeDto?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);
        public Task<List<EmployeeDropdownDto>> GetDropdownAsync() => _repository.GetDropdownAsync();

        public Task<List<TeacherDto>> GetTeachingStaffAsync(int examId = 0) => _repository.GetTeachingStaffAsync(examId);
        public Task<TeacherDto?> GetTeachingStaffByIdAsync(int id) => _repository.GetTeachingStaffByIdAsync(id);

        public async Task<EmployeeDto> CreateAsync(SaveEmployeeDto dto, int userId, string userIp)
        {
            if (string.IsNullOrWhiteSpace(dto.EmployeeCode))
                dto.EmployeeCode = await _repository.NextEmployeeCodeAsync(dto.IsTeachingStaff ? "T" : "EMP");

            if (await _repository.CodeExistsAsync(dto.EmployeeCode))
                throw new InvalidOperationException("Employee code already exists.");

            var entity = await _repository.CreateAsync(dto, userId, userIp);

            return (await _repository.GetByIdAsync(entity.Id))!;
        }

        public async Task<EmployeeDto?> UpdateAsync(int id, SaveEmployeeDto dto, int userId, string userIp)
        {
            if (await _repository.CodeExistsAsync(dto.EmployeeCode, id))
                throw new InvalidOperationException("Employee code already exists.");

            var entity = await _repository.UpdateAsync(id, dto, userId, userIp);
            if (entity == null) return null;

            return await _repository.GetByIdAsync(id);
        }

        public Task<bool> DeleteAsync(int id) => _repository.DeleteAsync(id);
    }
}
