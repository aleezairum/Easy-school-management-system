using School.API.DTOs;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;

namespace School.API.Repositories.Interfaces.Academic
{
    public interface ISMSExamResultRepository
    {
        // Distinct students who have results in the exam (optionally filtered by
        // class / section / single student), each with a resolved WhatsApp number
        // (Father -> Guardian -> Student). Used to enqueue Rcard PDFs.
        Task<List<RcardRecipientDto>> GetRcardRecipientsAsync(int examId, int classId, int sectionId, int studentId);

        Task<ResponseDto> SaveAsync(
            SMSExamResultSaveDto dto,
            int userId,
            string userIp);

        Task<List<SMSExamResultSaveDto>> GetAllAsync();
        Task<SMSExamResultSaveDto?> GetByIdAsync(int id);
        Task<List<SMSExamResultSaveDto>> FillComboAsync(int examId, int studentId, int subjectId = 0, int classId = 0, int sectionId = 0);
        Task<List<SMSExamResultSaveDto>> GetByExamAsync(int examId, int subjectId = 0, int classId = 0, int sectionId = 0);
        Task<ExamResultSheetDto> GetResultSheetAsync(int examId, int classId, int sectionId, int prevExamId = 0);
        Task<ExamResultCardDto> GetResultCardAsync(int examId, int classId, int sectionId, int prevExamId = 0, int studentId = 0);
        Task<List<ExamPositionRowDto>> GetPositionReportAsync(int examId, int classId);
        Task<List<ExamPositionRowDto>> GetPercentageReportAsync(int examId, int classId);
        Task<ExamExecutiveDto> GetExecutiveReportAsync(int examId, int classId, int sectionId);
        Task<List<ExamSubjectWiseRowDto>> GetSubjectWiseReportAsync(int examId, string? classIds, string? sectionIds);
        Task<List<ExamTeacherWiseRowDto>> GetTeacherWiseReportAsync(int examId, int teacherId);
        Task<List<ExamTeacherComparisonRowDto>> GetTeacherComparisonReportAsync(
            int examId, string? lClassIds, string? lSectionIds, string? rClassIds, string? rSectionIds);
        Task<ExamAnnouncementDto> GetAnnouncementReportAsync(int examId, string? classIds = null, string? sectionIds = null);
        Task<ResponseDto?> DeleteByIdAsync(int id);
    }
}
