using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.Academic;
using School.API.DTOs;
using School.API.Repositories.Interfaces.Academic;

namespace School.API.Repositories.Implementations.Academic
{
    public class ExamRepository : IExamRepository
    {
        private readonly SchoolDbContext _context;

        public ExamRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ExamListDto>> GetAllAsync()
        {
            return await _context.Exams
                .Include(e => e.Class)
                .Include(e => e.Subject)
                .OrderByDescending(e => e.ExamDate)
                .Select(e => new ExamListDto
                {
                    Id = e.Id,
                    ExamName = e.ExamName,
                    ExamTypeName = e.ExamType.ToString(),
                    ClassName = e.Class != null ? e.Class.VName : null,
                    SubjectName = e.Subject != null ? e.Subject.VName : null,
                    ExamDate = e.ExamDate,
                    TotalMarks = e.TotalMarks,
                    PassingMarks = e.PassingMarks,
                    StatusName = e.Status.ToString(),
                    IsActive = e.IsActive
                })
                .ToListAsync();
        }

        public async Task<ExamDto?> GetByIdAsync(int id)
        {
            return await _context.Exams
                .Include(e => e.Class)
                .Include(e => e.Subject)
                .Include(e => e.Session)
                .Where(e => e.Id == id)
                .Select(e => new ExamDto
                {
                    Id = e.Id,
                    ExamName = e.ExamName,
                    ExamType = e.ExamType,
                    ClassId = e.ClassId,
                    ClassName = e.Class != null ? e.Class.VName : null,
                    SubjectId = e.SubjectId,
                    SubjectName = e.Subject != null ? e.Subject.VName : null,
                    SessionId = e.SessionId,
                    SessionName = e.Session != null ? e.Session.VName : null,
                    ExamDate = e.ExamDate,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    TotalMarks = e.TotalMarks,
                    PassingMarks = e.PassingMarks,
                    Status = e.Status,
                    Description = e.Description,
                    RoomNumber = e.RoomNumber,
                    IsActive = e.IsActive,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.UpdatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<ExamListDto>> GetByClassAsync(int classId, int sessionId)
        {
            return await _context.Exams
                .Include(e => e.Subject)
                .Where(e => e.ClassId == classId && e.SessionId == sessionId && e.IsActive)
                .OrderByDescending(e => e.ExamDate)
                .Select(e => new ExamListDto
                {
                    Id = e.Id,
                    ExamName = e.ExamName,
                    ExamTypeName = e.ExamType.ToString(),
                    ClassName = e.Class != null ? e.Class.VName : null,
                    SubjectName = e.Subject != null ? e.Subject.VName : null,
                    ExamDate = e.ExamDate,
                    TotalMarks = e.TotalMarks,
                    PassingMarks = e.PassingMarks,
                    StatusName = e.Status.ToString(),
                    IsActive = e.IsActive
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<ExamDropdownDto>> GetDropdownAsync(int? classId = null, int? sessionId = null)
        {
            var query = _context.Exams
                .Include(e => e.Subject)
                .Where(e => e.IsActive);

            if (classId.HasValue)
                query = query.Where(e => e.ClassId == classId.Value);

            if (sessionId.HasValue)
                query = query.Where(e => e.SessionId == sessionId.Value);

            return await query
                .OrderByDescending(e => e.ExamDate)
                .Select(e => new ExamDropdownDto
                {
                    Id = e.Id,
                    ExamName = e.ExamName,
                    SubjectName = e.Subject != null ? e.Subject.VName : null
                })
                .ToListAsync();
        }

        public async Task<Exam> CreateAsync(CreateExamDto dto)
        {
            var entity = new Exam
            {
                ExamName = dto.ExamName,
                ExamType = dto.ExamType,
                ClassId = dto.ClassId,
                SubjectId = dto.SubjectId,
                SessionId = dto.SessionId,
                ExamDate = dto.ExamDate.Date,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                TotalMarks = dto.TotalMarks,
                PassingMarks = dto.PassingMarks,
                Status = dto.Status,
                Description = dto.Description,
                RoomNumber = dto.RoomNumber,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Exams.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<Exam?> UpdateAsync(int id, UpdateExamDto dto)
        {
            var entity = await _context.Exams.FindAsync(id);
            if (entity == null)
                return null;

            entity.ExamName = dto.ExamName;
            entity.ExamType = dto.ExamType;
            entity.ClassId = dto.ClassId;
            entity.SubjectId = dto.SubjectId;
            entity.SessionId = dto.SessionId;
            entity.ExamDate = dto.ExamDate.Date;
            entity.StartTime = dto.StartTime;
            entity.EndTime = dto.EndTime;
            entity.TotalMarks = dto.TotalMarks;
            entity.PassingMarks = dto.PassingMarks;
            entity.Status = dto.Status;
            entity.Description = dto.Description;
            entity.RoomNumber = dto.RoomNumber;
            entity.IsActive = dto.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.Exams.FindAsync(id);
            if (entity == null)
                return false;

            _context.Exams.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Exams.AnyAsync(e => e.Id == id);
        }

        // Exam Result methods
        public async Task<IEnumerable<ExamResultListDto>> GetResultsByExamAsync(int examId)
        {
            var exam = await _context.Exams.FindAsync(examId);
            var totalMarks = exam?.TotalMarks ?? 0;

            return await _context.ExamResults
                .Include(r => r.Student)
                .Where(r => r.ExamId == examId)
                .OrderBy(r => r.Student!.RollNo)
                .Select(r => new ExamResultListDto
                {
                    Id = r.Id,
                    StudentName = r.Student != null ? r.Student.NameOfStudent : null,
                    RollNumber = r.Student != null ? r.Student.RollNo : null,
                    MarksObtained = r.MarksObtained,
                    TotalMarks = totalMarks,
                    Grade = r.Grade,
                    IsAbsent = r.IsAbsent,
                    IsPassed = r.IsPassed
                })
                .ToListAsync();
        }

        public async Task<ExamResultDto?> GetResultByIdAsync(int id)
        {
            return await _context.ExamResults
                .Include(r => r.Exam)
                .Include(r => r.Student)
                .Where(r => r.Id == id)
                .Select(r => new ExamResultDto
                {
                    Id = r.Id,
                    ExamId = r.ExamId,
                    ExamName = r.Exam != null ? r.Exam.ExamName : null,
                    StudentId = r.StudentId,
                    StudentName = r.Student != null ? r.Student.NameOfStudent : null,
                    RollNumber = r.Student != null ? r.Student.RollNo : null,
                    MarksObtained = r.MarksObtained,
                    Grade = r.Grade,
                    IsAbsent = r.IsAbsent,
                    IsPassed = r.IsPassed,
                    Remarks = r.Remarks,
                    TotalMarks = r.Exam != null ? r.Exam.TotalMarks : 0,
                    PassingMarks = r.Exam != null ? r.Exam.PassingMarks : 0,
                    CreatedAt = r.CreatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<ExamResult> CreateResultAsync(CreateExamResultDto dto)
        {
            var exam = await _context.Exams.FindAsync(dto.ExamId);
            var isPassed = !dto.IsAbsent && dto.MarksObtained.HasValue &&
                           exam != null && dto.MarksObtained >= exam.PassingMarks;

            var entity = new ExamResult
            {
                ExamId = dto.ExamId,
                StudentId = dto.StudentId,
                MarksObtained = dto.IsAbsent ? null : dto.MarksObtained,
                Grade = dto.Grade,
                IsAbsent = dto.IsAbsent,
                IsPassed = isPassed,
                Remarks = dto.Remarks,
                CreatedAt = DateTime.UtcNow
            };

            _context.ExamResults.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteResultAsync(int id)
        {
            var entity = await _context.ExamResults.FindAsync(id);
            if (entity == null)
                return false;

            _context.ExamResults.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task SaveBulkResultsAsync(BulkExamResultDto dto)
        {
            var exam = await _context.Exams.FindAsync(dto.ExamId);
            if (exam == null)
                return;

            var existingResults = await _context.ExamResults
                .Where(r => r.ExamId == dto.ExamId)
                .ToListAsync();

            foreach (var result in dto.Results)
            {
                var existing = existingResults.FirstOrDefault(r => r.StudentId == result.StudentId);
                var isPassed = !result.IsAbsent && result.MarksObtained.HasValue &&
                               result.MarksObtained >= exam.PassingMarks;

                if (existing != null)
                {
                    existing.MarksObtained = result.IsAbsent ? null : result.MarksObtained;
                    existing.Grade = result.Grade;
                    existing.IsAbsent = result.IsAbsent;
                    existing.IsPassed = isPassed;
                    existing.Remarks = result.Remarks;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var newResult = new ExamResult
                    {
                        ExamId = dto.ExamId,
                        StudentId = result.StudentId,
                        MarksObtained = result.IsAbsent ? null : result.MarksObtained,
                        Grade = result.Grade,
                        IsAbsent = result.IsAbsent,
                        IsPassed = isPassed,
                        Remarks = result.Remarks,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.ExamResults.Add(newResult);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
