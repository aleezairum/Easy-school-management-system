using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.DTOs;
using School.API.Models;
using System.Security.Cryptography;
using System.Text;

namespace School.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersApiController : ControllerBase
    {
        private readonly SchoolDbContext _context;

        public UsersApiController(SchoolDbContext context)
        {
            _context = context;
        }

        // GET: api/UsersApi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserListDto>>> GetUsers()
        {
            var users = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .OrderBy(u => u.UserFullName)
                .Select(u => new UserListDto
                {
                    Id = u.Id,
                    UserFullName = u.UserFullName,
                    UserLogin = u.UserLogin,
                    Password = "********",
                    UserRoles = string.Join(", ", u.UserRoles.Select(ur => ur.Role.RoleName)),
                    IsActive = u.IsActive
                })
                .ToListAsync();

            return Ok(users);
        }

        // GET: api/UsersApi/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.Employee)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var result = new UserDto
            {
                Id = user.Id,
                EmployeeId = user.EmployeeId,
                EmployeeName = user.Employee?.FullName,
                UserFullName = user.UserFullName,
                UserLogin = user.UserLogin,
                Password = null, // Don't return password
                IsActive = user.IsActive,
                RoleIds = user.UserRoles.Select(ur => ur.RoleId).ToList(),
                RoleNames = string.Join(", ", user.UserRoles.Select(ur => ur.Role.RoleName))
            };

            return Ok(result);
        }

        // POST: api/UsersApi
        [HttpPost]
        public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto request)
        {
            // Check if login already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserLogin.ToLower() == request.UserLogin.ToLower());

            if (existingUser != null)
            {
                return BadRequest(new { message = "User login already exists" });
            }

            var user = new User
            {
                EmployeeId = request.EmployeeId,
                UserFullName = request.UserFullName,
                UserLogin = request.UserLogin,
                PasswordHash = HashPassword(request.Password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Add user roles
            if (request.RoleIds != null && request.RoleIds.Any())
            {
                var userRoles = request.RoleIds.Select(roleId => new UserRole
                {
                    UserId = user.Id,
                    RoleId = roleId,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                _context.UserRoles.AddRange(userRoles);
                await _context.SaveChangesAsync();
            }

            // Reload with roles
            var createdUser = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new UserDto
            {
                Id = user.Id,
                EmployeeId = user.EmployeeId,
                UserFullName = user.UserFullName,
                UserLogin = user.UserLogin,
                IsActive = user.IsActive,
                RoleIds = createdUser?.UserRoles.Select(ur => ur.RoleId).ToList() ?? new List<int>(),
                RoleNames = string.Join(", ", createdUser?.UserRoles.Select(ur => ur.Role.RoleName) ?? Enumerable.Empty<string>())
            });
        }

        // PUT: api/UsersApi/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UpdateUserDto request)
        {
            if (id != request.Id)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            // Check if login already exists (excluding current user)
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserLogin.ToLower() == request.UserLogin.ToLower() && u.Id != id);

            if (existingUser != null)
            {
                return BadRequest(new { message = "User login already exists" });
            }

            user.EmployeeId = request.EmployeeId;
            user.UserFullName = request.UserFullName;
            user.UserLogin = request.UserLogin;
            user.UpdatedAt = DateTime.UtcNow;

            // Update password only if provided
            if (!string.IsNullOrEmpty(request.Password))
            {
                user.PasswordHash = HashPassword(request.Password);
            }

            // Remove existing roles
            _context.UserRoles.RemoveRange(user.UserRoles);

            // Add new roles
            if (request.RoleIds != null && request.RoleIds.Any())
            {
                var userRoles = request.RoleIds.Select(roleId => new UserRole
                {
                    UserId = user.Id,
                    RoleId = roleId,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                _context.UserRoles.AddRange(userRoles);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "User updated successfully" });
        }

        // DELETE: api/UsersApi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            _context.UserRoles.RemoveRange(user.UserRoles);
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User deleted successfully" });
        }

        // GET: api/UsersApi/employees
        [HttpGet("employees")]
        public async Task<ActionResult<IEnumerable<EmployeeDropdownDto>>> GetEmployeesForDropdown()
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

        // GET: api/UsersApi/roles
        [HttpGet("roles")]
        public async Task<ActionResult<IEnumerable<RoleListDto>>> GetRolesForDropdown()
        {
            var roles = await _context.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.RoleName)
                .Select(r => new RoleListDto
                {
                    Id = r.Id,
                    RoleName = r.RoleName,
                    IsActive = r.IsActive
                })
                .ToListAsync();

            return Ok(roles);
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}
