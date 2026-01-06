using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.DTOs;
using School.API.Models;

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

        // POST: api/EmployeesApi
        [HttpPost]
        public async Task<ActionResult<EmployeeDto>> CreateEmployee(CreateEmployeeDto request)
        {
            // Check if employee code already exists
            var existingEmployee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeCode.ToLower() == request.EmployeeCode.ToLower());

            if (existingEmployee != null)
            {
                return BadRequest(new { message = "Employee code already exists" });
            }

            var employee = new Employee
            {
                EmployeeCode = request.EmployeeCode,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Phone = request.Phone,
                Department = request.Department,
                Designation = request.Designation,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, new EmployeeDto
            {
                Id = employee.Id,
                EmployeeCode = employee.EmployeeCode,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                FullName = employee.FirstName + " " + (employee.LastName ?? ""),
                Email = employee.Email,
                Phone = employee.Phone,
                Department = employee.Department,
                Designation = employee.Designation,
                IsActive = employee.IsActive
            });
        }

        // PUT: api/EmployeesApi/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEmployee(int id, UpdateEmployeeDto request)
        {
            if (id != request.Id)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            var employee = await _context.Employees.FindAsync(id);

            if (employee == null)
            {
                return NotFound();
            }

            // Check if employee code already exists (excluding current employee)
            var existingEmployee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeCode.ToLower() == request.EmployeeCode.ToLower() && e.Id != id);

            if (existingEmployee != null)
            {
                return BadRequest(new { message = "Employee code already exists" });
            }

            employee.EmployeeCode = request.EmployeeCode;
            employee.FirstName = request.FirstName;
            employee.LastName = request.LastName;
            employee.Email = request.Email;
            employee.Phone = request.Phone;
            employee.Department = request.Department;
            employee.Designation = request.Designation;
            employee.IsActive = request.IsActive;
            employee.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Employee updated successfully" });
        }

        // DELETE: api/EmployeesApi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                return NotFound();
            }

            // Check if employee is linked to a user
            if (employee.User != null)
            {
                return BadRequest(new { message = "Cannot delete employee linked to a user account" });
            }

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Employee deleted successfully" });
        }
    }
}
