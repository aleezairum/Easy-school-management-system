using School.API.DTOs.Academic;
using School.API.DTOs.Common;

namespace School.API.Services.Interfaces.Academic
{
    public interface ISMSExamResultService
    {
        Task<ResponseDto> SaveAsync(
            SMSExamResultSaveDto dto,
            int userId,
            string userIp);

        Task<List<ResponseDto>> SaveBulkAsync(
            SMSExamResultBulkDto bulk,
            int userId,
            string userIp);

        Task<List<SMSExamResultSaveDto>> GetAllAsync();
        Task<SMSExamResultSaveDto?> GetByIdAsync(int id);
        Task<List<SMSExamResultSaveDto>> FillComboAsync(int examId, int studentId, int subjectId = 0, int classId = 0, int sectionId = 0);
        Task<List<SMSExamResultSaveDto>> GetByExamAsync(int examId, int subjectId = 0, int classId = 0, int sectionId = 0);
        Task<ExamResultSheetDto> GetResultSheetAsync(int examId, int classId, int sectionId, int prevExamId = 0);
        Task<ExamResultCardDto> GetResultCardAsync(int examId, int classId, int sectionId, int prevExamId = 0, int studentId = 0);
        Task<List<ExamPositionRowDto>> GetPositionReportAsync(int examId, int classId, int prevExamId = 0);
        Task<List<ExamPositionRowDto>> GetPercentageReportAsync(int examId, int classId, int prevExamId = 0);
        Task<ExamExecutiveDto> GetExecutiveReportAsync(int examId, int classId, int sectionId);
        Task<List<ExamSubjectWiseRowDto>> GetSubjectWiseReportAsync(int examId, string? classIds, string? sectionIds);
        Task<List<ExamTeacherWiseRowDto>> GetTeacherWiseReportAsync(int examId, int teacherId);
        Task<List<ExamTeacherWiseRowDto>> GetAcademyTeacherWiseReportAsync(int examId, int teacherId);
        Task<List<ExamTeacherComparisonRowDto>> GetTeacherComparisonReportAsync(
            int examId, string? lClassIds, string? lSectionIds, string? rClassIds, string? rSectionIds);
        Task<List<ComparisonExamDto>> GetComparisonExamsAsync(int sessionId);
        Task<List<ComparisonRowDto>> GetComparisonAsync(IReadOnlyCollection<int> examIds);
        Task<ExamAnnouncementDto> GetAnnouncementReportAsync(int examId, string? classIds = null, string? sectionIds = null, int? prevExamId = null);
        Task<ResponseDto?> DeleteByIdAsync(int id);

        // A whole award list (Exam + Class + Section + Subject). Error is set when nothing was done.
        Task<(int Count, string? Error)> SetListDateAsync(SMSExamResultListDateDto dto, int userId);
        Task<(int Count, string? Error)> DeleteListAsync(int examId, int classId, int sectionId, int subjectId);
    }
}
