using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels;
using School.API.DTOs;

namespace School.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubjectController : ControllerBase
    {
        private readonly SchoolDbContext _context;

        public SubjectController(SchoolDbContext context)
        {
            _context = context;
        }

        // GET: api/subject
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubjectDto>>> GetSubjects([FromQuery] int? classId = null)
        {
            var query = _context.SMSSubjects.Include(s => s.Class).AsQueryable();

            if (classId.HasValue)
            {
                query = query.Where(s => s.ClassID == classId.Value);
            }

            var subjects = await query
                .OrderBy(s => s.Class!.VName)
                .ThenBy(s => s.VName)
                .Select(s => new SubjectDto
                {
                    VID = s.VID,
                    VName = s.VName,
                    ClassID = s.ClassID,
                    ClassName = s.Class!.VName,
                    IsActive = s.IsActive
                })
                .ToListAsync();

            return Ok(subjects);
        }

        // GET: api/subject/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SubjectDto>> GetSubject(int id)
        {
            var subject = await _context.SMSSubjects
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.VID == id);

            if (subject == null)
            {
                return NotFound(new { message = "Subject not found" });
            }

            return Ok(new SubjectDto
            {
                VID = subject.VID,
                VName = subject.VName,
                ClassID = subject.ClassID,
                ClassName = subject.Class!.VName,
                IsActive = subject.IsActive
            });
        }

        // POST: api/subject
        [HttpPost]
        public async Task<ActionResult<SubjectDto>> CreateSubject(CreateSubjectDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.VName))
            {
                return BadRequest(new { message = "Subject name is required" });
            }

            if (dto.ClassID <= 0)
            {
                return BadRequest(new { message = "Please select a class" });
            }

            // Validate class exists
            var classExists = await _context.SMSClasses.AnyAsync(c => c.VID == dto.ClassID);
            if (!classExists)
            {
                return BadRequest(new { message = "Invalid class selected" });
            }

            // Check for duplicate within the same class
            var existing = await _context.SMSSubjects
                .FirstOrDefaultAsync(s => s.ClassID == dto.ClassID && s.VName.ToLower() == dto.VName.Trim().ToLower());

            if (existing != null)
            {
                return BadRequest(new { message = "A subject with this name already exists in the selected class" });
            }

            var subject = new SMSSubject
            {
                VName = dto.VName.Trim(),
                ClassID = dto.ClassID,
                IsActive = dto.IsActive,
                InsertedDate = DateTime.UtcNow
            };

            _context.SMSSubjects.Add(subject);
            await _context.SaveChangesAsync();

            // Reload with class info
            await _context.Entry(subject).Reference(s => s.Class).LoadAsync();

            return CreatedAtAction(nameof(GetSubject), new { id = subject.VID }, new SubjectDto
            {
                VID = subject.VID,
                VName = subject.VName,
                ClassID = subject.ClassID,
                ClassName = subject.Class!.VName,
                IsActive = subject.IsActive
            });
        }

        // PUT: api/subject/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSubject(int id, UpdateSubjectDto dto)
        {
            if (id != dto.VID)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            var subject = await _context.SMSSubjects.FindAsync(id);

            if (subject == null)
            {
                return NotFound(new { message = "Subject not found" });
            }

            if (string.IsNullOrWhiteSpace(dto.VName))
            {
                return BadRequest(new { message = "Subject name is required" });
            }

            if (dto.ClassID <= 0)
            {
                return BadRequest(new { message = "Please select a class" });
            }

            // Validate class exists
            var classExists = await _context.SMSClasses.AnyAsync(c => c.VID == dto.ClassID);
            if (!classExists)
            {
                return BadRequest(new { message = "Invalid class selected" });
            }

            // Check for duplicate (excluding current record)
            var existing = await _context.SMSSubjects
                .FirstOrDefaultAsync(s => s.ClassID == dto.ClassID && s.VName.ToLower() == dto.VName.Trim().ToLower() && s.VID != id);

            if (existing != null)
            {
                return BadRequest(new { message = "A subject with this name already exists in the selected class" });
            }

            subject.VName = dto.VName.Trim();
            subject.ClassID = dto.ClassID;
            subject.IsActive = dto.IsActive;
            subject.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Reload with class info
            await _context.Entry(subject).Reference(s => s.Class).LoadAsync();

            return Ok(new SubjectDto
            {
                VID = subject.VID,
                VName = subject.VName,
                ClassID = subject.ClassID,
                ClassName = subject.Class!.VName,
                IsActive = subject.IsActive
            });
        }

        // DELETE: api/subject/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSubject(int id)
        {
            var subject = await _context.SMSSubjects.FindAsync(id);

            if (subject == null)
            {
                return NotFound(new { message = "Subject not found" });
            }

            _context.SMSSubjects.Remove(subject);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Subject deleted successfully" });
        }
    }
}
