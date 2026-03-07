// Services/Interfaces/Accounts/IApplyFeeService.cs
using School.API.DTOs.Common;
using School.API.DTOs.FeeManagement;

namespace School.API.Services.Interfaces.Accounts
{
    public interface IApplyFeeService
    {
        Task<List<StudentFeePreviewResultDto>> GetPreviewAsync(ApplyFeePreviewRequestDto dto);
        Task<ApplyFeeResponseDto> ApplyFeeAsync(ApplyFeeRequestDto dto, int userId, string userIp);
        Task<List<StudentFeeListDto>> GetListAsync(GetStudentFeeRequestDto dto);
        Task<ResponseDto> DeleteAsync(int vid, int userId, string userIp);
    }
}