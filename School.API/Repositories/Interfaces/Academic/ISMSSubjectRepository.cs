using School.API.DTOs.Academic;
using School.API.DTOs.Common;

namespace School.API.Repositories.Interfaces.Academic
{
    public interface ISMSSubjectRepository
    {
        Task<ResponseDto> SaveAsync(
            SMSSubjectSaveDto dto,
            int userId,
            string userIp);

        Task<List<SMSSubjectSaveDto>> GetAllAsync();
        Task<SMSSubjectSaveDto?> GetByIdAsync(int id);
        Task<List<SMSSubjectSaveDto>> FillComboAsync(int id);
        Task<ResponseDto?> DeleteByIdAsync(int id);
    }
}
