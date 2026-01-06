using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.DTOs;
using School.API.Models;

namespace School.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenusApiController : ControllerBase
    {
        private readonly SchoolDbContext _context;

        public MenusApiController(SchoolDbContext context)
        {
            _context = context;
        }

        // GET: api/MenusApi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MenuDto>>> GetMenus()
        {
            var menus = await _context.Menus
                .Include(m => m.Parent)
                .OrderBy(m => m.ParentId ?? 0)
                .ThenBy(m => m.DisplayOrder)
                .Select(m => new MenuDto
                {
                    Id = m.Id,
                    MenuTitle = m.MenuTitle,
                    Url = m.Url,
                    Icon = m.Icon,
                    ParentId = m.ParentId,
                    ParentTitle = m.Parent != null ? m.Parent.MenuTitle : null,
                    DisplayOrder = m.DisplayOrder,
                    Level = m.Level,
                    IsActive = m.IsActive
                })
                .ToListAsync();

            return Ok(menus);
        }

        // GET: api/MenusApi/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MenuDto>> GetMenu(int id)
        {
            var menu = await _context.Menus
                .Include(m => m.Parent)
                .Where(m => m.Id == id)
                .Select(m => new MenuDto
                {
                    Id = m.Id,
                    MenuTitle = m.MenuTitle,
                    Url = m.Url,
                    Icon = m.Icon,
                    ParentId = m.ParentId,
                    ParentTitle = m.Parent != null ? m.Parent.MenuTitle : null,
                    DisplayOrder = m.DisplayOrder,
                    Level = m.Level,
                    IsActive = m.IsActive
                })
                .FirstOrDefaultAsync();

            if (menu == null)
            {
                return NotFound();
            }

            return Ok(menu);
        }

        // GET: api/MenusApi/hierarchy
        [HttpGet("hierarchy")]
        public async Task<ActionResult<IEnumerable<MenuForPermissionDto>>> GetMenusHierarchy()
        {
            var menus = await _context.Menus
                .Where(m => m.IsActive)
                .OrderBy(m => m.DisplayOrder)
                .ToListAsync();

            // Build hierarchical list
            var result = new List<MenuForPermissionDto>();

            // Get root menus first
            var rootMenus = menus.Where(m => m.ParentId == null).OrderBy(m => m.DisplayOrder);

            foreach (var root in rootMenus)
            {
                result.Add(new MenuForPermissionDto
                {
                    Id = root.Id,
                    MenuTitle = root.MenuTitle,
                    ParentId = root.ParentId,
                    Level = root.Level,
                    DisplayOrder = root.DisplayOrder
                });

                // Add children
                AddChildren(menus, result, root.Id);
            }

            return Ok(result);
        }

        // GET: api/MenusApi/dropdown
        [HttpGet("dropdown")]
        public async Task<ActionResult<IEnumerable<object>>> GetMenusDropdown()
        {
            var menus = await _context.Menus
                .Where(m => m.IsActive)
                .OrderBy(m => m.DisplayOrder)
                .Select(m => new
                {
                    m.Id,
                    m.MenuTitle,
                    m.Level
                })
                .ToListAsync();

            return Ok(menus);
        }

        // POST: api/MenusApi
        [HttpPost]
        public async Task<ActionResult<MenuDto>> CreateMenu(CreateMenuDto request)
        {
            int level = 0;

            // Calculate level based on parent
            if (request.ParentId.HasValue)
            {
                var parent = await _context.Menus.FindAsync(request.ParentId.Value);
                if (parent == null)
                {
                    return BadRequest(new { message = "Parent menu not found" });
                }
                level = parent.Level + 1;
            }

            var menu = new Menu
            {
                MenuTitle = request.MenuTitle,
                Url = request.Url,
                Icon = request.Icon,
                ParentId = request.ParentId,
                DisplayOrder = request.DisplayOrder,
                Level = level,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Menus.Add(menu);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMenu), new { id = menu.Id }, new MenuDto
            {
                Id = menu.Id,
                MenuTitle = menu.MenuTitle,
                Url = menu.Url,
                Icon = menu.Icon,
                ParentId = menu.ParentId,
                DisplayOrder = menu.DisplayOrder,
                Level = menu.Level,
                IsActive = menu.IsActive
            });
        }

        // PUT: api/MenusApi/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMenu(int id, UpdateMenuDto request)
        {
            if (id != request.Id)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            var menu = await _context.Menus.FindAsync(id);

            if (menu == null)
            {
                return NotFound();
            }

            // Prevent setting self as parent
            if (request.ParentId == id)
            {
                return BadRequest(new { message = "Menu cannot be its own parent" });
            }

            int level = 0;

            // Calculate level based on parent
            if (request.ParentId.HasValue)
            {
                var parent = await _context.Menus.FindAsync(request.ParentId.Value);
                if (parent == null)
                {
                    return BadRequest(new { message = "Parent menu not found" });
                }
                level = parent.Level + 1;
            }

            menu.MenuTitle = request.MenuTitle;
            menu.Url = request.Url;
            menu.Icon = request.Icon;
            menu.ParentId = request.ParentId;
            menu.DisplayOrder = request.DisplayOrder;
            menu.Level = level;
            menu.IsActive = request.IsActive;
            menu.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Update children levels if necessary
            await UpdateChildrenLevels(id, level + 1);

            return Ok(new { message = "Menu updated successfully" });
        }

        // DELETE: api/MenusApi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMenu(int id)
        {
            var menu = await _context.Menus
                .Include(m => m.Children)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (menu == null)
            {
                return NotFound();
            }

            // Check if menu has children
            if (menu.Children.Any())
            {
                return BadRequest(new { message = "Cannot delete menu with child items. Delete children first." });
            }

            // Check if menu has role permissions
            var hasPermissions = await _context.RolePermissions.AnyAsync(rp => rp.MenuId == id);
            if (hasPermissions)
            {
                return BadRequest(new { message = "Cannot delete menu with associated role permissions" });
            }

            _context.Menus.Remove(menu);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Menu deleted successfully" });
        }

        private void AddChildren(List<Menu> allMenus, List<MenuForPermissionDto> result, int parentId)
        {
            var children = allMenus.Where(m => m.ParentId == parentId).OrderBy(m => m.DisplayOrder);

            foreach (var child in children)
            {
                result.Add(new MenuForPermissionDto
                {
                    Id = child.Id,
                    MenuTitle = child.MenuTitle,
                    ParentId = child.ParentId,
                    Level = child.Level,
                    DisplayOrder = child.DisplayOrder
                });

                // Recursively add children
                AddChildren(allMenus, result, child.Id);
            }
        }

        private async Task UpdateChildrenLevels(int parentId, int newLevel)
        {
            var children = await _context.Menus
                .Where(m => m.ParentId == parentId)
                .ToListAsync();

            foreach (var child in children)
            {
                child.Level = newLevel;
                await UpdateChildrenLevels(child.Id, newLevel + 1);
            }

            await _context.SaveChangesAsync();
        }
    }
}
