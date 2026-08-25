using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.HR;
using School.API.DTOs.HR;

namespace School.API.Controllers.HR
{
    // Seeded via Scripts/HrPayroll.sql — no CRUD screen (HR-Payroll-Final-Plan.md section 3).
    [ApiController]
    [Route("api/hr/leave-types")]
    public class LeaveTypeController : ControllerBase
    {
        private readonly SchoolDbContext _context;

        public LeaveTypeController(SchoolDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<LeaveTypeDto>>> GetAll()
        {
            var result = await _context.Set<HrLeaveType>()
                .AsNoTracking()
                .Where(l => l.IsActive)
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
    }
}
