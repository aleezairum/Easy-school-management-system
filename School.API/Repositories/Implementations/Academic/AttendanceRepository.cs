using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.Academic;
using School.API.DTOs;
using School.API.Repositories.Interfaces.Academic;

namespace School.API.Repositories.Implementations.Academic
{
    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly SchoolDbContext _context;

        public AttendanceRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AttendanceListDto>> GetAllAsync()
        {
            return await _context.Attendances
                .Include(a => a.Student)
                .Include(a => a.Class)
                .Include(a => a.Section)
                .OrderByDescending(a => a.AttendanceDate)
                .ThenBy(a => a.Student!.RollNo)
                .Select(a => new AttendanceListDto
                {
                    Id = a.Id,
                    StudentName = a.Student != null ? a.Student.NameOfStudent : null,
                    RollNumber = a.Student != null ? a.Student.RollNo : null,
                    ClassName = a.Class != null ? a.Class.VName : null,
                    SectionName = a.Section != null ? a.Section.VName : null,
                    AttendanceDate = a.AttendanceDate,
                    Status = a.Status,
                    StatusName = a.Status.ToString(),
                    TimeIn = a.TimeIn.HasValue ? a.TimeIn.Value.ToString(@"hh\:mm") : null,
                    TimeOut = a.TimeOut.HasValue ? a.TimeOut.Value.ToString(@"hh\:mm") : null
                })
                .Take(500)
                .ToListAsync();
        }

        public async Task<AttendanceDto?> GetByIdAsync(int id)
        {
            return await _context.Attendances
                .Include(a => a.Student)
                .Include(a => a.Class)
                .Include(a => a.Section)
                .Include(a => a.Session)
                .Where(a => a.Id == id)
                .Select(a => new AttendanceDto
                {
                    Id = a.Id,
                    StudentId = a.StudentId,
                    StudentName = a.Student != null ? a.Student.NameOfStudent : null,
                    RollNumber = a.Student != null ? a.Student.RollNo : null,
                    ClassId = a.ClassId,
                    ClassName = a.Class != null ? a.Class.VName : null,
                    SectionId = a.SectionId,
                    SectionName = a.Section != null ? a.Section.VName : null,
                    SessionId = a.SessionId,
                    SessionName = a.Session != null ? a.Session.VName : null,
                    AttendanceDate = a.AttendanceDate,
                    Status = a.Status,
                    TimeIn = a.TimeIn,
                    TimeOut = a.TimeOut,
                    Remarks = a.Remarks,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<AttendanceListDto>> GetByDateAsync(DateTime date)
        {
            return await _context.Attendances
                .Include(a => a.Student)
                .Include(a => a.Class)
                .Include(a => a.Section)
                .Where(a => a.AttendanceDate.Date == date.Date)
                .OrderBy(a => a.Class!.VName)
                .ThenBy(a => a.Section!.VName)
                .ThenBy(a => a.Student!.RollNo)
                .Select(a => new AttendanceListDto
                {
                    Id = a.Id,
                    StudentName = a.Student != null ? a.Student.NameOfStudent : null,
                    RollNumber = a.Student != null ? a.Student.RollNo : null,
                    ClassName = a.Class != null ? a.Class.VName : null,
                    SectionName = a.Section != null ? a.Section.VName : null,
                    AttendanceDate = a.AttendanceDate,
                    Status = a.Status,
                    StatusName = a.Status.ToString(),
                    TimeIn = a.TimeIn.HasValue ? a.TimeIn.Value.ToString(@"hh\:mm") : null,
                    TimeOut = a.TimeOut.HasValue ? a.TimeOut.Value.ToString(@"hh\:mm") : null
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<AttendanceListDto>> GetByClassSectionDateAsync(int classId, int sectionId, int sessionId, DateTime date)
        {
            return await _context.Attendances
                .Include(a => a.Student)
                .Where(a => a.ClassId == classId && a.SectionId == sectionId &&
                            a.SessionId == sessionId && a.AttendanceDate.Date == date.Date)
                .OrderBy(a => a.Student!.RollNo)
                .Select(a => new AttendanceListDto
                {
                    Id = a.Id,
                    StudentName = a.Student != null ? a.Student.NameOfStudent : null,
                    RollNumber = a.Student != null ? a.Student.RollNo : null,
                    ClassName = a.Class != null ? a.Class.VName : null,
                    SectionName = a.Section != null ? a.Section.VName : null,
                    AttendanceDate = a.AttendanceDate,
                    Status = a.Status,
                    StatusName = a.Status.ToString(),
                    TimeIn = a.TimeIn.HasValue ? a.TimeIn.Value.ToString(@"hh\:mm") : null,
                    TimeOut = a.TimeOut.HasValue ? a.TimeOut.Value.ToString(@"hh\:mm") : null
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<AttendanceListDto>> GetByStudentAsync(int studentId, int sessionId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.Attendances
                .Include(a => a.Class)
                .Include(a => a.Section)
                .Where(a => a.StudentId == studentId && a.SessionId == sessionId);

            if (fromDate.HasValue)
                query = query.Where(a => a.AttendanceDate >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(a => a.AttendanceDate <= toDate.Value.Date);

            return await query
                .OrderByDescending(a => a.AttendanceDate)
                .Select(a => new AttendanceListDto
                {
                    Id = a.Id,
                    StudentName = a.Student != null ? a.Student.NameOfStudent : null,
                    RollNumber = a.Student != null ? a.Student.RollNo : null,
                    ClassName = a.Class != null ? a.Class.VName : null,
                    SectionName = a.Section != null ? a.Section.VName : null,
                    AttendanceDate = a.AttendanceDate,
                    Status = a.Status,
                    StatusName = a.Status.ToString(),
                    TimeIn = a.TimeIn.HasValue ? a.TimeIn.Value.ToString(@"hh\:mm") : null,
                    TimeOut = a.TimeOut.HasValue ? a.TimeOut.Value.ToString(@"hh\:mm") : null
                })
                .ToListAsync();
        }

        public async Task<Attendance> CreateAsync(CreateAttendanceDto dto)
        {
            var entity = new Attendance
            {
                StudentId = dto.StudentId,
                ClassId = dto.ClassId,
                SectionId = dto.SectionId,
                SessionId = dto.SessionId,
                AttendanceDate = dto.AttendanceDate.Date,
                Status = dto.Status,
                TimeIn = dto.TimeIn,
                TimeOut = dto.TimeOut,
                Remarks = dto.Remarks,
                CreatedAt = DateTime.UtcNow
            };

            _context.Attendances.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<Attendance?> UpdateAsync(int id, UpdateAttendanceDto dto)
        {
            var entity = await _context.Attendances.FindAsync(id);
            if (entity == null)
                return null;

            entity.StudentId = dto.StudentId;
            entity.ClassId = dto.ClassId;
            entity.SectionId = dto.SectionId;
            entity.SessionId = dto.SessionId;
            entity.AttendanceDate = dto.AttendanceDate.Date;
            entity.Status = dto.Status;
            entity.TimeIn = dto.TimeIn;
            entity.TimeOut = dto.TimeOut;
            entity.Remarks = dto.Remarks;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.Attendances.FindAsync(id);
            if (entity == null)
                return false;

            _context.Attendances.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Attendances.AnyAsync(a => a.Id == id);
        }

        public async Task<bool> AttendanceExistsAsync(int studentId, DateTime date)
        {
            return await _context.Attendances
                .AnyAsync(a => a.StudentId == studentId && a.AttendanceDate.Date == date.Date);
        }

        public async Task SaveBulkAttendanceAsync(BulkAttendanceDto dto)
        {
            var existingAttendances = await _context.Attendances
                .Where(a => a.ClassId == dto.ClassId && a.SectionId == dto.SectionId &&
                            a.SessionId == dto.SessionId && a.AttendanceDate.Date == dto.AttendanceDate.Date)
                .ToListAsync();

            foreach (var studentAttendance in dto.Students)
            {
                var existing = existingAttendances
                    .FirstOrDefault(a => a.StudentId == studentAttendance.StudentId);

                if (existing != null)
                {
                    existing.Status = studentAttendance.Status;
                    existing.TimeIn = studentAttendance.TimeIn;
                    existing.TimeOut = studentAttendance.TimeOut;
                    existing.Remarks = studentAttendance.Remarks;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var newAttendance = new Attendance
                    {
                        StudentId = studentAttendance.StudentId,
                        ClassId = dto.ClassId,
                        SectionId = dto.SectionId,
                        SessionId = dto.SessionId,
                        AttendanceDate = dto.AttendanceDate.Date,
                        Status = studentAttendance.Status,
                        TimeIn = studentAttendance.TimeIn,
                        TimeOut = studentAttendance.TimeOut,
                        Remarks = studentAttendance.Remarks,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Attendances.Add(newAttendance);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<AttendanceSummaryDto>> GetAttendanceSummaryAsync(int classId, int sectionId, int sessionId, DateTime fromDate, DateTime toDate)
        {
            var attendances = await _context.Attendances
                .Include(a => a.Student)
                .Where(a => a.ClassId == classId && a.SectionId == sectionId &&
                            a.SessionId == sessionId &&
                            a.AttendanceDate >= fromDate.Date && a.AttendanceDate <= toDate.Date)
                .ToListAsync();

            var summary = attendances
                .GroupBy(a => new { a.StudentId, a.Student?.NameOfStudent, a.Student?.RollNo })
                .Select(g => new AttendanceSummaryDto
                {
                    StudentId = g.Key.StudentId,
                    StudentName = g.Key.NameOfStudent ?? "",
                    RollNumber = g.Key.RollNo,
                    TotalDays = g.Count(),
                    PresentDays = g.Count(a => a.Status == AttendanceStatus.Present),
                    AbsentDays = g.Count(a => a.Status == AttendanceStatus.Absent),
                    LateDays = g.Count(a => a.Status == AttendanceStatus.Late),
                    LeaveDays = g.Count(a => a.Status == AttendanceStatus.Leave),
                    AttendancePercentage = g.Count() > 0
                        ? Math.Round((decimal)g.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late) / g.Count() * 100, 2)
                        : 0
                })
                .OrderBy(s => s.RollNumber)
                .ToList();

            return summary;
        }
    }
}
