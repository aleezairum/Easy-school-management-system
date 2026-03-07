// Repositories/Interfaces/Accounts/IApplyFeeRepository.cs
using School.API.DTOs.Common;
using School.API.DTOs.FeeManagement;

namespace School.API.Repositories.Interfaces.Accounts
{
    public interface IApplyFeeRepository
    {
        Task<List<StudentFeePreviewResultDto>> GetPreviewAsync(ApplyFeePreviewRequestDto dto);
        Task<ResponseDto?> SaveStudentFeeAsync(SaveStudentFeeRequestDto dto, int userId, string userIp);
        Task<List<StudentFeeListDto>> GetListAsync(GetStudentFeeRequestDto dto);
        Task<ResponseDto> DeleteAsync(int vid, int userId, string userIp);
    }
}