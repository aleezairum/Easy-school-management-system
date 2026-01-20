using School.API.DTOs;
using School.API.Models;

namespace School.API.Repositories.Interfaces.Student
{
    public interface IStudentRepository
    {
        Task<List<StudentListDto>> GetAllAsync();
        Task<StudentDto?> GetByIdAsync(int id);
        Task<StudentDto?> GetByAdmissionIdAsync(int admissionId);
        Task<Models.Student> CreateAsync(CreateStudentDto dto);
        Task<Models.Student> CreateFromAdmissionAsync(CreateStudentFromAdmissionDto dto);
        Task<Models.Student?> UpdateAsync(UpdateStudentDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsByAdmissionIdAsync(int admissionId);
    }
}
