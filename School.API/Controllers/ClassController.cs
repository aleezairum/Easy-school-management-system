using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels;
using School.API.DTOs;

namespace School.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClassController : ControllerBase
    {
        private readonly SchoolDbContext _context;

        public ClassController(SchoolDbContext context)
        {
            _context = context;
        }

        // GET: api/class
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClassDto>>> GetClasses()
        {
            var classes = await _context.SMSClasses
                .OrderBy(c => c.VName)
                .Select(c => new ClassDto
                {
                    VID = c.VID,
                    VName = c.VName,
                    IsActive = c.IsActive
                })
                .ToListAsync();

            return Ok(classes);
        }

        // GET: api/class/dropdown
        [HttpGet("dropdown")]
        public async Task<ActionResult<IEnumerable<ClassDropdownDto>>> GetClassesDropdown()
        {
            var classes = await _context.SMSClasses
                .Where(c => c.IsActive)
                .OrderBy(c => c.VName)
                .Select(c => new ClassDropdownDto
                {
                    VID = c.VID,
                    VName = c.VName
                })
                .ToListAsync();

            return Ok(classes);
        }

        // GET: api/class/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ClassDto>> GetClass(int id)
        {
            var cls = await _context.SMSClasses.FindAsync(id);

            if (cls == null)
            {
                return NotFound(new { message = "Class not found" });
            }

            return Ok(new ClassDto
            {
                VID = cls.VID,
                VName = cls.VName,
                IsActive = cls.IsActive
            });
        }

        // POST: api/class
        [HttpPost]
        public async Task<ActionResult<ClassDto>> CreateClass(CreateClassDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.VName))
            {
                return BadRequest(new { message = "Class name is required" });
            }

            // Check for duplicate
            var existing = await _context.SMSClasses
                .FirstOrDefaultAsync(c => c.VName.ToLower() == dto.VName.Trim().ToLower());

            if (existing != null)
            {
                return BadRequest(new { message = "A class with this name already exists" });
            }

            var cls = new SMSClass
            {
                VName = dto.VName.Trim(),
                IsActive = dto.IsActive,
                InsertedDate = DateTime.UtcNow
            };

            _context.SMSClasses.Add(cls);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetClass), new { id = cls.VID }, new ClassDto
            {
                VID = cls.VID,
                VName = cls.VName,
                IsActive = cls.IsActive
            });
        }

        // PUT: api/class/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateClass(int id, UpdateClassDto dto)
        {
            if (id != dto.VID)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            var cls = await _context.SMSClasses.FindAsync(id);

            if (cls == null)
            {
                return NotFound(new { message = "Class not found" });
            }

            if (string.IsNullOrWhiteSpace(dto.VName))
            {
                return BadRequest(new { message = "Class name is required" });
            }

            // Check for duplicate (excluding current record)
            var existing = await _context.SMSClasses
                .FirstOrDefaultAsync(c => c.VName.ToLower() == dto.VName.Trim().ToLower() && c.VID != id);

            if (existing != null)
            {
                return BadRequest(new { message = "A class with this name already exists" });
            }

            cls.VName = dto.VName.Trim();
            cls.IsActive = dto.IsActive;
            cls.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new ClassDto
            {
                VID = cls.VID,
                VName = cls.VName,
                IsActive = cls.IsActive
            });
        }

        // DELETE: api/class/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClass(int id)
        {
            var cls = await _context.SMSClasses
                .Include(c => c.Sections)
                .Include(c => c.Subjects)
                .FirstOrDefaultAsync(c => c.VID == id);

            if (cls == null)
            {
                return NotFound(new { message = "Class not found" });
            }

            if (cls.Sections.Any() || cls.Subjects.Any())
            {
                return BadRequest(new { message = "Cannot delete class. It has associated sections or subjects. Please delete them first." });
            }

            _context.SMSClasses.Remove(cls);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Class deleted successfully" });
        }
    }
}
