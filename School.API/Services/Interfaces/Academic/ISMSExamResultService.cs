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
        Task<List<ExamPositionRowDto>> GetPositionReportAsync(int examId, int classId);
        Task<List<ExamPositionRowDto>> GetPercentageReportAsync(int examId, int classId);
        Task<ExamExecutiveDto> GetExecutiveReportAsync(int examId, int classId, int sectionId);
        Task<ExamAnnouncementDto> GetAnnouncementReportAsync(int examId);
        Task<ResponseDto?> DeleteByIdAsync(int id);
    }
}
