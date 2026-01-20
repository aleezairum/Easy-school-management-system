using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.DTOs;
using School.API.Models;

namespace School.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesApiController : ControllerBase
    {
        private readonly SchoolDbContext _context;

        public RolesApiController(SchoolDbContext context)
        {
            _context = context;
        }

        // GET: api/RolesApi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoleListDto>>> GetRoles()
        {
            var roles = await _context.Roles
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

        // GET: api/RolesApi/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RoleWithPermissionsDto>> GetRole(int id)
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Menu)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (role == null)
            {
                return NotFound();
            }

            var result = new RoleWithPermissionsDto
            {
                Id = role.Id,
                RoleName = role.RoleName,
                Description = role.Description,
                IsActive = role.IsActive,
                Permissions = role.RolePermissions.Select(rp => new RolePermissionDto
                {
                    Id = rp.Id,
                    RoleId = rp.RoleId,
                    MenuId = rp.MenuId,
                    MenuTitle = rp.Menu.MenuTitle,
                    IsView = rp.IsView,
                    IsInsert = rp.IsInsert,
                    IsUpdate = rp.IsUpdate,
                    IsDelete = rp.IsDelete,
                    IsActive = rp.IsActive,
                    IsBackDate = rp.IsBackDate,
                    IsPrint = rp.IsPrint
                }).ToList()
            };

            return Ok(result);
        }

        // GET: api/RolesApi/fetch?roleName=Admin
        [HttpGet("fetch")]
        public async Task<ActionResult<RoleWithPermissionsDto>> FetchRoleByName([FromQuery] string roleName)
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Menu)
                .FirstOrDefaultAsync(r => r.RoleName.ToLower() == roleName.ToLower());

            if (role == null)
            {
                return NotFound(new { message = $"Role '{roleName}' not found" });
            }

            // Get all menus
            var allMenus = await _context.Menus
                .Where(m => m.IsActive)
                .OrderBy(m => m.ParentId ?? 0)
                .ThenBy(m => m.DisplayOrder)
                .ToListAsync();

            // Build permissions for all menus
            var permissions = allMenus.Select(menu =>
            {
                var existingPermission = role.RolePermissions.FirstOrDefault(rp => rp.MenuId == menu.Id);
                return new RolePermissionDto
                {
                    Id = existingPermission?.Id ?? 0,
                    RoleId = role.Id,
                    MenuId = menu.Id,
                    MenuTitle = menu.MenuTitle,
                    IsView = existingPermission?.IsView ?? false,
                    IsInsert = existingPermission?.IsInsert ?? false,
                    IsUpdate = existingPermission?.IsUpdate ?? false,
                    IsDelete = existingPermission?.IsDelete ?? false,
                    IsActive = existingPermission?.IsActive ?? false,
                    IsBackDate = existingPermission?.IsBackDate ?? false,
                    IsPrint = existingPermission?.IsPrint ?? false
                };
            }).ToList();

            var result = new RoleWithPermissionsDto
            {
                Id = role.Id,
                RoleName = role.RoleName,
                Description = role.Description,
                IsActive = role.IsActive,
                Permissions = permissions
            };

            return Ok(result);
        }

        // GET: api/RolesApi/search?term=admin
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<RoleListDto>>> SearchRoles([FromQuery] string term)
        {
            var roles = await _context.Roles
                .Where(r => r.RoleName.Contains(term))
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

        // GET: api/RolesApi/menus
        [HttpGet("menus")]
        public async Task<ActionResult<IEnumerable<MenuForPermissionDto>>> GetMenusForPermission()
        {
            var menus = await _context.Menus
                .Where(m => m.IsActive)
                .OrderBy(m => m.ParentId ?? 0)
                .ThenBy(m => m.DisplayOrder)
                .Select(m => new MenuForPermissionDto
                {
                    Id = m.Id,
                    MenuTitle = m.MenuTitle,
                    ParentId = m.ParentId,
                    Level = m.Level,
                    DisplayOrder = m.DisplayOrder
                })
                .ToListAsync();

            return Ok(menus);
        }

        // POST: api/RolesApi
        [HttpPost]
        public async Task<ActionResult<RoleDto>> CreateRole(SaveRoleRequestDto request)
        {
            // Check if role name already exists
            var existingRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName.ToLower() == request.RoleName.ToLower());

            if (existingRole != null)
            {
                return BadRequest(new { message = "Role name already exists" });
            }

            var role = new Role
            {
                RoleName = request.RoleName,
                Description = request.Description,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            // Add permissions
            if (request.Permissions != null && request.Permissions.Any())
            {
                var permissions = request.Permissions.Select(p => new RolePermission
                {
                    RoleId = role.Id,
                    MenuId = p.MenuId,
                    IsView = p.IsView,
                    IsInsert = p.IsInsert,
                    IsUpdate = p.IsUpdate,
                    IsDelete = p.IsDelete,
                    IsActive = p.IsActive,
                    IsBackDate = p.IsBackDate,
                    IsPrint = p.IsPrint,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                _context.RolePermissions.AddRange(permissions);
                await _context.SaveChangesAsync();
            }

            return CreatedAtAction(nameof(GetRole), new { id = role.Id }, new RoleDto
            {
                Id = role.Id,
                RoleName = role.RoleName,
                Description = role.Description,
                IsActive = role.IsActive
            });
        }

        // PUT: api/RolesApi/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRole(int id, SaveRoleRequestDto request)
        {
            if (id != request.Id)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (role == null)
            {
                return NotFound();
            }

            // Check if role name already exists (excluding current role)
            var existingRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName.ToLower() == request.RoleName.ToLower() && r.Id != id);

            if (existingRole != null)
            {
                return BadRequest(new { message = "Role name already exists" });
            }

            role.RoleName = request.RoleName;
            role.Description = request.Description;
            role.IsActive = request.IsActive;
            role.UpdatedAt = DateTime.UtcNow;

            // Remove existing permissions
            _context.RolePermissions.RemoveRange(role.RolePermissions);

            // Add new permissions
            if (request.Permissions != null && request.Permissions.Any())
            {
                var permissions = request.Permissions.Select(p => new RolePermission
                {
                    RoleId = role.Id,
                    MenuId = p.MenuId,
                    IsView = p.IsView,
                    IsInsert = p.IsInsert,
                    IsUpdate = p.IsUpdate,
                    IsDelete = p.IsDelete,
                    IsActive = p.IsActive,
                    IsBackDate = p.IsBackDate,
                    IsPrint = p.IsPrint,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                _context.RolePermissions.AddRange(permissions);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Role updated successfully" });
        }

        // POST: api/RolesApi/save
        [HttpPost("save")]
        public async Task<IActionResult> SaveRole(SaveRoleRequestDto request)
        {
            if (request.Id == 0)
            {
                // Create new role
                var result = await CreateRole(request);
                return result.Result ?? Ok(result.Value);
            }
            else
            {
                // Update existing role
                return await UpdateRole(request.Id, request);
            }
        }

        // DELETE: api/RolesApi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .Include(r => r.UserRoles)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (role == null)
            {
                return NotFound();
            }

            if (role.UserRoles.Any())
            {
                return BadRequest(new { message = "Cannot delete role. It is assigned to users." });
            }

            _context.RolePermissions.RemoveRange(role.RolePermissions);
            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Role deleted successfully" });
        }
    }
}
