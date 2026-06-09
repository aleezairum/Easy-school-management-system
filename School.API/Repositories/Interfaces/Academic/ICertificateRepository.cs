using School.API.DTOs.Academic;

namespace School.API.Repositories.Interfaces.Academic
{
    public interface ICertificateRepository
    {
        Task<LeavingCertificateDto?> GetLeavingAsync(
            int studentId,
            DateTime? leavingDate,
            string? examination,
            string? marksObtained,
            string? totalMarks);

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
