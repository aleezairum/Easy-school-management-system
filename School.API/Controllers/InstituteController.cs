using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.DTOs;
using School.API.Models;

namespace School.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InstituteController : ControllerBase
    {
        private readonly SchoolDbContext _context;

        public InstituteController(SchoolDbContext context)
        {
            _context = context;
        }

        // GET: api/Institute
        //[HttpGet]
        //public async Task<ActionResult<IEnumerable<InstituteDto>>> GetInstitutes()
        //{
        //    var institutes = await _context.Institutes
        //        .OrderBy(i => i.Name)
        //        .Select(i => new InstituteDto
        //        {
        //            Id = i.Id,
        //            Name = i.Name,
        //            Code = i.Code,
        //            Address = i.Address,
        //            Phone = i.Phone,
        //            Email = i.Email,
        //            Website = i.Website,
        //            LogoUrl = i.LogoUrl,
        //            IsActive = i.IsActive
        //        })
        //        .ToListAsync();

        //    return Ok(institutes);
        //}

        //// GET: api/Institute/5
        //[HttpGet("{id}")]
        //public async Task<ActionResult<InstituteDto>> GetInstitute(int id)
        //{
        //    var institute = await _context.Institutes
        //        .Where(i => i.Id == id)
        //        .Select(i => new InstituteDto
        //        {
        //            Id = i.Id,
        //            Name = i.Name,
        //            Code = i.Code,
        //            Address = i.Address,
        //            Phone = i.Phone,
        //            Email = i.Email,
        //            Website = i.Website,
        //            LogoUrl = i.LogoUrl,
        //            IsActive = i.IsActive
        //        })
        //        .FirstOrDefaultAsync();

        //    if (institute == null)
        //    {
        //        return NotFound(new { message = "Institute not found" });
        //    }

        //    return Ok(institute);
        //}

        //// GET: api/Institute/dropdown
        //[HttpGet("dropdown")]
        //public async Task<ActionResult<IEnumerable<object>>> GetInstitutesDropdown()
        //{
        //    var institutes = await _context.Institutes
        //        .Where(i => i.IsActive)
        //        .OrderBy(i => i.Name)
        //        .Select(i => new
        //        {
        //            i.Id,
        //            i.Name,
        //            i.Code
        //        })
        //        .ToListAsync();

        //    return Ok(institutes);
        //}

        //// POST: api/Institute
        //[HttpPost]
        //public async Task<ActionResult<InstituteDto>> CreateInstitute(CreateInstituteDto request)
        //{
        //    // Check if institute code already exists
        //    var existingInstitute = await _context.Institutes
        //        .FirstOrDefaultAsync(i => i.Code.ToLower() == request.Code.ToLower());

        //    if (existingInstitute != null)
        //    {
        //        return BadRequest(new { message = "Institute code already exists" });
        //    }

        //    var institute = new Institute
        //    {
        //        Name = request.Name,
        //        Code = request.Code,
        //        Address = request.Address,
        //        Phone = request.Phone,
        //        Email = request.Email,
        //        Website = request.Website,
        //        LogoUrl = request.LogoUrl,
        //        IsActive = request.IsActive,
        //        CreatedAt = DateTime.UtcNow
        //    };

        //    _context.Institutes.Add(institute);
        //    await _context.SaveChangesAsync();

        //    return CreatedAtAction(nameof(GetInstitute), new { id = institute.Id }, new InstituteDto
        //    {
        //        Id = institute.Id,
        //        Name = institute.Name,
        //        Code = institute.Code,
        //        Address = institute.Address,
        //        Phone = institute.Phone,
        //        Email = institute.Email,
        //        Website = institute.Website,
        //        LogoUrl = institute.LogoUrl,
        //        IsActive = institute.IsActive
        //    });
        //}

        //// PUT: api/Institute/5
        //[HttpPut("{id}")]
        //public async Task<IActionResult> UpdateInstitute(int id, UpdateInstituteDto request)
        //{
        //    if (id != request.Id)
        //    {
        //        return BadRequest(new { message = "ID mismatch" });
        //    }

        //    var institute = await _context.Institutes.FindAsync(id);

        //    if (institute == null)
        //    {
        //        return NotFound();
        //    }

        //    // Check if institute code already exists (excluding current institute)
        //    var existingInstitute = await _context.Institutes
        //        .FirstOrDefaultAsync(i => i.Code.ToLower() == request.Code.ToLower() && i.Id != id);

        //    if (existingInstitute != null)
        //    {
        //        return BadRequest(new { message = "Institute code already exists" });
        //    }

        //    institute.Name = request.Name;
        //    institute.Code = request.Code;
        //    institute.Address = request.Address;
        //    institute.Phone = request.Phone;
        //    institute.Email = request.Email;
        //    institute.Website = request.Website;
        //    institute.LogoUrl = request.LogoUrl;
        //    institute.IsActive = request.IsActive;
        //    institute.UpdatedAt = DateTime.UtcNow;

        //    await _context.SaveChangesAsync();

        //    return Ok(new { message = "Institute updated successfully" });
        //}

        //// DELETE: api/Institute/5
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> DeleteInstitute(int id)
        //{
        //    var institute = await _context.Institutes.FindAsync(id);

        //    if (institute == null)
        //    {
        //        return NotFound();
        //    }

        //    _context.Institutes.Remove(institute);
        //    await _context.SaveChangesAsync();

        //    return Ok(new { message = "Institute deleted successfully" });
        //}
    }
}
