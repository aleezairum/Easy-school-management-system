using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.HR;
using School.API.DTOs.HR;

namespace School.API.Controllers.HR
{
    // Seeded via Scripts/HrPayroll.sql; also maintainable from the Payroll Setup screen.
    [ApiController]
    [Route("api/hr/leave-types")]
    public class LeaveTypeController : ControllerBase
    {
        private readonly SchoolDbContext _context;

        public LeaveTypeController(SchoolDbContext context)
        {
            _context = context;
        }

        // includeInactive is only used by the setup screen; every other caller
        // (leave application) wants the active list it has always received.
        [HttpGet]
        public async Task<ActionResult<List<LeaveTypeDto>>> GetAll([FromQuery] bool includeInactive = false)
        {
            var result = await _context.Set<HrLeaveType>()
                .AsNoTracking()
                .Where(l => includeInactive || l.IsActive)
                .OrderBy(l => l.Name)
                .Select(l => new LeaveTypeDto
                {
                    Id = l.Id,
                    Code = l.Code,
                    Name = l.Name,
                    IsPaid = l.IsPaid,
                    DeductDays = l.DeductDays,
                    MaxPerYear = l.MaxPerYear,
                    IsActive = l.IsActive
                })
                .ToListAsync();

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<LeaveTypeDto>> GetById(int id)
        {
            var entity = await _context.Set<HrLeaveType>().AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
            return entity == null ? NotFound() : Ok(ToDto(entity));
        }

        [HttpPost]
        public async Task<ActionResult<LeaveTypeDto>> Create(SaveLeaveTypeDto dto)
        {
            var error = await ValidateAsync(dto, 0);
            if (error != null) return BadRequest(new { message = error });

            var entity = new HrLeaveType();
            Apply(dto, entity);
            _context.Set<HrLeaveType>().Add(entity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToDto(entity));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<LeaveTypeDto>> Update(int id, SaveLeaveTypeDto dto)
        {
            var entity = await _context.Set<HrLeaveType>().FirstOrDefaultAsync(l => l.Id == id);
            if (entity == null) return NotFound();

            var error = await ValidateAsync(dto, id);
            if (error != null) return BadRequest(new { message = error });

            Apply(dto, entity);
            await _context.SaveChangesAsync();

            return Ok(ToDto(entity));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var entity = await _context.Set<HrLeaveType>().FirstOrDefaultAsync(l => l.Id == id);
            if (entity == null) return NotFound();

            // Leaves already recorded against this type must keep resolving their
            // name/deduction, so an in-use type is deactivated instead of removed.
            var inUse = await _context.Set<HrLeave>().AnyAsync(l => l.LeaveTypeId == id);
            if (inUse)
            {
                if (!entity.IsActive)
                    return Conflict(new { message = "This leave type is already inactive and is in use by existing leave records." });

                entity.IsActive = false;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Leave type is in use by existing leave records, so it was deactivated instead of deleted." });
            }

            _context.Set<HrLeaveType>().Remove(entity);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private async Task<string?> ValidateAsync(SaveLeaveTypeDto dto, int id)
        {
            if (string.IsNullOrWhiteSpace(dto.Code)) return "Code is required.";
            if (string.IsNullOrWhiteSpace(dto.Name)) return "Name is required.";
            if (dto.DeductDays < 0) return "Deduct days cannot be negative.";
            if (dto.MaxPerYear is < 0) return "Max per year cannot be negative.";

            var code = dto.Code.Trim();
            var duplicate = await _context.Set<HrLeaveType>()
                .AnyAsync(l => l.Id != id && l.Code == code);

            return duplicate ? $"Leave type code '{code}' already exists." : null;
        }

        private static void Apply(SaveLeaveTypeDto dto, HrLeaveType entity)
        {
            entity.Code = dto.Code.Trim();
            entity.Name = dto.Name.Trim();
            entity.IsPaid = dto.IsPaid;
            entity.DeductDays = dto.DeductDays;
            entity.MaxPerYear = dto.MaxPerYear;
            entity.IsActive = dto.IsActive;
        }

        private static LeaveTypeDto ToDto(HrLeaveType l) => new()
        {
            Id = l.Id,
            Code = l.Code,
            Name = l.Name,
            IsPaid = l.IsPaid,
            DeductDays = l.DeductDays,
            MaxPerYear = l.MaxPerYear,
            IsActive = l.IsActive
        };
    }
}
