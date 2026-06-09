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

        public Task<ExamAnnouncementDto> GetAnnouncementReportAsync(int examId)
            => _repo.GetAnnouncementReportAsync(examId);

        public Task<ResponseDto?> DeleteByIdAsync(int id)
            => _repo.DeleteByIdAsync(id);
    }
}
