using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.DTOs;

namespace School.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesApiController : ControllerBase
    {
        private readonly SchoolDbContext _context;

        public EmployeesApiController(SchoolDbContext context)
        {
            _context = context;
        }

        // GET: api/EmployeesApi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetEmployees()
        {
            var employees = await _context.Employees
                .Where(e => e.IsActive)
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .Select(e => new EmployeeDto
                {
                    Id = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    FullName = e.FirstName + " " + (e.LastName ?? ""),
                    Email = e.Email,
                    Phone = e.Phone,
                    Department = e.Department,
                    Designation = e.Designation,
                    IsActive = e.IsActive
                })
                .ToListAsync();

            return Ok(employees);
        }

        // GET: api/EmployeesApi/dropdown
        [HttpGet("dropdown")]
        public async Task<ActionResult<IEnumerable<EmployeeDropdownDto>>> GetEmployeesDropdown()
        {
            var employees = await _context.Employees
                .Where(e => e.IsActive)
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .Select(e => new EmployeeDropdownDto
                {
                    Id = e.Id,
                    FullName = e.FirstName + " " + (e.LastName ?? ""),
                    EmployeeCode = e.EmployeeCode
                })
                .ToListAsync();

            return Ok(employees);
        }

        // GET: api/EmployeesApi/5
        [HttpGet("{id}")]
        public async Task<ActionResult<EmployeeDto>> GetEmployee(int id)
        {
            var employee = await _context.Employees
                .Where(e => e.Id == id)
                .Select(e => new EmployeeDto
                {
                    Id = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    FullName = e.FirstName + " " + (e.LastName ?? ""),
                    Email = e.Email,
                    Phone = e.Phone,
                    Department = e.Department,
                    Designation = e.Designation,
                    IsActive = e.IsActive
                })
                .FirstOrDefaultAsync();

            if (employee == null)
            {
                return NotFound();
            }

            return Ok(employee);
        }
    }
}
