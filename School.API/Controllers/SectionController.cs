using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels;
using School.API.DTOs;

namespace School.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SectionController : ControllerBase
    {
        private readonly SchoolDbContext _context;

        public SectionController(SchoolDbContext context)
        {
            _context = context;
        }

        // GET: api/section
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SectionDto>>> GetSections([FromQuery] int? classId = null)
        {
            var query = _context.SMSSections.Include(s => s.Class).AsQueryable();

            if (classId.HasValue)
            {
                query = query.Where(s => s.ClassID == classId.Value);
            }

            var sections = await query
                .OrderBy(s => s.Class!.VName)
                .ThenBy(s => s.VName)
                .Select(s => new SectionDto
                {
                    VID = s.VID,
                    VName = s.VName,
                    ClassID = s.ClassID,
                    ClassName = s.Class!.VName,
                    IsActive = s.IsActive
                })
                .ToListAsync();

            return Ok(sections);
        }

        // GET: api/section/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SectionDto>> GetSection(int id)
        {
            var section = await _context.SMSSections
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.VID == id);

            if (section == null)
            {
                return NotFound(new { message = "Section not found" });
            }

            return Ok(new SectionDto
            {
                VID = section.VID,
                VName = section.VName,
                ClassID = section.ClassID,
                ClassName = section.Class!.VName,
                IsActive = section.IsActive
            });
        }

        // POST: api/section
        [HttpPost]
        public async Task<ActionResult<SectionDto>> CreateSection(CreateSectionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.VName))
            {
                return BadRequest(new { message = "Section name is required" });
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
            var existing = await _context.SMSSections
                .FirstOrDefaultAsync(s => s.ClassID == dto.ClassID && s.VName.ToLower() == dto.VName.Trim().ToLower());

            if (existing != null)
            {
                return BadRequest(new { message = "A section with this name already exists in the selected class" });
            }

            var section = new SMSSection
            {
                VName = dto.VName.Trim(),
                ClassID = dto.ClassID,
                IsActive = dto.IsActive,
                InsertedDate = DateTime.UtcNow
            };

            _context.SMSSections.Add(section);
            await _context.SaveChangesAsync();

            // Reload with class info
            await _context.Entry(section).Reference(s => s.Class).LoadAsync();

            return CreatedAtAction(nameof(GetSection), new { id = section.VID }, new SectionDto
            {
                VID = section.VID,
                VName = section.VName,
                ClassID = section.ClassID,
                ClassName = section.Class!.VName,
                IsActive = section.IsActive
            });
        }

        // PUT: api/section/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSection(int id, UpdateSectionDto dto)
        {
            if (id != dto.VID)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            var section = await _context.SMSSections.FindAsync(id);

            if (section == null)
            {
                return NotFound(new { message = "Section not found" });
            }

            if (string.IsNullOrWhiteSpace(dto.VName))
            {
                return BadRequest(new { message = "Section name is required" });
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
            var existing = await _context.SMSSections
                .FirstOrDefaultAsync(s => s.ClassID == dto.ClassID && s.VName.ToLower() == dto.VName.Trim().ToLower() && s.VID != id);

            if (existing != null)
            {
                return BadRequest(new { message = "A section with this name already exists in the selected class" });
            }

            section.VName = dto.VName.Trim();
            section.ClassID = dto.ClassID;
            section.IsActive = dto.IsActive;
            section.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Reload with class info
            await _context.Entry(section).Reference(s => s.Class).LoadAsync();

            return Ok(new SectionDto
            {
                VID = section.VID,
                VName = section.VName,
                ClassID = section.ClassID,
                ClassName = section.Class!.VName,
                IsActive = section.IsActive
            });
        }

        // DELETE: api/section/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSection(int id)
        {
            var section = await _context.SMSSections.FindAsync(id);

            if (section == null)
            {
                return NotFound(new { message = "Section not found" });
            }

            _context.SMSSections.Remove(section);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Section deleted successfully" });
        }
    }
}
