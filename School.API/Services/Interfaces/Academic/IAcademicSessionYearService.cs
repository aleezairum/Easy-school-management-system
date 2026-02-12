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

<<<<<<< HEAD
        Task<List<AcademicSessionYearSaveDto>> GetAllAsync();
        Task<AcademicSessionYearSaveDto?> GetByIdAsync(int id);
=======
        Task<List<AcademicSessionYear>> GetAllAsync();
        Task<AcademicSessionYear?> GetByIdAsync(int id);
>>>>>>> ddd2cfec04642aebc056f91a2df1715e14979d68
        Task<ResponseDto?> DeleteByIdAsync(int id);
    }
}
