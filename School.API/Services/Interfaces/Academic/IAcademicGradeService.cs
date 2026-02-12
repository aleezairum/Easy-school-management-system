using School.API.Data.DBModels.Academic;
using School.API.DTOs;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;

namespace School.API.Services.Interfaces.Academic
{
    public interface IAcademicGradeService
    {
        Task<ResponseDto> SaveAsync(
            AcademicGradeSaveDto dto,
            int userId,
            string userIp);

        Task<List<AcademicGrades>> GetAllAsync();
        Task<AcademicGrades?> GetByIdAsync(int id);
        Task<ResponseDto?> DeleteByIdAsync(int id);
    }
}
