using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.Repositories.Interfaces.Academic;
using School.API.Services.Interfaces.Academic;

namespace School.API.Services.Implementations.Academic
{
    public class SMSExamResultService : ISMSExamResultService
    {
        private readonly ISMSExamResultRepository _repo;

        public SMSExamResultService(ISMSExamResultRepository repo)
        {
            _repo = repo;
        }

        public Task<ResponseDto> SaveAsync(SMSExamResultSaveDto dto, int userId, string userIp)
            => _repo.SaveAsync(dto, userId, userIp);

        public async Task<List<ResponseDto>> SaveBulkAsync(SMSExamResultBulkDto bulk, int userId, string userIp)
        {
            var results = new List<ResponseDto>();

            foreach (var row in bulk.Results)
            {
                // Force the envelope fields onto every row so the client cannot
                // mix results from multiple exams/classes/sections/subjects in one payload.
                row.ExamID    = bulk.ExamID;
                row.ClassID   = bulk.ClassID;
                row.SectionID = bulk.SectionID;
                row.SubjectID = bulk.SubjectID;

                if (bulk.TotalMarks.HasValue) row.TotalMarks = bulk.TotalMarks;
                if (bulk.PassMarks.HasValue)  row.PassMarks  = bulk.PassMarks;

                var resp = await _repo.SaveAsync(row, userId, userIp);
                if (resp != null)
                    results.Add(resp);
            }

            return results;
        }

        public Task<List<SMSExamResultSaveDto>> GetAllAsync()
            => _repo.GetAllAsync();

        public Task<SMSExamResultSaveDto?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        public Task<List<SMSExamResultSaveDto>> FillComboAsync(int examId, int studentId, int subjectId = 0, int classId = 0, int sectionId = 0)
            => _repo.FillComboAsync(examId, studentId, subjectId, classId, sectionId);

        public Task<List<SMSExamResultSaveDto>> GetByExamAsync(int examId, int subjectId = 0, int classId = 0, int sectionId = 0)
            => _repo.GetByExamAsync(examId, subjectId, classId, sectionId);

        public Task<ExamResultSheetDto> GetResultSheetAsync(int examId, int classId, int sectionId, int prevExamId = 0)
            => _repo.GetResultSheetAsync(examId, classId, sectionId, prevExamId);

        public Task<ExamResultCardDto> GetResultCardAsync(int examId, int classId, int sectionId, int prevExamId = 0, int studentId = 0)
            => _repo.GetResultCardAsync(examId, classId, sectionId, prevExamId, studentId);

        public Task<List<ExamPositionRowDto>> GetPositionReportAsync(int examId, int classId)
            => _repo.GetPositionReportAsync(examId, classId);

        public Task<List<ExamPositionRowDto>> GetPercentageReportAsync(int examId, int classId)
            => _repo.GetPercentageReportAsync(examId, classId);

        public Task<ExamExecutiveDto> GetExecutiveReportAsync(int examId, int classId, int sectionId)
            => _repo.GetExecutiveReportAsync(examId, classId, sectionId);

        public Task<List<ExamSubjectWiseRowDto>> GetSubjectWiseReportAsync(int examId, string? classIds, string? sectionIds)
            => _repo.GetSubjectWiseReportAsync(examId, classIds, sectionIds);

        public Task<List<ExamTeacherWiseRowDto>> GetTeacherWiseReportAsync(int examId, int teacherId)
            => _repo.GetTeacherWiseReportAsync(examId, teacherId);

        public Task<List<ExamTeacherComparisonRowDto>> GetTeacherComparisonReportAsync(
            int examId, string? lClassIds, string? lSectionIds, string? rClassIds, string? rSectionIds)
            => _repo.GetTeacherComparisonReportAsync(examId, lClassIds, lSectionIds, rClassIds, rSectionIds);

        public Task<ExamAnnouncementDto> GetAnnouncementReportAsync(int examId, string? classIds = null, string? sectionIds = null, int? prevExamId = null)
            => _repo.GetAnnouncementReportAsync(examId, classIds, sectionIds, prevExamId);

        public Task<ResponseDto?> DeleteByIdAsync(int id)
            => _repo.DeleteByIdAsync(id);

        // Only the date moves; the marks stay as entered. Allowed on a finalised exam too:
        // the date decides the result card's attendance window, nothing parents see changes.
        public async Task<(int Count, string? Error)> SetListDateAsync(SMSExamResultListDateDto dto, int userId)
        {
            if (dto.ExamID <= 0 || dto.ClassID <= 0 || dto.SectionID <= 0 || dto.SubjectID <= 0)
                return (0, "Choose the exam, class, section and subject.");
            var n = await _repo.SetListDateAsync(dto, userId);
            return n == 0 ? (0, "No marks are saved for this list.") : (n, null);
        }

        // A finalised exam is what parents and teachers see on WhatsApp and in the app,
        // so its marks cannot be wiped from here: un-finalise it first (Exams screen).
        public async Task<(int Count, string? Error)> DeleteListAsync(int examId, int classId, int sectionId, int subjectId)
        {
            if (examId <= 0 || classId <= 0 || sectionId <= 0 || subjectId <= 0)
                return (0, "Choose the exam, class, section and subject.");
            if (await _repo.IsExamFinalizedAsync(examId))
                return (0, "This exam's result is finalised. Un-finalise it on the Exams screen first, then delete the list.");
            var n = await _repo.DeleteListAsync(examId, classId, sectionId, subjectId);
            return n == 0 ? (0, "No marks are saved for this list.") : (n, null);
        }
    }
}
