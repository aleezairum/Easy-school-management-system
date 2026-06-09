using School.API.DTOs.Academic;
using School.API.DTOs.Common;

namespace School.API.Services.Interfaces.Academic
{
    public interface ISMSExamService
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
