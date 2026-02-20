using School.API.Data.DBModels.Academic;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;

namespace School.API.Repositories.Interfaces.Academic
{
    public interface IStudentStatusRepository
    {
        Task<ResponseDto> SaveAsync(StudentStatusSaveDto dto, int userId, string userIp);
        Task<List<StudentStatus>> GetAllAsync();
        Task<StudentStatus?> GetByIdAsync(int id);
        Task<ResponseDto?> DeleteByIdAsync(int id);
    }
}
