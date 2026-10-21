using School.API.Data.DBModels.HR;
using School.API.DTOs;
using School.API.DTOs.HR;

namespace School.API.Repositories.Interfaces.HR
{
    public interface IEmployeeRepository
    {
        /// <param name="campusId">Only staff posted to this campus; null returns everybody.</param>
        Task<List<EmployeeListDto>> GetAllAsync(int? campusId = null);
        Task<EmployeeDto?> GetByIdAsync(int id);
        Task<string?> GetPhotoAsync(int id);
        Task<List<EmployeeDropdownDto>> GetDropdownAsync();

        // Teaching staff in the legacy SMSTeacher shape (VID = Employee.Id), for
        // the screens that still fetch api/teacher. examId > 0 narrows it to the
        // staff holding a subject assignment in that exam.
        Task<List<TeacherDto>> GetTeachingStaffAsync(int examId = 0);
        Task<TeacherDto?> GetTeachingStaffByIdAsync(int id);

        Task<Employee> CreateAsync(SaveEmployeeDto dto, int userId, string userIp);
        Task<Employee?> UpdateAsync(int id, SaveEmployeeDto dto, int userId, string userIp);
        Task<int> SetStaffCategoryAsync(List<int> employeeIds, int? staffCategoryId, int userId, string userIp);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> CodeExistsAsync(string code, int? excludeId = null);
        Task<string> NextEmployeeCodeAsync(string prefix);
    }
}
