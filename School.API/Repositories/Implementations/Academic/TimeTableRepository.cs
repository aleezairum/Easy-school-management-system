using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.Academic;
using School.API.DTOs;
using School.API.Repositories.Interfaces.Academic;

namespace School.API.Repositories.Implementations.Academic
{
    public class TimeTableRepository : ITimeTableRepository
    {
        private readonly SchoolDbContext _context;

        public TimeTableRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TimeTableListDto>> GetAllAsync()
        {
            return await _context.TimeTables
                .Include(t => t.Class)
                .Include(t => t.Section)
                .Include(t => t.Subject)
                .Include(t => t.Teacher)
                .OrderBy(t => t.DayOfWeek)
                .ThenBy(t => t.PeriodNumber)
                .Select(t => new TimeTableListDto
                {
                    Id = t.Id,
                    ClassName = t.Class != null ? t.Class.VName : null,
                    SectionName = t.Section != null ? t.Section.VName : null,
                    SubjectName = t.Subject != null ? t.Subject.VName : null,
                    TeacherName = t.Teacher != null ? t.Teacher.FirstName + " " + t.Teacher.LastName : null,
                    DayName = t.DayOfWeek.ToString(),
                    PeriodNumber = t.PeriodNumber,
                    StartTime = t.StartTime.ToString(@"hh\:mm"),
                    EndTime = t.EndTime.ToString(@"hh\:mm"),
                    RoomNumber = t.RoomNumber,
                    IsActive = t.IsActive
                })
                .ToListAsync();
        }

        public async Task<TimeTableDto?> GetByIdAsync(int id)
        {
            return await _context.TimeTables
                .Include(t => t.Class)
                .Include(t => t.Section)
                .Include(t => t.Subject)
                .Include(t => t.Teacher)
                .Include(t => t.Session)
                .Where(t => t.Id == id)
                .Select(t => new TimeTableDto
                {
                    Id = t.Id,
                    ClassId = t.ClassId,
                    ClassName = t.Class != null ? t.Class.VName : null,
                    SectionId = t.SectionId,
                    SectionName = t.Section != null ? t.Section.VName : null,
                    SubjectId = t.SubjectId,
                    SubjectName = t.Subject != null ? t.Subject.VName : null,
                    TeacherId = t.TeacherId,
                    TeacherName = t.Teacher != null ? t.Teacher.FirstName + " " + t.Teacher.LastName : null,
                    SessionId = t.SessionId,
                    SessionName = t.Session != null ? t.Session.VName : null,
                    DayOfWeek = t.DayOfWeek,
                    PeriodNumber = t.PeriodNumber,
                    StartTime = t.StartTime,
                    EndTime = t.EndTime,
                    RoomNumber = t.RoomNumber,
                    IsActive = t.IsActive,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<TimeTableListDto>> GetByClassSectionAsync(int classId, int sectionId, int sessionId)
        {
            return await _context.TimeTables
                .Include(t => t.Subject)
                .Include(t => t.Teacher)
                .Where(t => t.ClassId == classId && t.SectionId == sectionId && t.SessionId == sessionId && t.IsActive)
                .OrderBy(t => t.DayOfWeek)
                .ThenBy(t => t.PeriodNumber)
                .Select(t => new TimeTableListDto
                {
                    Id = t.Id,
                    ClassName = t.Class != null ? t.Class.VName : null,
                    SectionName = t.Section != null ? t.Section.VName : null,
                    SubjectName = t.Subject != null ? t.Subject.VName : null,
                    TeacherName = t.Teacher != null ? t.Teacher.FirstName + " " + t.Teacher.LastName : null,
                    DayName = t.DayOfWeek.ToString(),
                    PeriodNumber = t.PeriodNumber,
                    StartTime = t.StartTime.ToString(@"hh\:mm"),
                    EndTime = t.EndTime.ToString(@"hh\:mm"),
                    RoomNumber = t.RoomNumber,
                    IsActive = t.IsActive
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<TimeTableListDto>> GetByTeacherAsync(int teacherId, int sessionId)
        {
            return await _context.TimeTables
                .Include(t => t.Class)
                .Include(t => t.Section)
                .Include(t => t.Subject)
                .Where(t => t.TeacherId == teacherId && t.SessionId == sessionId && t.IsActive)
                .OrderBy(t => t.DayOfWeek)
                .ThenBy(t => t.PeriodNumber)
                .Select(t => new TimeTableListDto
                {
                    Id = t.Id,
                    ClassName = t.Class != null ? t.Class.VName : null,
                    SectionName = t.Section != null ? t.Section.VName : null,
                    SubjectName = t.Subject != null ? t.Subject.VName : null,
                    TeacherName = t.Teacher != null ? t.Teacher.FirstName + " " + t.Teacher.LastName : null,
                    DayName = t.DayOfWeek.ToString(),
                    PeriodNumber = t.PeriodNumber,
                    StartTime = t.StartTime.ToString(@"hh\:mm"),
                    EndTime = t.EndTime.ToString(@"hh\:mm"),
                    RoomNumber = t.RoomNumber,
                    IsActive = t.IsActive
                })
                .ToListAsync();
        }

        public async Task<TimeTable> CreateAsync(CreateTimeTableDto dto)
        {
            var entity = new TimeTable
            {
                ClassId = dto.ClassId,
                SectionId = dto.SectionId,
                SubjectId = dto.SubjectId,
                TeacherId = dto.TeacherId,
                SessionId = dto.SessionId,
                DayOfWeek = dto.DayOfWeek,
                PeriodNumber = dto.PeriodNumber,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                RoomNumber = dto.RoomNumber,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.TimeTables.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<TimeTable?> UpdateAsync(int id, UpdateTimeTableDto dto)
        {
            var entity = await _context.TimeTables.FindAsync(id);
            if (entity == null)
                return null;

            entity.ClassId = dto.ClassId;
            entity.SectionId = dto.SectionId;
            entity.SubjectId = dto.SubjectId;
            entity.TeacherId = dto.TeacherId;
            entity.SessionId = dto.SessionId;
            entity.DayOfWeek = dto.DayOfWeek;
            entity.PeriodNumber = dto.PeriodNumber;
            entity.StartTime = dto.StartTime;
            entity.EndTime = dto.EndTime;
            entity.RoomNumber = dto.RoomNumber;
            entity.IsActive = dto.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.TimeTables.FindAsync(id);
            if (entity == null)
                return false;

            _context.TimeTables.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.TimeTables.AnyAsync(t => t.Id == id);
        }

        public async Task<bool> HasConflictAsync(int classId, int sectionId, DayOfWeek dayOfWeek, int periodNumber, int sessionId, int? excludeId = null)
        {
            var query = _context.TimeTables
                .Where(t => t.ClassId == classId && t.SectionId == sectionId &&
                            t.DayOfWeek == dayOfWeek && t.PeriodNumber == periodNumber &&
                            t.SessionId == sessionId);

            if (excludeId.HasValue)
                query = query.Where(t => t.Id != excludeId.Value);

            return await query.AnyAsync();
        }
    }
}
