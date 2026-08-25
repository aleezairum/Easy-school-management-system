using School.API.DTOs.Academic;
using School.API.Repositories.Interfaces.Academic;
using School.API.Services.Interfaces.Academic;

namespace School.API.Services.Implementations.Academic
{
    public class CertificateService : ICertificateService
    {
        private readonly ICertificateRepository _repo;

        public CertificateService(ICertificateRepository repo)
        {
            _repo = repo;
        }

        public Task<LeavingCertificateDto?> GetLeavingAsync(
            int studentId,
            DateTime? leavingDate,
            string? examination,
            string? marksObtained,
            string? totalMarks,
            string? classAdmitted,
            string? classStudying)
            => _repo.GetLeavingAsync(studentId, leavingDate, examination, marksObtained, totalMarks, classAdmitted, classStudying);

        public Task<ProvisionalCertificateDto?> GetProvisionalAsync(
            int studentId,
            int? sessionId,
            string? boardRollNo,
            string? examRollNo,
            string? examination,
            string? marksObtained,
            string? totalMarks,
            string? conduct,
            string? moralCharacter,
            DateTime? issueDate)
            => _repo.GetProvisionalAsync(studentId, sessionId, boardRollNo, examRollNo, examination, marksObtained, totalMarks, conduct, moralCharacter, issueDate);
    }
}
