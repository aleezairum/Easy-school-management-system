using School.API.DTOs.HR;

namespace School.API.Services.Interfaces.HR
{
    public interface IPayComponentService
    {
        Task<List<PayComponentDto>> GetAllAsync();
        Task<PayComponentDto?> GetByIdAsync(int id);
        Task<PayComponentDto> CreateAsync(SavePayComponentDto dto);
        Task<PayComponentDto?> UpdateAsync(int id, SavePayComponentDto dto);
        Task<bool> DeleteAsync(int id);
        Task<PayComponentPreviewResultDto?> PreviewAsync(PayComponentPreviewRequestDto dto);
    }
}
