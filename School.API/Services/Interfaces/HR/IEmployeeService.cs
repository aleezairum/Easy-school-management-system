using School.API.DTOs;
using School.API.DTOs.HR;

namespace School.API.Services.Interfaces.HR
{
    public interface IEmployeeService
    {
        Task<List<EmployeeListDto>> GetAllAsync();
        Task<EmployeeDto?> GetByIdAsync(int id);
        Task<List<EmployeeDropdownDto>> GetDropdownAsync();

        // Backs the read-only api/teacher compatibility route. examId > 0 narrows
        // it to the staff holding a subject assignment in that exam.
        Task<List<TeacherDto>> GetTeachingStaffAsync(int examId = 0);
        Task<TeacherDto?> GetTeachingStaffByIdAsync(int id);

        // userId/userIp land in the employee's own audit columns — the ones the
        // teacher screen used to keep on its separate record.
        Task<EmployeeDto> CreateAsync(SaveEmployeeDto dto, int userId, string userIp);
        Task<EmployeeDto?> UpdateAsync(int id, SaveEmployeeDto dto, int userId, string userIp);
        Task<bool> DeleteAsync(int id);
    }
}
