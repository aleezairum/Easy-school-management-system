using School.API.Data.DBModels.Academic;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;

namespace School.API.Services.Interfaces.Academic
{
    public interface IAcademicSessionYearService
    {
        Task<ResponseDto> SaveAsync(
            AcademicSessionYearSaveDto dto,
            int userId,
            string userIp);

        Task<List<AcademicSessionYearSaveDto>> GetAllAsync();
        Task<AcademicSessionYearSaveDto?> GetByIdAsync(int id);
        Task<ResponseDto?> DeleteByIdAsync(int id);
    }
}
