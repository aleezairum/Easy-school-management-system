using School.API.Data.DBModels.HR;
using School.API.DTOs;
using School.API.DTOs.HR;

namespace School.API.Repositories.Interfaces.HR
{
    public interface IEmployeeRepository
    {
        Task<List<EmployeeListDto>> GetAllAsync();
        Task<EmployeeDto?> GetByIdAsync(int id);
        Task<List<EmployeeDropdownDto>> GetDropdownAsync();

        // Teaching staff in the legacy SMSTeacher shape (VID = Employee.Id), for
        // the screens that still fetch api/teacher. examId > 0 narrows it to the
        // staff holding a subject assignment in that exam.
        Task<List<TeacherDto>> GetTeachingStaffAsync(int examId = 0);
        Task<TeacherDto?> GetTeachingStaffByIdAsync(int id);

        Task<Employee> CreateAsync(SaveEmployeeDto dto, int userId, string userIp);
        Task<Employee?> UpdateAsync(int id, SaveEmployeeDto dto, int userId, string userIp);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> CodeExistsAsync(string code, int? excludeId = null);
        Task<string> NextEmployeeCodeAsync(string prefix);
    }
}
