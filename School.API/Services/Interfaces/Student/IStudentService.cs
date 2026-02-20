using School.API.DTOs;

namespace School.API.Services.Interfaces.Student
{
    public interface IStudentService
    {
        Task<List<StudentListDto>> GetAllAsync();
        Task<StudentDto?> GetByIdAsync(int id);
        Task<StudentDto?> GetByAdmissionIdAsync(int admissionId);
        Task<StudentDto> CreateAsync(CreateStudentDto dto);
        Task<StudentDto> CreateFromAdmissionAsync(CreateStudentFromAdmissionDto dto);
        Task<StudentDto?> UpdateAsync(UpdateStudentDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsByAdmissionIdAsync(int admissionId);
        Task<List<ParentLookupDto>> GetParentLookupsAsync();
        Task<StudentDto?> GetByFatherCNICAsync(string cnic);
        Task<StudentDto?> GetByMotherCNICAsync(string cnic);
        Task<StudentDto?> GetByGuardianCNICAsync(string cnic);
    }
}
