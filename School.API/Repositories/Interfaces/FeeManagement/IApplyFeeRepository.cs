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
        Task<List<StudentFeeReportDto>> GetReportAsync(GetStudentFeeReportRequestDto dto);
        Task<List<StudentFeeListDto>> GetPendingsAsync(int studentId, DateTime? asOn = null);
        Task<List<PendingFeeStudentDto>> GetPendingStudentsAsync(int classId, int sectionId, DateTime? asOn = null);
        Task<ResponseDto> DeleteAsync(int vid, int userId, string userIp);
    }
}