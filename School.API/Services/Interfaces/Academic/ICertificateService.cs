using School.API.DTOs.Academic;

namespace School.API.Services.Interfaces.Academic
{
    public interface ICertificateService
    {
        Task<LeavingCertificateDto?> GetLeavingAsync(
            int studentId,
            DateTime? leavingDate,
            string? examination,
            string? marksObtained,
            string? totalMarks,
            string? classAdmitted,
            string? classStudying);

        Task<ProvisionalCertificateDto?> GetProvisionalAsync(
            int studentId,
            int? sessionId,
            string? boardRollNo,
            string? examRollNo,
            string? examination,
            string? marksObtained,
            string? totalMarks,
            string? conduct,
            string? moralCharacter,
            DateTime? issueDate);
    }
}
