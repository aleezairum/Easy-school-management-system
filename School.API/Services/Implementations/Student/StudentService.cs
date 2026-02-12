using School.API.DTOs;
using School.API.Repositories.Interfaces.Student;
using School.API.Services.Interfaces.Student;

namespace School.API.Services.Implementations.Student
{
    public class StudentService : IStudentService
    {
        private readonly IStudentRepository _repository;

        public StudentService(IStudentRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<StudentListDto>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<StudentDto?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<StudentDto?> GetByAdmissionIdAsync(int admissionId)
        {
            return await _repository.GetByAdmissionIdAsync(admissionId);
        }

        public async Task<StudentDto> CreateAsync(CreateStudentDto dto)
        {
            var student = await _repository.CreateAsync(dto);
            return (await _repository.GetByIdAsync(student.Id))!;
        }

        public async Task<StudentDto> CreateFromAdmissionAsync(CreateStudentFromAdmissionDto dto)
        {
            // Check if student already exists for this admission
            if (await _repository.ExistsByAdmissionIdAsync(dto.AdmissionId))
            {
                throw new InvalidOperationException("A student already exists for this admission");
            }

            var student = await _repository.CreateFromAdmissionAsync(dto);
            return (await _repository.GetByIdAsync(student.Id))!;
        }

        public async Task<StudentDto?> UpdateAsync(UpdateStudentDto dto)
        {
            var student = await _repository.UpdateAsync(dto);
            if (student == null) return null;

            return await _repository.GetByIdAsync(dto.Id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        public async Task<bool> ExistsByAdmissionIdAsync(int admissionId)
        {
            return await _repository.ExistsByAdmissionIdAsync(admissionId);
        }
    }
}
