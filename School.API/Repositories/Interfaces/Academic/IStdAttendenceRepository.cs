using School.API.Data.DBModels.Student;
using School.API.DTOs.Common;
using School.API.DTOs.Student;

namespace School.API.Repositories.Interfaces.Student
{
    public interface IStdAttendenceRepository
    {
        Task<ResponseDto> SaveAsync(StdAttendenceSaveDto dto, int userId, string userIp);
        Task<List<StdAttendence>> GetAllAsync(StdAttendenceFilterDto filter);
        Task<StdAttendence?> GetByIdAsync(int id);
        Task<ResponseDto?> DeleteByIdAsync(int id, int userId, string userIp);
    }
}