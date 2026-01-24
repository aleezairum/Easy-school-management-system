using School.API.Data.DBModels.Academic;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;

namespace School.API.Repositories.Interfaces.Academic
{
    public interface IAcademicSessionYearRepository
    {
        Task<ResponseDto> SaveAsync(
            AcademicSessionYearSaveDto dto,
            int userId,
            string userIp);

        Task<List<AcademicSessionYear>> GetAllAsync();
        Task<AcademicSessionYear?> GetByIdAsync(int id);
        Task<ResponseDto?> DeleteByIdAsync(int id);
    }
}
