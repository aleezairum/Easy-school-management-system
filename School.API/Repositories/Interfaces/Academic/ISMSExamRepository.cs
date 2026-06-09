using School.API.DTOs.Academic;
using School.API.DTOs.Common;

namespace School.API.Repositories.Interfaces.Academic
{
    public interface ISMSExamRepository
    {
        Task<ResponseDto> SaveAsync(
            SMSExamSaveDto dto,
            int userId,
            string userIp);

        Task<List<SMSExamSaveDto>> GetAllAsync();
        Task<SMSExamSaveDto?> GetByIdAsync(int id);
        Task<List<SMSExamSaveDto>> FillComboAsync();
        Task<ResponseDto?> DeleteByIdAsync(int id);
    }
}
